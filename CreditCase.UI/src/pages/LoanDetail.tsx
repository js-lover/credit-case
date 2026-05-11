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
import { formatCurrency, formatDate } from '../utils/formatters';

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

  return (
    <PageLayout
      title={`Kredi #${loan.id} — ${LOAN_TYPE_LABELS[loan.loanType]}`}
      action={<Button variant="secondary" onClick={() => navigate('/loans')}>← Geri</Button>}
    >
      {/* Kredi özet kartları */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        <StatCard label="Ana Para" value={formatCurrency(loan.principalAmount)} />
        <StatCard label="Kalan Anapara" value={formatCurrency(loan.remainingPrincipal)} accent="default" />
        <StatCard label="Ödenen / Toplam" value={`${paidCount} / ${loan.term}`} sub={`${overdueCount} gecikmiş`} accent={overdueCount > 0 ? 'danger' : 'success'} />
        <StatCard label="Durum" value={loan.status === 0 ? 'Aktif' : 'Kapalı'} />
      </div>

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
              <tr key={inst.id} className="border-b border-[#E2E8F0] last:border-0 hover:bg-[#F8FAFC]">
                <td className="px-4 py-3 text-[#64748B]">{inst.installmentNumber}</td>
                <td className="px-4 py-3 font-medium">{formatCurrency(inst.amount)}</td>
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
                  {inst.status !== InstallmentStatus.Paid && (
                    <Button size="sm" onClick={() => openPayModal(inst)}>Öde</Button>
                  )}
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
