import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import {
  Loader2,
  Save,
  ArrowLeft,
  Plus,
  Trash2,
  Check,
  AlertCircle,
  CheckCircle,
  Clock,
} from "lucide-react";
import ErrorComponent from "@/components/Error";
import { useState, useEffect } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";

export const Route = createFileRoute("/_app/my-quizzes/$quizId")({
  component: RouteComponent,
});

interface Question {
  id: number;
  questionText: string;
  options: string[];
  correctOptionIndex: number;
  timeLimit: number;
}

interface Quiz {
  id: number;
  title: string;
  description: string;
  questions: Question[];
  creatorID: number;
}

function RouteComponent() {
  const { quizId } = Route.useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [questions, setQuestions] = useState<Question[]>([]);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [saveSuccess, setSaveSuccess] = useState(false);

  const { isPending, isError, data } = useQuery<Quiz>({
    queryKey: ["quiz", quizId],
    queryFn: async () => {
      const response = await fetch(`/api/quizzes/${quizId}`);
      if (!response.ok) {
        throw new Error("Failed to fetch quiz");
      }
      return response.json();
    },
  });

  // Initialize form state when data is loaded
  useEffect(() => {
    if (data) {
      setTitle(data.title);
      setDescription(data.description);
      setQuestions(data.questions || []);
    }
  }, [data]);

  const { mutate: updateQuiz, isPending: isSaving } = useMutation({
    mutationFn: async () => {
      const response = await fetch(`/api/quizzes/${quizId}`, {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          Title: title,
          Description: description,
          Questions: questions.map((q) => ({
            Id: q.id,
            QuestionText: q.questionText,
            Options: q.options,
            CorrectOptionIndex: q.correctOptionIndex,
            TimeLimit: q.timeLimit,
          })),
        }),
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new Error(
          errorData.message || `Failed to update quiz: ${response.statusText}`
        );
      }

      return response.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["quiz", quizId] });
      queryClient.invalidateQueries({ queryKey: ["my-quizzes"] });
      setSaveError(null);
      setSaveSuccess(true);
      setTimeout(() => setSaveSuccess(false), 3000);
    },
    onError: (error: Error) => {
      setSaveError(error.message || "Failed to save quiz. Please try again.");
      setSaveSuccess(false);
    },
  });

  const addQuestion = () => {
    setSaveError(null);
    const newQuestion: Question = {
      id:
        questions.length > 0 ? Math.max(...questions.map((q) => q.id)) + 1 : 1,
      questionText: "",
      options: ["", ""],
      correctOptionIndex: 0,
      timeLimit: 30, // default
    };
    setQuestions([...questions, newQuestion]);
  };

  const removeQuestion = (questionId: number) => {
    setSaveError(null);
    setQuestions(questions.filter((q) => q.id !== questionId));
  };

  const updateQuestion = (
    questionId: number,
    field: keyof Question,
    value: any
  ) => {
    setSaveError(null);
    setQuestions(
      questions.map((q) => (q.id === questionId ? { ...q, [field]: value } : q))
    );
  };

  const addOption = (questionId: number) => {
    setQuestions(
      questions.map((q) =>
        q.id === questionId ? { ...q, options: [...q.options, ""] } : q
      )
    );
  };

  const removeOption = (questionId: number, optionIndex: number) => {
    setQuestions(
      questions.map((q) => {
        if (q.id === questionId) {
          const newOptions = q.options.filter((_, i) => i !== optionIndex);
          let newCorrectIndex = q.correctOptionIndex;
          if (optionIndex === q.correctOptionIndex) {
            newCorrectIndex = 0;
          } else if (optionIndex < q.correctOptionIndex) {
            newCorrectIndex = q.correctOptionIndex - 1;
          }
          return {
            ...q,
            options: newOptions,
            correctOptionIndex: newCorrectIndex,
          };
        }
        return q;
      })
    );
  };

  const updateOption = (
    questionId: number,
    optionIndex: number,
    value: string
  ) => {
    setQuestions(
      questions.map((q) =>
        q.id === questionId
          ? {
              ...q,
              options: q.options.map((opt, i) =>
                i === optionIndex ? value : opt
              ),
            }
          : q
      )
    );
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
    <div>
      <div className="flex items-center justify-between mb-8">
        <div className="flex items-center gap-4">
          <Button
            variant="ghost"
            size="icon"
            onClick={() => navigate({ to: "/my-quizzes" })}
          >
            <ArrowLeft className="size-5" />
          </Button>
          <h1 className="text-4xl font-bold tracking-tight">Edit Quiz</h1>
        </div>
        <Button
          onClick={() => updateQuiz()}
          disabled={isSaving}
          className="gap-2"
        >
          {isSaving ? (
            <>
              <Loader2 className="size-4 animate-spin" />
              Saving...
            </>
          ) : (
            <>
              <Save className="size-4" />
              Save Quiz
            </>
          )}
        </Button>
      </div>

      {saveError && (
        <Alert variant="destructive" className="mb-6">
          <AlertCircle className="h-4 w-4" />
          <AlertTitle>Error</AlertTitle>
          <AlertDescription>{saveError}</AlertDescription>
        </Alert>
      )}

      {saveSuccess && (
        <Alert className="mb-6 border-green-500 bg-green-50 text-green-900">
          <CheckCircle className="h-4 w-4 text-green-600" />
          <AlertTitle>Success</AlertTitle>
          <AlertDescription>Quiz saved successfully!</AlertDescription>
        </Alert>
      )}

      <div className="space-y-6">
        {/* Title and Description */}
        <Card>
          <CardHeader>
            <CardTitle>Quiz Details</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="title">Title</Label>
              <Input
                id="title"
                value={title}
                onChange={(e) => {
                  setTitle(e.target.value);
                  setSaveError(null);
                }}
                placeholder="Enter quiz title"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="description">Description</Label>
              <Textarea
                id="description"
                value={description}
                onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => {
                  setDescription(e.target.value);
                  setSaveError(null);
                }}
                placeholder="Enter quiz description"
                rows={3}
              />
            </div>
          </CardContent>
        </Card>

        {/* Questions */}
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <h2 className="text-2xl font-semibold">Questions</h2>
            <Button onClick={addQuestion} variant="outline" className="gap-2">
              <Plus className="size-4" />
              Add Question
            </Button>
          </div>

          {questions.length === 0 ? (
            <Card>
              <CardContent className="py-12 text-center text-muted-foreground">
                <p className="mb-4">
                  No questions yet. Add your first question to get started.
                </p>
                <Button
                  onClick={addQuestion}
                  variant="outline"
                  className="gap-2"
                >
                  <Plus className="size-4" />
                  Add Question
                </Button>
              </CardContent>
            </Card>
          ) : (
            questions.map((question, questionIndex) => (
              <Card key={question.id}>
                <CardHeader>
                  <div className="flex items-center justify-between">
                    <CardTitle className="text-lg">
                      Question {questionIndex + 1}
                    </CardTitle>
                    <Button
                      variant="ghost"
                      size="icon"
                      onClick={() => removeQuestion(question.id)}
                    >
                      <Trash2 className="size-4 text-destructive" />
                    </Button>
                  </div>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="space-y-2">
                    <Label>Question Text</Label>
                    <Input
                      value={question.questionText}
                      onChange={(e) =>
                        updateQuestion(
                          question.id,
                          "questionText",
                          e.target.value
                        )
                      }
                      placeholder="Enter your question"
                    />
                  </div>

                  <div className="space-y-3">
                    <div className="flex items-center justify-between">
                      <Label>Answer Options</Label>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => addOption(question.id)}
                        className="gap-2"
                      >
                        <Plus className="size-3" />
                        Add Option
                      </Button>
                    </div>

                    <div className="space-y-2">
                      {question.options.map((option, optionIndex) => (
                        <div
                          key={optionIndex}
                          className="flex items-center gap-2 p-2 border rounded-lg hover:bg-accent/50 transition-colors"
                        >
                          <input
                            type="radio"
                            name={`question-${question.id}-correct`}
                            checked={
                              question.correctOptionIndex === optionIndex
                            }
                            onChange={() =>
                              updateQuestion(
                                question.id,
                                "correctOptionIndex",
                                optionIndex
                              )
                            }
                            className="shrink-0 w-4 h-4 cursor-pointer"
                          />
                          <div className="flex items-center gap-2 flex-1">
                            <Input
                              value={option}
                              onChange={(e) =>
                                updateOption(
                                  question.id,
                                  optionIndex,
                                  e.target.value
                                )
                              }
                              placeholder={`Option ${optionIndex + 1}`}
                              className="flex-1"
                            />
                            {question.correctOptionIndex === optionIndex && (
                              <Check className="size-4 text-green-600 shrink-0" />
                            )}
                          </div>
                          {question.options.length > 2 && (
                            <Button
                              variant="ghost"
                              size="icon"
                              onClick={() =>
                                removeOption(question.id, optionIndex)
                              }
                              className="shrink-0"
                            >
                              <Trash2 className="size-4 text-destructive" />
                            </Button>
                          )}
                        </div>
                      ))}
                    </div>
                    <p className="text-xs text-muted-foreground">
                      Select the radio button next to the correct answer
                    </p>
                  </div>

                  <div className="space-y-3 pt-4 border-t">
                    <div className="flex items-center gap-2">
                      <Clock className="size-4 text-muted-foreground" />
                      <Label htmlFor={`time-limit-${question.id}`}>
                        Time Limit
                      </Label>
                    </div>

                    <div className="space-y-2">
                      <div className="flex items-center gap-2">
                        <Input
                          id={`time-limit-${question.id}`}
                          type="number"
                          min={5}
                          max={300}
                          value={question.timeLimit}
                          onChange={(e) => {
                            const value = parseInt(e.target.value) || 5;
                            updateQuestion(question.id, "timeLimit", value);
                          }}
                          className="w-24"
                        />
                        <span className="text-sm text-muted-foreground">
                          seconds
                        </span>
                      </div>
                      <p className="text-xs text-amber-600">
                        Min: 5 seconds, Max: 5 minutes (300 seconds)
                      </p>

                      {/* Quick preset buttons */}
                      <div className="flex gap-2 items-center">
                        <p className="text-xs text-muted-foreground mr-1">
                          Quick presets:
                        </p>
                        {[15, 30, 60, 120, 180].map((seconds) => (
                          <Button
                            key={seconds}
                            variant="outline"
                            size="sm"
                            className="h-7 text-xs"
                            onClick={() =>
                              updateQuestion(question.id, "timeLimit", seconds)
                            }
                          >
                            {seconds}s
                          </Button>
                        ))}
                      </div>
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))
          )}
        </div>
      </div>
    </div>
  );
}
