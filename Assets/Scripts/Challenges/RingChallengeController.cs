using System.Collections.Generic;
using UnityEngine;

public sealed class RingChallengeController : ChallengeControllerBase
{
    private readonly HashSet<HoopScoreTrigger> scoredHoops = new();

    protected override int ChallengeNumber => 1;

    protected override int CurrentProgress =>
        scoredHoops.Count;

    protected override int TargetProgress =>
        hoops.Length;

    protected override void ResetChallengeState()
    {
        scoredHoops.Clear();

        foreach (HoopScoreTrigger hoop in hoops)
        {
            if (hoop != null)
                MarkHoop(hoop, false);
        }
    }

    protected override void OnHoopScored(HoopScoreTrigger hoop)
    {
        if (!scoredHoops.Add(hoop))
            return;

        MarkHoop(hoop, true);

        ReportProgress(hoop);

        if (scoredHoops.Count >= hoops.Length)
            Finish(true, $"Encestaste en los {hoops.Length} aros");
    }

    private static void MarkHoop(HoopScoreTrigger hoop, bool scored)
    {
        HoopVisual visual = hoop.Visual;

        if (visual == null)
            return;

        if (scored)
            visual.SetColor(HoopVisual.SuccessColor);
        else
            visual.ResetColor();

        visual.SetCheckVisible(scored);
    }
}
