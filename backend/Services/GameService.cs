using System.Collections.Concurrent;
using backend.Models;

namespace backend.Services;

public class GameService
{
    private readonly ConcurrentDictionary<string, Game> _games = new();
    private readonly Random _random = new();

    public Game CreateGame(int hostUserId, int quizId, List<QuizQuestion> questions)
    {
        string code;
        do
        {
            code = GenerateCode();
        } while (_games.ContainsKey(code));

        var game = new Game
        {
            Code = code,
            HostUserId = hostUserId,
            QuizId = quizId,
            Questions = questions,
            Phase = GamePhase.Lobby,
            CurrentQuestionIndex = -1,
        };

        _games[code] = game;
        return game;
    }

    public bool TryGetGame(string code, out Game? game)
    {
        var ok = _games.TryGetValue(code.ToUpperInvariant(), out var g);
        game = g;
        return ok;
    }

    public void RemoveGame(string code)
    {
        _games.TryRemove(code, out _);
    }

    public bool RemoveByConnection(string connectionId, out Game? game)
    {
        game = null;
        foreach (var kv in _games)
        {
            var g = kv.Value;
            if (g.Players.ContainsKey(connectionId))
            {
                g.Players.TryRemove(connectionId, out _);
                game = g;
                return true;
            }
        }
        return false;
    }

    private string GenerateCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return new string(Enumerable.Repeat(chars, 6).Select(s => s[_random.Next(s.Length)]).ToArray());
    }
}
