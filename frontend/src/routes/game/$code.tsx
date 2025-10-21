import { useEffect, useMemo, useRef, useState } from "react";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { createGameHub } from "@/lib/signalr";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { useAuth } from "@/context/AuthContext";

type LobbyPlayerDto = { username: string; isHost: boolean };
type LobbyUpdateDto = { code: string; players: LobbyPlayerDto[] };
type QuestionDto = { index: number; questionText: string; options: string[]; endsAt: string };
type LeaderboardEntryDto = { username: string; score: number };
type PlayerAnswerResultDto = { username: string; correct: boolean; points: number; timeMs: number };
type QuestionEndedDto = { index: number; correctOptionIndex: number; answers: PlayerAnswerResultDto[]; leaderboard: LeaderboardEntryDto[] };

export const Route = createFileRoute("/game/$code")({
  component: GameRoute,
});

function GameRoute() {
  const { code } = Route.useParams();
  const search = Route.useSearch() as { name?: string };
  const { user, isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const connectionRef = useRef<ReturnType<typeof createGameHub> | null>(null);

  const displayName = useMemo(() => {
    if (isAuthenticated && user) return user.username;
    return (search?.name || "").trim() || `Player-${Math.floor(Math.random()*1000)}`;
  }, [isAuthenticated, user, search]);

  const [lobby, setLobby] = useState<LobbyUpdateDto | null>(null);
  const [question, setQuestion] = useState<QuestionDto | null>(null);
  const [endsAt, setEndsAt] = useState<Date | null>(null);
  const [leaderboard, setLeaderboard] = useState<LeaderboardEntryDto[] | null>(null);
  const [selected, setSelected] = useState<number | null>(null);
  const [answerAccepted, setAnswerAccepted] = useState(false);
  const [joinError, setJoinError] = useState<string | null>(null);

  const timeLeft = useTimer(endsAt);

  useEffect(() => {
    const conn = createGameHub();
    connectionRef.current = conn;

    conn.on("LobbyUpdated", (data: LobbyUpdateDto) => {
      setJoinError(null);
      setLobby(data);
    });
    conn.on("QuestionStarted", (q: QuestionDto) => {
      setQuestion(q);
      setLeaderboard(null);
      setSelected(null);
      setAnswerAccepted(false);
      setEndsAt(new Date(q.endsAt));
    });
    conn.on("QuestionEnded", (dto: QuestionEndedDto) => {
      setLeaderboard(dto.leaderboard);
      setEndsAt(null);
    });
    conn.on("AnswerAccepted", () => setAnswerAccepted(true));
    conn.on("GameEnded", () => setEndsAt(null));

    conn.start()
      .then(async () => {
        try {
          await conn.invoke("JoinAsPlayer", code, displayName);
          setJoinError(null);
        } catch (e: unknown) {
          console.error(e);
          let msg = '' as string;
          if (typeof e === 'string') msg = e;
          else if (e && typeof e === 'object' && 'message' in e) {
            const m = (e as { message?: unknown }).message;
            if (typeof m === 'string') msg = m;
          }
          if (msg.toLowerCase().includes('not found')) setJoinError('Invalid game code.');
          else if (msg.toLowerCase().includes('already started')) setJoinError('Game already started.');
          else setJoinError('Failed to join game.');
        }
      })
      .catch(() => {
        setJoinError('Failed to connect to server.');
      });

    return () => { conn.stop(); };
  }, [code, displayName]);

  const submitAnswer = async (idx: number) => {
    if (selected !== null) return;
    setSelected(idx);
    await connectionRef.current?.invoke("SubmitAnswer", code, idx);
  };

  return (
    <div className="container mx-auto p-4 space-y-4">
      <h1 className="text-2xl font-bold">Game: {code}</h1>

      {joinError && (
        <Card>
          <CardHeader>
            <CardTitle>Unable to join</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-red-600 mb-4">{joinError}</div>
            <Button onClick={() => navigate({ to: '/play' })}>Back to Join</Button>
          </CardContent>
        </Card>
      )}

      {!joinError && !question && (
        <Card>
          <CardHeader>
            <CardTitle>Lobby</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-sm text-muted-foreground mb-2">Players</div>
            <div className="flex flex-wrap gap-2">
              {lobby?.players.map((p, i) => (
                <span key={i} className="px-3 py-1 rounded-full bg-secondary text-secondary-foreground text-sm">{p.username}</span>
              ))}
            </div>
            <div className="mt-4 text-sm text-muted-foreground">Waiting for host to start…</div>
          </CardContent>
        </Card>
      )}

      {!joinError && question && (
        <Card>
          <CardHeader>
            <CardTitle>Question {question.index + 1} {timeLeft !== null && (<span className="ml-2 text-sm text-muted-foreground">{timeLeft}s</span>)}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-lg font-medium mb-4">{question.questionText}</div>
            <ul className="grid grid-cols-1 md:grid-cols-2 gap-2">
              {question.options.map((o, idx) => (
                <li key={idx}>
                  <Button
                    disabled={selected !== null}
                    className={`w-full justify-start ${selected === idx ? "border-2 border-primary" : ""}`}
                    onClick={() => submitAnswer(idx)}
                  >
                    {o}
                  </Button>
                </li>
              ))}
            </ul>
            {answerAccepted && <div className="mt-2 text-sm text-muted-foreground">Answer submitted</div>}
          </CardContent>
        </Card>
      )}

      {!joinError && leaderboard && (
        <Card>
          <CardHeader>
            <CardTitle>Leaderboard</CardTitle>
          </CardHeader>
          <CardContent>
            <ol className="space-y-1">
              {leaderboard.map((e, i) => (
                <li key={i} className="flex justify-between"><span>{i + 1}. {e.username}</span><span>{e.score}</span></li>
              ))}
            </ol>
          </CardContent>
        </Card>
      )}
    </div>
  );
}

function useTimer(endsAt: Date | null) {
  const [left, setLeft] = useState<number | null>(null);
  useEffect(() => {
    if (!endsAt) {
      setLeft(null);
      return;
    }
    function calc() {
      const e = endsAt;
      if (!e) {
        setLeft(null);
        return;
      }
      const ms = Math.max(0, e.getTime() - Date.now());
      setLeft(Math.ceil(ms / 1000));
    }
    calc();
    const t = setInterval(calc, 200);
    return () => clearInterval(t);
  }, [endsAt]);
  return left;
}
