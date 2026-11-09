using ChessTrainer.Core.Models;
using Microsoft.JSInterop;

namespace ChessTrainer.Services;

public class StockfishInteropService(IJSRuntime jsRuntime)
{
    private record JsEvalResponse(int? Centipawns, int? MateInMoves, string BestMoveUci);

    public async Task<EvaluationResult> AnalyzePositionAsync(string fen, int depth = 13, int skillLevel = 20)
    {
        var raw = await jsRuntime.InvokeAsync<JsEvalResponse>(
            "stockfishInterop.analyze", fen, depth, skillLevel);

        // UCI ger score utifrån den som står på tur. Om Svart står på tur (" b "), invertera till Vits perspektiv.
        bool isBlackTurn = fen.Split(' ').ElementAtOrDefault(1) == "b";
        int multiplier = isBlackTurn ? -1 : 1;

        return new EvaluationResult
        {
            Centipawns = raw.Centipawns.HasValue ? raw.Centipawns.Value * multiplier : null,
            MateInMoves = raw.MateInMoves.HasValue ? raw.MateInMoves.Value * multiplier : null,
            BestMoveUci = raw.BestMoveUci ?? string.Empty
        };
    }
}