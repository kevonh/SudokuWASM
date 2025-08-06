namespace Sudoku.Services;

/// <summary>
/// Service to handle UI animations and feedback
/// </summary>
public class GameAnimationService : IDisposable
{
    private System.Timers.Timer? pointsAnimationTimer;
    private bool isPointsAnimating = false;
    private readonly Action onAnimationStateChanged;

    public bool IsPointsAnimating => isPointsAnimating;

    public GameAnimationService(Action onAnimationStateChanged)
    {
        this.onAnimationStateChanged = onAnimationStateChanged;
    }

    public void TriggerPointsAnimation()
    {
        try
        {
            isPointsAnimating = true;
            
            if (pointsAnimationTimer != null)
            {
                pointsAnimationTimer.Stop();
                pointsAnimationTimer.Dispose();
            }
            
            pointsAnimationTimer = new System.Timers.Timer(500);
            pointsAnimationTimer.Elapsed += (s, e) =>
            {
                try
                {
                    isPointsAnimating = false;
                    pointsAnimationTimer?.Stop();
                    pointsAnimationTimer?.Dispose();
                    pointsAnimationTimer = null;
                    onAnimationStateChanged?.Invoke();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Animation timer error: {ex.Message}");
                }
            };
            pointsAnimationTimer.Start();
            
            onAnimationStateChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Animation trigger error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (pointsAnimationTimer != null)
        {
            pointsAnimationTimer.Stop();
            pointsAnimationTimer.Dispose();
            pointsAnimationTimer = null;
        }
    }
}