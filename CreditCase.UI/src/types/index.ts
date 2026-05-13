/**
 * Uygulama genelinde kullanılan TypeScript tip tanımları.
 * Backend API DTO'larıyla birebir eşleştirilmiştir.
 */

// ── Enum sabitleri (erasableSyntaxOnly uyumlu) ─────────────────────────────────
// Vite, TypeScript `enum` sözdizimini desteklemez (erasableSyntaxOnly: true).
// Bunun yerine const nesnesi + type alias kullanılır; runtime davranışı aynıdır.

export const LoanType = { Personal: 0, Education: 1, Vehicle: 2 } as const;
export type LoanType = (typeof LoanType)[keyof typeof LoanType];

export const LoanStatus = { Active: 0, Closed: 1 } as const;
export type LoanStatus = (typeof LoanStatus)[keyof typeof LoanStatus];

export const InstallmentStatus = { Paid: 0, Unpaid: 1, Overdue: 2 } as const;
export type InstallmentStatus = (typeof InstallmentStatus)[keyof typeof InstallmentStatus];

export const PaymentStatus = { Successful: 0, Failed: 1 } as const;
export type PaymentStatus = (typeof PaymentStatus)[keyof typeof PaymentStatus];

// Mevcut LoanType/LoanStatus ile tutarlı: backend integer serialize eder.
export const ProfessionCategory = {
  Government: 0, Healthcare: 1, Finance: 2, Technology: 3,
  Education: 4, Commerce: 5, Services: 6, Construction: 7,
  Seasonal: 8, Other: 9,
} as const;
export type ProfessionCategory = (typeof ProfessionCategory)[keyof typeof ProfessionCategory];

export const EmploymentStatus = {
  Active: 0, PartTime: 1, Freelance: 2, Retired: 3, Unemployed: 4,
} as const;
export type EmploymentStatus = (typeof EmploymentStatus)[keyof typeof EmploymentStatus];

export const RiskCategory = {
  Low: 0, Medium: 1, High: 2, VeryHigh: 3,
} as const;
export type RiskCategory = (typeof RiskCategory)[keyof typeof RiskCategory];

export const ScoreCategory = {
  Kritik: 0, GelisimeAcik: 1, Dengeli: 2, Guvenli: 3, Prestijli: 4,
} as const;
export type ScoreCategory = (typeof ScoreCategory)[keyof typeof ScoreCategory];

// ── Customer ──────────────────────────────────────────────────────────────────

export interface CustomerResponse {
  id: number;
  firstName: string;
  lastName: string;
  identityNumber: string;
  email: string;
  phoneNumber: string;
  createdAt: string;
  dateOfBirth: string;
  monthlyIncome: number;
  professionCategory: ProfessionCategory;
  employmentStatus: EmploymentStatus;
}

/** GET /api/customers/{id}/summary — müşterinin tüm kredilerine ait borç özeti */
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
  dateOfBirth: string;
  monthlyIncome: number;
  professionCategory: ProfessionCategory;
  employmentStatus: EmploymentStatus;
}

export interface UpdateCustomerRequest {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  dateOfBirth: string;
  monthlyIncome: number;
  professionCategory: ProfessionCategory;
  employmentStatus: EmploymentStatus;
}

// ── Loan ──────────────────────────────────────────────────────────────────────

export interface LoanResponse {
  id: number;
  customerId: number;
  loanType: LoanType;
  principalAmount: number;
  rateAmount: number;
  term: number;               // ay cinsinden vade
  startDate: string;
  status: LoanStatus;
  remainingPrincipal: number; // ödenmemiş taksitlerin toplamı
  totalPayableAmount: number; // vade farkı dahil toplam geri ödeme
  installments: InstallmentResponse[];
}

export interface CreateLoanRequest {
  customerId: number;
  loanType: LoanType;
  principalAmount: number;
  term: number;
  startDate: string;
  isBalloonPayment: boolean;
}

// ── Installment ───────────────────────────────────────────────────────────────

export interface InstallmentResponse {
  id: number;
  loanId: number;
  installmentNumber: number;
  amount: number;
  dueDate: string;
  status: InstallmentStatus;
  isBalloon: boolean;
  payment: PaymentResponse | null;  // ödeme yapılmamışsa null
}

// ── Payment ───────────────────────────────────────────────────────────────────

