using UnityEngine;
using UnityEngine.SceneManagement;

public static class HoopPhysicsTuning
{
    public const float RimBounciness = 0.2f;

    public const float BoardBounciness = 0.35f;

    public const float RimFriction = 0.5f;

    public const float ChallengeHoopScale = 1.15f;

    private static PhysicsMaterial rimMaterial;
    private static PhysicsMaterial boardMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (HoopVisual hoop in root.GetComponentsInChildren<HoopVisual>(true))
                Tune(hoop);

            foreach (BallLauncher ball in root.GetComponentsInChildren<BallLauncher>(true))
            {
                if (!ball.TryGetComponent(out ShotAssist _))
                    ball.gameObject.AddComponent<ShotAssist>();
            }
        }
    }

    public static void Tune(HoopVisual hoop)
    {
        rimMaterial ??= Create("Aro", RimBounciness, RimFriction);
        boardMaterial ??= Create("Tablero", BoardBounciness, 0.4f);

        foreach (Collider collider in hoop.GetComponentsInChildren<Collider>(true))
        {
            if (collider.isTrigger)
                continue;

            bool rim = collider is CapsuleCollider;
            collider.sharedMaterial = rim ? rimMaterial : boardMaterial;
        }

        if (hoop.GetComponentInParent<HoopController>() == null)
            hoop.SetScale(ChallengeHoopScale);
    }

    private static PhysicsMaterial Create(string name, float bounciness, float friction) =>
        new(name)
        {
            bounciness = bounciness,
            bounceCombine = PhysicsMaterialCombine.Minimum,
            dynamicFriction = friction,
            staticFriction = friction,
            frictionCombine = PhysicsMaterialCombine.Average
        };
}
