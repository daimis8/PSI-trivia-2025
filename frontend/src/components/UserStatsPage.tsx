import { useAuth } from "@/context/AuthContext";
import { useQuery } from "@tanstack/react-query";
import { fetchUserStats } from "@/lib/stats";
import { Loader2 } from "lucide-react";

export function UserStatsPage() {
    const { user } = useAuth();
    const { data, isLoading, isError } = useQuery({
        enabled: !!user?.id,
        queryKey: ["user-stats", user?.id],
        queryFn: () => fetchUserStats(user!.id),
        staleTime: 60 * 1000,
    });

    if (!user) {
        return (
            <div className="flex items-center justify-center h-full">
                <p className="text-muted-foreground">Not authenticated.</p>
            </div>
        );
    }

    if (isLoading) {
        return (
            <div className="flex items-center justify-center h-full">
                <Loader2 className="size-6 animate-spin"/>
            </div>
        );
    }

    if (isError || !data) {
        return (
            <div className="flex items-center justify-center h-full">
                <p className="text-destructive">Failed to load stats.</p>
            </div>
        );
    }

    const winRate = 
        data.gamesPlayed > 0
        ? ((data.gamesWon / data.gamesPlayed) * 100).toFixed(1)
        : "0.0%";

    return (
        <div className="space-y-8">
            <header>
                <h1 className="text-4x1 font-bold tracking-tight mb-2">
                    Your Statistics
                </h1>
                <p className="text-muted-foreground">
                    Overview for {user.username}
                </p>
            </header>

            <section className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
                <StatsCard title="Games Played" value={data.gamesPlayed} />
                <StatsCard title="Games Won" value={data.gamesWon} subtitle={`${winRate}% win rate`} />
                <StatsCard title="Quizzes Created" value={data.quizzesCreated} />
                <StatsCard title="Quiz Plays (Total)" value={data.quizPlaysTotal} />
            </section>

            <section className="space-y-4">
                <h2 className="text-2x1 font-semibold">Insights</h2>
                <div className="grid gap-4 md:grid-cols-2">
                    <Insight 
                        label="Average Plays Per Created Quiz"
                        value={
                            data.quizzesCreated > 0
                            ? (data.quizPlaysTotal / data.quizzesCreated).toFixed(2)
                            : "0.00"
                        }
                    />
                    <Insight
                        label="Win Rate"
                        value={`${winRate}%`}
                    />
                </div>
            </section>
        </div>
    );
}

function StatsCard({
    title,
    value,
    subtitle,
}:{
    title: string;
    value: number | string;
    subtitle?: string;
}) {
    return (
        <div className="p-5 rounded-lg border bg-card flex flex-col gap-1">
      <p className="text-xs tracking-wide uppercase text-muted-foreground">
        {title}
      </p>
      <p className="text-3xl font-semibold">{value}</p>
      {subtitle && (
        <p className="text-xs text-muted-foreground mt-1">{subtitle}</p>
      )}
    </div>
    );
}

function Insight({ label, value }: { label: string; value: string }) {
  return (
    <div className="p-4 rounded-lg border bg-card flex flex-col gap-1">
      <span className="text-xs uppercase tracking-wide text-muted-foreground">
        {label}
      </span>
      <span className="text-xl font-medium">{value}</span>
    </div>
  );
}