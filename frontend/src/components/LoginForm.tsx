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
import {
  validateEmail,
  validateLoginPassword,
  validateUsername,
} from "@/lib/validation";

interface LoginFormProps {
  redirectUrl?: string;
}

export function LoginForm({ redirectUrl }: LoginFormProps) {
  const navigate = useNavigate();
  const { isAuthenticated, isLoading, login } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [username, setUsername] = useState("");
  const [usernameError, setUsernameError] = useState("");
  const [emailError, setEmailError] = useState("");
  const [passwordError, setPasswordError] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    if (isAuthenticated && !isLoading) {
      navigate({ to: redirectUrl || "/" });
    }
  }, [isAuthenticated, isLoading, navigate, redirectUrl]);

  const loginMutation = useMutation({
    mutationFn: async (credentials: {
      email: string;
      password: string;
      username: string;
    }) => {
      const response = await fetch("/api/login", {
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

    const emailValidation = validateEmail(email);
    const passwordValidation = validateLoginPassword(password);
    const usernameValidation = validateUsername(username);

    setEmailError(emailValidation);
    setPasswordError(passwordValidation);
    setUsernameError(usernameValidation);

    if (emailValidation || passwordValidation || usernameValidation) {
      return;
    }

    loginMutation.mutate({ email, password, username });
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

  const hasErrors = emailError || passwordError || usernameError || error;

  return (
    <Card className="bg-card-dark">
      <CardHeader>
        <CardTitle className="text-white">Sign in to your account</CardTitle>
        <CardDescription>
          Enter your email and password to sign in
        </CardDescription>
      </CardHeader>
      <CardContent>
        {hasErrors && (
          <AlertBox
            isLogin={true}
            emailError={emailError}
            passwordError={passwordError}
            usernameError={usernameError}
            serverError={error}
          />
        )}
        <form onSubmit={handleSubmit} noValidate>
          <FieldGroup>
            <Field>
              <FieldLabel htmlFor="username" className="text-white">
                Username
              </FieldLabel>
              <Input
                id="username"
                type="text"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                className={usernameError ? "border-red-500" : ""}
              />
            </Field>
            <Field>
              <FieldLabel htmlFor="email" className="text-white">
                Email
              </FieldLabel>
              <Input
                id="email"
                type="email"
                placeholder="m@example.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className={emailError ? "border-red-500" : ""}
              />
            </Field>
            <Field>
              <FieldLabel htmlFor="password" className="text-white">
                Password
              </FieldLabel>
              <Input
                id="password"
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className={passwordError ? "border-red-500" : ""}
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
