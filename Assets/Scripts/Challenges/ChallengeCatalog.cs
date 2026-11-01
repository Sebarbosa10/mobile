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
        new(1, "Carousel", "5 hoops spinning in a circle. Score in each one.", "Hoops", CarouselRoot),
        new(2, "Dodgy Hoop", "A hoop that won't sit still. Score 3 times.", "Baskets", EvasiveRoot, 3),
        new(3, "Time Attack", "8 baskets in 20 seconds.", "Points", FixedRoot, 8, 20f),
        new(4, "No Misses", "5 baskets in a row. One miss and you lose.", "Streak", FixedRoot, 5, noMisses: true),
        new(5, "Hoop Chase", "The dodgy hoop, on the clock: 4 baskets in 40 seconds.", "Baskets", EvasiveRoot, 4, 40f)
    };

    public static int Count =>
        Challenges.Length;

    public static ChallengeDefinition Get(int number) =>
        Challenges[number - 1];
}
