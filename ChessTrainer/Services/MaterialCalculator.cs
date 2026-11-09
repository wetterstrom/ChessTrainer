using ChessTrainer.Core.Models;

namespace ChessTrainer.Services;

public static class MaterialCalculator
{
    private static readonly Dictionary<char, int> PieceValues = new()
    {
        ['p'] = 1,
        ['n'] = 3,
        ['b'] = 3,
        ['r'] = 5,
        ['q'] = 9,
        ['k'] = 0
    };

    private static readonly Dictionary<char, int> StartingCounts = new()
    {
        ['P'] = 8,
        ['N'] = 2,
        ['B'] = 2,
        ['R'] = 2,
        ['Q'] = 1,
        ['p'] = 8,
        ['n'] = 2,
        ['b'] = 2,
        ['r'] = 2,
        ['q'] = 1
    };

    public static MaterialBalance CalculateFromFen(string fen)
    {
        if (string.IsNullOrWhiteSpace(fen))
            return new MaterialBalance(0, 0, [], []);

        string piecePlacement = fen.Split(' ')[0];
        var currentCounts = StartingCounts.Keys.ToDictionary(k => k, _ => 0);

        int whitePoints = 0;
        int blackPoints = 0;

        foreach (char c in piecePlacement)
        {
            char lower = char.ToLowerInvariant(c);
            if (!PieceValues.TryGetValue(lower, out int value))
                continue;

            if (char.IsUpper(c))
                whitePoints += value;
            else
                blackPoints += value;

            if (currentCounts.ContainsKey(c))
                currentCounts[c]++;
        }

        var capturedByWhite = new List<char>();
        var capturedByBlack = new List<char>();

        foreach (var (piece, startCount) in StartingCounts)
        {
            int missing = Math.Max(0, startCount - currentCounts[piece]);
            for (int i = 0; i < missing; i++)
            {
                // Om en svart pjäs (gemener) saknas har Vit slagit den
                if (char.IsLower(piece))
                    capturedByWhite.Add(piece);
                else
                    capturedByBlack.Add(piece);
            }
        }

        return new MaterialBalance(whitePoints, blackPoints, capturedByWhite, capturedByBlack);
    }
}