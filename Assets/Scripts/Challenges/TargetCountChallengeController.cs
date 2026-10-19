using UnityEngine;

public sealed class TargetCountChallengeController : ChallengeControllerBase
{
    [SerializeField, Min(1)]
    private int targetHits = 3;

    private int hitCount;

    protected override int ChallengeNumber => 2;

    protected override int CurrentProgress =>
        hitCount;

    protected override int TargetProgress =>
        targetHits;

    protected override void ResetChallengeState()
    {
        hitCount = 0;
    }

    protected override void OnHoopScored(HoopScoreTrigger hoop)
    {
        hitCount++;

        ReportProgress(hoop);

        if (hitCount >= targetHits)
            Finish(true, $"Encestaste {targetHits} veces");
    }
}
