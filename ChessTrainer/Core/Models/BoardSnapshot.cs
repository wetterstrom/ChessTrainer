namespace ChessTrainer.Core.Models;

public record BoardSnapshot(
    string Fen,
    string? MovePlayedUci,
    string? MovePlayedSan,
    MaterialBalance Material,
    EvaluationResult? Evaluation = null
);