namespace ChessTrainer.Core.Models;

public record EvaluationResult
{
    public int? Centipawns { get; init; }
    public int? MateInMoves { get; init; }
    public string BestMoveUci { get; init; } = string.Empty;

    // Formaterad sträng för UI, t.ex. "+1.4" eller "M3"
    public string FormattedScore
    {
        get
        {
            if (MateInMoves.HasValue)
                return $"M{Math.Abs(MateInMoves.Value)}";

            if (Centipawns.HasValue)
            {
                double pawns = Centipawns.Value / 100.0;
                return pawns >= 0 ? $"+{pawns:0.0}" : $"{pawns:0.0}";
            }

            return "0.0";
        }
    }

    // Räknar ut Vits vinstchans (0-100%) för Eval-baren
    public double WhiteWinPercentage
    {
        get
        {
            if (MateInMoves.HasValue)
                return MateInMoves.Value > 0 ? 100.0 : 0.0;

            if (!Centipawns.HasValue)
                return 50.0;

            double cp = Math.Clamp(Centipawns.Value, -1000, 1000);
            double multiplier = -0.00368208;
            return 50.0 + 50.0 * (2.0 / (1.0 + Math.Exp(multiplier * cp)) - 1.0);
        }
    }

    // Hämtar källrutan (t.ex. "e2" från "e2e4") för Nivå 1-tips
    public string HintSourceSquare =>
        BestMoveUci.Length >= 2 ? BestMoveUci[..2] : string.Empty;
}