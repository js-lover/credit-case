import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { customerService } from '../services/api/customerService';
import { loanService } from '../services/api/loanService';
import { installmentService } from '../services/api/installmentService';
import type { CustomerResponse, LoanResponse, InstallmentResponse } from '../types';
import { LoanStatus, InstallmentStatus } from '../types';
import { PageLayout } from '../components/layout/PageLayout';
import { StatCard } from '../components/ui/Card';
import { LoanStatusBadge } from '../components/ui/Badge';
import { Button } from '../components/ui/Button';
import { Spinner } from '../components/ui/Spinner';
import { formatCurrency, formatDate } from '../utils/formatters';

export function Dashboard() {
  const [customers, setCustomers] = useState<CustomerResponse[]>([]);
  const [loans, setLoans] = useState<LoanResponse[]>([]);
  const [installments, setInstallments] = useState<InstallmentResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    // GET /api/loans returns loans without installments (no eager load),
    // so installment-level stats are fetched separately.
    Promise.all([customerService.getAll(), loanService.getAll(), installmentService.getAll()])
      .then(([c, l, i]) => { setCustomers(c); setLoans(l); setInstallments(i); })
      .finally(() => setLoading(false));
  }, []);

  const activeLoans      = loans.filter(l => l.status === LoanStatus.Active).length;
  const totalOutstanding = installments
    .filter(i => i.status !== InstallmentStatus.Paid)
    .reduce((sum, i) => sum + i.amount, 0);
  const overdueCount     = installments.filter(i => i.status === InstallmentStatus.Overdue).length;

  const recentCustomers = [...customers]
    .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
    .slice(0, 5);

  if (loading) return <PageLayout title="Dashboard"><Spinner /></PageLayout>;

  return (
    <PageLayout title="Dashboard">
      {/* Özet kartlar */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        <StatCard label="Toplam Müşteri"    value={customers.length} />
        <StatCard label="Aktif Kredi"        value={activeLoans} accent="default" />
        <StatCard label="Bekleyen Borç"      value={formatCurrency(totalOutstanding)} accent="warning" />
        <StatCard
          label="Gecikmiş Taksit"
          value={overdueCount}
          accent={overdueCount > 0 ? 'danger' : 'success'}
        />
      </div>

      {/* Son eklenen müşteriler */}
      <h2 className="text-sm font-semibold text-[#64748B] uppercase tracking-wide mb-3">
        Son Müşteriler
      </h2>
      <div className="bg-white rounded-xl border border-[#E2E8F0] shadow-sm overflow-hidden mb-8">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-[#E2E8F0] text-left text-[#64748B]">
              {['Ad Soyad', 'TC Kimlik', 'E-posta', 'Telefon', 'Kayıt Tarihi', ''].map(h => (
                <th key={h} className="px-4 py-3 font-medium">{h}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {recentCustomers.map(c => (
              <tr key={c.id} className="border-b border-[#E2E8F0] last:border-0 hover:bg-[#F8FAFC]">
                <td className="px-4 py-3 font-medium text-[#0F172A]">{c.firstName} {c.lastName}</td>
                <td className="px-4 py-3 text-[#64748B] font-mono text-xs">{c.identityNumber}</td>
                <td className="px-4 py-3 text-[#64748B]">{c.email}</td>
                <td className="px-4 py-3 text-[#64748B]">{c.phoneNumber}</td>
                <td className="px-4 py-3 text-[#64748B]">{formatDate(c.createdAt)}</td>
                <td className="px-4 py-3">
                  <Button size="sm" variant="ghost" onClick={() => navigate(`/customers/${c.id}`)}>
                    Detay
                  </Button>
                </td>
              </tr>
            ))}
            {customers.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-8 text-center text-[#64748B]">Henüz müşteri yok.</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {/* Son krediler */}
      <h2 className="text-sm font-semibold text-[#64748B] uppercase tracking-wide mb-3">
        Son Krediler
      </h2>
      <div className="bg-white rounded-xl border border-[#E2E8F0] shadow-sm overflow-hidden">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-[#E2E8F0] text-left text-[#64748B]">
              {['#', 'Ana Para', 'Kalan', 'Durum', ''].map(h => (
                <th key={h} className="px-4 py-3 font-medium">{h}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {loans.slice(0, 5).map(l => (
              <tr key={l.id} className="border-b border-[#E2E8F0] last:border-0 hover:bg-[#F8FAFC]">
                <td className="px-4 py-3 text-[#64748B]">#{l.id}</td>
                <td className="px-4 py-3 font-medium">{formatCurrency(l.principalAmount)}</td>
                <td className="px-4 py-3 font-medium text-[#1B4FD8]">{formatCurrency(l.remainingPrincipal)}</td>
                <td className="px-4 py-3"><LoanStatusBadge status={l.status} /></td>
                <td className="px-4 py-3">
                  <Button size="sm" variant="ghost" onClick={() => navigate(`/loans/${l.id}`)}>
                    Taksitler →
                  </Button>
                </td>
              </tr>
            ))}
            {loans.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-8 text-center text-[#64748B]">Henüz kredi yok.</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </PageLayout>
  );
}
