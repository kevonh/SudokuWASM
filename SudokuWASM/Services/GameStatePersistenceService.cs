using Microsoft.Extensions.Logging;
using Sudoku.Models;

namespace Sudoku.Services;

/// <summary>
/// Service to handle game state persistence operations - loading and saving game state
/// </summary>
public class GameStatePersistenceService
{
    private readonly IGamePersistenceService _persistenceService;
    private readonly ILogger<GameStatePersistenceService> _logger;

    public GameStatePersistenceService(IGamePersistenceService persistenceService, ILogger<GameStatePersistenceService> logger)
    {
        _persistenceService = persistenceService;
        _logger = logger;
    }

    public GameState CreateGameState(
        SudokuBoard board,
        string currentGameId,
        string selectedDifficulty,
        int wrongGuessCount,
        int hintCount,
        int currentScore,
        bool isGameOver,
        bool isGameWon,
        bool pencilMode,
        bool[,] wrongCells,
        (int row, int col)? selectedCell,
        GameTimingService? timingService)
    {
        if (board == null || timingService == null) return new GameState();
        
        var gameState = new GameState
        {
            Id = currentGameId,
            SelectedDifficulty = selectedDifficulty,
            WrongGuessCount = wrongGuessCount,
            HintCount = hintCount,
            CurrentScore = currentScore,
            IsGameOver = isGameOver,
            IsGameWon = isGameWon,
            PencilMode = pencilMode,
            StartTime = timingService.StartTime,
            LastMoveTime = timingService.LastMoveTime,
            TotalElapsed = timingService.GetTotalElapsed()
        };
        
        GameState.CopyFromMultiArray(board.Grid, gameState.Grid);
        GameState.CopyFromMultiArray(board.Solution, gameState.Solution);
        GameState.CopyFromMultiArray(board.FixedCells, gameState.FixedCells);
        GameState.CopyFromMultiArray(board.HintCells, gameState.HintCells);
        GameState.CopyFromMultiArray(board.CorrectlySolvedCells, gameState.CorrectlySolvedCells);
        GameState.CopyFromMultiArray(wrongCells, gameState.WrongCells);
        
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                var key = $"{r},{c}";
                gameState.Notes[key] = board.Notes[r, c].ToArray();
            }
        }
        
        if (selectedCell.HasValue)
        {
            gameState.SelectedRow = selectedCell.Value.row;
            gameState.SelectedCol = selectedCell.Value.col;
        }
        
        return gameState;
    }

    public async Task SaveGameStateAsync(
        SudokuBoard? board,
        bool hasRendered,
        string currentGameId,
        string selectedDifficulty,
        int wrongGuessCount,
        int hintCount,
        int currentScore,
        bool isGameOver,
        bool isGameWon,
        bool pencilMode,
        bool[,] wrongCells,
        (int row, int col)? selectedCell,
        GameTimingService? timingService)
    {
        if (board == null || !hasRendered) return;
        
        try
        {
            var gameState = CreateGameState(
                board, currentGameId, selectedDifficulty, wrongGuessCount, 
                hintCount, currentScore, isGameOver, isGameWon, pencilMode, 
                wrongCells, selectedCell, timingService);
            
            await _persistenceService.SaveGameStateAsync(gameState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving game: {Message}", ex.Message);
        }
    }

    public bool LoadGameFromState(
        GameState gameState,
        SudokuBoard board,
        bool[,] wrongCells,
        GameTimingService? timingService,
        Action<string> setSelectedDifficulty,
        Action<int> setWrongGuessCount,
        Action<int> setHintCount,
        Action<int> setCurrentScore,
        Action<bool> setIsGameOver,
        Action<bool> setIsGameWon,
        Action<bool> setPencilMode,
        Action<string> setCurrentGameId,
        Action<(int row, int col)?> setSelectedCell)
    {
        try
        {
            GameState.CopyToMultiArray(gameState.Grid, board.Grid);
            GameState.CopyToMultiArray(gameState.Solution, board.Solution);
            GameState.CopyToMultiArray(gameState.FixedCells, board.FixedCells);
            GameState.CopyToMultiArray(gameState.HintCells, board.HintCells);
            GameState.CopyToMultiArray(gameState.CorrectlySolvedCells, board.CorrectlySolvedCells);
            GameState.CopyToMultiArray(gameState.WrongCells, wrongCells);
            
            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    var key = $"{r},{c}";
                    if (gameState.Notes.TryGetValue(key, out var notes))
                    {
                        board.Notes[r, c] = new HashSet<int>(notes);
                    }
                    else
                    {
                        board.Notes[r, c] = new HashSet<int>();
                    }
                }
            }
            
            setSelectedDifficulty(gameState.SelectedDifficulty);
            setWrongGuessCount(gameState.WrongGuessCount);
            setHintCount(gameState.HintCount);
            setCurrentScore(gameState.CurrentScore);
            setIsGameOver(gameState.IsGameOver);
            setIsGameWon(gameState.IsGameWon);
            setPencilMode(gameState.PencilMode);
            setCurrentGameId(gameState.Id);
            
            // Restore timing state first, then resume timer if game is active
            if (timingService != null)
            {
                // Use the new method that properly handles total elapsed time
                if (gameState.TotalElapsed != TimeSpan.Zero)
                {
                    // If we have total elapsed time, use it for more accurate restoration
                    timingService.RestoreTimeWithTotal(gameState.StartTime, gameState.LastMoveTime, gameState.TotalElapsed);
                }
                else
                {
                    // Fall back to the old method for backward compatibility
                    timingService.RestoreTime(gameState.StartTime, gameState.LastMoveTime);
                }
                
                // Resume the timer if the game is still active (not over and not won)
                if (!gameState.IsGameOver && !gameState.IsGameWon)
                {
                    timingService.ResumeTimer();
                }
            }
            
            if (gameState.SelectedRow.HasValue && gameState.SelectedCol.HasValue)
            {
                setSelectedCell((gameState.SelectedRow.Value, gameState.SelectedCol.Value));
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading game state: {Message}", ex.Message);
            return false;
        }
    }
}