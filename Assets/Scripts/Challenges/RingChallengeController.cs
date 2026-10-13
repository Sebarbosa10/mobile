using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Desafío 1 "Carrusel": varios aros girando en círculo. Hay que
/// encestar en cada uno (una sola vez cada uno) para ganar; el aro
/// acertado queda pintado de verde. No hay límite de tiempo.
/// </summary>
public sealed class RingChallengeController : ChallengeControllerBase
{
    private static readonly Color ScoredColor = new(0.25f, 0.85f, 0.35f);

    private readonly HashSet<HoopScoreTrigger> scoredHoops = new();
    private readonly Dictionary<HoopScoreTrigger, Color> originalColors = new();

    protected override int ChallengeNumber => 1;

    protected override int CurrentProgress =>
        scoredHoops.Count;

    protected override int TargetProgress =>
        hoops.Length;

    protected override void Awake()
    {
        base.Awake();

        foreach (HoopScoreTrigger hoop in hoops)
        {
            if (hoop == null)
                continue;

            Renderer hoopRenderer = hoop.GetComponent<Renderer>();

            if (hoopRenderer != null)
                originalColors[hoop] = hoopRenderer.material.color;
        }
    }

    protected override void ResetChallengeState()
    {
        scoredHoops.Clear();

        foreach (HoopScoreTrigger hoop in hoops)
        {
            if (hoop != null && originalColors.TryGetValue(hoop, out Color original))
                TintHoop(hoop, original);
        }
    }

    protected override void OnHoopScored(HoopScoreTrigger hoop)
    {
        if (!scoredHoops.Add(hoop))
            return;

        TintHoop(hoop, ScoredColor);

        ReportProgress(hoop);

        if (scoredHoops.Count >= hoops.Length)
            Finish(true, $"Encestaste en los {hoops.Length} aros");
    }

    private static void TintHoop(HoopScoreTrigger hoop, Color color)
    {
        Renderer hoopRenderer = hoop.GetComponent<Renderer>();

        if (hoopRenderer != null)
            hoopRenderer.material.color = color;
    }
}
