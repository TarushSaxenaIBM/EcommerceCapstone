const API_BASE = (import.meta.env.VITE_API_BASE_URL || '').replace(/\/$/, '');
const TOKEN_KEY = 'marketlane.token';

export class ApiError extends Error {
  constructor(message, status, errors) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.errors = errors;
  }
}

export function getToken() {
  return localStorage.getItem(TOKEN_KEY);
}

export function setToken(token) {
  if (token) {
    localStorage.setItem(TOKEN_KEY, token);
  } else {
    localStorage.removeItem(TOKEN_KEY);
  }
}

function firstValidationMessage(errors) {
  if (!errors) return null;
  const messages = Object.values(errors).flat().filter(Boolean);
  return messages.length > 0 ? messages[0] : null;
}

async function request(path, { method = 'GET', body, auth = true, signal } = {}) {
  const headers = { Accept: 'application/json' };

  if (body !== undefined) {
    headers['Content-Type'] = 'application/json';
  }

  const token = auth ? getToken() : null;
  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  let response;
  try {
    response = await fetch(`${API_BASE}${path}`, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
      signal,
    });
  } catch (error) {
    if (error.name === 'AbortError') throw error;
    throw new ApiError('Could not reach the server. Check your connection and try again.', 0);
  }

  if (response.status === 204) {
    return null;
  }

  const contentType = response.headers.get('content-type') || '';
  const data = contentType.includes('json') ? await response.json() : await response.text();

  if (!response.ok) {
    const message =
      firstValidationMessage(data?.errors) ||
      (typeof data === 'object' ? data?.detail || data?.title : null) ||
      `Request failed with status ${response.status}.`;

    throw new ApiError(message, response.status, data?.errors);
  }

  return data;
}

function queryString(params) {
  const search = new URLSearchParams();

  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') {
      search.set(key, String(value));
    }
  });

  const value = search.toString();
  return value ? `?${value}` : '';
}

export const api = {
  register: (payload) =>
    request('/api/auth/register', { method: 'POST', body: payload, auth: false }),
  login: (payload) => request('/api/auth/login', { method: 'POST', body: payload, auth: false }),
  me: () => request('/api/auth/me'),

  getProducts: (params, signal) => request(`/api/products${queryString(params)}`, { signal }),
  getProduct: (id, signal) => request(`/api/products/${id}`, { signal }),
  getCategories: (signal) => request('/api/categories', { signal }),
  searchProducts: (payload, signal) =>
    request('/api/products/search', {
      method: 'POST',
      body: payload,
      auth: false,
      signal,
    }),
  getSuggestions: (query, signal) =>
    request(`/api/products/suggestions${queryString({ query, take: 5 })}`, {
      auth: false,
      signal,
    }),

  getHomeRecommendations: (signal) =>
    request('/api/recommendations/home', { signal }),
  getRelatedRecommendations: (productId, signal) =>
    request(`/api/recommendations/related/${productId}`, { signal }),
  sendChat: (message, history) =>
    request('/api/chat', { method: 'POST', body: { message, history } }),

  getCart: (signal) => request('/api/cart', { signal }),
  addCartItem: (productId, quantity) =>
    request('/api/cart/items', { method: 'POST', body: { productId, quantity } }),
  updateCartItem: (itemId, quantity) =>
    request(`/api/cart/items/${itemId}`, { method: 'PUT', body: { quantity } }),
  removeCartItem: (itemId) => request(`/api/cart/items/${itemId}`, { method: 'DELETE' }),
  clearCart: () => request('/api/cart', { method: 'DELETE' }),

  checkout: (shippingAddress) =>
    request('/api/checkout', { method: 'POST', body: { shippingAddress } }),
  getOrders: (signal) => request('/api/orders', { signal }),
  getOrder: (id, signal) => request(`/api/orders/${id}`, { signal }),
  getPayment: (orderId, signal) => request(`/api/payments/${orderId}`, { signal }),
};
