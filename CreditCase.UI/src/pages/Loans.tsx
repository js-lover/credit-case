import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import { loanService } from '../services/api/loanService';
import { customerService } from '../services/api/customerService';
import type { LoanResponse, CustomerResponse, CreateLoanRequest } from '../types';
import { LoanType, LOAN_TYPE_LABELS } from '../types';
import { PageLayout } from '../components/layout/PageLayout';
import { Button } from '../components/ui/Button';
import { Card } from '../components/ui/Card';
import { Modal } from '../components/ui/Modal';
import { LoanStatusBadge } from '../components/ui/Badge';
import { Spinner } from '../components/ui/Spinner';
import { formatCurrency, formatDate } from '../utils/formatters';

function CreateLoanForm({ customers, onSubmit, onClose, loading }: {
  customers: CustomerResponse[];
  onSubmit: (data: CreateLoanRequest) => Promise<void>;
  onClose: () => void;
  loading: boolean;
}) {
  const today = new Date().toISOString().split('T')[0];
  const [form, setForm] = useState<CreateLoanRequest>({
    customerId: customers[0]?.id ?? 0,
    loanType: LoanType.Personal,
    principalAmount: 0,
    interestRate: 0,
    term: 12,
    startDate: today,
  });

  const set = (field: keyof CreateLoanRequest) => (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const raw = e.target.value;
    const val = (e.target.type === 'number' || e.target.tagName === 'SELECT') ? Number(raw) : raw;
    setForm(prev => ({ ...prev, [field]: val }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await onSubmit(form);
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {/* Müşteri */}
      <div>
        <label className="block text-sm font-medium text-[#0F172A] mb-1">Müşteri</label>
        <select value={form.customerId} onChange={set('customerId')}
          className="w-full border border-[#E2E8F0] rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-[#1B4FD8]/30">
          {customers.map(c => (
            <option key={c.id} value={c.id}>{c.firstName} {c.lastName}</option>
          ))}
        </select>
      </div>

      {/* Kredi türü */}
      <div>
        <label className="block text-sm font-medium text-[#0F172A] mb-1">Kredi Türü</label>
        <select value={form.loanType} onChange={set('loanType')}
          className="w-full border border-[#E2E8F0] rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-[#1B4FD8]/30">
          {Object.entries(LOAN_TYPE_LABELS).map(([k, v]) => (
            <option key={k} value={k}>{v}</option>
          ))}
        </select>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div>
          <label className="block text-sm font-medium text-[#0F172A] mb-1">Ana Para (₺)</label>
          <input type="number" min={1} step={0.01} value={form.principalAmount} onChange={set('principalAmount')} required
            className="w-full border border-[#E2E8F0] rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-[#1B4FD8]/30" />
        </div>
        <div>
          <label className="block text-sm font-medium text-[#0F172A] mb-1">Faiz Oranı (%)</label>
          <input type="number" min={0} step={0.01} value={form.interestRate} onChange={set('interestRate')} required
            className="w-full border border-[#E2E8F0] rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-[#1B4FD8]/30" />
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div>
          <label className="block text-sm font-medium text-[#0F172A] mb-1">Vade (Ay)</label>
          <input type="number" min={1} value={form.term} onChange={set('term')} required
            className="w-full border border-[#E2E8F0] rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-[#1B4FD8]/30" />
        </div>
        <div>
          <label className="block text-sm font-medium text-[#0F172A] mb-1">Başlangıç Tarihi</label>
          <input type="date" value={form.startDate} onChange={set('startDate')} required
            className="w-full border border-[#E2E8F0] rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-[#1B4FD8]/30" />
        </div>
      </div>

      <div className="flex justify-end gap-2 pt-2">
        <Button type="button" variant="secondary" onClick={onClose}>İptal</Button>
        <Button type="submit" loading={loading}>Kredi Oluştur</Button>
      </div>
    </form>
  );
}

export function Loans() {
  const [loans, setLoans] = useState<LoanResponse[]>([]);
  const [customers, setCustomers] = useState<CustomerResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [modal, setModal] = useState(false);
  const [saving, setSaving] = useState(false);
  const navigate = useNavigate();

  const load = async () => {
    try {
      setLoading(true);
      const [l, c] = await Promise.all([loanService.getAll(), customerService.getAll()]);
      setLoans(l);
      setCustomers(c);
    } finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  const customerName = (id: number) => {
    const c = customers.find(x => x.id === id);
    return c ? `${c.firstName} ${c.lastName}` : `#${id}`;
  };

  const handleCreate = async (data: CreateLoanRequest) => {
    setSaving(true);
    try {
      const loan = await loanService.create(data);
      toast.success('Kredi oluşturuldu. Taksit planı hazır.');
      setModal(false);
      navigate(`/loans/${loan.id}`);
    } finally { setSaving(false); }
  };

  return (
    <PageLayout
      title="Krediler"
      action={
        customers.length > 0
          ? <Button onClick={() => setModal(true)}>+ Yeni Kredi</Button>
          : undefined
      }
    >
      <Card>
        {loading ? <Spinner /> : (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-[#E2E8F0] text-left text-[#64748B]">
                {['Müşteri', 'Tür', 'Ana Para', 'Faiz', 'Vade', 'Başlangıç', 'Kalan', 'Durum', ''].map(h => (
                  <th key={h} className="px-4 py-3 font-medium">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {loans.map(l => (
                <tr key={l.id} className="border-b border-[#E2E8F0] last:border-0 hover:bg-[#F8FAFC]">
                  <td className="px-4 py-3 font-medium">{customerName(l.customerId)}</td>
                  <td className="px-4 py-3">{LOAN_TYPE_LABELS[l.loanType]}</td>
                  <td className="px-4 py-3">{formatCurrency(l.principalAmount)}</td>
                  <td className="px-4 py-3">%{l.interestRate}</td>
                  <td className="px-4 py-3">{l.term} ay</td>
                  <td className="px-4 py-3 text-[#64748B]">{formatDate(l.startDate)}</td>
                  <td className="px-4 py-3 font-medium text-[#1B4FD8]">{formatCurrency(l.remainingPrincipal)}</td>
                  <td className="px-4 py-3"><LoanStatusBadge status={l.status} /></td>
                  <td className="px-4 py-3">
                    <Button size="sm" variant="ghost" onClick={() => navigate(`/loans/${l.id}`)}>Taksitler →</Button>
                  </td>
                </tr>
              ))}
              {loans.length === 0 && (
                <tr><td colSpan={9} className="px-4 py-8 text-center text-[#64748B]">Henüz kredi yok.</td></tr>
              )}
            </tbody>
          </table>
        )}
      </Card>

      <Modal open={modal} onClose={() => setModal(false)} title="Yeni Kredi Oluştur">
        <CreateLoanForm
          customers={customers}
          onSubmit={handleCreate}
          onClose={() => setModal(false)}
          loading={saving}
        />
      </Modal>
    </PageLayout>
  );
}
