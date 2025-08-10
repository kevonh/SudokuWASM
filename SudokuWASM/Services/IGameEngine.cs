using Sudoku.Models;

namespace SudokuWASM.Services;

public interface IGameEngine
{
    Sudoku.SudokuBoard? Board { get; }
    bool IsGamePaused { get; }
    bool IsGameOver { get; }
    bool IsGameWon { get; }
    bool PencilMode { get; }
    int WrongGuessCount { get; }
    int HintCount { get; }
    int CurrentScore { get; }
    string ElapsedTime { get; }
    (int row, int col)? SelectedCell { get; }
    string Message { get; }
    GameStatistics Statistics { get; }

    Task InitializeGameAsync();                        // Load saved game or start new
    Task InitializeNewGameAsync(PuzzleOptions options); // Start new game with options
    Task SaveGameStateAsync();                        // Persist current state

    void PauseGame();
    void ResumeGame();
    void SelectCell(int row, int col);
    void DeselectCell();
    Task PlaceNumberAsync(int number);
    void Erase();
    void Undo();
    void TogglePencilMode();
    Task GiveHintAsync();

    void ShowStatistics();
    void HideStatistics();
    void RequestClearStats();
    Task ConfirmClearStatsAsync();

    event Action? StateChanged;                       // Notify UI when state updates
}
