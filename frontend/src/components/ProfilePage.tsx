import { useState } from "react";
import { useAuth } from "@/context/AuthContext";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { EditProfileDialog } from "@/components/EditProfileDialog";
import { PlayCircle, Star, Edit, Loader2 } from "lucide-react";
import { Link } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { fetchUserStats} from "@/lib/stats";
import type { UserStats } from "@/lib/stats";

export function ProfilePage() {
  const { user } = useAuth();
  const [isDialogOpen, setIsDialogOpen] = useState(false);

  const {
    data: stats,
    isLoading: statsLoading,
    isError: statsError,
  } = useQuery<UserStats>({
    enabled: !!user?.id,
    queryKey: ["user-stats", user?.id],
    queryFn: () => fetchUserStats(user!.id),
    staleTime: 60 * 1000,
  });

  return (
    <>
      <div className="flex justify-between items-center mb-8">
        <h1 className="text-4xl font-bold tracking-tight">
          Profile
        </h1>

        <Button
          variant="outline"
          className="gap-2"
          onClick={() => setIsDialogOpen(true)}
        >
          <Edit className="size-4" />
          Edit Profile
        </Button>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-1">
          <Card className="bg-card">
            <CardContent className="flex flex-col items-center text-center pt-6 gap-2">
              <Avatar className="size-32 mb-4">
                <AvatarImage src="https://github.com/shadcn.png" />
                <AvatarFallback className="text-2xl bg-primary text-primary-foreground">
                  {user?.username}
                </AvatarFallback>
              </Avatar>

              <h2 className="text-2xl font-bold mb-1">
                {user?.username}
              </h2>
              <Badge variant="secondary" className="mb-6">
                Pro quizzer
              </Badge>

              <Link to="/stats">
                <Button variant="link" className="text-sm p-0 h-auto">
                  View full stats →
                </Button>
              </Link>
            </CardContent>
          </Card>
        </div>

        <div className="lg:col-span-2 space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <StatCard 
              label="Games PLayed" 
              icon={<PlayCircle className="size-6 text-muted-foreground" />}
              value={statsLoading ? <LoadingValue /> : statsError ? "-" : stats?.gamesPlayed ?? 0}
            />
            <StatCard
              label="Games Won"
              icon={<Star className="size-6 text-muted-foreground" />}
              value={statsLoading ? <LoadingValue /> : statsError ? "-" : stats?.gamesWon ?? 0}
              subLabel={
                stats && stats.gamesPlayed > 0
                ? '${((stats.gamesWon / stats.gamesPlayed) * 100).toFixed(1)}% win rate'
                : "0.0% win rate"
              }
            />
          </div>
        </div>
      </div>

      <EditProfileDialog open={isDialogOpen} onOpenChange={setIsDialogOpen} />
    </>
  );
}

function StatCard({
  label,
  value,
  icon,
  subLabel,
}: {
  label: string;
  value: React.ReactNode;
  icon: React.ReactNode;
  subLabel?: string;
}) {
  return (
    <Card className="hover:shadow-md transition-shadow bg-card">
      <CardContent className="flex items-center gap-4 py-6">
        <div className="p-3 rounded-lg border bg-secondary/30">
          {icon}
        </div>
        <div className="flex-1">
          <div className="text-xs uppercase tracking-wide text-muted-foreground">{label}</div>
          <div className="text-3xl font-bold leading-tight mt-1">{value}</div>
          {subLabel && <div className="text-xs text-muted-foreground mt-1">{subLabel}</div>}
        </div>
      </CardContent>
    </Card>
  );
}

function LoadingValue() {
  return (
    <span className="inline-flex items-center gap-1 text-muted-foreground">
      <Loader2 className="size-4 animate-spin" />
    </span>
  );
}