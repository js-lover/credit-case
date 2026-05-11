import { useEffect, useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { customerService } from '../services/api/customerService';
import { loanService } from '../services/api/loanService';
import type { CustomerSummaryResponse, LoanResponse } from '../types';
import { LoanStatus, LOAN_TYPE_LABELS } from '../types';
import { PageLayout } from '../components/layout/PageLayout';
import { StatCard } from '../components/ui/Card';
import { LoanStatusBadge } from '../components/ui/Badge';
import { Button } from '../components/ui/Button';
import { Spinner } from '../components/ui/Spinner';
import { formatCurrency } from '../utils/formatters';

export function CustomerDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const customerId = Number(id);

  const [summary, setSummary] = useState<CustomerSummaryResponse | null>(null);
  const [loans, setLoans] = useState<LoanResponse[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const load = async () => {
      try {
        const [s, all] = await Promise.all([
          customerService.getSummary(customerId),
          loanService.getAll(),
        ]);
        setSummary(s);
        setLoans(all.filter(l => l.customerId === customerId));
      } catch {
        navigate('/customers');
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [customerId, navigate]);

  if (loading) return <PageLayout title="Müşteri Detayı"><Spinner /></PageLayout>;
  if (!summary) return null;

  const activeLoans = loans.filter(l => l.status === LoanStatus.Active).length;

  return (
    <PageLayout
      title={summary.fullName}
      action={<Button variant="secondary" onClick={() => navigate('/customers')}>← Geri</Button>}
    >
      {/* Borç özeti kartları */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        <StatCard label="Toplam Kredi" value={summary.totalLoans} sub={`${activeLoans} aktif`} />
        <StatCard label="Kalan Anapara" value={formatCurrency(summary.totalRemainingPrincipal)} accent="default" />
        <StatCard label="Bekleyen Borç" value={formatCurrency(summary.totalOutstandingDebt)} accent="warning" />
        <StatCard
          label="Gecikmiş Taksit"
          value={summary.overdueInstallments}
          sub={`${summary.paidInstallments} ödendi · ${summary.unpaidInstallments} bekliyor`}
          accent={summary.overdueInstallments > 0 ? 'danger' : 'success'}
        />
      </div>

      {/* Kredi listesi */}
      <h2 className="text-sm font-semibold text-[#64748B] uppercase tracking-wide mb-3">Krediler</h2>
      <div className="bg-white rounded-xl border border-[#E2E8F0] shadow-sm overflow-hidden">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-[#E2E8F0] text-left text-[#64748B]">
              {['Tür', 'Ana Para', 'Faiz', 'Vade', 'Kalan', 'Durum', ''].map(h => (
                <th key={h} className="px-4 py-3 font-medium">{h}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {loans.map(l => (
              <tr key={l.id} className="border-b border-[#E2E8F0] last:border-0 hover:bg-[#F8FAFC]">
                <td className="px-4 py-3 font-medium">{LOAN_TYPE_LABELS[l.loanType]}</td>
                <td className="px-4 py-3">{formatCurrency(l.principalAmount)}</td>
                <td className="px-4 py-3">%{l.interestRate}</td>
                <td className="px-4 py-3">{l.term} ay</td>
                <td className="px-4 py-3 font-medium text-[#1B4FD8]">{formatCurrency(l.remainingPrincipal)}</td>
                <td className="px-4 py-3"><LoanStatusBadge status={l.status} /></td>
                <td className="px-4 py-3">
                  <Link to={`/loans/${l.id}`}>
                    <Button size="sm" variant="ghost">Taksitler →</Button>
                  </Link>
                </td>
              </tr>
            ))}
            {loans.length === 0 && (
              <tr><td colSpan={7} className="px-4 py-8 text-center text-[#64748B]">Henüz kredi yok.</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </PageLayout>
  );
}