export interface PaymentResponse {
  id: number;
  installmentId: number;
  installmentNumber: number;
  dueDate: string;
  loanId: number;
  loanType: LoanType;
  customerId: number;
  customerFullName: string;
  paymentAmount: number;
  paymentDate: string;
  status: PaymentStatus;
}

export interface CreatePaymentRequest {
  installmentId: number;
  paymentAmount: number;
}

// ── Loan Evaluation ───────────────────────────────────────────────────────────

export interface LoanApplicationRequest {
  customerId: number;
  loanType: LoanType;
  requestedAmount: number;
  requestedTerm: number;
}

export interface InstallmentPlanRow {
  installmentNumber: number;
  principalPayment: number;
  netInterest: number;
  kkdf: number;
  bsmv: number;
  totalPayment: number;
  remainingBalance: number;
}

export interface LoanEvaluationResponse {
  id: number;
  customerId: number;
  customerFullName: string;
  requestedAmount: number;
  requestedTerm: number;
  requestedLoanType: LoanType;
  isApproved: boolean;
  approvedAmount: number;
  maximumAmount: number;
  maximumTerm: number;
  approvedRateAmount: number;
  riskLevel: RiskCategory;
  creditScoreCategory: ScoreCategory;
  creditScore: number;
  debtToIncomeRatio: number;
  monthlyInstallmentEstimate: number;
  grossRateAmount: number;
  annualCostRate: number;
  installmentPlan: InstallmentPlanRow[];
  rejectionReason: string | null;
  evaluationDate: string;
  expirationDate: string;
}

// ── API Error ─────────────────────────────────────────────────────────────────
// Backend ExceptionHandlingMiddleware tarafından üretilen standart hata formatı.

export interface ApiError {
  type: string;
  message: string;
  errors?: Record<string, string[]>;  // 400 validasyon hatalarında alan bazlı mesajlar
}

// ── Display helpers ───────────────────────────────────────────────────────────
// Enum değerlerinin UI'da Türkçe karşılıkları.

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

export const RISK_CATEGORY_LABELS: Record<RiskCategory, string> = {
  [RiskCategory.Low]:      'Düşük Risk',
  [RiskCategory.Medium]:   'Orta Risk',
  [RiskCategory.High]:     'Yüksek Risk',
  [RiskCategory.VeryHigh]: 'Çok Yüksek Risk',
};

export const SCORE_CATEGORY_LABELS: Record<ScoreCategory, string> = {
  [ScoreCategory.Kritik]:       'Kritik',
  [ScoreCategory.GelisimeAcik]: 'Gelişime Açık',
  [ScoreCategory.Dengeli]:      'Dengeli',
  [ScoreCategory.Guvenli]:      'Güvenli',
  [ScoreCategory.Prestijli]:    'Prestijli',
};

export const SCORE_CATEGORY_COLORS: Record<ScoreCategory, string> = {
  [ScoreCategory.Kritik]:       '#DC2626',
  [ScoreCategory.GelisimeAcik]: '#EA580C',
  [ScoreCategory.Dengeli]:      '#D97706',
  [ScoreCategory.Guvenli]:      '#16A34A',
  [ScoreCategory.Prestijli]:    '#2563EB',
};

export const PROFESSION_LABELS: Record<ProfessionCategory, string> = {
  [ProfessionCategory.Government]:   'Kamu',
  [ProfessionCategory.Healthcare]:   'Sağlık',
  [ProfessionCategory.Finance]:      'Finans',
  [ProfessionCategory.Technology]:   'Teknoloji',
  [ProfessionCategory.Education]:    'Eğitim',
  [ProfessionCategory.Commerce]:     'Ticaret',
  [ProfessionCategory.Services]:     'Hizmetler',
  [ProfessionCategory.Construction]: 'İnşaat',
  [ProfessionCategory.Seasonal]:     'Mevsimlik',
  [ProfessionCategory.Other]:        'Diğer',
};

export const EMPLOYMENT_STATUS_LABELS: Record<EmploymentStatus, string> = {
  [EmploymentStatus.Active]:     'Tam Zamanlı',
  [EmploymentStatus.PartTime]:   'Yarı Zamanlı',
  [EmploymentStatus.Freelance]:  'Serbest Meslek',
  [EmploymentStatus.Retired]:    'Emekli',
  [EmploymentStatus.Unemployed]: 'İşsiz',
};
