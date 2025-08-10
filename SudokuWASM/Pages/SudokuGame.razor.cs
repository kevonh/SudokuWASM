using Microsoft.AspNetCore.Components;
using Sudoku.Services;
using Sudoku.Models;
using SudokuWASM.Services;

namespace SudokuWASM.Pages;
public partial class SudokuGame 
{
    [Inject] private IGamePersistenceService PersistenceService { get; set; } = default!;
    [Inject] private GameStatePersistenceService StatePersistenceService { get; set; } = default!;
    [Inject] private IPuzzleGeneratorService PuzzleGenerator { get; set; } = default!;
    [Inject] private IGameEngine GameEngine { get; set; } = default!;

    private Sudoku.SudokuBoard? board;
    private string message = string.Empty;
    private int wrongGuessCount = 0;
    private int currentScore = 0;
    private int hintCount = 0;
    private const int MaxWrongGuesses = 4;
    private bool isGameOver = false;
    private bool isGameWon = false;
    private bool[,] wrongCells = new bool[9, 9];
    private (int row, int col)? selectedCell = null;
    private bool isInitialized = false;
    private bool showOptionsModal = false;
    private string selectedDifficulty = "Medium";
    private bool showStatistics = false;
    private bool showConfirmClearStats = false;
    private bool isPointsAnimating = false;
    private CancellationTokenSource? generationCts;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !isInitialized)
        {
            GameEngine.StateChanged += OnEngineStateChanged;
            await GameEngine.InitializeGameAsync();
            isInitialized = true;
            StateHasChanged();
        }
    }

    private void OnEngineStateChanged()
    {
        InvokeAsync(StateHasChanged);
    }

    // Called by GameOptionsModal when the user starts a new game.
    private async Task CreateNewGameAsync(PuzzleOptions options)
    {
        selectedDifficulty = options.Difficulty switch
        {
            DifficultyLevel.Easy => "Easy",
            DifficultyLevel.Medium => "Medium",
            DifficultyLevel.Hard => "Hard",
            DifficultyLevel.Expert => "Expert",
            _ => "Medium"
        };
        showOptionsModal = false;
        await GameEngine.InitializeNewGameAsync(options);
    }

    private void ToggleOptionsModal(bool value)
    {
        showOptionsModal = value;
        StateHasChanged();
    }

    private void OnGridClick()
    {
        GameEngine.DeselectCell();
    }

    private void OnCellClickTuple((int row, int col) cell)
    {
        GameEngine.SelectCell(cell.row, cell.col);
    }

    private void OnCellClick(int row, int col)
    {
        GameEngine.SelectCell(row, col);
    }

    private void Erase()
    {
        GameEngine.Erase();
    }

    private void Undo()
    {
        GameEngine.Undo();
    }

    private void TogglePencilMode()
    {
        GameEngine.TogglePencilMode();
    }

    private async Task GiveHint()
    {
        await GameEngine.GiveHintAsync();
    }

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

    private void RequestClearStats()
    {
        showConfirmClearStats = true;
        StateHasChanged();
    }

    private Task ConfirmClearStats()
    {
        showConfirmClearStats = false;
        var gameStatistics = new GameStatistics(); // Reset stats or call your persistence layer BOOGER
        StateHasChanged();
        return Task.CompletedTask;
    }
}
