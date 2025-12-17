export const API_BASE_URL = import.meta.env.VITE_API_URL || ''

export function getApiUrl(path: string): string {
    return `${API_BASE_URL}${path}`;
}
