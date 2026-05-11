import axios from 'axios';
import toast from 'react-hot-toast';
import type { ApiError } from '../../types';

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? 'http://localhost:5100/api',
  headers: { 'Content-Type': 'application/json' },
});

// Response interceptor — backend hata formatını yakalar ve toast gösterir
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    const data: ApiError = error.response?.data;

    if (!data) {
      toast.error('Sunucuya ulaşılamıyor.');
      return Promise.reject(error);
    }

    switch (error.response?.status) {
      case 400: {
        // Validation hatası: alan bazlı mesajları birleştir
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
        // İş kuralı ihlali: backend mesajını direkt göster
        toast.error(data.message);
        break;
      default:
        toast.error('Beklenmeyen bir hata oluştu.');
    }

    return Promise.reject(error);
  }
);

export default apiClient;
