/**
 * Constructs a deployment-safe media URL for uploaded images or absolute external URLs.
 * - Absolute URLs (e.g. starting with http://, https://, blob:) are returned as-is.
 * - Relative upload paths (e.g. /uploads/...) prepend VITE_BACKEND_URL origin if configured,
 *   or return relative paths for Vite proxy / server handling.
 */
export const getMediaUrl = (
  path?: string | null,
  fallback = 'https://placehold.co/400x600?text=No+Poster'
): string => {
  if (!path || path === 'undefined') {
    return fallback;
  }

  if (path.startsWith('http://') || path.startsWith('https://') || path.startsWith('blob:')) {
    return path;
  }

  const backendUrl = import.meta.env.VITE_BACKEND_URL
    ? import.meta.env.VITE_BACKEND_URL.replace(/\/$/, '')
    : '';
  const cleanPath = path.startsWith('/') ? path : `/${path}`;

  return backendUrl ? `${backendUrl}${cleanPath}` : cleanPath;
};
