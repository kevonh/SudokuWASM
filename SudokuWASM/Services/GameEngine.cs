using Sudoku.Models;
using Sudoku.Services; // for PuzzleOptions, DifficultyLevel, etc.

namespace SudokuWASM.Services;

public class GameEngine : IGameEngine, IDisposable
{
    private readonly IGamePersistenceService persistence;
    private readonly GameStatePersistenceService statePersistence;
    private readonly IPuzzleGeneratorService puzzleGenerator;
    private GameTimingService? timingService;

    public Sudoku.SudokuBoard? Board { get; private set; }
    public bool IsGamePaused { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool IsGameWon { get; private set; }
    public bool PencilMode { get; private set; }
    public int WrongGuessCount { get; private set; }
    public int HintCount { get; private set; }
    public int CurrentScore { get; private set; }
    public string ElapsedTime { get; private set; } = "00:00";
    public (int row, int col)? SelectedCell { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public GameStatistics Statistics { get; private set; } = new();

    public event Action? StateChanged;

    public GameEngine(
        IGamePersistenceService persistence,
        GameStatePersistenceService statePersistence,
        IPuzzleGeneratorService puzzleGenerator)
    {
        this.persistence = persistence;
        this.statePersistence = statePersistence;
        this.puzzleGenerator = puzzleGenerator;
    }

    public async Task InitializeGameAsync()
    {
        // Placeholder: attempt to load from storage, otherwise new game
        var saved = await persistence.LoadGameStateAsync();
        if (saved != null && !saved.IsGameWon && !saved.IsGameOver)
        {
            LoadFromState(saved);
            Message = "Resumed previous game";
        }
        else
        {
            await InitializeNewGameAsync(new PuzzleOptions { Difficulty = DifficultyLevel.Medium });
        }
        StateChanged?.Invoke();
    }

    public async Task InitializeNewGameAsync(PuzzleOptions options)
    {
        // Placeholder: generate a puzzle
        Board = await puzzleGenerator.GeneratePuzzleAsync(options, CancellationToken.None);
        WrongGuessCount = 0;
        HintCount = 0;
        CurrentScore = 0;
        IsGameOver = false;
        IsGameWon = false;
        IsGamePaused = true;
        SelectedCell = null;
        PencilMode = false;
        Message = "New puzzle generated!";
        timingService?.Dispose();
        timingService = new GameTimingService(UpdateElapsedTime);
        StateChanged?.Invoke();
    }

    public Task SaveGameStateAsync()
    {
        // Placeholder: save to persistence
        return Task.CompletedTask;
    }

    public void PauseGame()
    {
        if (Board == null || IsGamePaused) return;
        IsGamePaused = true;
        timingService?.PauseTimer();
        Message = "Game paused";
        StateChanged?.Invoke();
    }

    public void ResumeGame()
    {
        if (Board == null || !IsGamePaused) return;
        IsGamePaused = false;
        timingService?.ResumeTimer();
        Message = "Game resumed";
        StateChanged?.Invoke();
    }

    public void SelectCell(int row, int col)
    {
        if (Board == null || IsGamePaused) return;
        SelectedCell = (row, col);
        Message = string.Empty;
        StateChanged?.Invoke();
    }

    public void DeselectCell()
    {
        SelectedCell = null;
        StateChanged?.Invoke();
    }

    public Task PlaceNumberAsync(int number)
    {
        // Placeholder: implement placing a number and scoring
        return Task.CompletedTask;
    }

    public void Erase()
    {
        // Placeholder: remove number at selected cell
    }

    public void Undo()
    {
        // Placeholder: undo last action
    }

    public void TogglePencilMode()
    {
        PencilMode = !PencilMode;
        StateChanged?.Invoke();
    }

    public Task GiveHintAsync()
    {
        // Placeholder: fill one empty cell with correct value
        return Task.CompletedTask;
    }

    public void ShowStatistics()
    {
        // Placeholder: set a flag or message to show stats
    }

    public void HideStatistics()
    {
        // Placeholder: hide stats
    }

    public void RequestClearStats()
    {
        // Placeholder: show confirm dialog
    }

    public Task ConfirmClearStatsAsync()
    {
        // Placeholder: actually clear stats
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        timingService?.Dispose();
    }

    private void LoadFromState(GameState gameState)
    {
        // Placeholder: populate Board and other fields from GameState
    }

    private void UpdateElapsedTime()
    {
        if (timingService != null)
        {
            ElapsedTime = timingService.ElapsedTime;
            StateChanged?.Invoke();
        }
    }
}
