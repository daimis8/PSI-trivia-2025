import { useState } from "react";
import type { ReactNode } from "react";
import { useAuth } from "@/context/AuthContext";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { EditProfileDialog } from "@/components/EditProfileDialog";
import { useQuery } from "@tanstack/react-query";
import { Edit, Loader2, PlayCircle, Star, Trophy, BarChart3 } from "lucide-react";
import { fetchOwnProfile, fetchProfileById, type UserProfile } from "@/lib/profile";
import { getCreatorBadge, getQuizzerBadge } from "@/lib/badges";

type ProfilePageProps = {
  profileId?: number;
};

export function ProfilePage(props: ProfilePageProps = {}) {
  const { profileId } = props;
  const { user } = useAuth();
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const targetUserId = profileId ?? user?.id;
  const isOwnProfile = profileId === undefined || (user?.id === profileId);

  const {
    data: profile,
    isLoading,
    isError,
    error,
  } = useQuery<UserProfile>({
    enabled: !!targetUserId,
    queryKey: ["profile", profileId ?? "me"],
    queryFn: () => (profileId ? fetchProfileById(profileId) : fetchOwnProfile()),
    staleTime: 60 * 1000,
  });

  if (!targetUserId) {
    return <CenteredMessage message="Loading profile..." />;
  }

  if (isLoading && !profile) {
    return <CenteredSpinner />;
  }

  if (isError || !profile) {
    return (
      <CenteredMessage
        message={error instanceof Error ? error.message : "Unable to load profile."}
      />
    );
  }

  const { stats } = profile;
  const winRate = stats.gamesPlayed > 0
    ? `${((stats.gamesWon / stats.gamesPlayed) * 100).toFixed(1)}% win rate`
    : "0.0% win rate";
  const avgPlaysPerQuiz = stats.quizzesCreated > 0
    ? `${(stats.quizPlays / stats.quizzesCreated).toFixed(1)} avg plays / quiz`
    : undefined;
  const quizzerBadge = getQuizzerBadge(stats.gamesPlayed);
  const creatorBadge = getCreatorBadge(stats.quizPlays);

  return (
    <>
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between mb-8">
        <div>
          <p className="text-sm uppercase tracking-wide text-muted-foreground">
            {isOwnProfile ? "Your profile" : `${profile.username}'s profile`}
          </p>
          <h1 className="text-4xl font-bold tracking-tight">Profile</h1>
        </div>

        {isOwnProfile && (
          <Button
            variant="outline"
            className="gap-2"
            onClick={() => setIsDialogOpen(true)}
          >
            <Edit className="size-4" />
            Edit Profile
          </Button>
        )}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-1">
          <Card className="bg-card">
            <CardContent className="flex flex-col items-center text-center pt-6 gap-4">
              <Avatar className="size-32 mb-2">
                <AvatarFallback className="text-3xl bg-primary text-primary-foreground">
                  {getInitials(profile.username)}
                </AvatarFallback>
              </Avatar>

              <div>
                <h2 className="text-2xl font-bold">{profile.username}</h2>
                {isOwnProfile && profile.email && (
                  <p className="text-sm text-muted-foreground">{profile.email}</p>
                )}
              </div>

              <div className="flex flex-col items-center gap-3 w-full">
                {[quizzerBadge, creatorBadge].map((badge) => (
                  <div key={`${badge.title}-${badge.level}`} className="space-y-1 text-center">
                    <Badge variant="secondary" className="uppercase tracking-wide">
                      {badge.title}: <span className="font-semibold ml-1">{badge.level}</span>
                    </Badge>
                    <p className="text-xs text-muted-foreground">{badge.helperText}</p>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </div>

        <div className="lg:col-span-2 space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <StatCard
              label="Games Played"
              icon={<PlayCircle className="size-6 text-muted-foreground" />}
              value={stats.gamesPlayed}
            />
            <StatCard
              label="Games Won"
              icon={<Star className="size-6 text-muted-foreground" />}
              value={stats.gamesWon}
              subLabel={winRate}
            />
            <StatCard
              label="Quizzes Created"
              icon={<Trophy className="size-6 text-muted-foreground" />}
              value={stats.quizzesCreated}
            />
            <StatCard
              label="Total Quiz Plays"
              icon={<BarChart3 className="size-6 text-muted-foreground" />}
              value={stats.quizPlays}
              subLabel={avgPlaysPerQuiz}
            />
          </div>
        </div>
      </div>

      {isOwnProfile && (
        <EditProfileDialog open={isDialogOpen} onOpenChange={setIsDialogOpen} />
      )}
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
  value: number | ReactNode;
  icon: ReactNode;
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

function CenteredSpinner() {
  return (
    <div className="flex flex-1 items-center justify-center py-24 text-muted-foreground">
      <Loader2 className="size-5 animate-spin" />
    </div>
  );
}

function CenteredMessage({ message }: { message: string }) {
  return (
    <div className="flex flex-1 items-center justify-center py-24 text-muted-foreground">
      {message}
    </div>
  );
}

function getInitials(username: string) {
  return username
    .split(" ")
    .map((part) => part[0])
    .join("")
    .slice(0, 2)
    .toUpperCase();
}