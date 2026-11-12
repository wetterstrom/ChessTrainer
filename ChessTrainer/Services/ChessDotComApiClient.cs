using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ChessTrainer.Core.Models;

namespace ChessTrainer.Services;

public record ChessDotComArchiveDto(
    [property: JsonPropertyName("archives")] List<string> Archives
);

public record ChessDotComMonthlyResponse(
    [property: JsonPropertyName("games")] List<ChessDotComGameJson> Games
);

public record ChessDotComGameJson(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("pgn")] string Pgn,
    [property: JsonPropertyName("end_time")] long EndTime,
    [property: JsonPropertyName("white")] ChessDotComPlayerInfo White,
    [property: JsonPropertyName("black")] ChessDotComPlayerInfo Black
);

public record ChessDotComPlayerInfo(
    [property: JsonPropertyName("username")] string Username
);

public class ChessDotComApiClient(HttpClient httpClient)
{
    public async Task<List<LichessGameDto>> GetUserGamesAsync(string username, int maxCount = 15)
    {
        try
        {
            username = username.Trim().ToLowerInvariant();
            string archiveListUrl = $"https://api.chess.com/pub/player/{username}/games/archives";

            if (!httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", "ChessTrainerApp (kontakt@chess-trainer.local)");
            }

            var archiveResponse = await httpClient.GetFromJsonAsync<ChessDotComArchiveDto>(archiveListUrl);
            if (archiveResponse?.Archives is not { Count: > 0 }) return [];

            string latestMonthUrl = archiveResponse.Archives[^1];
            var monthlyData = await httpClient.GetFromJsonAsync<ChessDotComMonthlyResponse>(latestMonthUrl);

            if (monthlyData?.Games is not { Count: > 0 }) return [];

            var results = new List<LichessGameDto>();

            foreach (var g in monthlyData.Games.AsEnumerable().Reverse())
            {
                if (results.Count >= maxCount) break;

                var dtDateTime = DateTimeOffset.FromUnixTimeSeconds(g.EndTime).DateTime;
                var moves = ExtractMovesFromPgn(g.Pgn);
                string openingName = ExtractHeaderFromPgn(g.Pgn, "Opening") ?? "Chess.com-parti";

                results.Add(new LichessGameDto(
                        Id: g.Url.Split('/').Last(),
                        CreatedAt: g.EndTime * 1000,
                        Moves: string.Join(" ", moves),
                        Winner: null,
                        Players: null,
                        Opening: new LichessOpeningDto(openingName))

                {
                    CustomWhiteName = g.White.Username,
                    CustomBlackName = g.Black.Username
                });
            }

            return results;
        }
        catch
        {
            return [];
        }
    }

    private static List<string> ExtractMovesFromPgn(string pgn)
    {
        var moves = new List<string>();
        if (string.IsNullOrWhiteSpace(pgn)) return moves;

        // 1. Ta bort alla PGN-huvuden (rader inom klamrar [])
        var cleanLines = pgn.Split('\n')
            .Where(line => !line.TrimStart().StartsWith('['))
            .Select(line => line.Trim());

        string fullMoveText = string.Join(" ", cleanLines);

        // 2. Dela upp texten i ord
        var tokens = fullMoveText.Split([' ', '\r', '\t', '\n'], StringSplitOptions.RemoveEmptyEntries);

        foreach (var token in tokens)
        {
            // Hoppa över dragnummer (t.ex. "1.", "2."), resultat och kommentarer
            if (token.EndsWith('.') || token == "1-0" || token == "0-1" || token == "1/2-1/2" || token.Contains('$') || token.StartsWith('{') || token.EndsWith('}'))
                continue;

            // Om token ser ut som "1.e4" eller "3...Nf6", dela vid punkten
            if (token.Contains('.'))
            {
                var parts = token.Split('.');
                string actualMove = parts.Last(); // Ta sista delen efter eventuella punkter
                if (!string.IsNullOrWhiteSpace(actualMove) && !actualMove.Contains('.'))
                {
                    moves.Add(actualMove);
                }
            }
            else
            {
                moves.Add(token);
            }
        }

        return moves;
    }

    private static string? ExtractHeaderFromPgn(string pgn, string headerName)
    {
        foreach (var line in pgn.Split('\n'))
        {
            if (line.StartsWith($"[{headerName} "))
            {
                int start = line.IndexOf('"') + 1;
                int end = line.LastIndexOf('"');
                if (end > start)
                {
                    return line.Substring(start, end - start);
                }
            }
        }
        return null;
    }
}