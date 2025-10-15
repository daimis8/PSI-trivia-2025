import {
	Empty,
	EmptyContent,
	EmptyDescription,
	EmptyHeader,
	EmptyMedia,
	EmptyTitle,
} from "@/components/ui/empty";
import { Button } from "@/components/ui/button";
import { TriangleAlert } from "lucide-react";

export default function Error() {
	const refresh = () => {
		window.location.reload();
	}
	
	return (
		<Empty>
			<EmptyHeader>
				<EmptyMedia variant="icon" className="text-red-500">
					<TriangleAlert />
				</EmptyMedia>
				<EmptyTitle>Something went wrong</EmptyTitle>
				<EmptyDescription>Please try again later</EmptyDescription>
			</EmptyHeader>
			<EmptyContent>
				<Button onClick={refresh}>Refresh</Button>
			</EmptyContent>
		</Empty>
	);
}
