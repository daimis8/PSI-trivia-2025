using backend.DTOs;
using backend.Models;
using backend.Services;
using backend.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace backend.Hubs;

public class GameHub : Hub
{
    private readonly GameService _gameService;
    private readonly QuizService _quizService;
    private readonly IHubContext<GameHub> _hubContext;

    public GameHub(GameService gameService, QuizService quizService, IHubContext<GameHub> hubContext)
    {
        _gameService = gameService;
        _quizService = quizService;
        _hubContext = hubContext;
    }

    private string? GetUserId() => Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    private string? GetUsername() => Context.User?.FindFirst(ClaimTypes.Name)?.Value;

    [Authorize]
    public async Task JoinAsHost(string code)
    {
        if (!_gameService.TryGetGame(code, out var game) || game == null)
        {
            throw new HubException("Game not found");
        }

        var userId = GetUserId();
        if (userId == null || int.Parse(userId) != game.HostUserId)
        {
            throw new HubException("Forbidden");
        }

        game.HostConnectionId = Context.ConnectionId;
        await Groups.AddToGroupAsync(Context.ConnectionId, Group(code));
        await BroadcastLobby(game);
    }

    public async Task JoinAsPlayer(string code, string username)
    {
        if (!_gameService.TryGetGame(code, out var game) || game == null)
        {
            throw new HubException("Game not found");
        }
        if (game.Phase != GamePhase.Lobby)
        {
            throw new HubException("Game already started");
        }

        var player = new GamePlayer
        {
            ConnectionId = Context.ConnectionId,
            UserId = GetUserId() != null ? int.Parse(GetUserId()!) : null,
            Username = string.IsNullOrWhiteSpace(username) ? (GetUsername() ?? $"Player-{Context.ConnectionId[^4..]}") : username.Trim(),
            TotalScore = 0,
        };
        game.Players[Context.ConnectionId] = player;
        await Groups.AddToGroupAsync(Context.ConnectionId, Group(code));
        await BroadcastLobby(game);
    }

    [Authorize]
    public async Task StartGame(string code)
    {
        if (!_gameService.TryGetGame(code, out var game) || game == null)
            throw new HubException("Game not found");

        EnsureHost(game);
        // Using extension method to check if questions collection is empty
        if (game.Questions.IsNullOrEmpty())
            throw new HubException("Quiz has no questions");

        await StartQuestionInternal(game);
    }

    [Authorize]
    public async Task SkipQuestion(string code)
    {
        if (!_gameService.TryGetGame(code, out var game) || game == null)
            throw new HubException("Game not found");
        EnsureHost(game);
        await EndQuestionInternal(game, skipped: true);
    }

    [Authorize]
    public async Task NextQuestion(string code)
    {
        if (!_gameService.TryGetGame(code, out var game) || game == null)
            throw new HubException("Game not found");
        EnsureHost(game);
        if (game.CurrentQuestionIndex + 1 >= game.Questions.Count)
        {
            game.Phase = GamePhase.Ended;
            await Clients.Group(Group(code)).SendAsync("GameEnded");
            return;
        }
        await StartQuestionInternal(game);
    }

