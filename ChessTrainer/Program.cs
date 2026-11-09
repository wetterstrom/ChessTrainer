using ChessTrainer.Core.Interfaces;
using ChessTrainer.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ChessTrainer;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Registrera våra tjänster
builder.Services.AddScoped<GameHistoryManager>();
builder.Services.AddScoped<LichessApiClient>();
builder.Services.AddScoped<StockfishInteropService>();
builder.Services.AddScoped<IChessEvaluationService, ChessAnalysisFacade>();
builder.Services.AddScoped<IOpponentStrategy, MaiaOpponentService>();

await builder.Build().RunAsync();