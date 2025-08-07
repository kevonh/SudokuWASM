using Microsoft.AspNetCore.Components;
using Sudoku.Services;
using Sudoku.Models;

namespace SudokuWASM.Pages;
public partial class SudokuGame : IDisposable
{
    [Inject] private IGamePersistenceService PersistenceService { get; set; } = default!;
    [Inject] private GameStatePersistenceService StatePersistenceService { get; set; } = default!;

    private Sudoku.SudokuBoard? board;
    private string message = string.Empty;
    private int wrongGuessCount = 0;
    private int currentScore = 0;
    private int hintCount = 0;
    private const int maxWrongGuesses = 4;
    private bool isGameOver = false;
    private bool isGameWon = false;
    private bool[,] wrongCells = new bool[9, 9];
    private (int row, int col)? selectedCell = null;
    private bool isInitialized = false;
    private bool isLoading = true;
    private string elapsedTime = "00:00";
    private GameTimingService? gameTimingService;
    private bool pencilMode = false;
    private string selectedDifficulty = "Medium";
    private bool showStatistics = false;
    private bool showConfirmClearStats = false;
    private bool isPointsAnimating = false;
    private GameStatistics gameStatistics = new();
    private string currentGameId = Guid.NewGuid().ToString();

    // Simplified pause functionality - we'll use save/restore instead of separate arrays
    private bool isGamePaused = false;

    // Properties for advanced functionality
    private bool CanEditSelectedCell => 
        selectedCell.HasValue && 
        board != null && 
        board.CanEditCell(selectedCell.Value.row, selectedCell.Value.col) &&
        !isGamePaused;

