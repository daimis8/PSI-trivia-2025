import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Trophy } from "lucide-react";
import { cn } from "@/lib/utils";

interface Winner {
  username: string;
  score: number;
  rank: number;
}

interface WinnersPodiumProps {
  winners: Winner[];
  onPlayAgain?: () => void;
  onBackToHome?: () => void;
}

export function WinnersPodium({
  winners,
  onPlayAgain,
  onBackToHome,
}: WinnersPodiumProps) {
  const sortedWinners = [...winners].sort((a, b) => a.rank - b.rank);
  const first = sortedWinners.find((w) => w.rank === 1);
  const second = sortedWinners.find((w) => w.rank === 2);
  const third = sortedWinners.find((w) => w.rank === 3);

  const getPodiumHeight = (rank: number) => {
    switch (rank) {
      case 1:
        return "h-64";
      case 2:
        return "h-48";
      case 3:
        return "h-40";
      default:
        return "h-32";
    }
  };

  const getRankColor = (rank: number) => {
    switch (rank) {
      case 1:
        return "bg-gradient-to-b from-yellow-400 to-yellow-600";
      case 2:
        return "bg-gradient-to-b from-gray-300 to-gray-500";
      case 3:
        return "bg-gradient-to-b from-amber-500 to-amber-700";
      default:
        return "bg-gradient-to-b from-blue-400 to-blue-600";
    }
  };

  const PodiumPlace = ({ winner, rank }: { winner?: Winner; rank: number }) => {
    if (!winner) {
      return (
        <div className="flex flex-col items-center gap-4">
          <div className="w-48 h-48" />
          <div
            className={cn(
              "w-48 rounded-t-lg shadow-inner flex flex-col items-center justify-end pb-4 transition-all duration-500 opacity-30",
              getPodiumHeight(rank),
              getRankColor(rank)
            )}
          >
            <span className="text-6xl font-bold text-white/50">{rank}</span>
          </div>
        </div>
      );
    }

    return (
      <div className="flex flex-col items-center gap-4">
        <Card
          className={cn(
            "w-48 shadow-lg transition-all duration-300 hover:scale-105",
            rank === 1 && "ring-4 ring-yellow-500 ring-offset-2"
          )}
        >
          <CardContent className="pt-6 pb-4">
            <div className="flex flex-col items-center gap-3">
              <h3 className="text-xl font-bold text-center break-words max-w-full">
                {winner.username}
              </h3>

              <div className="flex flex-col items-center gap-1">
                <span className="text-3xl font-bold text-primary">
                  {winner.score}
                </span>
                <span className="text-sm text-muted-foreground">points</span>
              </div>

              <Badge
                className={cn(
                  "text-white font-semibold",
                  rank === 1 && "bg-yellow-500 hover:bg-yellow-600",
                  rank === 2 && "bg-gray-400 hover:bg-gray-500",
                  rank === 3 && "bg-amber-600 hover:bg-amber-700"
                )}
              >
                {rank === 1
                  ? "🥇 Winner"
                  : rank === 2
                    ? "🥈 2nd Place"
                    : "🥉 3rd Place"}
              </Badge>
            </div>
          </CardContent>
        </Card>

        <div
          className={cn(
            "w-48 rounded-t-lg shadow-inner flex flex-col items-center justify-end pb-4 transition-all duration-500",
            getPodiumHeight(rank),
            getRankColor(rank)
          )}
        >
          <span className="text-6xl font-bold text-white/90">{rank}</span>
        </div>
      </div>
    );
  };

  return (
    <div className="flex flex-col items-center gap-8 py-8">
      <div className="text-center space-y-2 mb-6">
        <h1 className="text-5xl font-bold tracking-tight flex items-center justify-center gap-3">
          Game Over!
        </h1>
      </div>

      <div className="flex items-end justify-center gap-8 mt-8">
        <div
          className={cn(
            "transition-all duration-700",
            second && "animate-in slide-in-from-left"
          )}
        >
          <PodiumPlace winner={second} rank={2} />
        </div>

        <div
          className={cn(
            "transition-all duration-700 relative -mt-8",
            first && "animate-in zoom-in"
          )}
        >
          <PodiumPlace winner={first} rank={1} />
        </div>

        <div
          className={cn(
            "transition-all duration-700",
            third && "animate-in slide-in-from-right"
          )}
        >
          <PodiumPlace winner={third} rank={3} />
        </div>
      </div>

      <div className="flex gap-4 mt-8">
        {onPlayAgain && (
          <Button onClick={onPlayAgain} size="lg" className="gap-2">
            <Trophy className="size-5" />
            Play Again
          </Button>
        )}
        {onBackToHome && (
          <Button
            onClick={onBackToHome}
            variant="outline"
            size="lg"
            className="gap-2"
          >
            Back to Home
          </Button>
        )}
      </div>

      {winners.length > 3 && (
        <div className="mt-8 w-full max-w-2xl">
          <h3 className="text-xl font-semibold mb-4 text-center">
            Other Participants
          </h3>
          <div className="space-y-2">
            {winners.slice(3).map((winner) => (
              <Card key={winner.username}>
                <CardContent className="flex items-center justify-between py-3">
                  <div className="flex items-center gap-3">
                    <Badge variant="outline">{winner.rank}th</Badge>
                    <span className="font-medium">{winner.username}</span>
                  </div>
                  <span className="text-lg font-semibold text-primary">
                    {winner.score} pts
                  </span>
                </CardContent>
              </Card>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
