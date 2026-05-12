// Para birimi formatlayıcı — Türk Lirası (15.000,00 ₺)
export const formatCurrency = (amount: number): string =>
  new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' }).format(amount);

// Tarih formatlayıcı — gün.ay.yıl
export const formatDate = (dateStr: string): string =>
  new Date(dateStr).toLocaleDateString('tr-TR');

// Tarih + saat
export const formatDateTime = (dateStr: string): string =>
  new Date(dateStr).toLocaleString('tr-TR');

// Yüzde formatlayıcı — %3.50
export const formatPercentage = (value: number, decimals: number = 2): string =>
  `${value.toFixed(decimals)}%`;

// Taksit sayısını ay/yıla dönüştür
export const formatTerm = (months: number): string => {
  if (months < 12) return `${months} ay`;
  const years = Math.floor(months / 12);
  const remainingMonths = months % 12;
  if (remainingMonths === 0) return `${years} yıl`;
  return `${years} yıl ${remainingMonths} ay`;
};

// Sayıyı binler ayırıcısı ile formatlayıcı
export const formatNumber = (value: number): string =>
  new Intl.NumberFormat('tr-TR').format(value);
