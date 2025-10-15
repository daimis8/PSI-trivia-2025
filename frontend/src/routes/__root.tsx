import {
  createRootRouteWithContext,
  Outlet,
} from "@tanstack/react-router";
import { TanStackRouterDevtools } from "@tanstack/react-router-devtools";
import { useAuth } from "@/context/AuthContext";
import type { QueryClient } from "@tanstack/react-query";
import { Navbar } from "@/components/Navbar";

interface RouterContext {
  queryClient: QueryClient;
  auth: {
    isAuthenticated: boolean;
    user: { id: number; email: string; username: string } | null;
  };
}

const RootLayout = () => {
  const { isLoading } = useAuth();

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        Loading...
      </div>
    );
  }

  return (
    <>
      <Navbar />
      <Outlet />
      <TanStackRouterDevtools />
    </>
  );
};

export const Route = createRootRouteWithContext<RouterContext>()({
  component: RootLayout,
});
