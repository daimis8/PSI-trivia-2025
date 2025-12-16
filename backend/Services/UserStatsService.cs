using backend.Models;
using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class UserStatsService
{
    private readonly AppDbContext _db;

    public UserStatsService(AppDbContext db)
    {
        _db = db;
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
        await _db.UserStats.AddAsync(stats);
        await _db.SaveChangesAsync();
        return stats;
    }
    
    // Get stats for a user
    public async Task<UserStats?> GetUserStatsAsync(int userId)
    {
        return await _db.UserStats.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId);
    }

    // Ensure user's stats exist
    public async Task<UserStats> EnsureAsync(int userId)
    {
        var stats = await _db.UserStats.FirstOrDefaultAsync(s => s.UserId == userId);
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
        await _db.SaveChangesAsync();
    }

    // Increase or decrease No. of quizzes created by a user
    public async Task ChangeQuizNoAsync (int userId, int change)
    {
        var stats = await EnsureAsync(userId);
        stats.QuizzesCreated += change;
        await _db.SaveChangesAsync();
    }

    // Increment No. of times a quiz has been played
    public async Task IncrementQuizPlaysAsync(int userId)
    {
        var stats = await EnsureAsync(userId);
        stats.QuizPlays += 1;
        await _db.SaveChangesAsync();
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
        var stats = await _db.UserStats.FirstOrDefaultAsync(s => s.UserId == userId);
        if (stats == null)
        {
            return false;
        }

        _db.UserStats.Remove(stats);
        await _db.SaveChangesAsync();
        return true;
    }

    // Get top players by games won
    public async Task<List<(int UserId, string Username, int GamesWon)>> GetTopPlayersAsync(int limit = 10)
    {
        return await _db.UserStats
            .Include(s => s.User)
            .OrderByDescending(s => s.GamesWon)
            .Take(limit)
            .Select(s => new ValueTuple<int, string, int>(s.UserId, s.User!.Username, s.GamesWon))
            .ToListAsync();
    }
}