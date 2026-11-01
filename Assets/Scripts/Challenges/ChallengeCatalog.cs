public sealed class ChallengeDefinition
{
    public ChallengeDefinition(
        int number,
        string name,
        string description,
        string progressLabel,
        int rootIndex,
        int targetBaskets = 0,
        float timeLimit = 0f,
        bool noMisses = false)
    {
        Number = number;
        Name = name;
        Description = description;
        ProgressLabel = progressLabel;
        RootIndex = rootIndex;
        TargetBaskets = targetBaskets;
        TimeLimit = timeLimit;
        NoMisses = noMisses;
    }

    public int Number { get; }

    public string Name { get; }

    public string Description { get; }

    public string ProgressLabel { get; }

    // Posición en ChallengeSelector de la raíz de la escena que usa este desafío
    public int RootIndex { get; }

    public int TargetBaskets { get; }

    // 0 = sin límite de tiempo
    public float TimeLimit { get; }

    // Un tiro sin encestar termina el desafío
    public bool NoMisses { get; }

    public int RequiredBaskets =>
        ChallengeProgress.RequiredBaskets(Number);
}

public static class ChallengeCatalog
{
    private const int CarouselRoot = 0;
    private const int EvasiveRoot = 1;
    private const int FixedRoot = 2;

    private static readonly ChallengeDefinition[] Challenges =
    {
        new(1, "Carrusel", "5 aros girando en círculo. Encestá en cada uno.", "Aros", CarouselRoot),
        new(2, "Aro esquivo", "Un aro que no se queda quieto. Encestá 3 veces.", "Encestadas", EvasiveRoot, 3),
        new(3, "Contrarreloj", "8 encestadas en 20 segundos.", "Puntos", FixedRoot, 8, 20f),
        new(4, "Sin fallar", "5 encestadas seguidas. Un error y perdés.", "Racha", FixedRoot, 5, noMisses: true),
        new(5, "Persecución", "El aro esquivo, con reloj: 4 encestadas en 40 segundos.", "Encestadas", EvasiveRoot, 4, 40f)
    };

    public static int Count =>
        Challenges.Length;

    public static ChallengeDefinition Get(int number) =>
        Challenges[number - 1];
}
