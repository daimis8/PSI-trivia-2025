using backend.Models;

namespace backend.Services;

public class UserStatsService
{
    private readonly DataStorage<int, UserStats> _storage;

    public UserStatsService()
    {
        _storage = new DataStorage<int, UserStats>("userStats.json");
    }

    // Add stats for a new user
    public async Task<UserStats> AddUserStatsAsync(int userId)
    {
        var stats = new UserStats
        {
            UserId = userId,
            GamesPlayed = 0,
            GamesWon = 0,
            QuizzesCreated = 0,
            QuizPlays = 0
        };
        await _storage.SetAsync(userId, stats);
        return stats;
    }
    
    // Get stats for a user
    public Task<UserStats?> GetUserStatsAsync(int userId)
    {
        var stats = _storage.Get(userId);
        return Task.FromResult(stats);
    }

    // Ensure user's stats exist
    public async Task<UserStats> EnsureAsync(int userId)
    {
        var stats = _storage.Get(userId);
        if (stats == null)
        {
            stats = await AddUserStatsAsync(userId);
        }
        return stats;
    }

    // Increment No. of games played and won, if any
    public async Task IncrementGameStatsAsync(int userId, bool won)
    {
        var stats = await EnsureAsync(userId);
        stats.GamesPlayed += 1;
        if (won)
        {
            stats.GamesWon += 1;
        }
        await _storage.SetAsync(userId, stats);
    }

    // Increase or decrease No. of quizzes created by a user
    public async Task ChangeQuizNoAsync (int userId, int change)
    {
        var stats = await EnsureAsync(userId);
        stats.QuizzesCreated += change;
        if (stats.QuizzesCreated < 0)
        {
            stats.QuizzesCreated = 0;
        }
        await _storage.SetAsync(userId, stats);
    }

    // Increment No. of times a quiz has been played
    public async Task IncrementQuizPlaysAsync(int userId)
    {
        var stats = await EnsureAsync(userId);
        stats.QuizPlays += 1;
        await _storage.SetAsync(userId, stats);
    }

    // Record play/win stats for users
    public async Task RecordGameStatsAsync(IEnumerable<int> playerIds, IEnumerable<int> winnerIds)
    {
        var winner = winnerIds.Distinct().ToHashSet();
        foreach (var uid in playerIds.Distinct())
        {
            await IncrementGameStatsAsync(uid, winner.Contains(uid));
        }
    }

    // Delete user stats
    public async Task<bool> DeleteUserStatsAsync(int userId)
    {
        return await _storage.RemoveAsync(userId);
    }
}