import apiClient from './client';
import type {
  CustomerResponse,
  CustomerSummaryResponse,
  CreateCustomerRequest,
  UpdateCustomerRequest,
} from '../../types';

const BASE = '/customers';

export const customerService = {
  getAll: () =>
    apiClient.get<CustomerResponse[]>(BASE).then((r) => r.data),

  getById: (id: number) =>
    apiClient.get<CustomerResponse>(`${BASE}/${id}`).then((r) => r.data),

  getSummary: (id: number) =>
    apiClient.get<CustomerSummaryResponse>(`${BASE}/${id}/summary`).then((r) => r.data),

  create: (body: CreateCustomerRequest) =>
    apiClient.post<CustomerResponse>(BASE, body).then((r) => r.data),

  update: (id: number, body: UpdateCustomerRequest) =>
    apiClient.put<CustomerResponse>(`${BASE}/${id}`, body).then((r) => r.data),

  delete: (id: number) =>
    apiClient.delete(`${BASE}/${id}`),
};
