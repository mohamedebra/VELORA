const API_BASE = (import.meta.env.VITE_API_BASE_URL || '/api').replace(/\/+$/, '');
export class ApiError extends Error {
  constructor(message, { status, code, details } = {}) { super(message); this.name = 'ApiError'; this.status = status; this.code = code; this.details = details; }
}
export async function request(path, options = {}) {
  const headers = new Headers(options.headers || {});
  headers.set('Accept', 'application/json');
  if (options.body && !(options.body instanceof FormData) && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json');
  let response;
  try { response = await fetch(`${API_BASE}${path.startsWith('/') ? path : `/${path}`}`, { credentials: 'include', ...options, headers }); }
  catch (cause) { throw new ApiError('تعذر الوصول إلى خادم البيانات', { code: 'NETWORK_ERROR', details: cause }); }
  if (response.status === 204) return null;
  const type = response.headers.get('content-type') || '';
  const body = type.includes('application/json') ? await response.json().catch(() => null) : await response.text().catch(() => '');
  if (!response.ok) {
    const message = typeof body === 'object' && body ? body.message || body.error?.message || `API request failed (${response.status})` : body || `API request failed (${response.status})`;
    throw new ApiError(message, { status: response.status, code: typeof body === 'object' && body ? body.code || body.error?.code : undefined, details: typeof body === 'object' && body ? body.details : undefined });
  }
  return body;
}
