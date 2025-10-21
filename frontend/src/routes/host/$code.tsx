import { useEffect, useRef, useState } from "react";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useAuth } from "@/context/AuthContext";
import { createGameHub } from "@/lib/signalr";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { WinnersPodium } from "@/components/WinnersPodium";

type LobbyPlayerDto = { username: string; isHost: boolean };
type LobbyUpdateDto = { code: string; players: LobbyPlayerDto[] };
type QuestionDto = {
  index: number;
  questionText: string;
  options: string[];
  endsAt: string;
};
type LeaderboardEntryDto = { username: string; score: number };
type PlayerAnswerResultDto = {
  username: string;
  correct: boolean;
  points: number;
  timeMs: number;
};
type QuestionEndedDto = {
  index: number;
  correctOptionIndex: number;
  answers: PlayerAnswerResultDto[];
  leaderboard: LeaderboardEntryDto[];
};

export const Route = createFileRoute("/host/$code")({
  component: HostRoute,
});

function HostRoute() {
  const { code } = Route.useParams();
  const { isAuthenticated } = useAuth();
  const nav = useNavigate();
  const connectionRef = useRef<ReturnType<typeof createGameHub> | null>(null);

  const [lobby, setLobby] = useState<LobbyUpdateDto | null>(null);
  const [question, setQuestion] = useState<QuestionDto | null>(null);
  const [endsAt, setEndsAt] = useState<Date | null>(null);
  const [leaderboard, setLeaderboard] = useState<LeaderboardEntryDto[] | null>(
    null
  );
  const [correctIndex, setCorrectIndex] = useState<number | null>(null);
  const [gameEnded, setGameEnded] = useState(false);

  const timeLeft = useTimer(endsAt);

  useEffect(() => {
    if (!isAuthenticated) {
      nav({ to: "/login" });
      return;
    }
  }, [isAuthenticated, nav]);

  useEffect(() => {
    const conn = createGameHub();
    connectionRef.current = conn;

    conn.on("LobbyUpdated", (data: LobbyUpdateDto) => setLobby(data));
    conn.on("QuestionStarted", (q: QuestionDto) => {
      setQuestion(q);
      setLeaderboard(null);
      // reset previous results
      setCorrectIndex(null);
      setEndsAt(new Date(q.endsAt));
    });
    conn.on("QuestionEnded", (dto: QuestionEndedDto) => {
      setLeaderboard(dto.leaderboard);
      setCorrectIndex(dto.correctOptionIndex);
      setEndsAt(null);
    });
    conn.on("GameEnded", () => {
      setEndsAt(null);
      setGameEnded(true);
    });

    conn
      .start()
      .then(() => conn.invoke("JoinAsHost", code))
      .catch(console.error);

    return () => {
      conn.stop();
    };
  }, [code]);

  const handleStart = async () => {
    await connectionRef.current?.invoke("StartGame", code);
  };
  const handleSkip = async () => {
    await connectionRef.current?.invoke("SkipQuestion", code);
  };
  const handleNext = async () => {
    await connectionRef.current?.invoke("NextQuestion", code);
  };

  if (gameEnded && leaderboard) {
    const winners = leaderboard.map((entry, index) => ({
      username: entry.username,
      score: entry.score,
      rank: index + 1,
    }));

    return (
      <div className="container mx-auto p-4">
        <WinnersPodium
          winners={winners}
          onBackToHome={() => nav({ to: "/" })}
        />
      </div>
    );
  }

  return (
    <div className="container mx-auto p-4 space-y-4">
      <h1 className="text-3xl font-bold">Host Game</h1>
      <Card>
        <CardHeader>
          <CardTitle>
            Join Code: <span className="font-mono tracking-widest">{code}</span>
          </CardTitle>
        </CardHeader>
        <CardContent>
          {lobby && (
            <div className="space-y-2">
              <div className="text-sm text-muted-foreground">Players</div>
              <div className="flex flex-wrap gap-2">
                {lobby.players.map((p, i) => (
                  <span
                    key={i}
                    className="px-3 py-1 rounded-full bg-secondary text-secondary-foreground text-sm"
                  >
                    {p.username}
                  </span>
                ))}
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {!question && (
        <Button className="border" onClick={handleStart}>
          Start
        </Button>
      )}

      {question && (
        <Card>
          <CardHeader>
            <CardTitle>
              Question {question.index + 1}{" "}
              {timeLeft !== null && (
                <span className="ml-2 text-sm text-muted-foreground">
                  {timeLeft}s
                </span>
              )}
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-lg font-medium mb-4">
              {question.questionText}
            </div>
            <ul className="grid grid-cols-1 md:grid-cols-2 gap-2">
              {question.options.map((o, idx) => (
                <li
                  key={idx}
                  className={`p-3 rounded border bg-card ${
                    leaderboard && correctIndex === idx
                      ? "border-green-500 bg-green-50"
                      : ""
                  }`}
                >
                  {o}
                </li>
              ))}
            </ul>
            {!leaderboard && (
              <div className="mt-4 flex gap-2">
                <Button variant="outline" onClick={handleSkip}>
                  Skip
                </Button>
              </div>
            )}
          </CardContent>
        </Card>
      )}

      {leaderboard && (
        <Card>
          <CardHeader>
            <CardTitle>Leaderboard</CardTitle>
          </CardHeader>
          <CardContent>
            <ol className="space-y-1">
              {leaderboard.map((e, i) => (
                <li key={i} className="flex justify-between">
                  <span>
                    {i + 1}. {e.username}
                  </span>
                  <span>{e.score}</span>
                </li>
              ))}
            </ol>
            <div className="mt-4">
              <Button onClick={handleNext}>Next</Button>
            </div>
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
