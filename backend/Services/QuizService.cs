using backend.Models;
using backend.Extensions;
using backend.Exceptions;

namespace backend.Services;

public class QuizService
{
    private readonly DataStorage<int, Quiz> _storage;

    public QuizService()
    {
        _storage = new DataStorage<int, Quiz>("quizzes.json");
    }

    private void ValidateQuiz(Quiz quiz)
    {
        // Validate title
        if (string.IsNullOrWhiteSpace(quiz.Title))
        {
            throw new QuizValidationException(
                "Quiz title cannot be empty or whitespace.",
                nameof(quiz.Title),
                quiz.Title
            );
        }

        if (quiz.Title.Length > 100)
        {
            throw new QuizValidationException(
                "Quiz title cannot exceed 100 characters.",
                nameof(quiz.Title),
                quiz.Title.Length
            );
        }

        // Validate questions (allow empty quizzes for initial creation)
        if (quiz.Questions != null && quiz.Questions.Count > 0)
        {
            if (quiz.Questions.Count > 50)
            {
                throw new QuizValidationException(
                    "Quiz cannot have more than 50 questions.",
                    nameof(quiz.Questions),
                    quiz.Questions.Count
                );
            }

            // Validate each question
            for (int i = 0; i < quiz.Questions.Count; i++)
            {
                var question = quiz.Questions[i];

                if (string.IsNullOrWhiteSpace(question.QuestionText))
                {
                    throw new QuizValidationException(
                        $"Question #{i + 1}: Question text cannot be empty.",
                        $"Questions[{i}].QuestionText",
                        question.QuestionText
                    );
                }

                if (question.Options == null || question.Options.Count < 2)
                {
                    throw new QuizValidationException(
                        $"Question #{i + 1}: Must have at least 2 options.",
                        $"Questions[{i}].Options",
                        question.Options?.Count ?? 0
                    );
                }

                if (question.Options.Count > 6)
                {
                    throw new QuizValidationException(
                        $"Question #{i + 1}: Cannot have more than 6 options.",
                        $"Questions[{i}].Options",
                        question.Options.Count
                    );
                }

                if (question.CorrectOptionIndex < 0 || question.CorrectOptionIndex >= question.Options.Count)
                {
                    throw new QuizValidationException(
                        $"Question #{i + 1}: Correct option index {question.CorrectOptionIndex} is out of range (0-{question.Options.Count - 1}).",
                        $"Questions[{i}].CorrectOptionIndex",
                        question.CorrectOptionIndex
                    );
                }

                if (question.TimeLimit < 5 || question.TimeLimit > 120)
                {
                    throw new QuizValidationException(
                        $"Question #{i + 1}: Time limit must be between 5 and 120 seconds.",
                        $"Questions[{i}].TimeLimit",
                        question.TimeLimit
                    );
                }

                // Check for empty options
                for (int j = 0; j < question.Options.Count; j++)
                {
                    if (string.IsNullOrWhiteSpace(question.Options[j]))
                    {
                        throw new QuizValidationException(
                            $"Question #{i + 1}: Option #{j + 1} cannot be empty.",
                            $"Questions[{i}].Options[{j}]",
                            question.Options[j]
                        );
                    }
                }
            }
        }
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
        // Validate quiz before creating
        ValidateQuiz(quiz);
        
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

        // Validate quiz before updating
        ValidateQuiz(updatedQuiz);

        updatedQuiz.ID = quizId;
        await _storage.SetAsync(quizId, updatedQuiz);
        return updatedQuiz;
    }
}