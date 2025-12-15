import { createFileRoute } from "@tanstack/react-router";
import { ProfilePage } from "@/components/ProfilePage";

export const Route = createFileRoute("/_app/profile/$profileId")({
  component: ProfileById,
});

function ProfileById() {
  const { profileId } = Route.useParams();
  const parsedId = Number(profileId);

  if (Number.isNaN(parsedId)) {
    return (
      <div className="flex flex-1 items-center justify-center text-muted-foreground">
        Invalid profile identifier.
      </div>
    );
  }

  return <ProfilePage profileId={parsedId} />;
}
