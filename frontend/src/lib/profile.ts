export interface UserStats {
  userId: number;
  gamesPlayed: number;
  gamesWon: number;
  quizzesCreated: number;
  quizPlays: number;
}

export interface UserProfile {
  userId: number;
  username: string;
  email?: string | null;
  stats: UserStats;
}

export interface UserSearchResult {
  userId: number;
  username: string;
  gamesPlayed: number;
  quizPlays: number;
}

async function request<T>(input: RequestInfo | URL, init?: RequestInit) {
  const response = await fetch(input, {
    credentials: "include",
    ...init,
  });

  if (!response.ok) {
    const message = await response.text().catch(() => "Failed to fetch profile data");
    throw new Error(message || response.statusText);
  }

  return response.json() as Promise<T>;
}

export function fetchOwnProfile() {
  return request<UserProfile>("/api/users/profile");
}

export function fetchProfileById(userId: number) {
  return request<UserProfile>(`/api/users/${userId}/profile`);
}

export function searchProfiles(query: string, limit = 5, signal?: AbortSignal) {
  const params = new URLSearchParams({
    query,
    limit: limit.toString(),
  });
  return request<UserSearchResult[]>(`/api/users/search?${params.toString()}`, {
    signal,
  });
}
