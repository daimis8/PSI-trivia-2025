import {
  createRootRouteWithContext,
  Link,
  Outlet,
  useNavigate,
} from "@tanstack/react-router";
import { TanStackRouterDevtools } from "@tanstack/react-router-devtools";
import { useAuth } from "@/context/AuthContext";
import type { QueryClient } from "@tanstack/react-query";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";

interface RouterContext {
  queryClient: QueryClient;
  auth: {
    isAuthenticated: boolean;
    user: { id: number; email: string; username: string } | null;
  };
}

const RootLayout = () => {
  const { isAuthenticated, user, logout, isLoading } = useAuth();
  const navigate = useNavigate();

  const handleLogout = async () => {
    await logout();
    navigate({ to: "/login" });
  };

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        Loading...
      </div>
    );
  }

  return (
    <>
      <div className="p-4 flex gap-4 items-center border-b">
        {isAuthenticated && (
          <Link to="/" className="[&.active]:font-bold">
            Home
          </Link>
        )}
        {!isAuthenticated ? (
          <>
            <Link to="/login" className="[&.active]:font-bold">
              Login
            </Link>
            <Link to="/register" className="[&.active]:font-bold">
              Register
            </Link>
          </>
        ) : (
          <div className="ml-auto flex gap-4 items-center">
            <Link
              to="/profile"
              className="flex items-center gap-2 hover:opacity-80 transition-opacity"
            >
              <Avatar>
                <AvatarImage
                  src="https://github.com/shadcn.png"
                  alt="profile-photo"
                />
                <AvatarFallback>{user?.username}</AvatarFallback>
              </Avatar>
              <span className="text-sm text-gray-600">{user?.username}</span>
            </Link>
            <button
              onClick={handleLogout}
              className="text-sm text-red-500 hover:underline"
            >
              Logout
            </button>
          </div>
        )}
      </div>
      <Outlet />
      <TanStackRouterDevtools />
    </>
  );
};

export const Route = createRootRouteWithContext<RouterContext>()({
  component: RootLayout,
});
