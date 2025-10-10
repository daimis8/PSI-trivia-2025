import { useAuth } from "@/context/AuthContext";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useEffect } from "react";

export const Route = createFileRoute("/")({
  component: Index,
});

function Index() {
  const { isAuthenticated, isLoading, user } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      navigate({ to: "/login" });
    }
  }, [isAuthenticated, isLoading]);

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        Loading...
      </div>
    );
  }
  if (!isAuthenticated) {
    return null;
  }
  return (
    <div className="flex flex-col justify-center items-center">
      <h3>Welcome!</h3>
      <p>Hey, {user?.email}</p>
    </div>
  );
}
