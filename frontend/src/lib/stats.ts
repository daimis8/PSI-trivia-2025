export interface UserStats {
  userId: number;
  gamesPlayed: number;
  gamesWon: number;
  quizzesCreated: number;
  quizPlaysTotal: number;
}

export async function fetchUserStats(userId: number): Promise<UserStats> {
  const response = await fetch(`/api/stats/users/${userId}`, { credentials: "include"});
  if (!response.ok) {
    throw new Error("Failed to fetch user stats");
  }
  return response.json();
}