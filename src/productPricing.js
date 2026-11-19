export function getProductPricing(product, basePrice = Number(product?.price) || 0) {
  const price = Math.max(0, Number(basePrice) || 0);
  const discountValue = Math.max(0, Number(product?.discountValue) || 0);
  const discountType = product?.discountType || 'percentage';
  const discountAmount = discountType === 'fixed' ? Math.min(price, discountValue) : Math.min(price, price * Math.min(100, discountValue) / 100);
  const salePrice = Math.max(0, price - discountAmount);
  const percentage = price > 0 ? Math.round((discountAmount / price) * 100) : 0;
  return { price, salePrice, discountAmount, percentage, hasDiscount: discountAmount > 0, label: discountType === 'fixed' ? `خصم ${new Intl.NumberFormat('ar-EG').format(discountAmount)} ج.م` : `خصم ${percentage}%` };
}
