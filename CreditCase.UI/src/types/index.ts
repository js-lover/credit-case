// ── Enum sabitleri (erasableSyntaxOnly uyumlu) ─────────────────────────────────

export const LoanType = { Personal: 0, Education: 1, Vehicle: 2 } as const;
export type LoanType = (typeof LoanType)[keyof typeof LoanType];

export const LoanStatus = { Active: 0, Closed: 1 } as const;
export type LoanStatus = (typeof LoanStatus)[keyof typeof LoanStatus];

export const InstallmentStatus = { Paid: 0, Unpaid: 1, Overdue: 2 } as const;
export type InstallmentStatus = (typeof InstallmentStatus)[keyof typeof InstallmentStatus];

export const PaymentStatus = { Successful: 0, Failed: 1 } as const;
export type PaymentStatus = (typeof PaymentStatus)[keyof typeof PaymentStatus];

// ── Customer ──────────────────────────────────────────────────────────────────

export interface CustomerResponse {
  id: number;
  firstName: string;
  lastName: string;
  identityNumber: string;
  email: string;
  phoneNumber: string;
  createdAt: string;
}

export interface CustomerSummaryResponse {
  customerId: number;
  fullName: string;
  totalLoans: number;
  totalRemainingPrincipal: number;
  totalOutstandingDebt: number;
  paidInstallments: number;
  unpaidInstallments: number;
  overdueInstallments: number;
}

export interface CreateCustomerRequest {
  firstName: string;
  lastName: string;
  identityNumber: string;
  email: string;
  phoneNumber: string;
}

export interface UpdateCustomerRequest {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
}

// ── Loan ──────────────────────────────────────────────────────────────────────

export interface LoanResponse {
  id: number;
  customerId: number;
  loanType: LoanType;
  principalAmount: number;
  interestRate: number;
  term: number;
  startDate: string;
  status: LoanStatus;
  remainingPrincipal: number;
  installments: InstallmentResponse[];
}

export interface CreateLoanRequest {
  customerId: number;
  loanType: LoanType;
  principalAmount: number;
  interestRate: number;
  term: number;
  startDate: string;
}

// ── Installment ───────────────────────────────────────────────────────────────

export interface InstallmentResponse {
  id: number;
  loanId: number;
  installmentNumber: number;
  amount: number;
  dueDate: string;
  status: InstallmentStatus;
  payment: PaymentResponse | null;
}

// ── Payment ───────────────────────────────────────────────────────────────────

export interface PaymentResponse {
  id: number;
  installmentId: number;
  paymentAmount: number;
  paymentDate: string;
  status: PaymentStatus;
}

export interface CreatePaymentRequest {
  installmentId: number;
  paymentAmount: number;
}

// ── API Error ─────────────────────────────────────────────────────────────────

export interface ApiError {
  type: string;
  message: string;
  errors?: Record<string, string[]>;
}

// ── Display helpers ───────────────────────────────────────────────────────────

export const LOAN_TYPE_LABELS: Record<LoanType, string> = {
  [LoanType.Personal]:  'Bireysel',
  [LoanType.Education]: 'Eğitim',
  [LoanType.Vehicle]:   'Taşıt',
};

export const LOAN_STATUS_LABELS: Record<LoanStatus, string> = {
  [LoanStatus.Active]: 'Aktif',
  [LoanStatus.Closed]: 'Kapalı',
};

export const INSTALLMENT_STATUS_LABELS: Record<InstallmentStatus, string> = {
  [InstallmentStatus.Paid]:    'Ödendi',
  [InstallmentStatus.Unpaid]:  'Bekliyor',
  [InstallmentStatus.Overdue]: 'Gecikmiş',
};
