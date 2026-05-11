import apiClient from './client';
import type { InstallmentResponse } from '../../types';

const BASE = '/installments';

export const installmentService = {
  getAll: () =>
    apiClient.get<InstallmentResponse[]>(BASE).then((r) => r.data),

  getById: (id: number) =>
    apiClient.get<InstallmentResponse>(`${BASE}/${id}`).then((r) => r.data),
};
