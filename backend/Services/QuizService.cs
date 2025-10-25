using backend.Models;
using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class QuizService
{
    private readonly ApplicationDbContext _context;

    public QuizService(ApplicationDbContext context)
    {
        _context = context;
    }

    // Get all quizzes
    public async Task<List<Quiz>> GetAllQuizzesAsync()
    {
        return await _context.Quizzes.ToListAsync();
    }

    // Get quizzes by user ID
    public async Task<List<Quiz>> GetQuizzesByUserIdAsync(string userId)
    {
        if (!int.TryParse(userId, out int creatorId))
        {
            return new List<Quiz>();
        }

        return await _context.Quizzes.Where(q => q.CreatorID == creatorId).ToListAsync();
    }

    public async Task<Quiz?> GetQuizByIdAsync(int quizId)
    {
        return await _context.Quizzes.FirstOrDefaultAsync(q => q.ID == quizId);
    }

    public async Task<Quiz> CreateQuizAsync(Quiz quiz)
    {
        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync();
        return quiz;
    }

    public async Task<bool> DeleteQuizAsync(int quizId)
    {
        var quiz = await _context.Quizzes.FindAsync(quizId);
        if (quiz == null)
        {
            return false;
        }

        _context.Quizzes.Remove(quiz);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Quiz?> UpdateQuizAsync(int quizId, Quiz updatedQuiz)
    {
        var existingQuiz = await _context.Quizzes.FindAsync(quizId);
        if (existingQuiz == null)
        {
            return null;
        }

        existingQuiz.Title = updatedQuiz.Title;
        existingQuiz.Description = updatedQuiz.Description;
        existingQuiz.Questions = updatedQuiz.Questions;

        _context.Quizzes.Update(existingQuiz);
        await _context.SaveChangesAsync();
        return existingQuiz;
    }
}