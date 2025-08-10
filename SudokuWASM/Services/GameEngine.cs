using Sudoku.Models;
using Sudoku.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SudokuWASM.Services;

public class GameEngine : IGameEngine, IDisposable
{
    private readonly IGamePersistenceService persistence;
    private readonly GameStatePersistenceService statePersistence;
    private readonly IPuzzleGeneratorService puzzleGenerator;

    private const int maxWrongGuesses = 4;
    private Sudoku.SudokuBoard? board;
    private bool[,] wrongCells = new bool[9, 9];
    private CancellationTokenSource? generationCts;
    private GameTimingService? timingService;

    public Sudoku.SudokuBoard? Board => board;
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
        var saved = await persistence.LoadGameStateAsync();
        if (saved != null && !saved.IsGameWon && !saved.IsGameOver)
        {
            LoadFromState(saved);
            Message = "Resumed previous game";
        }
        else
        {
            var options = new PuzzleOptions { Difficulty = DifficultyLevel.Medium };
            await InitializeNewGameAsync(options);
        }
        OnStateChanged();
    }

    public async Task InitializeNewGameAsync(PuzzleOptions options)
    {
        generationCts?.Cancel();
        generationCts = new CancellationTokenSource();

        // Generate a new puzzle with the requested difficulty
        board = await puzzleGenerator.GeneratePuzzleAsync(options, generationCts.Token);

        wrongCells = new bool[9, 9];
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

        OnStateChanged();
    }

    public Task SaveGameStateAsync()
    {
        if (board == null) return Task.CompletedTask;

        // Persist the current state using your existing GameStatePersistenceService
        var gameState = statePersistence.CreateGameState(
            board,
            Guid.NewGuid().ToString(),
            "Custom",
            WrongGuessCount,
            HintCount,
            CurrentScore,
            IsGameOver,
            IsGameWon,
            PencilMode,
            wrongCells,
            SelectedCell,
            timingService,
            IsGamePaused
        );
        return persistence.SaveGameStateAsync(gameState);
    }

    public void PauseGame()
    {
        if (board == null || IsGamePaused) return;
        IsGamePaused = true;
        timingService?.PauseTimer();
        Message = "Game paused";
        OnStateChanged();
    }

    public void ResumeGame()
    {
        if (board == null || !IsGamePaused) return;
        IsGamePaused = false;
        timingService?.ResumeTimer();
        Message = "Game resumed";
        OnStateChanged();
    }

    public void SelectCell(int row, int col)
    {
        if (board == null || IsGamePaused) return;
        SelectedCell = (row, col);
        Message = string.Empty;
        OnStateChanged();
    }

    public void DeselectCell()
    {
        SelectedCell = null;
        OnStateChanged();
    }

    public async Task PlaceNumberAsync(int number)
    {
        if (board == null || SelectedCell == null || IsGamePaused) return;

        var (r, c) = SelectedCell.Value;

        // Cannot edit fixed cells or already correctly solved cells
        if (board.FixedCells[r, c] || board.CorrectlySolvedCells[r, c]) return;

        // If pencil mode: toggle note
        if (PencilMode)
        {
            if (board.Notes[r, c].Contains(number))
                board.Notes[r, c].Remove(number);
            else
                board.Notes[r, c].Add(number);
            OnStateChanged();
            return;
        }

        // Normal mode: place a number
        if (number == board.Solution[r, c])
        {
            // Correct guess
            board.Grid[r, c] = number;
            board.CorrectlySolvedCells[r, c] = true;
            wrongCells[r, c] = false;
            CurrentScore += GetPointsForMove();
            Message = "Correct!";
        }
        else
        {
            // Wrong guess
            wrongCells[r, c] = true;
            WrongGuessCount++;
            CurrentScore = Math.Max(0, CurrentScore - 5); // deduct points
            Message = "Wrong guess!";
        }

        // Check for win/lose conditions
        if (WrongGuessCount >= maxWrongGuesses)
        {
            IsGameOver = true;
            Message = "Game over!";
        }
        else if (board.CorrectlySolvedCells.Cast<bool>().All(x => x))
        {
            IsGameWon = true;
            Message = "You solved the puzzle!";
        }

        await SaveGameStateAsync();
        OnStateChanged();
    }

    public void Erase()
    {
        if (board == null || SelectedCell == null || IsGamePaused) return;
        var (r, c) = SelectedCell.Value;
        if (board.FixedCells[r, c]) return;

        board.Grid[r, c] = 0;
        board.CorrectlySolvedCells[r, c] = false;
        wrongCells[r, c] = false;
        board.Notes[r, c].Clear();
        OnStateChanged();
    }

    public void Undo()
    {
        // Placeholder: implement undo stack if needed
    }

    public void TogglePencilMode()
    {
        PencilMode = !PencilMode;
        Message = PencilMode ? "Pencil mode on" : "Pencil mode off";
        OnStateChanged();
    }

    public async Task GiveHintAsync()
    {
        if (board == null || IsGamePaused) return;

        // Find the first empty cell and fill it with the correct value
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                if (board.Grid[r, c] == 0)
                {
                    board.Grid[r, c] = board.Solution[r, c];
                    board.CorrectlySolvedCells[r, c] = true;
                    HintCount++;
                    CurrentScore = Math.Max(0, CurrentScore - 10);
                    Message = $"Hint applied at {r + 1},{c + 1}";
                    await SaveGameStateAsync();
                    OnStateChanged();
                    return;
                }
            }
        }
    }

    public void ShowStatistics()
    {
        // You can update a flag here if you want statistics to be displayed
        Message = "Statistics shown";
        OnStateChanged();
    }

    public void HideStatistics()
    {
        Message = "Statistics hidden";
        OnStateChanged();
    }

    public void RequestClearStats()
    {
        // Set a flag or message to trigger confirmation in UI
        Message = "Confirm clear statistics?";
        OnStateChanged();
    }

    public async Task ConfirmClearStatsAsync()
    {
        Statistics = new GameStatistics();
        // Optionally clear persisted state if your persistence supports it
        Message = "Statistics cleared";
        OnStateChanged();
    }

    public void Dispose()
    {
        timingService?.Dispose();
        generationCts?.Cancel();
    }

    private void LoadFromState(GameState gameState)
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

        WrongGuessCount = gameState.WrongGuessCount;
        HintCount = gameState.HintCount;
        CurrentScore = gameState.CurrentScore;
        IsGameOver = gameState.IsGameOver;
        IsGameWon = gameState.IsGameWon;
        PencilMode = gameState.PencilMode;
        IsGamePaused = gameState.IsPaused;

        if (gameState.SelectedRow.HasValue && gameState.SelectedCol.HasValue)
            SelectedCell = (gameState.SelectedRow.Value, gameState.SelectedCol.Value);

        timingService = new GameTimingService(UpdateElapsedTime);
        timingService.RestoreWithPauseState(
            gameState.StartTime,
            gameState.LastMoveTime,
            gameState.TotalElapsed,
            IsGamePaused
        );
    }

    private void UpdateElapsedTime()
    {
        if (timingService != null)
        {
            ElapsedTime = timingService.ElapsedTime;
            OnStateChanged();
        }
    }

    private int GetPointsForMove()
    {
        // Simple scoring: base points minus penalties for hints
        int basePoints = 20;
        int hintPenalty = HintCount * 2;
        return Math.Max(5, basePoints - hintPenalty);
    }

    private void OnStateChanged()
    {
        StateChanged?.Invoke();
    }

    public bool[,] WrongCells
    {
        get
        {
            var copy = new bool[9, 9];
            Array.Copy(wrongCells, copy, wrongCells.Length);
            return copy;
        }
    }
}
