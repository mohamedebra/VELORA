import { useEffect, useState } from 'react';
import { fetchStoreProducts, fetchAdminProducts, replaceAdminProducts } from './services/api/catalog.js';

export const defaultMirrors = [
  { id:'arch-gold', name:'مراية قوس بإطار ذهبي', type:'arch', price:2850, size:'60 × 160 سم', finish:'ذهبي مطفي', description:'مراية طولية بتصميم قوس أنيق وإطار معدني ذهبي. تضيف لمسة هادئة ومضيئة للمدخل أو غرفة النوم.', tone:'#d8c8b6' },
  { id:'round-black', name:'مراية دائرية بإطار أسود', type:'round', price:1650, size:'80 سم قطر', finish:'أسود مطفي', description:'تصميم دائري بسيط بإطار أسود رفيع، يناسب الحمام أو ركن القهوة أو أي مساحة عصرية.', tone:'#c6d0c4' },
  { id:'organic', name:'مراية عضوية بتصميم حر', type:'organic', price:2200, size:'70 × 100 سم', finish:'بدون إطار', description:'شكل انسيابي مميز بحواف ناعمة، يمنح الحائط شخصية مختلفة ويكمل الديكور العصري.', tone:'#d5c6ba' },
  { id:'full-length', name:'مراية طولية بإطار خشبي', type:'tall', price:3450, size:'70 × 180 سم', finish:'خشب طبيعي', description:'مراية كاملة الطول بإطار خشبي دافئ. مناسبة لغرفة الملابس ويمكن إسنادها على الحائط أو تعليقها.', tone:'#c4c7bb' },
  { id:'pill', name:'مراية بيضاوية بحزام جلد', type:'pill', price:1950, size:'50 × 100 سم', finish:'حزام جلد بني', description:'مراية بيضاوية معلقة بحزام جلد أنيق، تجمع بين الخامات الطبيعية والتصميم العملي.', tone:'#d9cfc2' },
  { id:'double-arch', name:'مراية قوس مزدوجة', type:'double', price:3100, size:'65 × 150 سم', finish:'شامبين', description:'قوس مزدوج بإطار معدني بلون الشامبين، قطعة ديكور عملية تضيف عمقاً وإضاءة للمكان.', tone:'#c8d0d0' },
  { id:'pottery-vase', category:'decor', name:'فازة طينية بملمس حجري', type:'vase', price:680, size:'24 × 30 سم', finish:'طين طبيعي', description:'قطعة يدوية بطابع طبيعي وملمس حجري هادئ، تكمل الرف أو طاولة المدخل.', tone:'#d8c9b8' },
  { id:'brass-candles', category:'decor', name:'شمعدان نحاسي بملمس معتّق', type:'candle', price:920, size:'قطعتان · 18 سم', finish:'نحاس معتّق', description:'زوج من الشمعدانات بتشطيب نحاسي دافئ يضيف لمسة حرفية أنيقة للطاولات والكونسول.', tone:'#d9c9b2' },
  { id:'brass-tray', category:'decor', name:'صحن تقديم نحاسي مطروق', type:'tray', price:760, size:'قطر 32 سم', finish:'نحاس مطروق', description:'صحن تقديم يدوي بتفاصيل مطروقة، مناسب للتنسيق اليومي وعرض القطع الصغيرة.', tone:'#c9ad8d' },
  { id:'woven-mirror', category:'mirror', name:'مراية دائرية بإطار منسوج', type:'round', price:2400, size:'75 سم قطر', finish:'حبل قطني طبيعي', description:'مراية دائرية محاطة بإطار منسوج يدوياً، بتفاصيل طبيعية تضيف دفئاً للجدار.', tone:'#ded4c2' },
];

const STORAGE_KEY = 'miro.products';
export function getMirrors() {
  try {
    const saved = JSON.parse(localStorage.getItem(STORAGE_KEY) || 'null');
    return Array.isArray(saved) ? saved : defaultMirrors;
  } catch { return defaultMirrors; }
}
export function saveMirrors(items) {
  try { localStorage.setItem(STORAGE_KEY, JSON.stringify(items)); } catch { /* The API remains the primary store when browser storage is full. */ }
  window.dispatchEvent(new Event('miro:catalog-updated'));
}
export async function loadStoreProducts() { return fetchStoreProducts(); }
export async function loadAdminProducts() { return fetchAdminProducts(); }
export async function saveAdminProducts(items) { return replaceAdminProducts(items); }
export function useMirrors() {
  const [items, setItems] = useState(getMirrors);
  useEffect(() => {
    const update = () => setItems(getMirrors());
    fetchStoreProducts().then((remoteItems) => {
      if (!Array.isArray(remoteItems)) return;
      try { localStorage.setItem(STORAGE_KEY, JSON.stringify(remoteItems)); } catch {}
      setItems(remoteItems);
    }).catch(() => setItems([]));
    window.addEventListener('storage', update);
    window.addEventListener('miro:catalog-updated', update);
    return () => { window.removeEventListener('storage', update); window.removeEventListener('miro:catalog-updated', update); };
  }, []);
  return items;
}
