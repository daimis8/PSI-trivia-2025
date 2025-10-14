import { useAuth } from "@/context/AuthContext";
import { createFileRoute } from "@tanstack/react-router";

export const Route = createFileRoute("/_app/")({
  component: Index,
});

function Index() {
  const { user } = useAuth();

  return (
    <div className="flex flex-col justify-center items-center">
      <h3>Welcome!</h3>
      <p>Hey, {user?.email}</p>
    </div>
  );
}
