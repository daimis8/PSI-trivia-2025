import { createFileRoute, redirect, Outlet } from "@tanstack/react-router";

export const Route = createFileRoute("/_app")({
    beforeLoad: ({ context, location }) => {
        if (!context.auth.isAuthenticated) {
            throw redirect({
                to: "/login",
                search: {
                    redirect: location.href,
                },
            });
        }
    },
    component: AuthLayout,
});

function AuthLayout() {
    return <Outlet />;
}
