using ChessTrainer.Core.Interfaces;

namespace ChessTrainer.Services;

public class MaiaOpponentService(
    LichessApiClient lichessClient,
    StockfishInteropService stockfish) : IOpponentStrategy
{
    public async Task<string> GetNextMoveUciAsync(string fen, int targetRating = 1500)
    {
        // 1. Hämta autentiskt mänskligt drag från Lichess-spelare på samma rating
        string? humanDbMove = await lichessClient.GetHumanLikeMoveUciAsync(fen, targetRating);
        if (!string.IsNullOrEmpty(humanDbMove))
        {
            return humanDbMove;
        }

        // 2. Om ställningen är ny (utanför databasen), simulera mänsklig nivå via Skill Level & begränsat djup
        (int skillLevel, int depth) = targetRating switch
        {
            <= 1100 => (3, 5),
            <= 1500 => (8, 7),
            _ => (14, 10)
        };

        var eval = await stockfish.AnalyzePositionAsync(fen, depth, skillLevel);
        return eval.BestMoveUci;
    }
}