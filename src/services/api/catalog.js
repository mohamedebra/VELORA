import { request } from './http.js';

export const fetchStoreProducts = () => request('/products');
export const fetchStoreSettings = () => request('/store-settings');
export const saveAdminStoreSettings = (settings) => request('/admin/store-settings', {
  method: 'PUT',
  body: JSON.stringify(settings),
});
export const fetchAdminProducts = () => request('/admin/products');
export const replaceAdminProducts = (products) => request('/admin/products', {
  method: 'PUT',
  body: JSON.stringify(products),
});
export const fetchAdminRequests = () => request('/admin/requests');
export const submitStoreRequest = (payload) => request('/requests', {
  method: 'POST',
  body: JSON.stringify(payload),
});
export const updateStoreRequest = (id, status) => request(`/admin/requests/${encodeURIComponent(id)}`, {
  method: 'PATCH',
  body: JSON.stringify({ status }),
});
