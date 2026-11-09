using ChessTrainer.Core.Interfaces;
using ChessTrainer.Core.Models;

namespace ChessTrainer.Services;

public class ChessAnalysisFacade(
    LichessApiClient lichessClient,
    StockfishInteropService stockfish) : IChessEvaluationService
{
    public async Task<EvaluationResult> EvaluatePositionAsync(string fen, int depth = 14)
    {
        var cloudEval = await lichessClient.GetCloudEvalAsync(fen);
        if (cloudEval is not null && !string.IsNullOrEmpty(cloudEval.BestMoveUci))
        {
            return cloudEval;
        }

        // Fallback till lokal Stockfish med maximal styrka (Skill Level 20)
        return await stockfish.AnalyzePositionAsync(fen, depth, skillLevel: 20);
    }
}