import { createFileRoute } from "@tanstack/react-router";
import { UserStatsPage } from "@/components/UserStatsPage";

export const Route = createFileRoute("/_app/stats")({
  component: UserStatsPage,
});