    private string DisplayScore => currentScore.ToString("N0");

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !isInitialized)
        {
            await Task.Delay(1000); // Simulate loading
            await LoadGameStatisticsAsync();
            await InitializeGameAsync();
            isInitialized = true;
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task InitializeGameAsync()
    {
        // Check if there's a saved game first
        var savedGame = await PersistenceService.LoadGameStateAsync();
        
        if (savedGame != null && !savedGame.IsGameWon && !savedGame.IsGameOver)
        {
            // Load the saved game
            LoadGameFromState(savedGame);
            message = "Resumed previous game";
        }
        else
        {
            // Start a new game
            InitializeNewGame();
        }
    }

    private void LoadGameFromState(GameState gameState)
    {
        board = new Sudoku.SudokuBoard();
        
        // Restore board state
        GameState.CopyToMultiArray(gameState.Grid, board.Grid);
        GameState.CopyToMultiArray(gameState.Solution, board.Solution);
        GameState.CopyToMultiArray(gameState.FixedCells, board.FixedCells);
        GameState.CopyToMultiArray(gameState.HintCells, board.HintCells);
        GameState.CopyToMultiArray(gameState.CorrectlySolvedCells, board.CorrectlySolvedCells);
        GameState.CopyToMultiArray(gameState.WrongCells, wrongCells);
        
        // Restore notes
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                var key = $"{r},{c}";
                if (gameState.Notes.ContainsKey(key))
                {
                    board.Notes[r, c].Clear();
                    foreach (var note in gameState.Notes[key])
                    {
                        board.Notes[r, c].Add(note);
                    }
                }
            }
        }
        
        // Restore game state
        currentGameId = gameState.Id;
        selectedDifficulty = gameState.SelectedDifficulty;
        wrongGuessCount = gameState.WrongGuessCount;
        hintCount = gameState.HintCount;
        currentScore = gameState.CurrentScore;
        isGameOver = gameState.IsGameOver;
        isGameWon = gameState.IsGameWon;
        pencilMode = gameState.PencilMode;
        
        // Restore pause state - always start paused regardless of saved state
        isGamePaused = true;
        ClearVisibleBoard();
        
        if (gameState.SelectedRow.HasValue && gameState.SelectedCol.HasValue)
        {
            selectedCell = (gameState.SelectedRow.Value, gameState.SelectedCol.Value);
        }
        
        // Initialize timing service and restore time
        gameTimingService = new GameTimingService(OnTimerUpdated);
        gameTimingService.RestoreWithPauseState(gameState.StartTime, gameState.LastMoveTime, gameState.TotalElapsed, true); // always paused
        // Do NOT resume timer here
    }

    private void InitializeNewGame()
    {
        // Always reset to default difficulty if not set
        if (string.IsNullOrEmpty(selectedDifficulty) || !new[]{"Easy","Medium","Hard","Expert"}.Contains(selectedDifficulty))
        {
            selectedDifficulty = "Medium";
        }
        currentGameId = Guid.NewGuid().ToString();
        board = new Sudoku.SudokuBoard();
        board.GenerateNewPuzzle(GetDifficultyClues(selectedDifficulty)); 
        wrongCells = new bool[9, 9];
        wrongGuessCount = 0;
        currentScore = 0;
        hintCount = 0;
        isGameOver = false;
        isGameWon = false;
        isGamePaused = true; // always start paused
        selectedCell = null;
        pencilMode = false;
        message = "New puzzle generated!";
        
        // Initialize timing service
        gameTimingService?.Dispose();
        gameTimingService = new GameTimingService(OnTimerUpdated);
        // Do NOT start timer here
        
        // Save the initial game state
        _ = Task.Run(SaveGameStateAsync);
        StateHasChanged(); // Ensure UI updates
    }

    private void OnTimerUpdated()
    {
        if (gameTimingService != null)
        {
            elapsedTime = gameTimingService.ElapsedTime;
            InvokeAsync(StateHasChanged);
        }
    }

    private void TogglePause()
    {
        if (board == null || isGameOver || isGameWon) return;

        if (isGamePaused)
        {
            // Resume the game
            ResumeGame();
        }
        else
        {
            // Pause the game
            PauseGame();
        }
    }

    private void PauseGame()
    {
        if (board == null || isGamePaused) return;

        // Save current state first (this preserves the actual progress in the saved game)
        _ = Task.Run(SaveGameStateAsync);

        // Clear the visible board (but keep fixed cells visible)
        ClearVisibleBoard();

        // Pause the timer
        gameTimingService?.PauseTimer();
        
        isGamePaused = true;
        message = "Game paused";
        
        StateHasChanged();
    }

    private void ResumeGame()
    {
        if (board == null || !isGamePaused) return;

        // Reload from saved state to restore the actual progress
        _ = Task.Run(async () =>
        {
            var savedGame = await PersistenceService.LoadGameStateAsync();
            if (savedGame != null)
            {
                // Restore the board state without changing pause status yet
                GameState.CopyToMultiArray(savedGame.Grid, board.Grid);
                
                // Restore notes
                for (int r = 0; r < 9; r++)
                {
                    for (int c = 0; c < 9; c++)
                    {
                        var key = $"{r},{c}";
                        if (savedGame.Notes.ContainsKey(key))
                        {
                            board.Notes[r, c].Clear();
                            foreach (var note in savedGame.Notes[key])
                            {
                                board.Notes[r, c].Add(note);
                            }
                        }
                    }
                }
            }

            // Resume the timer
            gameTimingService?.ResumeTimer();
            
            isGamePaused = false;
            message = "Game resumed";
            
            // Save the updated pause state
            await SaveGameStateAsync();
            await InvokeAsync(StateHasChanged);
        });
    }

    private void ClearVisibleBoard()
    {
        if (board == null) return;

        // Clear all user-entered values but keep fixed cells
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                if (!board.FixedCells[r, c])
                {
                    board.Grid[r, c] = 0;
                    board.Notes[r, c].Clear();
                }
            }
        }
    }

    private void OnGridClick()
    {
        // If the game is paused and user clicks anywhere on the grid, resume
        if (isGamePaused)
        {
            ResumeGame();
        }
    }

    private int GetDifficultyClues(string difficulty) => difficulty switch
    {
        "Easy" => 45,
        "Medium" => 35,
        "Hard" => 25,
        "Expert" => 20,
        _ => 35
    };

    private void OnCellClick(int row, int col)
    {
        if (board == null || isGameOver || isGameWon) return;
        
        // If paused, resume the game instead of selecting a cell
        if (isGamePaused)
        {
            ResumeGame();
            return;
        }
        
        selectedCell = (row, col);
        _ = Task.Run(SaveGameStateAsync);
        StateHasChanged();
    }

    // Helper method to handle tuple parameter for OnCellClick
    private void OnCellClickTuple((int row, int col) cellCoordinates)
    {
        OnCellClick(cellCoordinates.row, cellCoordinates.col);
    }

    private void OnNumberClick(int number)
    {
        if (pencilMode)
            OnNumberPadClick(number);
        else
            PlaceNumber(number);
    }

    private void OnNumberPadClick(int number)
    {
        if (board == null || selectedCell == null || isGameOver || isGameWon || isGamePaused) return;
        var (row, col) = selectedCell.Value;

        if (!board.CanEditCell(row, col)) return;

        if (pencilMode)
        {
            // Toggle note
            board.ToggleNote(row, col, number);
        }
        else
        {
            PlaceNumber(number);
        }
        
        _ = Task.Run(SaveGameStateAsync);
        StateHasChanged();
    }

    private void PlaceNumber(int number)
    {
        if (board == null || selectedCell == null || isGameOver || isGameWon || isGamePaused) return;
        var (row, col) = selectedCell.Value;

        if (!board.CanEditCell(row, col)) return;

        gameTimingService?.RecordMove();

        if (board.IsCorrectMove(row, col, number))
        {
            board.SetCell(row, col, number);
            board.ClearNotes(row, col); // Clear notes when placing a number
            currentScore += GetPointsForMove();
            message = $"+{GetPointsForMove()} points!";
            wrongCells[row, col] = false;
            _ = Task.Run(AnimatePointsAsync);

            if (board.IsComplete())
            {
                isGameWon = true;
                gameTimingService?.StopTimer();
                message = "Puzzle Complete!";
                _ = Task.Run(async () => {
                    await RecordCompletedGameAsync();
                    await UpdateStatisticsAsync();
                    await PersistenceService.DeleteGameStateAsync(); // Clear saved game
                });
            }
            else
            {
                _ = Task.Run(SaveGameStateAsync);
            }
        }
        else
        {
            // Place the wrong number so it can be displayed and styled in red
            board.SetCell(row, col, number);
            board.ClearNotes(row, col); // Clear notes when placing a number
            wrongGuessCount++;
            wrongCells[row, col] = true;
            currentScore = Math.Max(0, currentScore - 5);
            message = "Wrong move! (-5 points)";

            if (wrongGuessCount >= maxWrongGuesses)
            {
                isGameOver = true;
                gameTimingService?.StopTimer();
                message = "Game Over!";
                _ = Task.Run(async () => {
                    await RecordCompletedGameAsync();
                    await UpdateStatisticsAsync();
                    await PersistenceService.DeleteGameStateAsync(); // Clear saved game
                });
            }
            else
            {
                _ = Task.Run(SaveGameStateAsync);
            }
        }

        StateHasChanged();
    }

    private async Task SaveGameStateAsync()
    {
        if (board == null || gameTimingService == null || isGameOver || isGameWon) return;

        try
        {
            var gameState = StatePersistenceService.CreateGameState(
                board,
                currentGameId,
                selectedDifficulty,
                wrongGuessCount,
                hintCount,
                currentScore,
                isGameOver,
                isGameWon,
                pencilMode,
                wrongCells,
                selectedCell,
                gameTimingService,
                isGamePaused
            );

            await PersistenceService.SaveGameStateAsync(gameState);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving game state: {ex.Message}");
        }
    }

    private async Task RecordCompletedGameAsync()
    {
        if (gameTimingService == null) return;

        var completedGame = new CompletedGame
        {
            Id = currentGameId,
            Difficulty = selectedDifficulty,
            Score = currentScore,
            CompletionTime = gameTimingService.GetTotalElapsed(),
            HintsUsed = hintCount,
            WrongMoves = wrongGuessCount,
            IsPerfect = wrongGuessCount == 0 && hintCount == 0,
            CompletedAt = DateTime.Now
        };

        await PersistenceService.RecordCompletedGameAsync(completedGame);
    }

    private int GetPointsForMove()
    {
        return selectedDifficulty switch
        {
            "Easy" => 5,
            "Medium" => 10,
            "Hard" => 15,
            "Expert" => 20,
            _ => 10
        };
    }

    private async Task AnimatePointsAsync()
    {
        isPointsAnimating = true;
        StateHasChanged();
        await Task.Delay(500);
        isPointsAnimating = false;
        StateHasChanged();
    }

    private bool IsNumberPadButtonDisabled(int number)
    {
        if (board == null || isGamePaused) return true;
        
        // Count how many times this number appears on the board
        int count = 0;
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                if (board.Grid[r, c] == number)
                    count++;
            }
        }
        
        return count >= 9; // Disable if all 9 instances are placed
    }

    private void Erase()
    {
        if (board == null || selectedCell == null || isGameOver || isGameWon || isGamePaused) return;
        var (row, col) = selectedCell.Value;

        if (board.CanEditCell(row, col))
        {
            board.SetCell(row, col, 0);
            board.ClearNotes(row, col);
            wrongCells[row, col] = false;
            gameTimingService?.RecordMove();
            _ = Task.Run(SaveGameStateAsync);
            StateHasChanged();
        }
    }

    private void Undo()
    {
        // Simple undo - just erase the current cell
        Erase();
    }

    private void TogglePencilMode()
    {
        if (isGamePaused) return;
        
        pencilMode = !pencilMode;
        message = pencilMode ? "Notes mode ON" : "Notes mode OFF";
        _ = Task.Run(SaveGameStateAsync);
        StateHasChanged();
    }

    private void GiveHint()
    {
        if (board == null || isGamePaused) return;

        var emptyCells = new List<(int row, int col)>();
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                if (board.Grid[r, c] == 0)
                    emptyCells.Add((r, c));
            }
        }

        if (emptyCells.Count > 0)
        {
            var random = new Random();
            var (row, col) = emptyCells[random.Next(emptyCells.Count)];
            board.AddHint(row, col, board.Solution[row, col]);
            board.ClearNotes(row, col);
            wrongCells[row, col] = false;
            hintCount++;
            currentScore = Math.Max(0, currentScore - 10);
            message = "Hint used! (-10 points)";
            gameTimingService?.RecordMove();
            _ = Task.Run(SaveGameStateAsync);
            StateHasChanged();
        }
    }

    private void SetDifficulty(string difficulty)
    {
        selectedDifficulty = difficulty;
        StateHasChanged();
    }

    private void NewGame()
    {
        GenerateNewGame();
    }

    private void GenerateNewGame()
    {
        // Clear any existing saved game
        _ = Task.Run(async () => await PersistenceService.DeleteGameStateAsync());
        InitializeNewGame();
        StateHasChanged();
    }

    private async Task ShowStatisticsAsync()
    {
        await LoadGameStatisticsAsync();
        showStatistics = true;
        StateHasChanged();
    }

    public async Task ClearStatsAsync()
    {
        await PersistenceService.ClearAllDataAsync();
        gameStatistics = new GameStatistics();
        showConfirmClearStats = false;
        showStatistics = false;
        message = "All statistics cleared!";
        StateHasChanged();
        await Task.Delay(2000);
        message = "";
        StateHasChanged();
    }

    private async Task LoadGameStatisticsAsync()
    {
        gameStatistics = await PersistenceService.GetStatisticsAsync();
    }

    private async Task UpdateStatisticsAsync()
    {
        // Statistics are automatically updated by RecordCompletedGameAsync
        // Just reload them to refresh the UI
        gameStatistics = await PersistenceService.GetStatisticsAsync();
    }

    private string GetCellCSS(int row, int col)
    {
        var stylingService = new Sudoku.Services.CellStylingService(board, wrongCells, selectedCell, false);
        return stylingService.GetCellCSS(row, col);
    }

    private string GetCellTextCSS(int row, int col)
    {
        var stylingService = new Sudoku.Services.CellStylingService(board, wrongCells, selectedCell, false);
        return stylingService.GetCellTextCSS(row, col);
    }

    private string GetMobileCellCSS(int row, int col)
    {
        var stylingService = new Sudoku.Services.CellStylingService(board, wrongCells, selectedCell, true);
        return stylingService.GetCellCSS(row, col);
    }

    private string GetMobileCellTextCSS(int row, int col)
    {
        var stylingService = new Sudoku.Services.CellStylingService(board, wrongCells, selectedCell, true);
        return stylingService.GetCellTextCSS(row, col);
    }

    private string GetNotesTextCSS(bool isMobile)
    {
        var stylingService = new Sudoku.Services.CellStylingService(board, wrongCells, selectedCell, isMobile);
        return stylingService.GetNotesTextCSS();
    }

    public void Dispose()
    {
        gameTimingService?.Dispose();
    }
}