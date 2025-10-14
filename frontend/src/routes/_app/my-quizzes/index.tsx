import { Button } from "@/components/ui/button";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { Loader2, PlusCircle } from "lucide-react";
import ErrorComponent from "@/components/Error";
import { Empty, EmptyContent, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from "@/components/ui/empty";
import { CircleQuestionMark } from "lucide-react";

export const Route = createFileRoute("/_app/my-quizzes/")({
	component: RouteComponent,
});

function RouteComponent() {
  const queryClient = useQueryClient();

	const { isPending, isError, data } = useQuery({
		queryKey: ["my-quizzes"],
		queryFn: async () => {
			const response = await fetch("/api/quizzes/my");
			if (!response.ok) {
				throw new Error("Failed to fetch quizzes");
			}
			return response.json();
		},
	});

  const { mutate: createQuiz, isPending: isCreating } = useMutation({
    mutationFn: async () => {
      const response = await fetch("/api/quizzes/my", {
        method: "POST",
      });

      if (!response.ok) {
        throw new Error("Failed to create quiz");
      }

      return response.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["my-quizzes"] });
    },
  });

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
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {data.map((quiz: any) => (
            <div key={quiz.id} className="border p-4 rounded-lg shadow-sm hover:shadow-md transition-shadow">
              <h2 className="text-xl font-semibold mb-2">{quiz.title}</h2>
              <p className="text-gray-600 mb-4">{quiz.description}</p>
              <Button variant="default" size="sm">Edit Quiz</Button>
            </div>
          ))}
        </div>
      )}
		</>
	);
}
