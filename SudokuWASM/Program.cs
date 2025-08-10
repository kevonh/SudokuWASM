using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SudokuWASM;
using Sudoku.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register game persistence services
builder.Services.AddScoped<IGamePersistenceService, LocalStorageGamePersistenceService>();
builder.Services.AddScoped<GameStatePersistenceService>();

// Register the modular GameEngine
builder.Services.AddScoped<SudokuWASM.Services.IGameEngine, SudokuWASM.Services.GameEngine>();

await builder.Build().RunAsync();
