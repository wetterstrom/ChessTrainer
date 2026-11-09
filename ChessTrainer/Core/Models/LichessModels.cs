using System.Text.Json.Serialization;

namespace ChessTrainer.Core.Models;

// Modeller för användarens egna partier (NDJSON från Lichess)
public record LichessGameDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("createdAt")] long CreatedAt,
    [property: JsonPropertyName("moves")] string Moves,
    [property: JsonPropertyName("winner")] string? Winner,
    [property: JsonPropertyName("players")] LichessPlayersDto Players,
    [property: JsonPropertyName("opening")] LichessOpeningDto? Opening
)
{
    public string WhiteName => Players.White.User?.Name ?? "Anonym";
    public string BlackName => Players.Black.User?.Name ?? "Anonym";
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
public record LichessExplorerResponseDto(
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
}