using ChessTrainer.Core.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ChessTrainer.Services;

public class LichessApiClient(HttpClient httpClient)
{
    private readonly Random _random = new();

    // Hämta användarens senaste partier
    public async Task<List<LichessGameDto>> GetUserGamesAsync(string username, int maxGames = 15)
    {
        var games = new List<LichessGameDto>();
        if (string.IsNullOrWhiteSpace(username)) return games;

        string url = $"https://lichess.org/api/games/user/{Uri.EscapeDataString(username.Trim())}?max={maxGames}&opening=true&pgnInJson=false";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-ndjson"));

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode) return games;

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var game = JsonSerializer.Deserialize<LichessGameDto>(line);
            if (game is not null && !string.IsNullOrWhiteSpace(game.Moves))
            {
                games.Add(game);
            }
        }

        return games;
    }

    // Hämta Cloud Eval (returnerar null om ställningen inte finns i Lichess molncache)
    public async Task<EvaluationResult?> GetCloudEvalAsync(string fen)
    {
        try
        {
            string url = $"https://lichess.org/api/cloud-eval?fen={Uri.EscapeDataString(fen)}&multiPv=1";
            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var dto = await response.Content.ReadFromJsonAsync<LichessCloudEvalDto>();
            var firstPv = dto?.Pvs?.FirstOrDefault();
            if (firstPv is null) return null;

            string bestMove = firstPv.Moves.Split(' ').FirstOrDefault() ?? string.Empty;

            return new EvaluationResult
            {
                Centipawns = firstPv.Cp,
                MateInMoves = firstPv.Mate,
                BestMoveUci = bestMove
            };
        }
        catch
        {
            return null;
        }
    }

    // Hämta ett viktat mänskligt drag från Lichess databas för en specifik rating
    public async Task<string?> GetHumanLikeMoveUciAsync(string fen, int targetRating = 1500)
    {
        try
        {
            // Mappa målets rating till Lichess-intervall (t.ex. 1400,1600)
            string ratingBucket = targetRating switch
            {
                <= 1200 => "1000,1200",
                <= 1500 => "1400,1600",
                _ => "1800,2000"
            };

            string url = $"https://explorer.lichess.ovh/lichess?variant=standard&speeds=rapid,classical&ratings={ratingBucket}&fen={Uri.EscapeDataString(fen)}";
            var dto = await httpClient.GetFromJsonAsync<LichessExplorerResponseDto>(url);

            if (dto?.Moves is null || dto.Moves.Count == 0)
                return null;

            int totalGamesInPosition = dto.Moves.Sum(m => m.TotalGames);
            if (totalGamesInPosition < 5)
                return null; // För få mänskliga partier i denna ställning -> låt lokal motor ta över

            // Viktat slumpmässigt val (sannolikhet proportionell mot hur ofta människor på nivån spelar draget)
            int roll = _random.Next(0, totalGamesInPosition);
            int cumulative = 0;

            foreach (var move in dto.Moves)
            {
                cumulative += move.TotalGames;
                if (roll < cumulative)
                    return move.Uci;
            }

            return dto.Moves[0].Uci;
        }
        catch
        {
            return null;
        }
    }

    // 4. Hämta full öppningsstatistik för Öppningskompassen
    public async Task<LichessExplorerResponseDto?> GetOpeningExplorerStatsAsync(string fen, int targetRating = 1500)
    {
        try
        {
            string ratingBucket = targetRating switch
            {
                <= 1200 => "1000,1200",
                <= 1500 => "1400,1600",
                _ => "1800,2000"
            };

            string url = $"https://explorer.lichess.ovh/lichess?variant=standard&speeds=rapid,classical&ratings={ratingBucket}&topGames=0&recentGames=0&fen={Uri.EscapeDataString(fen)}";
            return await httpClient.GetFromJsonAsync<LichessExplorerResponseDto>(url);
        }
        catch
        {
            return null;
        }
    }
}