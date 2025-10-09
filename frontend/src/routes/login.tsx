import { ButtonRegisterLogin } from "@/components/ui/ButtonRegisterLogin";
import { useState } from "react";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { AuthForm } from "@/components/ui/AuthForm";
import { FormInput } from "@/components/ui/FormInput";
import { useMutation } from "@tanstack/react-query";

export const Route = createFileRoute("/login")({
  component: Login,
});

function Login() {
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");

  const loginMutation = useMutation({
    mutationFn: async (credentials: { email: string; password: string }) => {
      const response = await fetch("http://localhost:5203/api/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(credentials),
      });

      if (!response.ok) {
        const data = await response.json();
        throw new Error(data.message || "Login failed");
      }

      return response.json();
    },
    onSuccess: () => {
      navigate({ to: "/" });
    },
    onError: (error: Error) => {
      setError(error.message);
    },
  });

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError("");
    loginMutation.mutate({ email, password });
  };

  return (
    <AuthForm title="Login" error={error} onSubmit={handleSubmit}>
      <FormInput
        label="Email"
        type="email"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
      />
      <FormInput
        label="Password"
        type="password"
        value={password}
        onChange={(e) => setPassword(e.target.value)}
      />
      <ButtonRegisterLogin
        type="submit"
        className="border-2 border-black bg-amber-500 rounded-sm"
        disabled={loginMutation.isPending}
      >
        {loginMutation.isPending ? "Logging in..." : "Login"}
      </ButtonRegisterLogin>
    </AuthForm>
  );
}
