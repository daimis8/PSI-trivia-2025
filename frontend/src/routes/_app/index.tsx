import { useAuth } from "@/context/AuthContext";
import { createFileRoute, Link } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Skeleton } from "@/components/ui/skeleton";
import { Badge } from "@/components/ui/badge";
import { apiFetch } from "@/lib/api";

export const Route = createFileRoute("/_app/")({
  component: Index,
});

interface TopPlayer {
  userId: number;
  username: string;
  gamesWon: number;
}

interface TopQuiz {
  quizId: number;
  title: string;
  creatorUsername: string;
  timesPlayed: number;
}

function Index() {
  const { user } = useAuth();

  const {
    data: topPlayers,
    isLoading: loadingPlayers,
    isError: errorPlayers,
  } = useQuery<TopPlayer[]>({
    queryKey: ["top-players"],
    queryFn: async () => {
      const response = await apiFetch("/api/leaderboard/top-players?limit=10");
      if (!response.ok) {
        throw new Error("Failed to fetch top players");
      }
      return response.json();
    },
  });

  const {
    data: topQuizzes,
    isLoading: loadingQuizzes,
    isError: errorQuizzes,
  } = useQuery<TopQuiz[]>({
    queryKey: ["top-quizzes"],
    queryFn: async () => {
      const response = await apiFetch("/api/leaderboard/top-quizzes?limit=10");
      if (!response.ok) {
        throw new Error("Failed to fetch top quizzes");
      }
      return response.json();
    },
  });

  return (
    <div className="container mx-auto py-8 px-4">
      <div className="mb-8">
        <h1 className="text-4xl font-bold mb-2">
          Welcome back, {user?.username || user?.email}!
        </h1>
        <p className="text-muted-foreground">
          Check out the top players and most popular quizzes
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 items-start">
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <CardTitle>Top Players</CardTitle>
            </div>
            <CardDescription>Players with the most wins</CardDescription>
          </CardHeader>
          <CardContent>
            {loadingPlayers ? (
              <div className="space-y-2">
                {[...Array(5)].map((_, i) => (
                  <Skeleton key={i} className="h-12 w-full" />
                ))}
              </div>
            ) : errorPlayers ? (
              <p className="text-destructive">Failed to load top players</p>
            ) : !topPlayers || topPlayers.length === 0 ? (
              <p className="text-muted-foreground text-center py-8">
                No players yet. Be the first to win!
              </p>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="w-12">Rank</TableHead>
                    <TableHead>Player</TableHead>
                    <TableHead className="text-right">Wins</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {topPlayers.map((player, index) => (
                    <TableRow key={player.userId}>
                      <TableCell className="font-medium">
                        {index === 0 && (
                          <span className="text-yellow-500">🥇</span>
                        )}
                        {index === 1 && (
                          <span className="text-gray-400">🥈</span>
                        )}
                        {index === 2 && (
                          <span className="text-orange-600">🥉</span>
                        )}
                        {index > 2 && <span>{index + 1}</span>}
                      </TableCell>
                      <TableCell>
                        <div className="flex items-center gap-2">
                          <Link
                            to="/profile/$profileId"
                            params={{ profileId: player.userId.toString() }}
                            className="text-primary hover:underline"
                          >
                            {player.username}
                          </Link>
                          {user?.id === player.userId && (
                            <Badge variant="outline" className="text-xs">
                              You
                            </Badge>
                          )}
                        </div>
                      </TableCell>
                      <TableCell className="text-right font-semibold">
                        {player.gamesWon}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <CardTitle>Top Quizzes</CardTitle>
            </div>
            <CardDescription>Most played quizzes</CardDescription>
          </CardHeader>
          <CardContent>
            {loadingQuizzes ? (
              <div className="space-y-2">
                {[...Array(5)].map((_, i) => (
                  <Skeleton key={i} className="h-12 w-full" />
                ))}
              </div>
            ) : errorQuizzes ? (
              <p className="text-destructive">Failed to load top quizzes</p>
            ) : !topQuizzes || topQuizzes.length === 0 ? (
              <p className="text-muted-foreground text-center py-8">
                No quizzes yet. Create the first one!
              </p>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="w-12">Rank</TableHead>
                    <TableHead>Quiz</TableHead>
                    <TableHead className="text-right">Plays</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {topQuizzes.map((quiz, index) => (
                    <TableRow key={quiz.quizId}>
                      <TableCell className="font-medium">
                        {index === 0 && (
                          <span className="text-yellow-500">🥇</span>
                        )}
                        {index === 1 && (
                          <span className="text-gray-400">🥈</span>
                        )}
                        {index === 2 && (
                          <span className="text-orange-600">🥉</span>
                        )}
                        {index > 2 && <span>{index + 1}</span>}
                      </TableCell>
                      <TableCell>
                        <div>
                          <Link
                            to="/quizes/$quizId"
                            params={{ quizId: quiz.quizId.toString() }}
                            className="font-medium text-primary hover:underline"
                          >
                            {quiz.title}
                          </Link>
                          <div className="text-sm text-muted-foreground">
                            by {quiz.creatorUsername}
                          </div>
                        </div>
                      </TableCell>
                      <TableCell className="text-right font-semibold">
                        {quiz.timesPlayed}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
