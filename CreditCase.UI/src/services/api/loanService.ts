import apiClient from './client';
import type { LoanResponse, CreateLoanRequest } from '../../types';

const BASE = '/loans';

export const loanService = {
  getAll: () =>
    apiClient.get<LoanResponse[]>(BASE).then((r) => r.data),

  getById: (id: number) =>
    apiClient.get<LoanResponse>(`${BASE}/${id}`).then((r) => r.data),

  create: (body: CreateLoanRequest) =>
    apiClient.post<LoanResponse>(BASE, body).then((r) => r.data),
};
