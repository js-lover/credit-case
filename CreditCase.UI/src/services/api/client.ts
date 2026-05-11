import axios from 'axios';
import toast from 'react-hot-toast';
import type { ApiError } from '../../types';

/**
 * Merkezi Axios instance. Tüm servis modülleri bu istemciyi kullanır.
 * baseURL .env dosyasındaki VITE_API_URL değişkeninden okunur.
 */
const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? 'http://localhost:5100/api',
  headers: { 'Content-Type': 'application/json' },
});

/**
 * Global response interceptor — backend hata formatını (ApiError) yakalar ve toast gösterir.
 * Sayfa bileşenleri yalnızca başarı durumunu yönetir; hata akışı burada merkezi olarak ele alınır.
 *
 * Backend hata formatı:
 *   { type, message, errors? }  →  400 için errors alanı alan bazlı mesajlar içerir.
 */
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    const data: ApiError = error.response?.data;

    if (!data) {
      // Sunucuya erişilemeyen durum (ağ hatası, CORS vb.)
      toast.error('Sunucuya ulaşılamıyor.');
      return Promise.reject(error);
    }

    switch (error.response?.status) {
      case 400: {
        // Validasyon hatası: alan bazlı mesajları birleştirerek tek toast olarak göster
        const fieldErrors = data.errors
          ? Object.values(data.errors).flat().join(' ')
          : data.message;
        toast.error(fieldErrors);
        break;
      }
      case 404:
        toast.error('Kayıt bulunamadı.');
        break;
      case 422:
        // İş kuralı ihlali: backend'den gelen açıklayıcı mesajı direkt göster
        toast.error(data.message);
        break;
      default:
        toast.error('Beklenmeyen bir hata oluştu.');
    }

    return Promise.reject(error);
  }
);

export default apiClient;
