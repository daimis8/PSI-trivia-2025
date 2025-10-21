using backend.Models;
using backend.Extensions;

namespace backend.Services;

public class QuizService
{
    private readonly DataStorage<int, Quiz> _storage;

    public QuizService()
    {
        _storage = new DataStorage<int, Quiz>("quizzes.json");
    }

    // Get all quizzes
    public Task<List<Quiz>> GetAllQuizzesAsync()
    {
        return Task.FromResult(_storage.GetAll().ToList());
    }

    // Get quizzes by user ID
    public Task<List<Quiz>> GetQuizzesByUserIdAsync(string userId)
    {
        if (!int.TryParse(userId, out int creatorId))
        {
            return Task.FromResult(new List<Quiz>());
        }

        return Task.FromResult(_storage.GetAll().Where(q => q.CreatorID == creatorId).ToList());
    }

    public Task<Quiz?> GetQuizByIdAsync(int quizId)
    {
        var quiz = _storage.GetAll().FirstOrDefault(q => q.ID == quizId);
        return Task.FromResult(quiz);
    }

    public async Task<Quiz> CreateQuizAsync(Quiz quiz)
    {
        var quizzes = _storage.GetAll().ToList();
        // Using extension method to check if collection is empty
        quiz.ID = quizzes.IsNullOrEmpty() ? 1 : quizzes.Max(q => q.ID) + 1;
        await _storage.SetAsync(quiz.ID, quiz);
        return quiz;
    }

    public async Task<bool> DeleteQuizAsync(int quizId)
    {
        var quiz = await GetQuizByIdAsync(quizId);
        if (quiz == null)
        {
            return false;
        }

        await _storage.RemoveAsync(quizId);
        return true;
    }

    public async Task<Quiz?> UpdateQuizAsync(int quizId, Quiz updatedQuiz)
    {
        var existingQuiz = await GetQuizByIdAsync(quizId);
        if (existingQuiz == null)
        {
            return null;
        }

        updatedQuiz.ID = quizId;
        await _storage.SetAsync(quizId, updatedQuiz);
        return updatedQuiz;
    }
}