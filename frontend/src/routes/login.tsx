import { createFileRoute, redirect } from "@tanstack/react-router";
import { LoginForm } from "@/components/LoginForm";

type LoginSearch = {
  redirect?: string;
};

export const Route = createFileRoute("/login")({
  validateSearch: (search: Record<string, unknown>): LoginSearch => {
    return {
      redirect: (search.redirect as string) || undefined,
    };
  },
  beforeLoad: ({ context, search }) => {
    if (context.auth.isAuthenticated) {
      throw redirect({
        to: search.redirect || "/",
      });
    }
  },
  component: Login,
});

function Login() {
  const search = Route.useSearch();

  return (
    <div className="flex min-h-screen items-center justify-center p-4">
      <div className="w-full max-w-md">
        <LoginForm redirectUrl={search.redirect} />
      </div>
    </div>
  );
}
