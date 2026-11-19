import React, { useState } from 'react';
import { ArrowLeft, Eye, EyeOff, LockKeyhole, Mail, ShieldCheck } from 'lucide-react';

export default function StaffLogin({ onLogin, busy, apiNotice }) {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState('');

  const submit = async (event) => {
    event.preventDefault();
    setError('');
    try {
      await onLogin({ email: email.trim().toLowerCase(), password });
    } catch (cause) {
      if (cause?.status === 401 || cause?.status === 403) setError('البريد الإلكتروني أو كلمة المرور غير صحيحة، أو الحساب غير مفعّل.');
      else if (cause?.status === 503) setError('بيانات حساب الداشبورد غير مضبوطة على الخادم. راجع إعدادات VeloraAdmin.');
      else if (cause?.status === 404 || cause?.code === 'NETWORK_ERROR' || cause?.code === 'AUTH_API_NOT_READY') setError('خدمة تسجيل الدخول غير متصلة حاليًا. الشاشة جاهزة، وتسجيل الدخول سيعمل بعد ربط الباك إند.');
      else setError(cause?.message || 'حصلت مشكلة. راجع البيانات وحاول تاني.');
    }
  };

  const heading = 'أهلًا بعودتك';
  const description = 'سجّل دخولك ببريدك الإلكتروني وكلمة المرور للمتابعة.';

  return <main className="staff-login-page" dir="rtl">
    <div className="staff-login-decoration staff-login-decoration-one" />
    <div className="staff-login-decoration staff-login-decoration-two" />
    <section className="staff-login-card">
      <header className="staff-login-brand"><img src="/velora-logo.png" alt="VELORA" /><span><b>لوحة المتجر</b><small>إدارة متجر VELORA</small></span></header>
      <div className="staff-login-intro"><span className="staff-login-icon"><ShieldCheck size={22} /></span><p className="staff-login-eyebrow">مساحة آمنة لإدارة VELORA</p><h1>{heading}</h1><p>{description}</p></div>
      {apiNotice && <div className="staff-login-api-note" role="status">{apiNotice}</div>}
      <form className="staff-login-form" onSubmit={submit}>
        <label htmlFor="staff-email">البريد الإلكتروني</label><div className="staff-login-input-wrap"><Mail size={17} /><input id="staff-email" type="email" autoComplete="username" dir="ltr" placeholder="name@company.com" value={email} onChange={(event) => setEmail(event.target.value)} required /></div>
        <label htmlFor="staff-password">كلمة المرور</label>
        <div className="staff-login-input-wrap"><LockKeyhole size={17} /><input id="staff-password" type={showPassword ? 'text' : 'password'} autoComplete="current-password" placeholder="أدخل كلمة المرور" value={password} onChange={(event) => setPassword(event.target.value)} required minLength={8} /><button type="button" className="staff-password-toggle" aria-label={showPassword ? 'إخفاء كلمة المرور' : 'إظهار كلمة المرور'} onClick={() => setShowPassword((value) => !value)}>{showPassword ? <EyeOff size={17} /> : <Eye size={17} />}</button></div>
        {error && <p className="staff-login-error" role="alert">{error}</p>}
        <button className="staff-login-submit" type="submit" disabled={busy}>{busy ? 'جارٍ التحقق…' : <>تسجيل الدخول<ArrowLeft size={17} /></>}</button>
      </form>
      <footer className="staff-login-footer"><ShieldCheck size={14} /> دخول مخصص لحسابات الفريق المصرّح لها</footer>
    </section>
    <small className="staff-login-copyright">© {new Date().getFullYear()} VELORA · جميع الحقوق محفوظة</small>
  </main>;
}
