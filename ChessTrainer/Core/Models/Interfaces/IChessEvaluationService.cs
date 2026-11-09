using ChessTrainer.Core.Models;

namespace ChessTrainer.Core.Interfaces;

public interface IChessEvaluationService
{
    Task<EvaluationResult> EvaluatePositionAsync(string fen, int depth = 14);
}