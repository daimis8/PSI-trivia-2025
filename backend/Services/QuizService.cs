using backend.Models;

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
        quiz.ID = quizzes.Any() ? quizzes.Max(q => q.ID) + 1 : 1;
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
}