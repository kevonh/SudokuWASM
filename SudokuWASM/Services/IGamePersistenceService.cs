using Sudoku.Models;

namespace Sudoku.Services;

public interface IGamePersistenceService
{
    Task SaveGameStateAsync(GameState gameState);
    Task<GameState?> LoadGameStateAsync();
    Task DeleteGameStateAsync();
    Task<bool> HasSavedGameAsync();
    
    Task<GameStatistics> GetStatisticsAsync();
    Task SaveStatisticsAsync(GameStatistics statistics);
    Task RecordCompletedGameAsync(CompletedGame game);
    
    Task<List<CompletedGame>> GetRecentGamesAsync(int count = 10);
    Task ClearAllDataAsync();
}