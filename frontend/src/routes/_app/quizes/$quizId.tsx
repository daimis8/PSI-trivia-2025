import { createFileRoute, Link, useNavigate } from "@tanstack/react-router";
import { useQuery, useMutation } from "@tanstack/react-query";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Play, Loader2 } from "lucide-react";
import { getApiUrl } from "@/lib/api";

export const Route = createFileRoute("/_app/quizes/$quizId")({
  component: QuizById,
});

interface QuizQuestion {
  id: number;
  questionText: string;
  options: string[];
  timeLimit: number;
}

interface Quiz {
  id: number;
  creatorID: number;
  creatorUsername: string;
  title: string;
  description: string;
  timesPlayed: number;
  questions: QuizQuestion[];
}

function QuizById() {
  const { quizId } = Route.useParams();
  const parsedId = Number(quizId);
  const navigate = useNavigate();

  const {
    data: quiz,
    isLoading,
    isError,
  } = useQuery<Quiz>({
    queryKey: ["quiz-public", parsedId],
    queryFn: async () => {
      const response = await fetch(getApiUrl(`/api/quizzes/${parsedId}/public`));
      if (!response.ok) {
        throw new Error("Failed to fetch quiz");
      }
      return response.json();
    },
    enabled: !Number.isNaN(parsedId),
  });

  const { mutate: startGame, isPending: isStarting } = useMutation({
    mutationFn: async (quizId: number) => {
      const res = await fetch(getApiUrl("/api/games"), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ quizId }),
        credentials: "include",
      });
      if (!res.ok) {
        const error = await res.json();
        throw new Error(error.message || "Failed to create game");
      }
      return res.json() as Promise<{ code: string }>;
    },
    onSuccess: (data) => {
      navigate({ to: `/host/${data.code}` });
    },
  });

  if (Number.isNaN(parsedId)) {
    return (
      <div className="flex flex-1 items-center justify-center text-muted-foreground">
        Invalid quiz identifier.
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="container mx-auto py-8 px-4 max-w-4xl">
        <Skeleton className="h-12 w-3/4 mb-4" />
        <Skeleton className="h-6 w-1/2 mb-8" />
        <div className="space-y-4">
          {[...Array(3)].map((_, i) => (
            <Skeleton key={i} className="h-40 w-full" />
          ))}
        </div>
      </div>
    );
  }

  if (isError || !quiz) {
    return (
      <div className="flex flex-1 items-center justify-center text-destructive">
        Failed to load quiz. Please try again later.
      </div>
    );
  }

  return (
    <div className="container mx-auto py-8 px-4 max-w-4xl">
      <div className="mb-8">
        <div className="flex items-start justify-between gap-4 mb-4">
          <div className="flex-1">
            <h1 className="text-4xl font-bold mb-2">{quiz.title}</h1>
            <p className="text-muted-foreground mb-2">{quiz.description}</p>
            <div className="flex items-center gap-4 text-sm text-muted-foreground">
              <span>
                Created by{" "}
                <Link
                  to="/profile/$profileId"
                  params={{ profileId: quiz.creatorID.toString() }}
                  className="text-primary hover:underline"
                >
                  {quiz.creatorUsername}
                </Link>
              </span>
              <span>•</span>
              <span>Played {quiz.timesPlayed} times</span>
              <span>•</span>
              <span>{quiz.questions.length} questions</span>
            </div>
          </div>
          <Button
            onClick={() => startGame(parsedId)}
            disabled={isStarting || quiz.questions.length === 0}
            size="lg"
            className="gap-2 shrink-0"
          >
            {isStarting ? (
              <>
                <Loader2 className="size-4 animate-spin" />
                Starting...
              </>
            ) : (
              <>
                <Play className="size-4" />
                Start Quiz
              </>
            )}
          </Button>
        </div>
      </div>

      <div className="space-y-6">
        <h2 className="text-2xl font-semibold">Questions</h2>
        {quiz.questions.map((question, index) => (
          <Card key={question.id}>
            <CardHeader>
              <div className="flex items-start justify-between gap-4">
                <CardTitle className="flex items-start gap-3">
                  <Badge variant="secondary" className="shrink-0">
                    {index + 1}
                  </Badge>
                  <span>{question.questionText}</span>
                </CardTitle>
                <Badge variant="outline" className="shrink-0">
                  {question.timeLimit}s
                </Badge>
              </div>
              <CardDescription>Choose the correct answer</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                {question.options.map((option, optionIndex) => (
                  <div
                    key={optionIndex}
                    className="p-4 border rounded-lg bg-muted/30 hover:bg-muted/50 transition-colors"
                  >
                    <div className="flex items-center gap-2">
                      <Badge variant="outline" className="shrink-0">
                        {String.fromCharCode(65 + optionIndex)}
                      </Badge>
                      <span>{option}</span>
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}
