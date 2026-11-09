namespace ChessTrainer.Core.Interfaces;

public interface IOpponentStrategy
{
    Task<string> GetNextMoveUciAsync(string fen, int targetRating = 1500);
}