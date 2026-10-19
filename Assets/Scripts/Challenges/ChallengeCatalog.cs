public sealed class ChallengeDefinition
{
    public ChallengeDefinition(
        int number,
        string name,
        string description,
        string progressLabel)
    {
        Number = number;
        Name = name;
        Description = description;
        ProgressLabel = progressLabel;
    }

    public int Number { get; }

    public string Name { get; }

    public string Description { get; }

    public string ProgressLabel { get; }

    public int RequiredBaskets =>
        ChallengeProgress.RequiredBaskets(Number);
}

public static class ChallengeCatalog
{
    private static readonly ChallengeDefinition[] Challenges =
    {
        new(1, "Carrusel", "5 aros girando en círculo. Encestá en cada uno.", "Aros"),
        new(2, "Aro esquivo", "Un aro que no se queda quieto. Encestá 3 veces.", "Encestadas"),
        new(3, "Contrarreloj", "8 encestadas en 20 segundos.", "Puntos")
    };

    public static int Count =>
        Challenges.Length;

    public static ChallengeDefinition Get(int number) =>
        Challenges[number - 1];
}
