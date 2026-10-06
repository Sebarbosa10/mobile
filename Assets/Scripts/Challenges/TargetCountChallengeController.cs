using UnityEngine;

/// <summary>
/// Desafío 2 "Aro esquivo": encestar una cantidad fija de veces en un
/// aro que se mueve de forma esquiva (ver EvasiveHoopMover). No importa
/// cuántos tiros falles en el camino, solo cuenta cuántas veces encestaste.
/// </summary>
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
