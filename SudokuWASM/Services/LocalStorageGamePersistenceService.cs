using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Sudoku.Models;

namespace Sudoku.Services;

public class LocalStorageGamePersistenceService : IGamePersistenceService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<LocalStorageGamePersistenceService> _logger;
    private const string CURRENT_GAME_KEY = "sudoku_current_game";
    private const string STATISTICS_KEY = "sudoku_statistics";
    private const string RECENT_GAMES_KEY = "sudoku_recent_games";

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public LocalStorageGamePersistenceService(IJSRuntime jsRuntime, ILogger<LocalStorageGamePersistenceService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task SaveGameStateAsync(GameState gameState)
    {
        try
        {
            gameState.SavedAt = DateTime.Now;
            var json = JsonSerializer.Serialize(gameState, _jsonOptions);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", CURRENT_GAME_KEY, json);
            _logger.LogDebug("Game state saved successfully. Size: {Size} characters", json.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving game state: {Message}", ex.Message);
        }
    }

    public async Task<GameState?> LoadGameStateAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", CURRENT_GAME_KEY);
            if (string.IsNullOrEmpty(json))
            {
                _logger.LogDebug("No saved game state found in localStorage");
                return null;
            }

            _logger.LogDebug("Loading game state. Size: {Size} characters", json.Length);
            var gameState = JsonSerializer.Deserialize<GameState>(json, _jsonOptions);
            _logger.LogDebug("Game state loaded successfully. ID: {GameId}", gameState?.Id);
            return gameState;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading game state: {Message}", ex.Message);
            return null;
        }
    }

    public async Task DeleteGameStateAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", CURRENT_GAME_KEY);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting game state: {Message}", ex.Message);
        }
    }

    public async Task<bool> HasSavedGameAsync()
    {
        var gameState = await LoadGameStateAsync();
        return gameState != null && !gameState.IsGameWon && !gameState.IsGameOver;
    }

    public async Task<GameStatistics> GetStatisticsAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", STATISTICS_KEY);
            if (string.IsNullOrEmpty(json))
                return new GameStatistics { FirstPlayed = DateTime.Now };

            return JsonSerializer.Deserialize<GameStatistics>(json, _jsonOptions) ?? new GameStatistics();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading statistics: {Message}", ex.Message);
            return new GameStatistics();
        }
    }

    public async Task SaveStatisticsAsync(GameStatistics statistics)
    {
        try
        {
            var json = JsonSerializer.Serialize(statistics, _jsonOptions);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", STATISTICS_KEY, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving statistics: {Message}", ex.Message);
        }
    }

    public async Task RecordCompletedGameAsync(CompletedGame game)
    {
        try
        {
            // Update statistics
            var stats = await GetStatisticsAsync();
            stats.GamesPlayed++;
            stats.LastPlayed = DateTime.Now;
            
            if (stats.FirstPlayed == default)
                stats.FirstPlayed = DateTime.Now;

            if (game.Score > 0) // Only count as won if score > 0
            {
                stats.GamesWon++;
                
                if (game.IsPerfect)
                    stats.PerfectGames++;
                
                if (game.Score > stats.BestScore)
                    stats.BestScore = game.Score;
                
                if (game.CompletionTime < stats.BestTime)
                    stats.BestTime = game.CompletionTime;
            }

            // Update difficulty-specific stats
            if (!stats.DifficultyStats.ContainsKey(game.Difficulty))
                stats.DifficultyStats[game.Difficulty] = new DifficultyStats();

            var diffStats = stats.DifficultyStats[game.Difficulty];
            diffStats.Played++;
            
            if (game.Score > 0)
            {
                diffStats.Won++;
                
                if (game.IsPerfect)
                    diffStats.Perfect++;
                
                if (game.Score > diffStats.BestScore)
                    diffStats.BestScore = game.Score;
                
                if (game.CompletionTime < diffStats.BestTime)
                    diffStats.BestTime = game.CompletionTime;
            }

            await SaveStatisticsAsync(stats);

            // Save to recent games
            var recentGames = await GetRecentGamesAsync();
            recentGames.Insert(0, game);
            
            // Keep only last 50 games
            if (recentGames.Count > 50)
                recentGames = recentGames.Take(50).ToList();

            var gamesJson = JsonSerializer.Serialize(recentGames, _jsonOptions);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", RECENT_GAMES_KEY, gamesJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording completed game: {Message}", ex.Message);
        }
    }

    public async Task<List<CompletedGame>> GetRecentGamesAsync(int count = 10)
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", RECENT_GAMES_KEY);
            if (string.IsNullOrEmpty(json))
                return new List<CompletedGame>();

            var games = JsonSerializer.Deserialize<List<CompletedGame>>(json, _jsonOptions) ?? new List<CompletedGame>();
            return games.Take(count).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading recent games: {Message}", ex.Message);
            return new List<CompletedGame>();
        }
    }

    public async Task ClearAllDataAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", CURRENT_GAME_KEY);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", STATISTICS_KEY);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", RECENT_GAMES_KEY);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing data: {Message}", ex.Message);
        }
    }
}