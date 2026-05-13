import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import { loanService } from '../services/api/loanService';
import { customerService } from '../services/api/customerService';
import { paymentService } from '../services/api/paymentService';
import type { LoanResponse, InstallmentResponse, CustomerResponse } from '../types';
import { InstallmentStatus, LOAN_TYPE_LABELS, PROFESSION_LABELS, EMPLOYMENT_STATUS_LABELS } from '../types';
import { PageLayout } from '../components/layout/PageLayout';
import { Card, StatCard } from '../components/ui/Card';
import { InstallmentBadge, LoanStatusBadge } from '../components/ui/Badge';
import { Button } from '../components/ui/Button';
import { Modal } from '../components/ui/Modal';
import { Spinner } from '../components/ui/Spinner';
import { formatCurrency, formatDate, formatTerm } from '../utils/formatters';
import { getLoanWarnings, validateInstallmentPlan } from '../utils/loanCalculations';

// ── Vergi sabitleri ───────────────────────────────────────────────────────────
const KKDF = 0.15;
const BSMV = 0.05;

interface AmortizationRow {
  installmentNumber: number;
  principalPayment: number;
  netInterest: number;
  kkdf: number;
  bsmv: number;
  totalPayment: number;
  remainingBalance: number;
}

function buildAmortizationTable(principal: number, netRate: number, term: number): AmortizationRow[] {
  const grossRate = netRate * (1 + KKDF + BSMV);
  const r = grossRate / 100;
  if (r === 0 || term <= 0 || principal <= 0) return [];
  const factor = Math.pow(1 + r, term);
  const monthly = principal * r * factor / (factor - 1);

  const rows: AmortizationRow[] = [];
  let remaining = principal;

  for (let i = 1; i <= term; i++) {
    const grossInterest = Math.round(remaining * r * 100) / 100;
    const netInterest   = Math.round(grossInterest / (1 + KKDF + BSMV) * 100) / 100;
    const kkdf          = Math.round(netInterest * KKDF * 100) / 100;
    const bsmv          = Math.round(netInterest * BSMV * 100) / 100;

    let principalPayment: number;
    let totalPayment: number;
    if (i === term) {
      principalPayment = remaining;
      totalPayment     = Math.round((principalPayment + grossInterest) * 100) / 100;
    } else {
      principalPayment = Math.round((monthly - grossInterest) * 100) / 100;
      totalPayment     = Math.round(monthly * 100) / 100;
    }
    remaining = Math.max(0, Math.round((remaining - principalPayment) * 100) / 100);

    rows.push({ installmentNumber: i, principalPayment, netInterest, kkdf, bsmv, totalPayment, remainingBalance: remaining });
  }
  return rows;
}

