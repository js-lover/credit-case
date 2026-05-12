import type { LoanResponse } from '../types';
import { formatCurrency } from './formatters';

/**
 * Kredi hesaplama yardımcıları
 * Amortizasyon formülü ve faiz hesaplamaları
 */

/** Aylık taksit tutarını hesapla (Amortizasyon) */
export const calculateMonthlyInstallment = (
  principal: number,
  monthlyRatePercent: number,
  termMonths: number
): number => {
  if (termMonths <= 0) return 0;

  const r = monthlyRatePercent / 100;
  if (r === 0) return principal / termMonths;

  const factor = Math.pow(1 + r, termMonths);
  const monthly = (principal * (r * factor)) / (factor - 1);
  return Math.round(monthly * 100) / 100;
};

/** Toplam faiz miktarını hesapla */
export const calculateTotalInterest = (totalPayable: number, principal: number): number => {
  return Math.round((totalPayable - principal) * 100) / 100;
};

/** Kredi oranını yıllık eşivalente dönüştür */
export const convertMonthlyToAnnualRate = (monthlyRate: number): number => {
  // Yaklaşık dönüştürme: Annual ≈ Monthly * 12
  // Kesin: (1 + r)^12 - 1
  const r = monthlyRate / 100;
  const annualFactor = Math.pow(1 + r, 12) - 1;
  return Math.round(annualFactor * 100 * 100) / 100;
};

/** Borç-gelir oranını hesapla */
export const calculateDebtToIncomeRatio = (
  monthlyInstallment: number,
  monthlyIncome: number
): number => {
  if (monthlyIncome <= 0) return 0;
  return Math.round((monthlyInstallment / monthlyIncome) * 100 * 100) / 100;
};

/** Kredi ödeme yüzdesini hesapla */
export const calculatePaymentPercentage = (paidInstallments: number, totalInstallments: number): number => {
  if (totalInstallments <= 0) return 0;
  return Math.round((paidInstallments / totalInstallments) * 100);
};

/** Kalan kredinin gün sayısını hesapla */
export const calculateRemainingDays = (startDate: string, termMonths: number): number => {
  const start = new Date(startDate);
  const end = new Date(start);
  end.setMonth(end.getMonth() + termMonths);
  
  const now = new Date();
  const remaining = end.getTime() - now.getTime();
  return Math.ceil(remaining / (1000 * 60 * 60 * 24));
};

/** Kredi hakkında detaylı rapor oluştur */
export const generateLoanSummary = (loan: LoanResponse) => {
  const paidInstallments = loan.installments.filter(i => i.status === 0).length;
  const totalInstallments = loan.term;
  const paidTotal = loan.installments
    .filter(i => i.status === 0)
    .reduce((sum, i) => sum + (i.payment?.paymentAmount ?? i.amount), 0);

  const monthlyInstallment = calculateMonthlyInstallment(
    loan.principalAmount,
    loan.rateAmount,
    loan.term
  );

  const totalInterest = calculateTotalInterest(loan.totalPayableAmount, loan.principalAmount);
  const annualRate = convertMonthlyToAnnualRate(loan.rateAmount);
  const paymentPercentage = calculatePaymentPercentage(paidInstallments, totalInstallments);

  return {
    monthlyInstallment,
    totalInterest,
    annualRate,
    paymentPercentage,
    paidInstallments,
    paidTotal,
    remainingInstallments: totalInstallments - paidInstallments,
  };
};

/** Taksit planı doğruluğunu valide et */
export const validateInstallmentPlan = (loan: LoanResponse): { valid: boolean; errors: string[] } => {
  const errors: string[] = [];

  if (loan.installments.length !== loan.term) {
    errors.push(`Taksit sayısı hatalı: ${loan.installments.length} (Beklenen: ${loan.term})`);
  }

  const sumOfInstallments = loan.installments.reduce((sum, i) => sum + i.amount, 0);
  const expectedSum = loan.totalPayableAmount;
  
  if (Math.abs(sumOfInstallments - expectedSum) > 1) {
    errors.push(`Taksit toplamı hatalı: ${formatCurrency(sumOfInstallments)} (Beklenen: ${formatCurrency(expectedSum)})`);
  }

  const calculatedInterest = loan.totalPayableAmount - loan.principalAmount;
  if (calculatedInterest < 0) {
    errors.push('Toplam ödeme, anapara tutarından düşük olamaz');
  }

  if (loan.term <= 0) {
    errors.push('Kredi vadesi pozitif olmalıdır');
  }

  if (loan.rateAmount < 0) {
    errors.push('Aylık faiz oranı negatif olamaz');
  }

  return {
    valid: errors.length === 0,
    errors,
  };
};

/** Kredi hakkında uyarıları belirle */
export const getLoanWarnings = (loan: LoanResponse): string[] => {
  const warnings: string[] = [];

  const overdueInstallments = loan.installments.filter(i => i.status === 2).length;
  if (overdueInstallments > 0) {
    warnings.push(`${overdueInstallments} taksit gecikmiş durumda`);
  }

  const paymentPercentage = calculatePaymentPercentage(
    loan.installments.filter(i => i.status === 0).length,
    loan.term
  );
  if (paymentPercentage > 80) {
    warnings.push('Kredi neredeyse tamamlanmış durumda');
  }

  if (loan.rateAmount > 7) {
    warnings.push('Aylık vade oranı 7 üzerinde (çok yüksek)');
  }

  return warnings;
};
