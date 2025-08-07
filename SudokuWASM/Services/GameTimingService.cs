using Sudoku.Models;

namespace Sudoku.Services;

/// <summary>
/// Service to handle game timing and scoring logic
/// </summary>
public class GameTimingService : IDisposable
{
    private System.Timers.Timer? timer;
    private DateTime startTime;
    private DateTime lastMoveTime;
    private string elapsedTime = "00:00";
    private readonly Action onTimeUpdated;
    private TimeSpan cumulativeElapsed = TimeSpan.Zero; // Total time elapsed (including previous sessions)
    private DateTime sessionStartTime; // When the current timer session started
    private bool isPaused = false;

    public DateTime StartTime => startTime;
    public DateTime LastMoveTime => lastMoveTime;
    public string ElapsedTime => elapsedTime;
    public bool IsPaused => isPaused;

    public GameTimingService(Action onTimeUpdated)
    {
        this.onTimeUpdated = onTimeUpdated;
    }

    public void StartTimer()
    {
        StopTimer();
        startTime = DateTime.Now;
        lastMoveTime = DateTime.Now;
        sessionStartTime = DateTime.Now;
        cumulativeElapsed = TimeSpan.Zero;
        isPaused = false;
        
        // Calculate initial elapsed time
        UpdateElapsedTime();
        
        StartTimerInternal();
    }

    private void StartTimerInternal()
    {
        if (timer == null && !isPaused)
        {
            sessionStartTime = DateTime.Now;
            
            timer = new System.Timers.Timer(1000);
            timer.Elapsed += (s, e) =>
            {
                try
                {
                    UpdateElapsedTime();
                    onTimeUpdated?.Invoke();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Timer error: {ex.Message}");
                }
            };
            timer.Start();
        }
    }

    public void PauseTimer()
    {
        if (!isPaused && timer != null)
        {
            // Add current session time to cumulative elapsed before pausing
            if (sessionStartTime != default)
            {
                cumulativeElapsed += DateTime.Now - sessionStartTime;
            }
            
            isPaused = true;
            StopTimerInternal();
            UpdateElapsedTime();
            onTimeUpdated?.Invoke();
        }
    }

    public void ResumeTimer()
    {
        if (isPaused)
        {
            isPaused = false;
            StartTimerInternal();
        }
        else if (timer == null)
        {
            // Resume the timer for a restored game
            StartTimerInternal();
        }
    }

    public void StopTimer()
    {
        if (timer != null)
        {
            // Add the current session time to cumulative elapsed before stopping
            if (sessionStartTime != default && !isPaused)
            {
                cumulativeElapsed += DateTime.Now - sessionStartTime;
            }
            
            StopTimerInternal();
        }
        isPaused = false;
    }

    private void StopTimerInternal()
    {
        if (timer != null)
        {
            timer.Stop();
            timer.Dispose();
            timer = null;
        }
    }

    public void RecordMove()
    {
        if (!isPaused)
        {
            lastMoveTime = DateTime.Now;
        }
    }

    public void RestoreTime(DateTime originalStart, DateTime originalLastMove)
    {
        // Calculate how much time has already elapsed
        var timeAlreadyElapsed = originalLastMove - originalStart;
        RestoreTimeWithTotal(originalStart, originalLastMove, timeAlreadyElapsed);
    }

    public void RestoreTimeWithTotal(DateTime originalStart, DateTime originalLastMove, TimeSpan totalElapsed)
    {
        // Store the original start time and last move time
        startTime = originalStart;
        lastMoveTime = originalLastMove;
        
        // Set cumulative elapsed to the total elapsed time from the saved state
        cumulativeElapsed = totalElapsed;
        
        // Don't set sessionStartTime yet - it will be set when timer actually starts
        
        // Immediately update the elapsed time display when restoring
        UpdateElapsedTime();
        
        // Notify the UI to update
        onTimeUpdated?.Invoke();
    }

    public void RestoreWithPauseState(DateTime originalStart, DateTime originalLastMove, TimeSpan totalElapsed, bool wasPaused)
    {
        RestoreTimeWithTotal(originalStart, originalLastMove, totalElapsed);
        isPaused = wasPaused;
    }

    private void UpdateElapsedTime()
    {
        TimeSpan totalElapsed;
        
        if (timer != null && timer.Enabled && sessionStartTime != default && !isPaused)
        {
            // Timer is running - add current session time to cumulative elapsed
            var currentSessionElapsed = DateTime.Now - sessionStartTime;
            totalElapsed = cumulativeElapsed + currentSessionElapsed;
        }
        else
        {
            // Timer is not running or paused - just use cumulative elapsed
            totalElapsed = cumulativeElapsed;
        }
        
        elapsedTime = totalElapsed.ToString(@"mm\:ss");
    }

    public TimeSpan GetTotalElapsed()
    {
        if (timer != null && timer.Enabled && sessionStartTime != default && !isPaused)
        {
            // Timer is running - add current session time to cumulative elapsed
            var currentSessionElapsed = DateTime.Now - sessionStartTime;
            return cumulativeElapsed + currentSessionElapsed;
        }
        else
        {
            // Timer is not running or paused - just return cumulative elapsed
            return cumulativeElapsed;
        }
    }

    public void Dispose()
    {
        StopTimer();
    }
}