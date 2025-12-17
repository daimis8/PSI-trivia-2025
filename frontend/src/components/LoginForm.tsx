import { useState, useEffect } from "react";
import { useNavigate } from "@tanstack/react-router";
import { useMutation } from "@tanstack/react-query";
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
  FieldDescription,
  FieldGroup,
  FieldLabel,
} from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { AlertBox } from "@/components/AlertBox";
import { validateLoginPassword } from "@/lib/validation";
import { getApiUrl } from "@/lib/api";

interface LoginFormProps {
  redirectUrl?: string;
}

export function LoginForm({ redirectUrl }: LoginFormProps) {
  const navigate = useNavigate();
  const { isAuthenticated, isLoading, login } = useAuth();
  const [identifier, setIdentifier] = useState("");
  const [password, setPassword] = useState("");
  const [identifierError, setIdentifierError] = useState("");
  const [passwordError, setPasswordError] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    if (isAuthenticated && !isLoading) {
      navigate({ to: redirectUrl || "/" });
    }
  }, [isAuthenticated, isLoading, navigate, redirectUrl]);

  const loginMutation = useMutation({
    mutationFn: async (credentials: {
      identifier: string;
      password: string;
    }) => {
      const response = await fetch(getApiUrl("/api/login"), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(credentials),
        credentials: "include",
      });

      if (!response.ok) {
        const data = await response.json();
        throw new Error(data.message || "Login failed");
      }

      return response.json();
    },
    onSuccess: (data) => {
      login(data.user);
      navigate({ to: redirectUrl || "/" });
    },
    onError: (error: Error) => {
      setError(error.message);
    },
  });

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError("");

    let identifierValidation = "";
    if (!identifier.trim()) {
      identifierValidation = "Email or username is required";
    }

    const passwordValidation = validateLoginPassword(password);

    setIdentifierError(identifierValidation);
    setPasswordError(passwordValidation);

    if (identifierValidation || passwordValidation) {
      return;
    }

    loginMutation.mutate({ identifier, password });
  };

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        Loading...
      </div>
    );
  }

  if (isAuthenticated) {
    return null;
  }

  const hasErrors = identifierError || passwordError || error;

  return (
    <Card className="bg-card">
      <CardHeader>
        <CardTitle>Sign in to your account</CardTitle>
        <CardDescription>
          Enter your email or username and password to sign in
        </CardDescription>
      </CardHeader>
      <CardContent>
        {hasErrors && (
          <AlertBox
            isLogin={true}
            emailError={identifierError}
            passwordError={passwordError}
            serverError={error}
          />
        )}
        <form onSubmit={handleSubmit} noValidate>
          <FieldGroup>
            <Field>
              <FieldLabel htmlFor="identifier">Email or Username</FieldLabel>
              <Input
                id="identifier"
                type="text"
                placeholder="Enter your email or username"
                value={identifier}
                onChange={(e) => setIdentifier(e.target.value)}
                className={`${identifierError ? "border-red-500" : ""}`}
              />
            </Field>
            <Field>
              <FieldLabel htmlFor="password">Password</FieldLabel>
              <Input
                id="password"
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className={`${passwordError ? "border-red-500" : ""}`}
              />
            </Field>
            <FieldGroup>
              <Field>
                <Button
                  type="submit"
                  disabled={loginMutation.isPending}
                  className="border border-white cursor-pointer"
                >
                  {loginMutation.isPending ? "Signing in..." : "Sign in"}
                </Button>
                <FieldDescription className="px-6 text-center">
                  Don&apos;t have an account?{" "}
                  <a href="/register">Create account</a>
                </FieldDescription>
              </Field>
            </FieldGroup>
          </FieldGroup>
        </form>
      </CardContent>
    </Card>
  );
}
