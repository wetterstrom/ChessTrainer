using System.Collections.ObjectModel;
using ChessDotNet;

namespace ChessTrainer.Services;

public static class ChessMoveParser
{
    public static string ToUci(Move move)
    {
        string promo = move.Promotion.HasValue ? char.ToLowerInvariant(move.Promotion.Value).ToString() : "";
        return $"{move.OriginalPosition.ToString().ToLowerInvariant()}{move.NewPosition.ToString().ToLowerInvariant()}{promo}";
    }

    public static bool TryApplyUciMove(ChessGame game, string uci, out string newFen, out string appliedUci)
    {
        newFen = game.GetFen();
        appliedUci = string.Empty;

        if (string.IsNullOrWhiteSpace(uci) || uci.Length < 4)
            return false;

        string from = uci[..2];
        string to = uci.Substring(2, 2);
        char? promo = uci.Length > 4 ? char.ToUpperInvariant(uci[4]) : null;

        var move = new Move(from, to, game.WhoseTurn, promo);
        if (!game.IsValidMove(move))
            return false;

        game.MakeMove(move, true);
        newFen = game.GetFen();
        appliedUci = ToUci(move);
        return true;
    }

    public static void LoadSanMovesIntoHistory(string sanMovesText, GameHistoryManager history)
    {
        history.Reset();
        if (string.IsNullOrWhiteSpace(sanMovesText)) return;

        var game = new ChessGame();
        var tokens = sanMovesText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var san in tokens)
        {
            var matchedMove = FindMoveBySan(game, san);
            if (matchedMove is null) break;

            game.MakeMove(matchedMove, true);
            history.RecordMove(game.GetFen(), ToUci(matchedMove), san);
        }

        history.JumpToMove(0);
    }

    private static Move? FindMoveBySan(ChessGame game, string rawSan)
    {
        string san = rawSan.TrimEnd('+', '#', '!', '?');
        Player player = game.WhoseTurn;
        ReadOnlyCollection<Move> legalMoves = game.GetValidMoves(player);

        // 1. Rockad (Kort & Lång)
        if (san is "O-O" or "0-0")
        {
            return legalMoves.FirstOrDefault(m =>
                game.GetPieceAt(m.OriginalPosition)?.GetFenCharacter() is 'K' or 'k' &&
                m.OriginalPosition.File == ChessDotNet.File.E &&
                m.NewPosition.File == ChessDotNet.File.G);
        }

        if (san is "O-O-O" or "0-0-0")
        {
            return legalMoves.FirstOrDefault(m =>
                game.GetPieceAt(m.OriginalPosition)?.GetFenCharacter() is 'K' or 'k' &&
                m.OriginalPosition.File == ChessDotNet.File.E &&
                m.NewPosition.File == ChessDotNet.File.C);
        }

        // 2. Bonde-promotering (t.ex. e8=Q eller exd8=N)
        char? promotion = null;
        if (san.Contains('='))
        {
            var parts = san.Split('=');
            san = parts[0];
            promotion = char.ToUpperInvariant(parts[1][0]);
        }

        if (san.Length < 2) return null;

        string targetSquare = san[^2..];
        string prefix = san[..^2].Replace("x", "");

        char pieceType = 'P';
        if (prefix.Length > 0 && char.IsUpper(prefix[0]))
        {
            pieceType = prefix[0];
            prefix = prefix[1..];
        }

        char? disambigFile = null;
        char? disambigRank = null;
        foreach (char c in prefix)
        {
            if (c is >= 'a' and <= 'h') disambigFile = c;
            if (c is >= '1' and <= '8') disambigRank = c;
        }

        foreach (var move in legalMoves)
        {
            if (!string.Equals(move.NewPosition.ToString(), targetSquare, StringComparison.OrdinalIgnoreCase))
                continue;

            if (move.Promotion != promotion)
                continue;

            var piece = game.GetPieceAt(move.OriginalPosition);
            if (piece is null) continue;

            if (char.ToUpperInvariant(piece.GetFenCharacter()) != pieceType)
                continue;

            string origStr = move.OriginalPosition.ToString().ToLowerInvariant();
            if (disambigFile.HasValue && origStr[0] != disambigFile.Value)
                continue;
            if (disambigRank.HasValue && origStr[1] != disambigRank.Value)
                continue;

            return move;
        }

        return null;
    }

    // This method is used to convert a SAN move to UCI format, given the current game state.
}