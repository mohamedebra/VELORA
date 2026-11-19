import { useEffect, useState } from 'react';
import { fetchStoreSettings, submitStoreRequest } from './services/api/catalog.js';

export const WHATSAPP_NUMBER = '201009846536';
export function useWhatsAppNumber() {
  const [number, setNumber] = useState(WHATSAPP_NUMBER);
  useEffect(() => {
    let active = true;
    const load = () => fetchStoreSettings().then(settings => {
      if (active && /^\d{8,15}$/.test(settings?.whatsAppNumber || '')) setNumber(settings.whatsAppNumber);
    }).catch(() => {});
    load();
    window.addEventListener('velora:settings-updated', load);
    return () => { active = false; window.removeEventListener('velora:settings-updated', load); };
  }, []);
  return number;
}
const CART_KEY = 'velora.cart';
const REQUESTS_KEY = 'velora.requests';
export function getBag() {
  try { const value = JSON.parse(localStorage.getItem(CART_KEY) || '[]'); return Array.isArray(value) ? value : []; }
  catch { return []; }
}
export function saveBag(value) { localStorage.setItem(CART_KEY, JSON.stringify(value)); window.dispatchEvent(new Event('velora:bag-updated')); }
export function useBag() {
  const [bag, setBag] = useState(getBag);
  useEffect(() => {
    const update = () => setBag(getBag());
    window.addEventListener('storage', update);
    window.addEventListener('velora:bag-updated', update);
    return () => { window.removeEventListener('storage', update); window.removeEventListener('velora:bag-updated', update); };
  }, []);
  return bag;
}
export async function saveRequest(type, payload) {
  const request = { id:`VR-${crypto.randomUUID()}`, type, status:'جديد', createdAt:new Date().toISOString(), ...payload };
  const saved = await submitStoreRequest(request);
  let items = [];
  try { items = JSON.parse(localStorage.getItem(REQUESTS_KEY) || '[]'); } catch {}
  try { localStorage.setItem(REQUESTS_KEY, JSON.stringify([saved, ...items.filter(item => item.id !== saved.id)])); } catch {}
  window.dispatchEvent(new Event('velora:requests-updated'));
  return saved;
}
export function getRequests() {
  try { const value = JSON.parse(localStorage.getItem(REQUESTS_KEY) || '[]'); return Array.isArray(value) ? value : []; }
  catch { return []; }
}
