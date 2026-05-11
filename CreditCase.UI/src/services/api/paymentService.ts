import apiClient from './client';
import type { PaymentResponse, CreatePaymentRequest } from '../../types';

const BASE = '/payments';

export const paymentService = {
  getAll: () =>
    apiClient.get<PaymentResponse[]>(BASE).then((r) => r.data),

  create: (body: CreatePaymentRequest) =>
    apiClient.post<PaymentResponse>(BASE, body).then((r) => r.data),
};
