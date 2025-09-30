import { Button } from '@/components/ui/button'
import { createFileRoute } from '@tanstack/react-router'
import { useQuery } from '@tanstack/react-query'

export const Route = createFileRoute('/')({
    component: Index,
})

function Index() {
    const { data, error, isFetching, refetch } = useQuery({
        queryKey: ['backend-hello'],
        enabled: false,
        retry: false,
        queryFn: async () => {
            const response = await fetch('/api')
            if (!response.ok) {
                throw new Error(`Request failed with status ${response.status}`)
            }
            return response.text()
        },
    })

    return (
        <div className="p-2">
            <h3>Welcome Home!</h3>
            <div className="flex items-center gap-2">
                <Button onClick={() => void refetch()}>Click me</Button>
                {isFetching ? (
                    <span className="text-sm text-gray-500">Loading...</span>
                ) : null}
            </div>
            {error ? (
                <div className="mt-2 text-red-600">
                    {error instanceof Error ? error.message : String(error)}
                </div>
            ) : null}
            {data ? <div className="mt-2">Response: {data}</div> : null}
        </div>
    )
}