using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public sealed class StylizedRendererFeature : ScriptableRendererFeature
{
    [SerializeField]
    private Shader posterizeShader;

    [SerializeField]
    private Shader stylizedShader;

    [SerializeField]
    private RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingPostProcessing;

    private Material posterizeMaterial;
    private Material stylizedMaterial;
    private FullScreenMaterialPass posterizePass;
    private FullScreenMaterialPass stylizedPass;

    public override void Create()
    {
        posterizePass = new FullScreenMaterialPass("Stylized Posterize");
        stylizedPass = new FullScreenMaterialPass("Stylized Post");
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        StylizedLook look = StylizedLook.Active;

        if (look == null)
            return;

        if (look.PosterizeEnabled && EnsureMaterial(ref posterizeMaterial, posterizeShader))
        {
            look.ApplyPosterize(posterizeMaterial);
            posterizePass.Setup(posterizeMaterial, passEvent, false);
            renderer.EnqueuePass(posterizePass);
        }

        if (look.StylizedEnabled && EnsureMaterial(ref stylizedMaterial, stylizedShader))
        {
            look.ApplyStylized(stylizedMaterial);
            stylizedPass.Setup(stylizedMaterial, passEvent + 1, look.OutlineEnabled);
            renderer.EnqueuePass(stylizedPass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(posterizeMaterial);
        CoreUtils.Destroy(stylizedMaterial);
        posterizeMaterial = null;
        stylizedMaterial = null;
    }

    private static bool EnsureMaterial(ref Material material, Shader shader)
    {
        if (material != null && material.shader == shader)
            return true;

        CoreUtils.Destroy(material);
        material = null;

        if (shader == null || !shader.isSupported)
            return false;

        material = CoreUtils.CreateEngineMaterial(shader);
        return true;
    }

    private sealed class FullScreenMaterialPass : ScriptableRenderPass
    {
        private sealed class PassData
        {
            public TextureHandle source;
            public Material material;
        }

        private readonly string label;
        private Material material;
        private bool needsDepth;

        public FullScreenMaterialPass(string passName)
        {
            label = passName;
            profilingSampler = new ProfilingSampler(passName);
            requiresIntermediateTexture = true;
        }

        public void Setup(Material passMaterial, RenderPassEvent passEvent, bool depth)
        {
            material = passMaterial;
            renderPassEvent = passEvent;
            needsDepth = depth;
            ConfigureInput(depth ? ScriptableRenderPassInput.Depth : ScriptableRenderPassInput.None);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            if (cameraData.cameraType == CameraType.Preview || cameraData.cameraType == CameraType.Reflection)
                return;

            UniversalResourceData resources = frameData.Get<UniversalResourceData>();

            if (resources.isActiveTargetBackBuffer || material == null)
                return;

            TextureHandle source = resources.activeColorTexture;
            TextureDesc desc = renderGraph.GetTextureDesc(source);
            desc.name = label;
            desc.clearBuffer = false;
            TextureHandle destination = renderGraph.CreateTexture(desc);

            using (IRasterRenderGraphBuilder builder =
                   renderGraph.AddRasterRenderPass(label, out PassData data, profilingSampler))
            {
                data.source = source;
                data.material = material;

                builder.UseTexture(source, AccessFlags.Read);

                if (needsDepth && resources.cameraDepthTexture.IsValid())
                    builder.UseTexture(resources.cameraDepthTexture, AccessFlags.Read);

                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                builder.SetRenderFunc(static (PassData d, RasterGraphContext context) =>
                    Blitter.BlitTexture(context.cmd, d.source, new Vector4(1f, 1f, 0f, 0f), d.material, 0));
            }

            resources.cameraColor = destination;
        }
    }
}
