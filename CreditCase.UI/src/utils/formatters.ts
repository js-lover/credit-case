// Para birimi formatlayıcı — Türk Lirası (15.000,00 ₺)
export const formatCurrency = (amount: number): string =>
  new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' }).format(amount);

// Tarih formatlayıcı — gün.ay.yıl
export const formatDate = (dateStr: string): string =>
  new Date(dateStr).toLocaleDateString('tr-TR');

// Tarih + saat
export const formatDateTime = (dateStr: string): string =>
  new Date(dateStr).toLocaleString('tr-TR');
