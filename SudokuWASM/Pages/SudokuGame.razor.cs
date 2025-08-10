using Microsoft.AspNetCore.Components;
using Sudoku.Services;
using Sudoku.Models;
using System.Threading;
using System.Threading.Tasks;

namespace SudokuWASM.Pages;
public partial class SudokuGame 
{
    [Inject] private IGamePersistenceService PersistenceService { get; set; } = default!;
    [Inject] private GameStatePersistenceService StatePersistenceService { get; set; } = default!;
    [Inject] private IPuzzleGeneratorService PuzzleGenerator { get; set; } = default!;

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
    private string elapsedTime = "00:00";
    private GameTimingService? gameTimingService;
    private bool pencilMode = false;
    private string selectedDifficulty = "Medium";
    private bool showStatistics = false;
    private bool showConfirmClearStats = false;
    private bool isPointsAnimating = false;
    private GameStatistics gameStatistics = new();
    private string currentGameId = Guid.NewGuid().ToString();
    private bool isGamePaused = false;
    private CancellationTokenSource? generationCts;
    private bool showOptionsModal = false;

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
            await InitializeGameAsync();
            isInitialized = true;
            StateHasChanged();
        }
    }

    private async Task InitializeGameAsync()
    {
        var savedGame = await PersistenceService.LoadGameStateAsync();
        if (savedGame != null && !savedGame.IsGameWon && !savedGame.IsGameOver)
        {
            LoadGameFromState(savedGame);
            message = "Resumed previous game";
        }
        else
        {
            await InitializeNewGameAsync();
        }
    }

    private void LoadGameFromState(GameState gameState)
    {
        board = new Sudoku.SudokuBoard();
        GameState.CopyToMultiArray(gameState.Grid, board.Grid);
        GameState.CopyToMultiArray(gameState.Solution, board.Solution);
        GameState.CopyToMultiArray(gameState.FixedCells, board.FixedCells);
        GameState.CopyToMultiArray(gameState.HintCells, board.HintCells);
        GameState.CopyToMultiArray(gameState.CorrectlySolvedCells, board.CorrectlySolvedCells);
        GameState.CopyToMultiArray(gameState.WrongCells, wrongCells);

        foreach (var kvp in gameState.Notes)
        {
            var parts = kvp.Key.Split(',');
            int r = int.Parse(parts[0]);
            int c = int.Parse(parts[1]);
            board.Notes[r, c].Clear();
            foreach (var note in kvp.Value)
                board.Notes[r, c].Add(note);
        }

        currentGameId = gameState.Id;
        selectedDifficulty = gameState.SelectedDifficulty;
        wrongGuessCount = gameState.WrongGuessCount;
        hintCount = gameState.HintCount;
        currentScore = gameState.CurrentScore;
        isGameOver = gameState.IsGameOver;
        isGameWon = gameState.IsGameWon;
        pencilMode = gameState.PencilMode;

        // Use the persisted pause state
        isGamePaused = gameState.IsPaused;

        if (gameState.SelectedRow.HasValue && gameState.SelectedCol.HasValue)
            selectedCell = (gameState.SelectedRow.Value, gameState.SelectedCol.Value);

        gameTimingService = new GameTimingService(OnTimerUpdated);
        gameTimingService.RestoreWithPauseState(
            gameState.StartTime,
            gameState.LastMoveTime,
            gameState.TotalElapsed,
            isGamePaused
        );
    }

    private async Task InitializeNewGameAsync()
    {
        if (string.IsNullOrEmpty(selectedDifficulty) || !new[] { "Easy", "Medium", "Hard", "Expert" }.Contains(selectedDifficulty))
        {
            selectedDifficulty = "Medium";
        }
        currentGameId = Guid.NewGuid().ToString();
        generationCts?.Cancel();
        generationCts = new CancellationTokenSource();
        var options = new PuzzleOptions
        {
            Difficulty = selectedDifficulty switch
            {
                "Easy" => DifficultyLevel.Easy,
                "Medium" => DifficultyLevel.Medium,
                "Hard" => DifficultyLevel.Hard,
                "Expert" => DifficultyLevel.Expert,
                _ => DifficultyLevel.Medium
            },
            // Add more options as needed (Symmetry, Variant, Seed, ClueCount)
        };
        board = await PuzzleGenerator.GeneratePuzzleAsync(options, generationCts.Token);
        wrongCells = new bool[9, 9];
        wrongGuessCount = 0;
        currentScore = 0;
        hintCount = 0;
        isGameOver = false;
        isGameWon = false;
        isGamePaused = true;
        selectedCell = null;
        pencilMode = false;
        message = "New puzzle generated!";
        gameTimingService?.Dispose();
        gameTimingService = new GameTimingService(OnTimerUpdated);
        StateHasChanged();
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
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    private void PauseGame()
    {
        if (board == null || isGamePaused) return;
        isGamePaused = true;
        gameTimingService?.PauseTimer();
        message = "Game paused";
        StateHasChanged();
    }

    private void ResumeGame()
    {
        if (board == null || !isGamePaused) return;
        isGamePaused = false;
        gameTimingService?.ResumeTimer();
        message = "Game resumed";
        StateHasChanged();
    }

    // Called by the modal when the user starts a new game.
    // Accepts PuzzleOptions from GameOptionsModal and generates a new puzzle.
    private async Task CreateNewGameAsync(PuzzleOptions options)
    {
        // Map difficulty back to string for storage; adjust as needed.
        selectedDifficulty = options.Difficulty switch
        {
            DifficultyLevel.Easy => "Easy",
            DifficultyLevel.Medium => "Medium",
            DifficultyLevel.Hard => "Hard",
            DifficultyLevel.Expert => "Expert",
            _ => "Medium"
        };
        showOptionsModal = false;
        await InitializeNewGameAsync();
    }

    // Toggles the visibility of the New Game options modal.
    private void ToggleOptionsModal(bool value)
    {
        showOptionsModal = value;
        StateHasChanged();
    }

    // Handles click on the Sudoku grid’s background (e.g. deselect a cell).
    private void OnGridClick()
    {
        selectedCell = null;
        StateHasChanged();
    }

    // Handles click on an individual cell (tuple wrapper used by the Razor file).
    private void OnCellClickTuple((int row, int col) cell)
    {
        OnCellClick(cell.row, cell.col);
    }

    // Handles click on a row/column cell.  Selects the cell and clears any message.
    private void OnCellClick(int row, int col)
    {
        if (board == null || isGamePaused) return;
        selectedCell = (row, col);
        message = string.Empty;
        StateHasChanged();
    }

    // Compute CSS classes for each cell based on selection and wrong guesses.
    private string GetCellCSS(int row, int col)
    {
        if (board == null) return "";
        var styling = new Sudoku.Services.CellStylingService(board, wrongCells, selectedCell, false);
        return styling.GetCellCSS(row, col);
    }

    private string GetCellTextCSS(int row, int col)
    {
        if (board == null) return "";
        var styling = new Sudoku.Services.CellStylingService(board, wrongCells, selectedCell, false);
        return styling.GetCellTextCSS(row, col);
    }

    private string GetNotesTextCSS(bool isMobile)
    {
        if (board == null) return "";
        var styling = new Sudoku.Services.CellStylingService(board, wrongCells, selectedCell, isMobile);
        return styling.GetNotesTextCSS();
    }

    // Show/hide statistics modal.
    private Task ShowStatisticsAsync()
    {
        showStatistics = true;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void HideStatistics()
    {
        showStatistics = false;
        StateHasChanged();
    }

    // Confirmation dialog logic for clearing stats.
    private void RequestClearStats()
    {
        showConfirmClearStats = true;
        StateHasChanged();
    }

    private Task ConfirmClearStats()
    {
        showConfirmClearStats = false;
        // Reset gameStatistics to initial values; replace with real persistence if needed.
        gameStatistics = new GameStatistics();
        StateHasChanged();
        return Task.CompletedTask;
    }

    // Game actions
    private void Undo()
    {
        // TODO: Implement undo logic.  For now, leave it empty to satisfy the compiler.
    }

    private void Erase()
    {
        if (!CanEditSelectedCell || board == null) return;
        var (row, col) = selectedCell!.Value;
        board.Grid[row, col] = 0;
        board.CorrectlySolvedCells[row, col] = false;
        wrongCells[row, col] = false;
        StateHasChanged();
    }

    private void TogglePencilMode()
    {
        pencilMode = !pencilMode;
        StateHasChanged();
    }

    private void GiveHint()
    {
        if (board == null || isGamePaused) return;

        // Simple hint: fill the first empty cell with the solution value.
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                if (board.Grid[r, c] == 0)
                {
                    board.Grid[r, c] = board.Solution[r, c];
                    board.CorrectlySolvedCells[r, c] = true;
                    hintCount++;
                    StateHasChanged();
                    return;
                }
            }
        }
    }
}
