using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class QuizService
{
    private readonly AppDbContext _db;

    public QuizService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Quiz>> GetAllQuizzesAsync()
    {
        return await _db.Quizzes
            .Include(q => q.Questions)
            .AsNoTracking()
            .OrderBy(q => q.ID)
            .ToListAsync();
    }

    public async Task<List<Quiz>> GetQuizzesByUserIdAsync(string userId)
    {
        if (!int.TryParse(userId, out var creatorId))
        {
            return new List<Quiz>();
        }

        return await _db.Quizzes
            .Include(q => q.Questions)
            .Where(q => q.CreatorID == creatorId)
            .AsNoTracking()
            .OrderBy(q => q.ID)
            .ToListAsync();
    }

    public async Task<Quiz?> GetQuizByIdAsync(int quizId)
    {
        return await _db.Quizzes
            .Include(q => q.Questions)
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.ID == quizId);
    }

    public async Task<Quiz> CreateQuizAsync(Quiz quiz)
    {
        await _db.Quizzes.AddAsync(quiz);
        await _db.SaveChangesAsync();
        return quiz;
    }

    public async Task<bool> DeleteQuizAsync(int quizId)
    {
        var quiz = await _db.Quizzes.FirstOrDefaultAsync(q => q.ID == quizId);
        if (quiz == null)
        {
            return false;
        }

        _db.Quizzes.Remove(quiz);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<Quiz?> UpdateQuizAsync(int quizId, Quiz updatedQuiz)
    {
        var quiz = await _db.Quizzes
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.ID == quizId);

        if (quiz == null)
        {
            return null;
        }

        quiz.Title = updatedQuiz.Title;
        quiz.Description = updatedQuiz.Description;

        var incomingIds = updatedQuiz.Questions
            .Where(q => q.Id > 0)
            .Select(q => q.Id)
            .ToHashSet();

        var toRemove = quiz.Questions
            .Where(q => !incomingIds.Contains(q.Id))
            .ToList();

        if (toRemove.Count > 0)
        {
            _db.QuizQuestions.RemoveRange(toRemove);
            foreach (var remove in toRemove)
            {
                quiz.Questions.Remove(remove);
            }
        }

        foreach (var question in updatedQuiz.Questions)
        {
            var options = question.Options?.ToList() ?? new List<string>();

            if (question.Id > 0)
            {
                var existing = quiz.Questions.FirstOrDefault(q => q.Id == question.Id);
                if (existing != null)
                {
                    existing.QuestionText = question.QuestionText;
                    existing.Options = options;
                    existing.CorrectOptionIndex = question.CorrectOptionIndex;
                    existing.TimeLimit = question.TimeLimit;
                    continue;
                }
            }

            quiz.Questions.Add(new QuizQuestion
            {
                QuestionText = question.QuestionText,
                Options = options,
                CorrectOptionIndex = question.CorrectOptionIndex,
                TimeLimit = question.TimeLimit
            });
        }

        await _db.SaveChangesAsync();
        return quiz;
    }
}