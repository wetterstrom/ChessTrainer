namespace ChessTrainer.Core.Models;

public record MaterialBalance(
    int WhitePoints,
    int BlackPoints,
    IReadOnlyList<char> CapturedByWhite,
    IReadOnlyList<char> CapturedByBlack)
{
    // Positivt tal = Vit leder, Negativt tal = Svart leder
    public int Difference => WhitePoints - BlackPoints;

    public string AdvantageText => Difference switch
    {
        > 0 => $"+{Difference} (Vit)",
        < 0 => $"+{Math.Abs(Difference)} (Svart)",
        _ => "Lika (0)"
    };
}