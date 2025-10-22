namespace backend.Models;

public readonly struct ScoreResult
{
    public int Points { get; }
    public bool IsCorrect { get; }
    public double RemainingTimeRatio { get; }
    public long ElapsedMilliseconds { get; }

    public ScoreResult(int points, bool isCorrect, double remainingTimeRatio, long elapsedMilliseconds)
    {
        Points = points;
        IsCorrect = isCorrect;
        RemainingTimeRatio = remainingTimeRatio;
        ElapsedMilliseconds = elapsedMilliseconds;
    }

    public static ScoreResult Calculate(
        bool isCorrect, 
        DateTimeOffset answerTime, 
        DateTimeOffset questionStart, 
        DateTimeOffset questionEnd)
    {
        var elapsed = answerTime - questionStart;
        var total = questionEnd - questionStart;

        // Clamp elapsed time
        if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;
        if (elapsed > total) elapsed = total;

        // Calculate remaining time ratio
        var remainingRatio = 1.0 - (elapsed.TotalMilliseconds / total.TotalMilliseconds);
        if (remainingRatio < 0) remainingRatio = 0;

        // Award points only for correct answers
        var points = isCorrect ? (int)Math.Round(1000 * remainingRatio) : 0;

        return new ScoreResult(points, isCorrect, remainingRatio, (long)elapsed.TotalMilliseconds);
    }
}
