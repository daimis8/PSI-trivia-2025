export const API_BASE_URL = import.meta.env.VITE_API_URL || ''

export function getApiUrl(path: string): string {
    return `${API_BASE_URL}${path}`;
}

export function apiFetch(input: string | URL | Request, init?: RequestInit): Promise<Response> {
    const url = typeof input === 'string' ? getApiUrl(input) : input;
    return fetch(url, {
        credentials: 'include',
        ...init,
    });
}
