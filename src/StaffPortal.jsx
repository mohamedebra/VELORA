import React, { useCallback, useEffect, useState } from 'react';
import { ApiError } from './services/api/http.js';
import { getStaffSession, loginStaff, logoutStaff, heartbeatStaff } from './services/api/staffAuth.js';
import AdminDashboard from './AdminDashboard.jsx';
import StaffLogin from './StaffLogin.jsx';
import './admin.css';
import './products.css';

const allowedRoles = new Set(['owner', 'content_manager']);
const extractUser = (response) => response?.data?.user || response?.user || response?.data || null;

export default function StaffPortal() {
  const [sessionUser, setSessionUser] = useState(null);
  const [checking, setChecking] = useState(true);
  const [busy, setBusy] = useState(false);
  const [apiNotice, setApiNotice] = useState('');
  const [activeSection, setActiveSection] = useState('portal');
  const onPresenceUpdate = useCallback((section) => setActiveSection(section), []);

  useEffect(() => {
    let active = true;
    getStaffSession().then((response) => {
      const user = extractUser(response);
      if (active && user && allowedRoles.has(user.role)) setSessionUser(user);
    }).catch((error) => {
      if (!active) return;
      if (error?.status !== 401 && error?.status !== 403) {
        setApiNotice('تعذر الاتصال بخادم تسجيل الدخول. تأكد أن الـ backend يعمل ثم حدّث الصفحة.');
      }
    }).finally(() => { if (active) setChecking(false); });
    return () => { active = false; };
  }, []);

  const handleLogin = async (credentials) => {
    setBusy(true);
    try {
      const response = await loginStaff(credentials);
      const user = extractUser(response);
      if (!user || !allowedRoles.has(user.role)) throw new ApiError('بيانات الحساب المستلمة غير مكتملة.', { code: 'INVALID_STAFF_SESSION' });
      setSessionUser(user);
      setApiNotice('');
    } finally { setBusy(false); }
  };

  useEffect(() => {
    if (!sessionUser) return undefined;
    const ping = () => heartbeatStaff(activeSection).catch(() => {});
    ping();
    const timer = window.setInterval(ping, 30000);
    const onVisibility = () => { if (document.visibilityState === 'visible') ping(); };
    document.addEventListener('visibilitychange', onVisibility);
    return () => { window.clearInterval(timer); document.removeEventListener('visibilitychange', onVisibility); };
  }, [sessionUser, activeSection]);

  const handleLogout = async () => {
    try { await logoutStaff(); } finally { setSessionUser(null); }
  };

  if (checking) return <main className="staff-login-loading" dir="rtl"><img src="/velora-logo.png" alt="VELORA" /><span>جارٍ التحقق من جلسة الدخول…</span></main>;
  if (!sessionUser) return <StaffLogin onLogin={handleLogin} busy={busy} apiNotice={apiNotice} />;
  return <AdminDashboard sessionUser={sessionUser} onLogout={handleLogout} onPresenceUpdate={onPresenceUpdate} />;
}
