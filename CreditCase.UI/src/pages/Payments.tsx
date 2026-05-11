import { useEffect, useState } from 'react';
import { paymentService } from '../services/api/paymentService';
import type { PaymentResponse } from '../types';
import { PaymentStatus } from '../types';
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

export function Payments() {
  const [payments, setPayments] = useState<PaymentResponse[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    paymentService.getAll()
      .then(setPayments)
      .finally(() => setLoading(false));
  }, []);

  return (
    <PageLayout title="Ödeme Geçmişi">
      <Card>
        {loading ? <Spinner /> : (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-[#E2E8F0] text-left text-[#64748B]">
                {['#', 'Taksit ID', 'Ödeme Tutarı', 'Ödeme Tarihi', 'Durum'].map(h => (
                  <th key={h} className="px-4 py-3 font-medium">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {payments.map(p => (
                <tr key={p.id} className="border-b border-[#E2E8F0] last:border-0 hover:bg-[#F8FAFC]">
                  <td className="px-4 py-3 text-[#64748B]">{p.id}</td>
                  <td className="px-4 py-3 text-[#64748B]">#{p.installmentId}</td>
                  <td className="px-4 py-3 font-medium">{formatCurrency(p.paymentAmount)}</td>
                  <td className="px-4 py-3 text-[#64748B]">{formatDate(p.paymentDate)}</td>
                  <td className="px-4 py-3"><PaymentStatusBadge status={p.status} /></td>
                </tr>
              ))}
              {payments.length === 0 && (
                <tr>
                  <td colSpan={5} className="px-4 py-8 text-center text-[#64748B]">Henüz ödeme yok.</td>
                </tr>
              )}
            </tbody>
          </table>
        )}
      </Card>
    </PageLayout>
  );
}
