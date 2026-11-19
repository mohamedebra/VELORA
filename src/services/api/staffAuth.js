import { request } from './http.js';
const json = (method, payload) => ({ method, body: JSON.stringify(payload) });
export const loginStaff = (payload) => request('/auth/staff/login', json('POST', payload));
export const logoutStaff = () => request('/auth/staff/logout', { method: 'POST' });
export const getStaffSession = () => request('/auth/session');
export const requestStaffPasswordReset = (email) => request('/auth/staff/password/forgot', json('POST', { email }));
export const resetStaffPassword = (token, password) => request('/auth/staff/password/reset', json('POST', { token, password }));

export const heartbeatStaff = (section) => request('/auth/staff/presence/heartbeat', json('POST', { section }));
