import { useState, useEffect } from "react";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useAuth } from "@/context/AuthContext";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Field,
  FieldGroup,
  FieldLabel,
} from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { AlertBox } from "@/components/AlertBox";
import { getApiUrl } from "@/lib/api";

export const Route = createFileRoute("/play")({
  component: RouteComponent,
});

function RouteComponent() {
  const { user, isAuthenticated, isLoading } = useAuth();
  const navigate = useNavigate();
  const [code, setCode] = useState("");
  const [username, setUsername] = useState("");
  const [codeError, setCodeError] = useState("");
  const [usernameError, setUsernameError] = useState("");
  const [error, setError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Auto-fill username if authenticated
  useEffect(() => {
    if (isAuthenticated && user) {
      setUsername(user.username);
    }
  }, [isAuthenticated, user]);

  const validateCode = (code: string): string => {
    const trimmed = code.trim();
    if (!trimmed) return "Game code is required";
    if (!/^[A-Z]{6}$/.test(trimmed)) return "Game code must be 6 uppercase letters";
    return "";
  };

  const validateUsernameField = (username: string): string => {
    if (!username.trim()) {
      return "Username is required";
    }
    if (username.length < 3) {
      return "Username must be at least 3 characters";
    }
    if (username.length > 20) {
      return "Username must be at most 20 characters";
    }
    return "";
  };

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError("");

    const codeValidation = validateCode(code);
    const usernameValidation = validateUsernameField(username);

    setCodeError(codeValidation);
    setUsernameError(usernameValidation);

    if (codeValidation || usernameValidation) {
      return;
    }

    setIsSubmitting(true);

    try {
      const res = await fetch(getApiUrl(`/api/games/${code}/exists`), { credentials: "include" });
      if (!res.ok) {
        setCodeError("Invalid game code");
        return;
      }
      navigate({ to: `/game/${code}?name=${encodeURIComponent(username)}` });
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to join game");
    } finally {
      setIsSubmitting(false);
    }
  };

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        Loading...
      </div>
    );
  }

  const hasErrors = codeError || usernameError || error;

  return (
    <div className="flex min-h-screen items-center justify-center p-4">
      <div className="w-full max-w-md">
        <Card className="bg-card">
          <CardHeader>
            <CardTitle>Join a Game</CardTitle>
            <CardDescription>
              Enter the game code to join a trivia quiz
            </CardDescription>
          </CardHeader>
          <CardContent>
            {hasErrors && (
              <AlertBox
                isLogin={false}
                emailError=""
                passwordError=""
                usernameError={usernameError}
                serverError={error || codeError}
              />
            )}
            <form onSubmit={handleSubmit} noValidate>
              <FieldGroup>
                <Field>
                  <FieldLabel htmlFor="code">Game Code</FieldLabel>
                  <Input
                    id="code"
                    type="text"
                    placeholder="Enter game code"
                    value={code}
                    onChange={(e) => setCode(e.target.value.toUpperCase())}
                    className={`${codeError ? "border-red-500" : ""} text-center text-2xl font-bold tracking-widest`}
                    autoComplete="off"
                  />
                </Field>
                <Field>
                  <FieldLabel htmlFor="username">Username</FieldLabel>
                  <Input
                    id="username"
                    type="text"
                    placeholder="Enter your username"
                    value={username}
                    onChange={(e) => setUsername(e.target.value)}
                    disabled={isAuthenticated}
                    className={`${usernameError ? "border-red-500" : ""} ${isAuthenticated ? "opacity-70 cursor-not-allowed" : ""
                      }`}
                  />
                  {isAuthenticated && (
                    <p className="mt-1 text-sm text-muted-foreground">
                      Using your account username
                    </p>
                  )}
                </Field>
                <FieldGroup>
                  <Field>
                    <Button
                      type="submit"
                      disabled={isSubmitting}
                      className="border border-white cursor-pointer w-full"
                    >
                      {isSubmitting ? "Joining..." : "Join Game"}
                    </Button>
                  </Field>
                </FieldGroup>
              </FieldGroup>
            </form>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
