using ChessDotNet;
using ChessTrainer.Core.Models;

namespace ChessTrainer.Services;

public record PlayerReviewStats(
    double Accuracy,
    Dictionary<MoveQuality, int> Counts
);

public record GameReviewSummary(
    PlayerReviewStats White,
    PlayerReviewStats Black,
    List<double> WhiteWinPercentages,
    string WhiteName,
    string BlackName
);

public class GameReviewService(StockfishInteropService stockfish)
{
    public async Task<GameReviewSummary> AnalyzeFullGameAsync(
        GameHistoryManager history,
        Action<int, int>? onProgress = null)
    {
        int total = history.Timeline.Count;

        if (history.Timeline[0].Evaluation is null)
        {
            var startEval = await EvaluateSafeAsync(history.Timeline[0].Fen);
            history.UpdateSnapshotAt(0, startEval, MoveQuality.None, 0);
        }

        var whiteCounts = Enum.GetValues<MoveQuality>().ToDictionary(k => k, _ => 0);
        var blackCounts = Enum.GetValues<MoveQuality>().ToDictionary(k => k, _ => 0);

        double whiteAccSum = 0; int whiteMoves = 0;
        double blackAccSum = 0; int blackMoves = 0;

        for (int i = 1; i < total; i++)
        {
            onProgress?.Invoke(i, total - 1);
            await Task.Delay(1);

            var prevSnap = history.Timeline[i - 1];
            var currSnap = history.Timeline[i];

            var eval = currSnap.Evaluation ?? await EvaluateSafeAsync(currSnap.Fen);

            bool whiteMoved = i % 2 == 1;
            double prevWin = whiteMoved
                ? (prevSnap.Evaluation?.WhiteWinPercentage ?? 50.0)
                : 100.0 - (prevSnap.Evaluation?.WhiteWinPercentage ?? 50.0);

            double currWin = whiteMoved
                ? eval.WhiteWinPercentage
                : 100.0 - eval.WhiteWinPercentage;

            double winLoss = Math.Max(0, prevWin - currWin);
            string bestMoveBefore = prevSnap.Evaluation?.BestMoveUci ?? "";
            bool playedEngineBest = !string.IsNullOrEmpty(bestMoveBefore) &&
                string.Equals(currSnap.MovePlayedUci, bestMoveBefore, StringComparison.OrdinalIgnoreCase);

            int prevMatDiff = whiteMoved ? prevSnap.Material.Difference : -prevSnap.Material.Difference;
            int currMatDiff = whiteMoved ? currSnap.Material.Difference : -currSnap.Material.Difference;
            bool sacrificedMaterial = currMatDiff < prevMatDiff && winLoss < 2.0 && currWin > 45.0;

            MoveQuality quality = ClassifyMove(i, playedEngineBest, sacrificedMaterial, winLoss, prevWin, currWin);

            history.UpdateSnapshotAt(i, eval, quality, currWin - prevWin);

            // Samla statistik för tabellen och precision
            double loss = Math.Max(0, -(currWin - prevWin));
            double moveAcc = Math.Clamp(103.1668 * Math.Exp(-0.04354 * loss) - 3.1669, 0, 100);

            if (whiteMoved)
            {
                whiteCounts[quality]++;
                whiteAccSum += moveAcc;
                whiteMoves++;
            }
            else
            {
                blackCounts[quality]++;
                blackAccSum += moveAcc;
                blackMoves++;
            }
        }

        var whiteWinPcts = history.Timeline.Select(s => s.Evaluation?.WhiteWinPercentage ?? 50.0).ToList();

        var whiteStats = new PlayerReviewStats(whiteMoves > 0 ? Math.Round(whiteAccSum / whiteMoves, 1) : 100, whiteCounts);
        var blackStats = new PlayerReviewStats(blackMoves > 0 ? Math.Round(blackAccSum / blackMoves, 1) : 100, blackCounts);

        return new GameReviewSummary(
            whiteStats,
            blackStats,
            whiteWinPcts,
            "Vit",
            "Svart"
        );
    }

    private async Task<EvaluationResult> EvaluateSafeAsync(string fen)
    {
        try
        {
            var game = new ChessGame(fen);
            var legalMoves = game.GetValidMoves(game.WhoseTurn);

            if (legalMoves.Count == 0)
            {
                if (game.IsInCheck(Player.White))
                    return new EvaluationResult { MateInMoves = -1, BestMoveUci = "" };
                if (game.IsInCheck(Player.Black))
                    return new EvaluationResult { MateInMoves = 1, BestMoveUci = "" };

                return new EvaluationResult { Centipawns = 0, BestMoveUci = "" };
            }

            return await stockfish.AnalyzePositionAsync(fen, depth: 8, skillLevel: 20);
        }
        catch
        {
            return new EvaluationResult { Centipawns = 0, BestMoveUci = "" };
        }
    }

    private static MoveQuality ClassifyMove(
        int moveIndex,
        bool playedEngineBest,
        bool sacrificedMaterial,
        double winLoss,
        double prevWin,
        double currWin)
    {
        if (moveIndex <= 6 && winLoss <= 2.5)
            return MoveQuality.Book;

        if (sacrificedMaterial && playedEngineBest)
            return MoveQuality.Brilliant;

        if (playedEngineBest && (currWin - prevWin >= 12.0 || (prevWin is >= 35 and <= 65 && winLoss <= 0.5)))
            return MoveQuality.Great;

        if (playedEngineBest || winLoss <= 1.2)
            return MoveQuality.Best;

        if (winLoss <= 3.5)
            return MoveQuality.Excellent;

        if (winLoss <= 7.0)
            return MoveQuality.Good;

        if (winLoss <= 13.0)
            return MoveQuality.Inaccuracy;

        if (winLoss <= 22.0)
            return MoveQuality.Mistake;

        return MoveQuality.Blunder;
    }
}