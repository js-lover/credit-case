import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { paymentService } from '../services/api/paymentService';
import { customerService } from '../services/api/customerService';
import type { PaymentResponse, CustomerResponse } from '../types';
import { PaymentStatus } from '../types';
import { LOAN_TYPE_LABELS } from '../types';
import { PageLayout } from '../components/layout/PageLayout';
import { Card } from '../components/ui/Card';
import { Spinner } from '../components/ui/Spinner';
import { formatCurrency, formatDate } from '../utils/formatters';

function PaymentStatusBadge({ status }: { status: PaymentStatus }) {
  return status === PaymentStatus.Successful ? (
    <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-700">
      Başarılı
    </span>
  ) : (
    <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-700">
      Başarısız
    </span>
  );
}

function OnTimeBadge({ dueDate, paymentDate }: { dueDate: string; paymentDate: string }) {
  // Saat farkını görmezden gel: yalnızca tarih kısmını karşılaştır.
  const isOnTime = paymentDate.slice(0, 10) <= dueDate.slice(0, 10);
  return isOnTime ? (
    <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-700">
      Zamanında
    </span>
  ) : (
    <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-700">
      Gecikmiş
    </span>
  );
}

export function Payments() {
  const [payments, setPayments] = useState<PaymentResponse[]>([]);
  const [customers, setCustomers] = useState<CustomerResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedCustomerId, setSelectedCustomerId] = useState<number | null>(null);
  const navigate = useNavigate();

  useEffect(() => {
    Promise.all([paymentService.getAll(), customerService.getAll()])
      .then(([p, c]) => {
        setPayments(p);
        setCustomers(c);
      })
      .finally(() => setLoading(false));
  }, []);

  const filtered = selectedCustomerId === null
    ? payments
    : payments.filter(p => p.customerId === selectedCustomerId);

  // Seçili müşteri için özet istatistikler
  const totalPaid = filtered.reduce((s, p) => s + p.paymentAmount, 0);
  const onTimeCount = filtered.filter(p => new Date(p.paymentDate) <= new Date(p.dueDate)).length;
  const lateCount = filtered.length - onTimeCount;

  return (
    <PageLayout title="Ödeme Geçmişi">
      {/* Müşteri filtresi */}
      <Card className="mb-4">
        <div className="flex flex-wrap items-center gap-3 p-1">
          <label className="text-sm font-medium text-[#0F172A] whitespace-nowrap">Müşteri Filtresi:</label>
          <select
            value={selectedCustomerId ?? ''}
            onChange={e => setSelectedCustomerId(e.target.value === '' ? null : Number(e.target.value))}
            className="border border-[#E2E8F0] rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-[#1B4FD8]/30 min-w-[220px]"
          >
            <option value="">Tüm müşteriler</option>
            {customers.map(c => (
              <option key={c.id} value={c.id}>{c.firstName} {c.lastName} — {c.identityNumber}</option>
            ))}
          </select>

          {/* Özet istatistikler (seçili müşteri için) */}
          {filtered.length > 0 && (
            <div className="ml-auto flex items-center gap-6 text-sm">
              <span className="text-[#64748B]">
                <span className="font-semibold text-[#0F172A]">{filtered.length}</span> ödeme
              </span>
              <span className="text-[#64748B]">
                Toplam: <span className="font-semibold text-[#1B4FD8]">{formatCurrency(totalPaid)}</span>
              </span>
              <span className="text-green-700 font-medium">{onTimeCount} zamanında</span>
              {lateCount > 0 && <span className="text-red-600 font-medium">{lateCount} gecikmiş</span>}
            </div>
          )}
        </div>
      </Card>

      <Card>
        {loading ? <Spinner /> : (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-[#E2E8F0] text-left text-[#64748B]">
                <th className="px-4 py-3 font-medium">#</th>
                <th className="px-4 py-3 font-medium">Müşteri</th>
                <th className="px-4 py-3 font-medium">Kredi No</th>
                <th className="px-4 py-3 font-medium">Taksit</th>
                <th className="px-4 py-3 font-medium">Vade Tarihi</th>
                <th className="px-4 py-3 font-medium">Ödeme Tarihi</th>
                <th className="px-4 py-3 font-medium">Ödeme Tutarı</th>
                <th className="px-4 py-3 font-medium">Zamanlama</th>
                <th className="px-4 py-3 font-medium">Durum</th>
                <th className="px-4 py-3 font-medium"></th>
              </tr>
            </thead>
            <tbody>
              {filtered.map(p => (
                <tr key={p.id} className="border-b border-[#E2E8F0] last:border-0 hover:bg-[#F8FAFC]">
                  <td className="px-4 py-3 text-[#64748B] font-mono text-xs">{p.id}</td>
                  <td className="px-4 py-3">
                    <button
                      onClick={() => navigate(`/customers/${p.customerId}`)}
                      className="text-left group"
                    >
                      <p className="font-medium text-[#0F172A] group-hover:text-[#1B4FD8] transition-colors">
                        {p.customerFullName || `Müşteri #${p.customerId}`}
                      </p>
                    </button>
                  </td>
                  <td className="px-4 py-3">
                    <button
                      onClick={() => navigate(`/loans/${p.loanId}`)}
                      className="text-[#1B4FD8] hover:underline text-xs font-mono"
                    >
                      #{p.loanId} — {LOAN_TYPE_LABELS[p.loanType]}
                    </button>
                  </td>
                  <td className="px-4 py-3 text-[#64748B]">
                    {p.installmentNumber}. taksit
                  </td>
                  <td className="px-4 py-3 text-[#64748B]">{formatDate(p.dueDate)}</td>
                  <td className="px-4 py-3 text-[#64748B]">{formatDate(p.paymentDate)}</td>
                  <td className="px-4 py-3 font-semibold text-[#0F172A]">{formatCurrency(p.paymentAmount)}</td>
                  <td className="px-4 py-3">
                    <OnTimeBadge dueDate={p.dueDate} paymentDate={p.paymentDate} />
                  </td>
                  <td className="px-4 py-3"><PaymentStatusBadge status={p.status} /></td>
                  <td className="px-4 py-3">
                    <button
                      onClick={() => navigate(`/loans/${p.loanId}`)}
                      className="text-xs text-[#64748B] hover:text-[#1B4FD8] transition-colors"
                    >
                      Krediye git →
                    </button>
                  </td>
                </tr>
              ))}
              {filtered.length === 0 && (
                <tr>
                  <td colSpan={10} className="px-4 py-8 text-center text-[#64748B]">
                    {selectedCustomerId !== null
                      ? 'Bu müşteriye ait ödeme kaydı bulunamadı.'
                      : 'Henüz ödeme yok.'}
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        )}
      </Card>
    </PageLayout>
  );
}
