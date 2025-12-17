import { Button } from "@/components/ui/button";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { Loader2, PlusCircle, CircleQuestionMark, Edit, Trash2, FileText, ListChecks, AlertTriangle, Play, BarChart3 } from "lucide-react";
import ErrorComponent from "@/components/Error";
import { Empty, EmptyContent, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from "@/components/ui/empty";
import { apiFetch } from "@/lib/api";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { useState } from "react";

export const Route = createFileRoute("/_app/my-quizzes/")({
  component: RouteComponent,
});

function RouteComponent() {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  interface Question { id: number; questionText: string; options: string[]; correctOptionIndex: number }
  interface MyQuiz { id: number; title: string; description: string; questions: Question[]; creatorID: number; timesPlayed: number; }
  const [quizToDelete, setQuizToDelete] = useState<MyQuiz | null>(null);

  const { isPending, isError, data } = useQuery<MyQuiz[]>({
    queryKey: ["my-quizzes"],
    queryFn: async () => {
      const response = await apiFetch("/api/quizzes/my");
      if (!response.ok) {
        throw new Error("Failed to fetch quizzes");
      }
      return response.json();
    },
  });

  const { mutate: createQuiz, isPending: isCreating } = useMutation({
    mutationFn: async () => {
      const response = await apiFetch("/api/quizzes/my", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
      });

      if (!response.ok) {
        throw new Error("Failed to create quiz");
      }

      return response.json();
    },
    onSuccess: (data) => {
      // Navigate to the edit page for the newly created quiz
      navigate({ to: `/my-quizzes/${data.id}` });
    },
  });

  const { mutate: deleteQuiz, isPending: isDeleting } = useMutation({
    mutationFn: async (quizId: number) => {
      const response = await apiFetch(`/api/quizzes/${quizId}`, {
        method: "DELETE",
      });

      if (!response.ok) {
        throw new Error("Failed to delete quiz");
      }

      return true;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["my-quizzes"] });
      setDeleteDialogOpen(false);
      setQuizToDelete(null);
    },
  });

  const { mutate: startGame, isPending: isStarting } = useMutation({
    mutationFn: async (quizId: number) => {
      const res = await apiFetch("/api/games", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ quizId }),
      });
      if (!res.ok) throw new Error("Failed to create game");
      return res.json() as Promise<{ code: string }>;
    },
    onSuccess: (data) => {
      navigate({ to: `/host/${data.code}` });
    },
  });

  const handleDeleteClick = (quiz: MyQuiz) => {
    setQuizToDelete(quiz);
    setDeleteDialogOpen(true);
  };

  const handleDeleteConfirm = () => {
    if (quizToDelete) {
      deleteQuiz(quizToDelete.id);
    }
  };

  if (isPending) {
    return (
      <div className="flex items-center justify-center h-full flex-1">
        <Loader2 className="animate-spin" />
      </div>
    );
  }

  if (isError) {
    return (
      <div className="flex items-center justify-center h-full flex-1">
        <ErrorComponent />
      </div>
    );
  }
  return (
    <>
      <div className="flex justify-between items-center mb-8">
        <h1 className="text-4xl font-bold tracking-tight">
          Your quizzes
        </h1>

        <Button variant="outline" className="gap-2" onClick={() => createQuiz()} disabled={isCreating}>
          <PlusCircle className="size-4" />
          {isCreating ? "Creating..." : "Create a Quiz"}
        </Button>
      </div>
      {data.length === 0 ? (
        <Empty className="border border-dashed">
          <EmptyHeader>
            <EmptyMedia variant="icon">
              <CircleQuestionMark />
            </EmptyMedia>
            <EmptyTitle>You don't have any quizzes</EmptyTitle>
            <EmptyDescription>
              Create a quiz to get started.
            </EmptyDescription>
          </EmptyHeader>
          <EmptyContent>
            <Button variant="outline" size="sm" onClick={() => createQuiz()} disabled={isCreating} className="gap-2">
              {isCreating ? "Creating..." : "Create Quiz"}
            </Button>
          </EmptyContent>
        </Empty>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {data.map((quiz: MyQuiz) => (
            <Card key={quiz.id} className="hover:shadow-lg transition-shadow duration-300 flex flex-col">
              <CardHeader className="pb-3">
                <div className="flex items-start justify-between gap-2">
                  <div className="flex-1">
                    <CardTitle className="text-xl line-clamp-2 mb-2">
                      {quiz.title || "Untitled Quiz"}
                    </CardTitle>
                    <div className="flex items-center gap-2 text-sm text-muted-foreground">
                      <ListChecks className="size-4" />
                      <span>{quiz.questions?.length || 0} question{quiz.questions?.length !== 1 ? 's' : ''}</span>
                    </div>
                    <div className="flex item-center gap-2 text-sm text-muted-foreground">
                      <BarChart3 className="size-4" />
                      <span>{quiz.timesPlayed ?? 0} play{(quiz.timesPlayed ?? 0) === 1 ? '' : 's'}</span>
                    </div>
                  </div>
                </div>
              </CardHeader>

              <CardContent className="pb-3 flex-1">
                <div className="flex items-start gap-2 text-muted-foreground">
                  <FileText className="size-4 mt-0.5 shrink-0" />
                  <CardDescription className="line-clamp-3">
                    {quiz.description || "No description provided yet."}
                  </CardDescription>
                </div>
              </CardContent>

              <CardFooter className="pt-3 flex gap-2">
                <Button
                  variant="default"
                  size="sm"
                  className="flex-1 gap-2"
                  onClick={() => navigate({ to: `/my-quizzes/${quiz.id}` })}
                >
                  <Edit className="size-4" />
                  Edit
                </Button>
                <Button
                  variant="secondary"
                  size="sm"
                  className="gap-2"
                  onClick={() => startGame(quiz.id)}
                  disabled={isStarting}
                >
                  <Play className="size-4" />
                  {isStarting ? "Starting..." : "Start"}
                </Button>
                <Button
                  variant="destructive"
                  size="sm"
                  className="gap-2"
                  onClick={() => handleDeleteClick(quiz)}
                >
                  <Trash2 className="size-4" />
                </Button>
              </CardFooter>
            </Card>
          ))}
        </div>
      )}

      <Dialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <AlertTriangle className="size-5 text-destructive" />
              Delete Quiz
            </DialogTitle>
            <DialogDescription>
              Are you sure you want to delete "{quizToDelete?.title || "this quiz"}"?
              This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="gap-2 sm:gap-0">
            <Button
              variant="outline"
              onClick={() => setDeleteDialogOpen(false)}
              disabled={isDeleting}
            >
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={handleDeleteConfirm}
              disabled={isDeleting}
              className="gap-2"
            >
              {isDeleting ? (
                <>
                  <Loader2 className="size-4 animate-spin" />
                  Deleting...
                </>
              ) : (
                <>
                  <Trash2 className="size-4" />
                  Delete
                </>
              )}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
