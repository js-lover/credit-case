import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import { loanService } from '../services/api/loanService';
import { paymentService } from '../services/api/paymentService';
import type { LoanResponse, InstallmentResponse } from '../types';
import { InstallmentStatus, LOAN_TYPE_LABELS } from '../types';
import { PageLayout } from '../components/layout/PageLayout';
import { Card, StatCard } from '../components/ui/Card';
import { InstallmentBadge, LoanStatusBadge } from '../components/ui/Badge';
import { Button } from '../components/ui/Button';
import { Modal } from '../components/ui/Modal';
import { Spinner } from '../components/ui/Spinner';
import { formatCurrency, formatDate, formatTerm, formatPercentage } from '../utils/formatters';
import { getLoanWarnings, validateInstallmentPlan } from '../utils/loanCalculations';

export function LoanDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const loanId = Number(id);

  const [loan, setLoan] = useState<LoanResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [payTarget, setPayTarget] = useState<InstallmentResponse | null>(null);
  const [paying, setPaying] = useState(false);

  const load = async () => {
    try {
      setLoading(true);
      setLoan(await loanService.getById(loanId));
    } catch {
      navigate('/loans');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, [loanId]);

  const openPayModal = (inst: InstallmentResponse) => {
    setPayTarget(inst);
  };

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

  // Sıralı ödeme: en düşük numaralı ödenmemiş taksit.
  const unpaidInstallments = loan.installments
    .filter(i => i.status !== InstallmentStatus.Paid)
    .sort((a, b) => a.installmentNumber - b.installmentNumber);
  const nextPayableId = unpaidInstallments[0]?.id ?? null;

  // Kredi başlangıç ve bitiş tarihlerini hesapla
  const loanStartDate = new Date(loan.startDate);
  const loanEndDate = new Date(loanStartDate);
  loanEndDate.setMonth(loanEndDate.getMonth() + loan.term);

  // Faiz ve faiz oranını hesapla
  const totalInterest = loan.totalPayableAmount - loan.principalAmount;
  const interestRate = loan.rateAmount; // Aylık oran

  // Hesaplama hataları varsa bunu kontrol et
  const validationResult = validateInstallmentPlan(loan);
  const loanWarnings = getLoanWarnings(loan);

  return (
    <PageLayout
      title={`Kredi #${loan.id} — ${LOAN_TYPE_LABELS[loan.loanType]}`}
      action={<Button variant="secondary" onClick={() => navigate('/loans')}>← Geri</Button>}
    >
      {/* Kredi başlık kartı - tarihler ve durum */}
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

      {/* Kredi özet kartları - finansal detaylar */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        <StatCard label="Ana Para" value={formatCurrency(loan.principalAmount)} />
        <StatCard
          label="Toplam Ödenecek"
          value={formatCurrency(loan.totalPayableAmount)}
          sub={`Faiz: ${formatCurrency(totalInterest)}`}
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
          value={formatPercentage(interestRate)} 
          sub="Aylık" 
          accent="default"
        />
      </div>

      {/* Ek kredi detayları */}
      <Card className="mb-6">
        <div className="grid grid-cols-2 md:grid-cols-3 gap-4 p-5">
          <div className="border-r border-gray-200 pr-4">
            <p className="text-xs text-gray-500 font-semibold">TOPLAM FAİZ</p>
            <p className="text-xl font-bold text-gray-900 mt-1">{formatCurrency(totalInterest)}</p>
          </div>
          <div className="border-r border-gray-200 pr-4">
            <p className="text-xs text-gray-500 font-semibold">GECİKMİŞ TRANŞ</p>
            <p className={`text-xl font-bold mt-1 ${overdueCount > 0 ? 'text-red-600' : 'text-green-600'}`}>
              {overdueCount === 0 ? '—' : `${overdueCount} taksit`}
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

      {/* Uyarılar bölümü */}
      {(loanWarnings.length > 0 || !validationResult.valid) && (
        <Card className="mb-6 border-amber-200 bg-amber-50">
          <div className="p-5">
            <h3 className="font-semibold text-amber-900 mb-3">⚠️ Önemli Bilgiler</h3>
            <ul className="space-y-2">
              {loanWarnings.map((warning, idx) => (
                <li key={idx} className="text-sm text-amber-800 flex items-start">
                  <span className="mr-2">•</span>
                  <span>{warning}</span>
                </li>
              ))}
              {validationResult.errors.map((error, idx) => (
                <li key={`error-${idx}`} className="text-sm text-red-700 flex items-start font-medium">
                  <span className="mr-2">✗</span>
                  <span>{error}</span>
                </li>
              ))}
            </ul>
          </div>
        </Card>
      )}

      {/* Taksit planı */}
      <Card>
        <div className="px-4 py-3 border-b border-[#E2E8F0] flex items-center justify-between">
          <h2 className="text-sm font-semibold text-[#0F172A]">Taksit Planı</h2>
          <LoanStatusBadge status={loan.status} />
        </div>
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-[#E2E8F0] text-left text-[#64748B]">
              {['#', 'Tutar', 'Son Ödeme', 'Ödeme Tarihi', 'Durum', ''].map(h => (
                <th key={h} className="px-4 py-3 font-medium">{h}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {loan.installments.map(inst => (
              <tr key={inst.id} className={`border-b border-[#E2E8F0] last:border-0 hover:bg-[#F8FAFC] ${inst.isBalloon ? 'bg-amber-50' : ''}`}>
                <td className="px-4 py-3 text-[#64748B]">
                  {inst.installmentNumber}
                  {inst.isBalloon && (
                    <span className="ml-1 px-1.5 py-0.5 text-[10px] font-semibold bg-amber-100 text-amber-700 rounded">BALON</span>
                  )}
                </td>
                <td className={`px-4 py-3 font-medium ${inst.isBalloon ? 'text-amber-700' : ''}`}>{formatCurrency(inst.amount)}</td>
                <td className={`px-4 py-3 ${inst.status === InstallmentStatus.Overdue ? 'text-red-600 font-medium' : 'text-[#64748B]'}`}>
                  {formatDate(inst.dueDate)}
                </td>
                <td className="px-4 py-3 text-[#64748B]">
                  {inst.payment ? formatDate(inst.payment.paymentDate) : '—'}
                </td>
                <td className="px-4 py-3">
                  <InstallmentBadge status={inst.status} />
                </td>
                <td className="px-4 py-3 text-right">
                  {inst.id === nextPayableId ? (
                    <Button size="sm" onClick={() => openPayModal(inst)}>Öde</Button>
                  ) : inst.status !== InstallmentStatus.Paid ? (
                    <span className="text-xs text-[#64748B]">Önceki bekliyor</span>
                  ) : null}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
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
