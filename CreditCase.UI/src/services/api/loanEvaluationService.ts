import apiClient from './client';
import type { LoanApplicationRequest, LoanEvaluationResponse } from '../../types';

const BASE = '/loans';

export const loanEvaluationService = {
  evaluate: (request: LoanApplicationRequest) =>
    apiClient.post<LoanEvaluationResponse>(`${BASE}/evaluate`, request).then(r => r.data),

  getById: (evaluationId: number) =>
    apiClient.get<LoanEvaluationResponse>(`${BASE}/evaluation/${evaluationId}`).then(r => r.data),

  getMaximumEligibility: (customerId: number) =>
    apiClient.get<LoanEvaluationResponse>(`${BASE}/maximum-eligibility/${customerId}`).then(r => r.data),

  getByCustomer: (customerId: number) =>
    apiClient.get<LoanEvaluationResponse[]>(`/customers/${customerId}/evaluations`).then(r => r.data),
};
