import type { ReactNode } from "react";

interface AuthFormProps {
  title: string;
  error?: string;
  onSubmit: (e: React.FormEvent<HTMLFormElement>) => void;
  children: ReactNode;
}

export function AuthForm({ title, error, onSubmit, children }: AuthFormProps) {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-3">
      <h3 className="text-2xl font-bold">{title}</h3>
      {error && <p className="text-center text-red-500">{error}</p>}
      <form className="flex w-full max-w-md flex-col gap-4" onSubmit={onSubmit}>
        {children}
      </form>
    </div>
  );
}
