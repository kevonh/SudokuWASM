using System.Text.Json.Serialization;

namespace Sudoku.Models;

public class GameState
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    // Store 2D arrays as jagged arrays for JSON compatibility
    public int[][] Grid { get; set; } = new int[9][];
    public int[][] Solution { get; set; } = new int[9][];
    public bool[][] FixedCells { get; set; } = new bool[9][];
    public bool[][] HintCells { get; set; } = new bool[9][];
    public bool[][] CorrectlySolvedCells { get; set; } = new bool[9][];
    public bool[][] WrongCells { get; set; } = new bool[9][];
    
    // Game metadata
    public string SelectedDifficulty { get; set; } = "Medium";
    public int WrongGuessCount { get; set; }
    public int HintCount { get; set; }
    public int CurrentScore { get; set; }
    public bool IsGameOver { get; set; }
    public bool IsGameWon { get; set; }
    public bool PencilMode { get; set; }
    
    // Simplified pause functionality - just a flag
    public bool IsPaused { get; set; }
    
    // Timing information
    public DateTime StartTime { get; set; }
    public DateTime LastMoveTime { get; set; }
    public TimeSpan TotalElapsed { get; set; }
    
    // Selected cell
    public int? SelectedRow { get; set; }
    public int? SelectedCol { get; set; }
    
    // Notes (serialized as dictionary for JSON compatibility)
    public Dictionary<string, int[]> Notes { get; set; } = new();
    
    public DateTime SavedAt { get; set; } = DateTime.Now;

    public GameState()
    {
        // Initialize jagged arrays
        for (int i = 0; i < 9; i++)
        {
            Grid[i] = new int[9];
            Solution[i] = new int[9];
            FixedCells[i] = new bool[9];
            HintCells[i] = new bool[9];
            CorrectlySolvedCells[i] = new bool[9];
            WrongCells[i] = new bool[9];
        }
    }

    // Helper methods to convert between 2D arrays and jagged arrays
    public static void CopyFromMultiArray<T>(T[,] source, T[][] destination)
    {
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                destination[r][c] = source[r, c];
            }
        }
    }

    public static void CopyToMultiArray<T>(T[][] source, T[,] destination)
    {
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                destination[r, c] = source[r][c];
            }
        }
    }
}

public class GameStatistics
{
    public int GamesPlayed { get; set; }
    public int GamesWon { get; set; }
    public int PerfectGames { get; set; } // No hints, no mistakes
    public int BestScore { get; set; }
    public TimeSpan BestTime { get; set; } = TimeSpan.MaxValue;
    
    // Per-difficulty statistics
    public Dictionary<string, DifficultyStats> DifficultyStats { get; set; } = new();
    
    public DateTime LastPlayed { get; set; }
    public DateTime FirstPlayed { get; set; } = DateTime.Now;
}

public class DifficultyStats
{
    public int Played { get; set; }
    public int Won { get; set; }
    public int Perfect { get; set; }
    public int BestScore { get; set; }
    public TimeSpan BestTime { get; set; } = TimeSpan.MaxValue;
    public double WinRate => Played > 0 ? (double)Won / Played * 100 : 0;
}

public class CompletedGame
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Difficulty { get; set; } = "Medium";
    public int Score { get; set; }
    public TimeSpan CompletionTime { get; set; }
    public int HintsUsed { get; set; }
    public int WrongMoves { get; set; }
    public bool IsPerfect { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.Now;
}