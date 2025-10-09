import { ButtonRegisterLogin } from "@/components/ui/ButtonRegisterLogin";
import { useState } from "react";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { AuthForm } from "@/components/ui/AuthForm";
import { FormInput } from "@/components/ui/FormInput";
import { useMutation } from "@tanstack/react-query";

export const Route = createFileRoute("/register")({
  component: Register,
});

function Register() {
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState("");

  const registerMutation = useMutation({
    mutationFn: async (userData: { email: string; password: string }) => {
      const response = await fetch("http://localhost:5203/api/users", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(userData),
      });

      if (!response.ok) {
        const data = await response.json();
        throw new Error(data.message || "Registration failed");
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

    if (password !== confirmPassword) {
      setError("Passwords do not match");
      return;
    }

    registerMutation.mutate({ email, password });
  };

  return (
    <AuthForm title="Register" error={error} onSubmit={handleSubmit}>
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
      <FormInput
        label="Confirm Password"
        type="password"
        value={confirmPassword}
        onChange={(e) => setConfirmPassword(e.target.value)}
      />
      <ButtonRegisterLogin
        type="submit"
        className="border-2 border-black bg-amber-500 rounded-sm"
        disabled={registerMutation.isPending}
      >
        {registerMutation.isPending ? "Registering..." : "Register"}
      </ButtonRegisterLogin>
    </AuthForm>
  );
}
