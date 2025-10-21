using backend.DTOs;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace backend.Hubs;

public class GameHub : Hub
{
    private readonly GameService _gameService;
    private readonly QuizService _quizService;

    public GameHub(GameService gameService, QuizService quizService)
    {
        _gameService = gameService;
        _quizService = quizService;
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
        if (!game.Questions.Any())
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

        var elapsed = now - game.QuestionStartTime.Value;
        var total = game.QuestionEndTime.Value - game.QuestionStartTime.Value;
        if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;
        if (elapsed > total) elapsed = total;

        var q = game.Questions[game.CurrentQuestionIndex];
        var correct = optionIndex == q.CorrectOptionIndex;
        var remainingRatio = 1.0 - (elapsed.TotalMilliseconds / total.TotalMilliseconds);
        if (remainingRatio < 0) remainingRatio = 0;
        var points = correct ? (int)Math.Round(1000 * remainingRatio) : 0;

        player.HasAnsweredCurrent = true;
        player.SelectedOptionIndex = optionIndex;
        player.AnswerTimeMs = (long)elapsed.TotalMilliseconds;
        if (correct)
            player.TotalScore += points;

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

        var now = DateTimeOffset.UtcNow;
        game.QuestionStartTime = now;
        game.QuestionEndTime = now.AddSeconds(game.QuestionDurationSeconds);

        var q = game.Questions[game.CurrentQuestionIndex];
        var dto = new QuestionDto
        {
            Index = game.CurrentQuestionIndex,
            QuestionText = q.QuestionText,
            Options = q.Options,
            EndsAt = game.QuestionEndTime.Value
        };

        await Clients.Group(Group(game.Code)).SendAsync("QuestionStarted", dto);

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(game.QuestionDurationSeconds), game.CurrentTimerCts.Token);
                await EndQuestionInternal(game);
            }
            catch (TaskCanceledException) { }
        });
    }

    private async Task EndQuestionInternal(Game game, bool skipped = false)
    {
        if (game.Phase != GamePhase.Question)
            return;

        game.CurrentTimerCts?.Cancel();
        game.Phase = GamePhase.Leaderboard;

        var q = game.Questions[game.CurrentQuestionIndex];
        var answers = game.Players.Values.Select(p => new PlayerAnswerResultDto
        {
            Username = p.Username,
            Correct = p.SelectedOptionIndex == q.CorrectOptionIndex && p.HasAnsweredCurrent,
            Points = (p.SelectedOptionIndex == q.CorrectOptionIndex && p.HasAnsweredCurrent && p.AnswerTimeMs.HasValue)
                ? (int)Math.Round(1000 * Math.Max(0, 1.0 - (p.AnswerTimeMs.Value / (game.QuestionDurationSeconds * 1000.0))))
                : 0,
            TimeMs = (long)(p.AnswerTimeMs ?? (game.QuestionDurationSeconds * 1000))
        }).ToList();

        var leaderboard = game.Players.Values
            .OrderByDescending(p => p.TotalScore)
            .Select(p => new LeaderboardEntryDto { Username = p.Username, Score = p.TotalScore })
            .ToList();

        var endedDto = new QuestionEndedDto
        {
            Index = game.CurrentQuestionIndex,
            CorrectOptionIndex = q.CorrectOptionIndex,
            Answers = answers,
            Leaderboard = leaderboard
        };

        await Clients.Group(Group(game.Code)).SendAsync("QuestionEnded", endedDto);
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
        var payload = new LobbyUpdateDto
        {
            Code = game.Code,
            Players = game.Players.Values
                .Select(p => new LobbyPlayerDto { Username = p.Username, IsHost = false })
                .ToList()
        };

        await Clients.Group(Group(game.Code)).SendAsync("LobbyUpdated", payload);
    }

    private static string Group(string code) => $"game_{code.ToUpperInvariant()}";
}
