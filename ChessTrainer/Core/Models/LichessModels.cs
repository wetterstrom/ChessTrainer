using System.Text.Json.Serialization;

namespace ChessTrainer.Core.Models;

// Modeller för användarens egna partier (NDJSON från Lichess)
public record LichessGameDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("createdAt")] long CreatedAt,
    [property: JsonPropertyName("moves")] string Moves,
    [property: JsonPropertyName("winner")] string? Winner,
    [property: JsonPropertyName("players")] LichessPlayersDto? Players,
    [property: JsonPropertyName("opening")] LichessOpeningDto? Opening
)
{
    public string? CustomWhiteName { get; init; }
    public string? CustomBlackName { get; init; }

    public string WhiteName => CustomWhiteName ?? Players?.White?.User?.Name ?? "Anonym";
    public string BlackName => CustomBlackName ?? Players?.Black?.User?.Name ?? "Anonym";
    public DateTime PlayedDate => DateTimeOffset.FromUnixTimeMilliseconds(CreatedAt).LocalDateTime;
}

public record LichessPlayersDto(
    [property: JsonPropertyName("white")] LichessPlayerDetailDto White,
    [property: JsonPropertyName("black")] LichessPlayerDetailDto Black
);

public record LichessPlayerDetailDto(
    [property: JsonPropertyName("user")] LichessUserDto? User,
    [property: JsonPropertyName("rating")] int? Rating
);

public record LichessUserDto([property: JsonPropertyName("name")] string Name);
public record LichessOpeningDto([property: JsonPropertyName("name")] string Name);

// Modeller för Lichess Cloud Evaluation API
public record LichessCloudEvalDto(
    [property: JsonPropertyName("pvs")] List<LichessPvDto>? Pvs
);

public record LichessPvDto(
    [property: JsonPropertyName("moves")] string Moves,
    [property: JsonPropertyName("cp")] int? Cp,
    [property: JsonPropertyName("mate")] int? Mate
);

// Modeller för Lichess Opening Explorer (Mänskliga drag på vald rating)
// 3. Modeller för Lichess Opening Explorer (Mänskliga drag + Öppningskompass)
public record LichessExplorerResponseDto(
    [property: JsonPropertyName("opening")] LichessOpeningDto? Opening,
    [property: JsonPropertyName("moves")] List<LichessExplorerMoveDto> Moves
);

public record LichessExplorerMoveDto(
    [property: JsonPropertyName("uci")] string Uci,
    [property: JsonPropertyName("san")] string San,
    [property: JsonPropertyName("white")] int WhiteWins,
    [property: JsonPropertyName("draws")] int Draws,
    [property: JsonPropertyName("black")] int BlackWins
)
{
    public int TotalGames => WhiteWins + Draws + BlackWins;
    public double WhitePct => TotalGames > 0 ? Math.Round(WhiteWins * 100.0 / TotalGames, 1) : 0;
    public double DrawPct => TotalGames > 0 ? Math.Round(Draws * 100.0 / TotalGames, 1) : 0;
    public double BlackPct => TotalGames > 0 ? Math.Round(100.0 - WhitePct - DrawPct, 1) : 0;
}