using Microsoft.AspNetCore.Components;
using Sudoku.Services;
using Sudoku.Models;
using SudokuWASM.Services;

namespace SudokuWASM.Pages;

public partial class SudokuGame : IDisposable
{
    [Inject] private IGameEngine GameEngine { get; set; } = default!;
    private Sudoku.SudokuBoard? board;
    private bool showStatistics = false;
    private bool showConfirmClearStats = false;
    private bool isInitialized = false;
    private bool showOptionsModal = false;
    private bool isPointsAnimating = false;
    private const int maxWrongGuesses = 4;

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
        board = GameEngine.Board;
        InvokeAsync(StateHasChanged);
    }

    private string GetCellCSS(int row, int col)
    {
        if (board == null) return "";
        var styling = new Sudoku.Services.CellStylingService(board, GameEngine.WrongCells, GameEngine.SelectedCell, false);
        return styling.GetCellCSS(row, col);
    }

    private string GetCellTextCSS(int row, int col)
    {
        if (board == null) return "";
        var styling = new Sudoku.Services.CellStylingService(board, GameEngine.WrongCells, GameEngine.SelectedCell, false);
        return styling.GetCellTextCSS(row, col);
    }

    private string GetNotesTextCSS(bool isMobile)
    {
        if (board == null) return "";
        var styling = new Sudoku.Services.CellStylingService(board, GameEngine.WrongCells, GameEngine.SelectedCell, isMobile);
        return styling.GetNotesTextCSS();
    }

    private Task ShowStatisticsAsync()
    {
        showStatistics = true;
        GameEngine.ShowStatistics();
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void HideStatistics()
    {
        showStatistics = false;
        GameEngine.HideStatistics();
        StateHasChanged();
    }

    private void RequestClearStats()
    {
        showConfirmClearStats = true;
        GameEngine.RequestClearStats();
        StateHasChanged();
    }

    private async Task ConfirmClearStats()
    {
        showConfirmClearStats = false;
        await GameEngine.ConfirmClearStatsAsync();
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

    private async Task GiveHint()
    {
        await GameEngine.GiveHintAsync();
    }

    private async Task CreateNewGameAsync(PuzzleOptions options)
    {
        showOptionsModal = false;
        await GameEngine.InitializeNewGameAsync(options);
    }

    private void Undo()
    {
        GameEngine.Undo();
    }

    private void Erase()
    {
        GameEngine.Erase();
    }

    private void TogglePencilMode()
    {
        GameEngine.TogglePencilMode();
    }

    private void ToggleOptionsModal(bool value)
    {
        showOptionsModal = value;
    }

    public void Dispose()
    {
        GameEngine.StateChanged -= OnEngineStateChanged;
    }
}