export function LoanDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const loanId = Number(id);

  const [loan, setLoan] = useState<LoanResponse | null>(null);
  const [customer, setCustomer] = useState<CustomerResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [payTarget, setPayTarget] = useState<InstallmentResponse | null>(null);
  const [paying, setPaying] = useState(false);

  const load = async () => {
    try {
      setLoading(true);
      const l = await loanService.getById(loanId);
      setLoan(l);
      customerService.getById(l.customerId).then(setCustomer).catch(() => {});
    } catch {
      navigate('/loans');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, [loanId]);

  const handlePay = async () => {
    if (!payTarget) return;
    setPaying(true);
    try {
      await paymentService.create({ installmentId: payTarget.id, paymentAmount: payTarget.amount });
      toast.success(`${payTarget.installmentNumber}. taksit ödendi.`);
      setPayTarget(null);
      load();
    } finally { setPaying(false); }
  };

  if (loading) return <PageLayout title="Kredi Detayı"><Spinner /></PageLayout>;
  if (!loan) return null;

  const paidCount    = loan.installments.filter(i => i.status === InstallmentStatus.Paid).length;
  const overdueCount = loan.installments.filter(i => i.status === InstallmentStatus.Overdue).length;
  const paidTotal    = loan.installments
    .filter(i => i.status === InstallmentStatus.Paid)
    .reduce((s, i) => s + (i.payment?.paymentAmount ?? i.amount), 0);

  const unpaidInstallments = loan.installments
    .filter(i => i.status !== InstallmentStatus.Paid)
    .sort((a, b) => a.installmentNumber - b.installmentNumber);
  const nextPayableId = unpaidInstallments[0]?.id ?? null;

  const loanStartDate = new Date(loan.startDate);
  const loanEndDate   = new Date(loanStartDate);
  loanEndDate.setMonth(loanEndDate.getMonth() + loan.term);

  const totalInterest = loan.totalPayableAmount - loan.principalAmount;
  const grossRate     = Math.round(loan.rateAmount * (1 + KKDF + BSMV) * 10000) / 10000;
  const annualCost    = Math.round(((Math.pow(1 + grossRate / 100, 12) - 1) * 100) * 100) / 100;

  const validationResult = validateInstallmentPlan(loan);
  const loanWarnings     = getLoanWarnings(loan);

  const amortizationTable = buildAmortizationTable(loan.principalAmount, loan.rateAmount, loan.term);
  const amortMap = new Map(amortizationTable.map(r => [r.installmentNumber, r]));

  // Müşterinin yaşını hesapla
  const customerAge = customer?.dateOfBirth
    ? Math.floor((Date.now() - new Date(customer.dateOfBirth).getTime()) / (365.25 * 24 * 3600 * 1000))
    : null;

  return (
    <PageLayout
      title={`Kredi #${loan.id} — ${LOAN_TYPE_LABELS[loan.loanType]}`}
      action={<Button variant="secondary" onClick={() => navigate('/loans')}>← Geri</Button>}
    >
      {/* Müşteri bilgi kartı */}
      {customer && (
        <Card className="mb-4 border-slate-200 bg-slate-50">
          <div className="px-5 py-3 flex flex-wrap items-center gap-x-6 gap-y-2">
            <div>
              <p className="text-[10px] text-[#64748B] font-semibold uppercase tracking-wide">Kredi Sahibi</p>
              <button
                onClick={() => navigate(`/customers/${customer.id}`)}
                className="text-base font-bold text-[#1B4FD8] hover:underline mt-0.5"
              >
                {customer.firstName} {customer.lastName}
              </button>
            </div>
            <div className="h-8 w-px bg-[#E2E8F0] hidden sm:block" />
            <div>
              <p className="text-[10px] text-[#64748B]">TC Kimlik</p>
              <p className="text-sm font-medium text-[#0F172A]">{customer.identityNumber}</p>
            </div>
            {customerAge !== null && (
              <div>
                <p className="text-[10px] text-[#64748B]">Yaş</p>
                <p className="text-sm font-medium text-[#0F172A]">{customerAge}</p>
              </div>
            )}
            <div>
              <p className="text-[10px] text-[#64748B]">Meslek</p>
              <p className="text-sm font-medium text-[#0F172A]">{PROFESSION_LABELS[customer.professionCategory]}</p>
            </div>
            <div>
              <p className="text-[10px] text-[#64748B]">İstihdam</p>
              <p className="text-sm font-medium text-[#0F172A]">{EMPLOYMENT_STATUS_LABELS[customer.employmentStatus]}</p>
            </div>
            <div>
              <p className="text-[10px] text-[#64748B]">Aylık Gelir</p>
              <p className="text-sm font-medium text-[#0F172A]">{formatCurrency(customer.monthlyIncome)}</p>
            </div>
            <div>
              <p className="text-[10px] text-[#64748B]">E-posta</p>
              <p className="text-sm font-medium text-[#0F172A]">{customer.email}</p>
            </div>
          </div>
        </Card>
      )}

      {/* Kredi başlık kartı */}
      <Card className="mb-6 bg-linear-to-r from-blue-50 to-blue-100 border-blue-200">
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 p-5">
          <div>
            <p className="text-xs text-blue-600 font-semibold">BAŞLANGIÇ TARİHİ</p>
            <p className="text-lg font-bold text-blue-900 mt-1">{formatDate(loan.startDate)}</p>
          </div>
          <div>
            <p className="text-xs text-blue-600 font-semibold">BİTİŞ TARİHİ</p>
            <p className="text-lg font-bold text-blue-900 mt-1">{formatDate(loanEndDate.toISOString())}</p>
          </div>
          <div>
            <p className="text-xs text-blue-600 font-semibold">KREDİ DURUMU</p>
            <p className={`text-lg font-bold mt-1 ${loan.status === 0 ? 'text-green-600' : 'text-gray-600'}`}>
              {loan.status === 0 ? '✓ Aktif' : '○ Kapalı'}
            </p>
          </div>
          <div>
            <p className="text-xs text-blue-600 font-semibold">VADE</p>
            <p className="text-lg font-bold text-blue-900 mt-1">{formatTerm(loan.term)}</p>
          </div>
        </div>
      </Card>

      {/* Finansal özet kartları */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        <StatCard label="Ana Para" value={formatCurrency(loan.principalAmount)} />
        <StatCard
          label="Toplam Ödenecek"
          value={formatCurrency(loan.totalPayableAmount)}
          sub={`Ek ödeme: ${formatCurrency(totalInterest)}`}
          accent="warning"
        />
        <StatCard
          label="Ödenen / Kalan"
          value={`${formatCurrency(paidTotal)} / ${formatCurrency(loan.remainingPrincipal)}`}
          sub={`${paidCount} / ${loan.term} taksit`}
          accent={overdueCount > 0 ? 'danger' : 'success'}
        />
        <StatCard
          label="Vade Oranı"
          value={String(loan.rateAmount)}
          sub={`Net · Brüt: ${grossRate}`}
          accent="default"
        />
      </div>

      {/* Ek kredi detayları */}
      <Card className="mb-6">
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 p-5">
          <div className="border-r border-gray-200 pr-4">
            <p className="text-xs text-gray-500 font-semibold">TOPLAM EK ÖDEME</p>
            <p className="text-xl font-bold text-gray-900 mt-1">{formatCurrency(totalInterest)}</p>
          </div>
          <div className="border-r border-gray-200 pr-4">
            <p className="text-xs text-gray-500 font-semibold">YMO (YILLIK)</p>
            <p className="text-xl font-bold text-gray-900 mt-1">%{annualCost.toFixed(2)}</p>
          </div>
          <div className="border-r border-gray-200 pr-4">
            <p className="text-xs text-gray-500 font-semibold">GECİKMİŞ TAKSİT SAYISI</p>
            <p className={`text-xl font-bold mt-1 ${overdueCount > 0 ? 'text-red-600' : 'text-green-600'}`}>
              {overdueCount === 0 ? '—' : `${overdueCount}`}
            </p>
          </div>
          <div>
            <p className="text-xs text-gray-500 font-semibold">ÖDEMENİN %'Sİ</p>
            <p className="text-xl font-bold text-gray-900 mt-1">
              {loan.term > 0 ? Math.round((paidCount / loan.term) * 100) : 0}%
            </p>
          </div>
        </div>
      </Card>

      {/* Uyarılar */}
      {(loanWarnings.length > 0 || !validationResult.valid) && (
        <Card className="mb-6 border-amber-200 bg-amber-50">
          <div className="p-5">
            <h3 className="font-semibold text-amber-900 mb-3">⚠ Önemli Bilgiler</h3>
            <ul className="space-y-2">
              {loanWarnings.map((w, i) => (
                <li key={i} className="text-sm text-amber-800 flex items-start">
                  <span className="mr-2">•</span><span>{w}</span>
                </li>
              ))}
              {validationResult.errors.map((e, i) => (
                <li key={`e-${i}`} className="text-sm text-red-700 flex items-start font-medium">
                  <span className="mr-2">✗</span><span>{e}</span>
                </li>
              ))}
            </ul>
          </div>
        </Card>
      )}

      {/* Taksit planı */}
      <Card>
        <div className="px-4 py-3 border-b border-[#E2E8F0] flex items-center justify-between">
          <div>
            <h2 className="text-sm font-semibold text-[#0F172A]">Taksit Planı</h2>
            <p className="text-[10px] text-[#64748B] mt-0.5">KKDF (%15) ve BSMV (%5) dahil amortisman</p>
          </div>
          <LoanStatusBadge status={loan.status} />
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-[#E2E8F0] text-left text-[#64748B]">
                <th className="px-3 py-3 font-medium">#</th>
                <th className="px-3 py-3 font-medium text-right">Anapara</th>
                <th className="px-3 py-3 font-medium text-right">Net Vade Farkı</th>
                <th className="px-3 py-3 font-medium text-right text-amber-600">KKDF</th>
                <th className="px-3 py-3 font-medium text-right text-amber-600">BSMV</th>
                <th className="px-3 py-3 font-medium text-right text-[#1B4FD8]">Taksit</th>
                <th className="px-3 py-3 font-medium">Son Ödeme</th>
                <th className="px-3 py-3 font-medium">Ödeme Tarihi</th>
                <th className="px-3 py-3 font-medium">Durum</th>
                <th className="px-3 py-3" />
              </tr>
            </thead>
            <tbody>
              {loan.installments.map(inst => {
                const row = amortMap.get(inst.installmentNumber);
                return (
                  <tr
                    key={inst.id}
                    className={`border-b border-[#E2E8F0] last:border-0 hover:bg-[#F8FAFC] ${inst.isBalloon ? 'bg-amber-50' : ''}`}
                  >
                    <td className="px-3 py-3 text-[#64748B]">
                      {inst.installmentNumber}
                      {inst.isBalloon && (
                        <span className="ml-1 px-1.5 py-0.5 text-[10px] font-semibold bg-amber-100 text-amber-700 rounded">BALON</span>
                      )}
                    </td>
                    <td className="px-3 py-3 text-right text-[#0F172A]">
                      {row ? formatCurrency(row.principalPayment) : '—'}
                    </td>
                    <td className="px-3 py-3 text-right text-[#64748B]">
                      {row ? formatCurrency(row.netInterest) : '—'}
                    </td>
                    <td className="px-3 py-3 text-right text-amber-600">
                      {row ? formatCurrency(row.kkdf) : '—'}
                    </td>
                    <td className="px-3 py-3 text-right text-amber-600">
                      {row ? formatCurrency(row.bsmv) : '—'}
                    </td>
                    <td className={`px-3 py-3 text-right font-semibold text-[#1B4FD8] ${inst.isBalloon ? 'text-amber-700' : ''}`}>
                      {formatCurrency(inst.amount)}
                    </td>
                    <td className={`px-3 py-3 ${inst.status === InstallmentStatus.Overdue ? 'text-red-600 font-medium' : 'text-[#64748B]'}`}>
                      {formatDate(inst.dueDate)}
                    </td>
                    <td className="px-3 py-3 text-[#64748B]">
                      {inst.payment ? formatDate(inst.payment.paymentDate) : '—'}
                    </td>
                    <td className="px-3 py-3">
                      <InstallmentBadge status={inst.status} />
                    </td>
                    <td className="px-3 py-3 text-right">
                      {inst.id === nextPayableId ? (
                        <Button size="sm" onClick={() => setPayTarget(inst)}>Öde</Button>
                      ) : inst.status !== InstallmentStatus.Paid ? (
                        <span className="text-xs text-[#64748B]">Önceki bekliyor</span>
                      ) : null}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </Card>

      {/* Ödeme modalı */}
      <Modal
        open={!!payTarget}
        onClose={() => setPayTarget(null)}
        title={`${payTarget?.installmentNumber}. Taksiti Öde`}
        footer={
          <>
            <Button variant="secondary" onClick={() => setPayTarget(null)}>İptal</Button>
            <Button loading={paying} onClick={handlePay}>Ödemeyi Onayla</Button>
          </>
        }
      >
        <div className="bg-[#F1F5F9] rounded-lg p-3 text-sm space-y-2">
          <div className="flex justify-between">
            <span className="text-[#64748B]">Taksit no</span>
            <span className="font-medium">{payTarget?.installmentNumber}. taksit</span>
          </div>
          <div className="flex justify-between">
            <span className="text-[#64748B]">Ödenecek tutar</span>
            <span className="font-semibold text-[#0F172A]">{formatCurrency(payTarget?.amount ?? 0)}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-[#64748B]">Son ödeme tarihi</span>
            <span>{payTarget ? formatDate(payTarget.dueDate) : ''}</span>
          </div>
        </div>
      </Modal>
    </PageLayout>
  );
}
