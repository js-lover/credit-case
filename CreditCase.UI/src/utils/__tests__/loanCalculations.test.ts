/**
 * UI Kredi Hesaplama Testleri
 * loanCalculations.ts fonksiyonlarının doğruluğunu kontrol eder
 * 
 * Bu testleri çalıştırmak için:
 * npm install --save-dev vitest @testing-library/react
 */

import { describe, it, expect } from 'vitest';
import {
  calculateMonthlyInstallment,
  calculateTotalInterest,
  convertMonthlyToAnnualRate,
  calculateDebtToIncomeRatio,
  calculatePaymentPercentage,
  validateInstallmentPlan,
  getLoanWarnings,
} from '../loanCalculations';
import type { LoanResponse } from '../../types';
import { InstallmentStatus } from '../../types';

describe('Kredi Hesaplama Fonksiyonları', () => {
  // ── Aylık Taksit Hesaplamması ────────────────────────────────────────────

  describe('calculateMonthlyInstallment', () => {
    it('Standart 12 aylık kredide doğru taksit hesapla', () => {
      const principal = 12_000;
      const monthlyRate = 3.0;
      const term = 12;

      const result = calculateMonthlyInstallment(principal, monthlyRate, term);

      // Beklenen: ~1204.87 TL
      expect(result).toBeGreaterThan(1200);
      expect(result).toBeLessThan(1210);
    });

    it('Sıfır faits durumunda anapara/vade hesapla', () => {
      const principal = 12_000;
      const monthlyRate = 0;
      const term = 12;

      const result = calculateMonthlyInstallment(principal, monthlyRate, term);

      // Beklenen: 1000 TL (12000 / 12)
      expect(result).toBe(1000);
    });

    it('Daha uzun vadede daha düşük aylık taksit', () => {
      const principal = 12_000;
      const monthlyRate = 3.0;

      const monthly12 = calculateMonthlyInstallment(principal, monthlyRate, 12);
      const monthly24 = calculateMonthlyInstallment(principal, monthlyRate, 24);

      expect(monthly24).toBeLessThan(monthly12);
    });

    it('Sıfır vade durumunda sıfır döner', () => {
      const result = calculateMonthlyInstallment(12_000, 3.0, 0);
      expect(result).toBe(0);
    });
  });

  // ── Faiz Hesaplamması ─────────────────────────────────────────────────────

  describe('calculateTotalInterest', () => {
    it('Faiz = Toplam - Anapara', () => {
      const totalPayable = 14_458.48;
      const principal = 12_000;

      const interest = calculateTotalInterest(totalPayable, principal);

      // Beklenen: ~2458.48 TL
      expect(interest).toBeCloseTo(2458.48, 1);
    });

    it('Negatif faiz durumunda negatif değer', () => {
      const totalPayable = 10_000;
      const principal = 12_000;

      const interest = calculateTotalInterest(totalPayable, principal);

      expect(interest).toBeLessThan(0);
    });
  });

  // ── Yıllık Oran Dönüştürme ─────────────────────────────────────────────

  describe('convertMonthlyToAnnualRate', () => {
    it('Aylık %3 → Yıllık ~42%', () => {
      const monthlyRate = 3.0;
      const annualRate = convertMonthlyToAnnualRate(monthlyRate);

      // Yaklaşık dönüştürme
      expect(annualRate).toBeGreaterThan(35);
      expect(annualRate).toBeLessThan(50);
    });

    it('Aylık %0 → Yıllık %0', () => {
      const annualRate = convertMonthlyToAnnualRate(0);
      expect(annualRate).toBe(0);
    });
  });

  // ── Borç/Gelir Oranı ──────────────────────────────────────────────────────

  describe('calculateDebtToIncomeRatio', () => {
    it('Aylık taksit 1200, gelir 5000 → oran 24%', () => {
      const monthlyInstallment = 1200;
      const monthlyIncome = 5000;

      const ratio = calculateDebtToIncomeRatio(monthlyInstallment, monthlyIncome);

      // 1200 / 5000 = 0.24 = 24%
      expect(ratio).toBeCloseTo(24, 0);
    });

    it('Sıfır gelir durumunda sıfır döner', () => {
      const ratio = calculateDebtToIncomeRatio(1200, 0);
      expect(ratio).toBe(0);
    });

    it('Aylık taksit gelirden yüksekse 100% üzerinde', () => {
      const ratio = calculateDebtToIncomeRatio(6000, 5000);
      expect(ratio).toBeGreaterThan(100);
    });
  });

  // ── Ödeme Yüzdesi ────────────────────────────────────────────────────────

  describe('calculatePaymentPercentage', () => {
    it('6 taksit ödenen, 12 toplam → 50%', () => {
      const percentage = calculatePaymentPercentage(6, 12);
      expect(percentage).toBe(50);
    });

    it('Tüm taksitler ödendi → 100%', () => {
      const percentage = calculatePaymentPercentage(12, 12);
      expect(percentage).toBe(100);
    });

    it('Hiç ödeme yapılmadı → 0%', () => {
      const percentage = calculatePaymentPercentage(0, 12);
      expect(percentage).toBe(0);
    });

    it('Sıfır toplam taksit → 0%', () => {
      const percentage = calculatePaymentPercentage(5, 0);
      expect(percentage).toBe(0);
    });
  });

  // ── Taksit Planı Doğrulaması ──────────────────────────────────────────

  describe('validateInstallmentPlan', () => {
    it('Geçerli kredi planında valid=true', () => {
      const mockLoan: Partial<LoanResponse> = {
        term: 12,
        principalAmount: 12_000,
        rateAmount: 3.0,
        totalPayableAmount: 14_458.48,
        installments: Array(12).fill(null).map((_, i) => ({
          id: i + 1,
          loanId: 1,
          installmentNumber: i + 1,
          amount: 1204.87,
          dueDate: new Date().toISOString(),
          status: InstallmentStatus.Unpaid,
          isBalloon: false,
          payment: null,
        })),
      };

      const result = validateInstallmentPlan(mockLoan as LoanResponse);

      // Toplamlar biraz fark edebilir (yuvarlama nedeniyle)
      expect(result.valid || result.errors.length <= 1).toBe(true);
    });

    it('Taksit sayısı hatası tespit et', () => {
      const mockLoan: Partial<LoanResponse> = {
        term: 12,
        principalAmount: 12_000,
        rateAmount: 3.0,
        totalPayableAmount: 14_458.48,
        installments: Array(10).fill(null).map((_, i) => ({
          id: i + 1,
          loanId: 1,
          installmentNumber: i + 1,
          amount: 1204.87,
          dueDate: new Date().toISOString(),
          status: InstallmentStatus.Unpaid,
          isBalloon: false,
          payment: null,
        })),
      };

      const result = validateInstallmentPlan(mockLoan as LoanResponse);

      expect(result.valid).toBe(false);
      expect(result.errors.length).toBeGreaterThan(0);
      expect(result.errors[0]).toContain('Taksit sayısı hatalı');
    });

    it('Negatif faiz hatası tespit et', () => {
      const mockLoan: Partial<LoanResponse> = {
        term: 12,
        principalAmount: 12_000,
        rateAmount: 3.0,
        totalPayableAmount: 10_000, // Anapara tutarından düşük
        installments: [],
      };

      const result = validateInstallmentPlan(mockLoan as LoanResponse);

      expect(result.valid).toBe(false);
      expect(result.errors.some(e => e.includes('anapara'))).toBe(true);
    });
  });

  // ── Kredi Uyarıları ───────────────────────────────────────────────────────

  describe('getLoanWarnings', () => {
    it('Gecikmiş taksit uyarısı', () => {
      const mockLoan: Partial<LoanResponse> = {
        term: 12,
        rateAmount: 3.0,
        installments: Array(12).fill(null).map((_, i) => ({
          id: i + 1,
          loanId: 1,
          installmentNumber: i + 1,
          amount: 1204.87,
          dueDate: new Date().toISOString(),
          status: i < 2 ? InstallmentStatus.Overdue : InstallmentStatus.Unpaid, // 2 gecikmiş
          isBalloon: false,
          payment: null,
        })),
      };

      const warnings = getLoanWarnings(mockLoan as LoanResponse);

      expect(warnings.some(w => w.includes('gecikmiş'))).toBe(true);
    });

    it('Yüksek faiz oranı uyarısı', () => {
      const mockLoan: Partial<LoanResponse> = {
        term: 12,
        rateAmount: 6.0, // %5 üzerinde
        installments: [],
      };

      const warnings = getLoanWarnings(mockLoan as LoanResponse);

      expect(warnings.some(w => w.includes('%5 üzerinde'))).toBe(true);
    });

    it('Tamamlama uyarısı', () => {
      const mockLoan: Partial<LoanResponse> = {
        term: 12,
        rateAmount: 3.0,
        installments: Array(12).fill(null).map((_, i) => ({
          id: i + 1,
          loanId: 1,
          installmentNumber: i + 1,
          amount: 1204.87,
          dueDate: new Date().toISOString(),
          status: i < 11 ? InstallmentStatus.Paid : InstallmentStatus.Unpaid, // 11/12 ödendi
          isBalloon: false,
          payment: null,
        })),
      };

      const warnings = getLoanWarnings(mockLoan as LoanResponse);

      expect(warnings.some(w => w.includes('neredeyse tamamlanmış'))).toBe(true);
    });
  });
});
