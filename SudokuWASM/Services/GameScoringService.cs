using Sudoku.Models;

namespace Sudoku.Services;

/// <summary>
/// Service to handle game scoring logic
/// </summary>
public class GameScoringService
{
    public int CalculatePointsForMove(string difficulty, DateTime lastMoveTime)
    {
        var secondsSinceLastMove = (DateTime.Now - lastMoveTime).TotalSeconds;
        int basePoints = GetBasePointsPerMove(difficulty);
        double timeBonus = Math.Max(0, (10 - secondsSinceLastMove) / 10.0 * 0.5);
        int bonusPoints = (int)(basePoints * timeBonus);
        
        Console.WriteLine($"Move timing: {secondsSinceLastMove:F2}s since last move, base: {basePoints}, bonus: {bonusPoints}, total: {basePoints + bonusPoints}");
        return basePoints + bonusPoints;
    }

    public int CalculateCompletionBonus(string difficulty, DateTime startTime, int wrongGuessCount, int hintCount)
    {
        var totalMinutes = (DateTime.Now - startTime).TotalMinutes;
        int baseBonus = GetDifficultyMultiplier(difficulty);
        
        var (minTime, maxTime, maxBonusPercent) = difficulty switch
        {
            "Easy" => (0.5, 5.0, 0.25),
            "Medium" => (1.0, 10.0, 0.30),
            "Hard" => (2.0, 15.0, 0.35),
            "Expert" => (3.0, 20.0, 0.40),
            _ => (10.0, 30.0, 0.30)
        };
        
        double timeMultiplier = totalMinutes <= minTime ? maxBonusPercent :
                               totalMinutes >= maxTime ? 0.0 :
                               maxBonusPercent * (maxTime - totalMinutes) / (maxTime - minTime);
        
        int timeBonus = (int)(baseBonus * timeMultiplier);
        int wrongMovePenalty = wrongGuessCount * GetBasePointsPerMove(difficulty) * 5;
        int hintPenalty = hintCount * GetBasePointsPerMove(difficulty) * 3;
        
        return Math.Max(0, baseBonus + timeBonus - wrongMovePenalty - hintPenalty);
    }

    /// <summary>
    /// Calculate the current display score by applying wrong guess penalties to the base score
    /// </summary>
    public int CalculateCurrentDisplayScore(int baseScore, int wrongGuessCount, string difficulty)
    {
        int wrongMovePenalty = wrongGuessCount * GetBasePointsPerMove(difficulty) * 2;
        return Math.Max(0, baseScore - wrongMovePenalty);
    }

    public int GetBasePointsPerMove(string difficulty) => difficulty switch
    {
        "Easy" => 10,
        "Medium" => 25,
        "Hard" => 50,
        "Expert" => 75,
        _ => 25
    };

    public int GetDifficultyMultiplier(string difficulty) => difficulty switch
    {
        "Easy" => 100,
        "Medium" => 500,
        "Hard" => 1500,
        "Expert" => 3000,
        _ => 500
    };

    public int GetCluesCount(string difficulty) => difficulty switch
    {
        "Easy" => 40,
        "Medium" => 30,
        "Hard" => 22,
        "Expert" => 17,
        _ => 30
    };
}