    public async Task SubmitAnswer(string code, int optionIndex)
    {
        if (!_gameService.TryGetGame(code, out var game) || game == null)
            throw new HubException("Game not found");
        if (game.Phase != GamePhase.Question)
            return;

        if (!game.Players.TryGetValue(Context.ConnectionId, out var player))
            return;
        if (player.HasAnsweredCurrent)
            return;

        var now = DateTimeOffset.UtcNow;
        if (game.QuestionStartTime == null || game.QuestionEndTime == null)
            return;

        if (now > game.QuestionEndTime.Value)
            return;

        var q = game.Questions[game.CurrentQuestionIndex];
        var correct = optionIndex == q.CorrectOptionIndex;
        var scoreResult = ScoreResult.Calculate(
            correct,
            now,
            game.QuestionStartTime.Value,
            game.QuestionEndTime.Value
        );

        player.HasAnsweredCurrent = true;
        player.SelectedOptionIndex = optionIndex;
        player.AnswerTimeMs = scoreResult.ElapsedMilliseconds;
        if (scoreResult.IsCorrect)
            player.TotalScore += scoreResult.Points;

        // If all players have answered, end early
        var allAnswered = game.Players.Values.All(p => p.HasAnsweredCurrent);
        if (allAnswered)
        {
            await EndQuestionInternal(game);
        }
        else
        {
            await Clients.Caller.SendAsync("AnswerAccepted");
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_gameService.RemoveByConnection(Context.ConnectionId, out var game) && game != null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, Group(game.Code));
            if (game.HostConnectionId == Context.ConnectionId)
            {
                // End game if host leaves
                game.Phase = GamePhase.Ended;
                await Clients.Group(Group(game.Code)).SendAsync("GameEnded");
                _gameService.RemoveGame(game.Code);
            }
            else
            {
                await BroadcastLobby(game);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    private async Task StartQuestionInternal(Game game)
    {
        // Cancel previous timer if any
        game.CurrentTimerCts?.Cancel();
        game.CurrentTimerCts = new CancellationTokenSource();

        game.Phase = GamePhase.Question;
        game.CurrentQuestionIndex = game.CurrentQuestionIndex + 1;
        foreach (var p in game.Players.Values)
        {
            p.HasAnsweredCurrent = false;
            p.SelectedOptionIndex = null;
            p.AnswerTimeMs = null;
        }

        var q = game.Questions[game.CurrentQuestionIndex];
        var questionDurationSeconds = q.TimeLimit;

        var now = DateTimeOffset.UtcNow;
        game.QuestionStartTime = now;
        game.QuestionEndTime = now.AddSeconds(questionDurationSeconds);

        var dto = new QuestionDto(
            Index: game.CurrentQuestionIndex,
            QuestionText: q.QuestionText,
            Options: q.Options,
            EndsAt: game.QuestionEndTime.Value
        );

        await Clients.Group(Group(game.Code)).SendAsync("QuestionStarted", dto);

        var hubContext = _hubContext;
        var groupName = Group(game.Code);
        
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(questionDurationSeconds), game.CurrentTimerCts.Token);
                await EndQuestionWithContext(game, hubContext, groupName);
            }
            catch (TaskCanceledException) 
            { 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in question timer: {ex.Message}");
            }
        });
    }

    private async Task EndQuestionInternal(Game game, bool skipped = false)
    {
        await EndQuestionWithContext(game, _hubContext, Group(game.Code));
    }

    private async Task EndQuestionWithContext(Game game, IHubContext<GameHub> hubContext, string groupName)
    {
        // Prevent race conditions
        lock (game)
        {
            if (game.Phase != GamePhase.Question)
                return;

            game.CurrentTimerCts?.Cancel();
            game.Phase = GamePhase.Leaderboard;
        }

        var q = game.Questions[game.CurrentQuestionIndex];
        var questionDurationMs = q.TimeLimit * 1000.0;
        
        var answers = game.Players.Values.Select(p => 
        {
            bool isCorrect = p.HasAnsweredCurrent && 
                           p.SelectedOptionIndex.HasValue && 
                           p.SelectedOptionIndex.Value == q.CorrectOptionIndex;
            
            int points = 0;
            if (isCorrect && p.AnswerTimeMs.HasValue && questionDurationMs > 0)
            {
                double ratio = 1.0 - (p.AnswerTimeMs.Value / questionDurationMs);
                points = (int)Math.Round(1000 * Math.Max(0, ratio));
            }
            
            return new PlayerAnswerResultDto(
                Username: p.Username,
                Correct: isCorrect,
                Points: points,
                TimeMs: p.AnswerTimeMs ?? (long)questionDurationMs
            );
        }).ToList();

        // Create leaderboard and use IComparable<LeaderboardEntry> for sorting
        var leaderboardEntries = game.Players.Values
            .Select(p => new LeaderboardEntry 
            { 
                Username = p.Username, 
                Score = p.TotalScore 
            })
            .ToList();
        
        // Sort using the IComparable implementation (sorts by score desc, then by username)
        leaderboardEntries.Sort();
        
        // Convert to DTOs for response
        var leaderboard = leaderboardEntries
            .Select(e => new LeaderboardEntryDto(Username: e.Username, Score: e.Score))
            .ToList();

        var endedDto = new QuestionEndedDto(
            Index: game.CurrentQuestionIndex,
            CorrectOptionIndex: q.CorrectOptionIndex,
            Answers: answers,
            Leaderboard: leaderboard
        );

        await hubContext.Clients.Group(groupName).SendAsync("QuestionEnded", endedDto);
    }

    private void EnsureHost(Game game)
    {
        var userId = GetUserId();
        if (userId == null || int.Parse(userId) != game.HostUserId)
        {
            throw new HubException("Forbidden");
        }
    }

    private async Task BroadcastLobby(Game game)
    {
        var payload = new LobbyUpdateDto(
            Code: game.Code,
            Players: game.Players.Values
                .Select(p => new LobbyPlayerDto(Username: p.Username, IsHost: false))
                .ToList()
        );

        await Clients.Group(Group(game.Code)).SendAsync("LobbyUpdated", payload);
    }

    private static string Group(string code) => $"game_{code.ToUpperInvariant()}";
}
