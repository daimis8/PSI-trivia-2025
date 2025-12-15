export interface BadgeInfo {
  title: string;
  level: string;
  helperText: string;
}

export function getQuizzerBadge(gamesPlayed: number): BadgeInfo {
  if (gamesPlayed >= 25) {
    return {
      title: "Quizzer",
      level: "Pro",
      helperText: `${gamesPlayed} plays`,
    };
  }

  if (gamesPlayed >= 10) {
    return {
      title: "Quizzer",
      level: "Intermediate",
      helperText: `${gamesPlayed} plays`,
    };
  }

  return {
    title: "Quizzer",
    level: "Newbie",
    helperText: `${gamesPlayed} plays`,
  };
}

export function getCreatorBadge(quizPlays: number): BadgeInfo {
  if (quizPlays >= 50) {
    return {
      title: "Quiz Creator",
      level: "Established",
      helperText: `${quizPlays} plays`,
    };
  }

  if (quizPlays >= 25) {
    return {
      title: "Quiz Creator",
      level: "Known",
      helperText: `${quizPlays} plays`,
    };
  }

  return {
    title: "Quiz Creator",
    level: "New",
    helperText: `${quizPlays} plays`,
  };
}
