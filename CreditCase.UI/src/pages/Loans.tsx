import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import { loanService } from '../services/api/loanService';
import { customerService } from '../services/api/customerService';
import { loanEvaluationService } from '../services/api/loanEvaluationService';
import type { LoanResponse, CustomerResponse, CreateLoanRequest, LoanEvaluationResponse } from '../types';
import { LoanType, RiskCategory, LOAN_TYPE_LABELS, RISK_CATEGORY_LABELS } from '../types';
import { PageLayout } from '../components/layout/PageLayout';
import { Button } from '../components/ui/Button';
import { Card } from '../components/ui/Card';
import { Modal } from '../components/ui/Modal';
import { LoanStatusBadge } from '../components/ui/Badge';
import { Spinner } from '../components/ui/Spinner';
import { formatCurrency, formatDate } from '../utils/formatters';

// ── Vade seçenekleri ve tutara göre kısıtlama ────────────────────────────────

const TERM_OPTIONS = [3, 6, 12, 24, 36, 48, 60, 84, 120];

// Backend CreateLoanRequestValidator.GetMaxTermForAmount ile eşdeğer.
const getMaxTermForAmount = (amount: number): number => {
  if (amount <= 10_000) return 24;
  if (amount <= 50_000) return 60;
  if (amount <= 150_000) return 84;
  return 120;
};

// ── Risk badge ────────────────────────────────────────────────────────────────

const RISK_COLORS: Record<RiskCategory, string> = {
  [RiskCategory.Low]:      'bg-green-100 text-green-700',
  [RiskCategory.Medium]:   'bg-amber-100 text-amber-700',
  [RiskCategory.High]:     'bg-orange-100 text-orange-700',
  [RiskCategory.VeryHigh]: 'bg-red-100 text-red-700',
};

// ── Kredi oluşturma formu ─────────────────────────────────────────────────────

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
    principalAmount: 10000,
    term: 12,
    startDate: today,
    isBalloonPayment: false,
  });

  const [maxEligibility, setMaxEligibility] = useState<LoanEvaluationResponse | null>(null);
  const [eligibilityLoading, setEligibilityLoading] = useState(false);
  const [evaluation, setEvaluation] = useState<LoanEvaluationResponse | null>(null);
  const [evaluating, setEvaluating] = useState(false);

  // Müşteri seçilince maksimum uygunluğu getir
  useEffect(() => {
    if (!form.customerId) return;
    let cancelled = false;
    setMaxEligibility(null);
    setEvaluation(null);
    setEligibilityLoading(true);
    loanEvaluationService.getMaximumEligibility(form.customerId)
      .then(r => { if (!cancelled) setMaxEligibility(r); })
      .catch(() => {})
      .finally(() => { if (!cancelled) setEligibilityLoading(false); });
    return () => { cancelled = true; };
  }, [form.customerId]);

  // Form değişince önceki değerlendirmeyi sıfırla
  const handleChange = (field: keyof CreateLoanRequest, value: unknown) => {
    setForm(prev => {
      const next = { ...prev, [field]: value } as CreateLoanRequest;
      if (field === 'loanType' && Number(value) !== LoanType.Vehicle) {
        next.isBalloonPayment = false;
      }
      // Tutar değişince mevcut vade kısıt dışına çıkmış olabilir — sıfırla.
      if (field === 'principalAmount') {
        const maxTerm = getMaxTermForAmount(Number(value));
        if (next.term > maxTerm) {
          next.term = TERM_OPTIONS.filter(t => t <= maxTerm).at(-1) ?? 12;
        }
      }
      return next;
    });
    setEvaluation(null);
  };

  // Değerlendirme çalıştır
  const handleEvaluate = async () => {
    setEvaluating(true);
    try {
      const result = await loanEvaluationService.evaluate({
        customerId: form.customerId,
        loanType: form.loanType,
        requestedAmount: form.principalAmount,
        requestedTerm: form.term,
      });
      setEvaluation(result);
      if (!result.isApproved) {
        toast.error(result.rejectionReason ?? 'Kredi başvurusu reddedildi.');
      }
    } finally {
      setEvaluating(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await onSubmit(form);
  };

  const maxAmount = maxEligibility?.maximumAmount ?? 0;
  const isOverMax = maxAmount > 0 && form.principalAmount > maxAmount;
  const canCreate = evaluation?.isApproved && !isOverMax;

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {/* Müşteri */}
      <div>
        <label className="block text-sm font-medium text-[#0F172A] mb-1">Müşteri</label>
        <select
          value={form.customerId}
          onChange={e => handleChange('customerId', Number(e.target.value))}
          className="w-full border border-[#E2E8F0] rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-[#1B4FD8]/30"
        >
          {customers.map(c => (
            <option key={c.id} value={c.id}>{c.firstName} {c.lastName}</option>
          ))}
        </select>
      </div>

      {/* Kredi türü */}
      <div>
        <label className="block text-sm font-medium text-[#0F172A] mb-1">Kredi Türü</label>
        <select
          value={form.loanType}
          onChange={e => handleChange('loanType', Number(e.target.value))}
          className="w-full border border-[#E2E8F0] rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-[#1B4FD8]/30"
        >
          {Object.entries(LOAN_TYPE_LABELS).map(([k, v]) => (
            <option key={k} value={k}>{v}</option>
          ))}
        </select>
      </div>

      {/* Tutar */}
      <div>
        <label className="block text-sm font-medium text-[#0F172A] mb-1">
          Kredi Tutarı (₺)
          {eligibilityLoading && <span className="ml-2 text-xs text-[#64748B]">Uygunluk hesaplanıyor…</span>}
          {maxAmount > 0 && !eligibilityLoading && (
            <span className="ml-2 text-xs text-[#64748B]">
              Maks. uygun: <span className="font-semibold text-[#1B4FD8]">{formatCurrency(maxAmount)}</span>
            </span>
          )}
        </label>
        <input
          type="number"
          min={1000}
          max={maxAmount > 0 ? maxAmount : 1_000_000}
          step={1000}
          value={form.principalAmount}
          onChange={e => handleChange('principalAmount', Number(e.target.value))}
          required
          className={`w-full border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 ${
            isOverMax
              ? 'border-red-400 focus:ring-red-300 bg-red-50'
              : 'border-[#E2E8F0] focus:ring-[#1B4FD8]/30'
          }`}
        />
        {isOverMax && (
          <p className="mt-1 text-xs text-red-600">
            Girilen tutar maksimum uygun miktarı ({formatCurrency(maxAmount)}) aşıyor.
          </p>
        )}
        <p className="mt-1 text-xs text-[#64748B]">Min: {formatCurrency(1000)} · Max: {maxAmount > 0 ? formatCurrency(maxAmount) : formatCurrency(1_000_000)}</p>
      </div>

      {/* Vade */}
      <div>
        <label className="block text-sm font-medium text-[#0F172A] mb-1">
          Vade
          <span className="ml-2 text-xs text-[#64748B]">
            (bu tutar için maks. {getMaxTermForAmount(form.principalAmount)} ay)
          </span>
        </label>
        <div className="flex flex-wrap gap-2">
          {TERM_OPTIONS.filter(t => t <= getMaxTermForAmount(form.principalAmount)).map(t => (
            <button
              key={t}
              type="button"
              onClick={() => handleChange('term', t)}
              className={`px-3 py-1.5 rounded-lg text-sm font-medium border transition-colors ${
                form.term === t
                  ? 'bg-[#1B4FD8] text-white border-[#1B4FD8]'
                  : 'bg-white text-[#64748B] border-[#E2E8F0] hover:border-[#1B4FD8]/50'
              }`}
            >
              {t < 12 ? `${t} ay` : t === 12 ? '1 yıl' : t % 12 === 0 ? `${t / 12} yıl` : `${t} ay`}
            </button>
          ))}
        </div>
      </div>

      {/* Başlangıç tarihi */}
      <div>
        <label className="block text-sm font-medium text-[#0F172A] mb-1">Başlangıç Tarihi</label>
        <input
          type="date"
          value={form.startDate}
          onChange={e => handleChange('startDate', e.target.value)}
          required
          className="w-full border border-[#E2E8F0] rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-[#1B4FD8]/30"
        />
      </div>

      {/* Balon ödeme — yalnızca Taşıt kredisinde aktif */}
      {form.loanType === LoanType.Vehicle && (
        <label className="flex items-center gap-2 cursor-pointer select-none">
          <input
            type="checkbox"
            checked={form.isBalloonPayment}
            onChange={e => handleChange('isBalloonPayment', e.target.checked)}
            className="w-4 h-4 rounded border-[#E2E8F0] text-[#1B4FD8]"
          />
          <span className="text-sm font-medium text-[#0F172A]">Balon Ödeme</span>
          <span className="text-xs text-[#64748B]">(son taksit yüksek, öncekiler düşük)</span>
        </label>
      )}

      {/* Değerlendirme sonucu */}
      {evaluation && (
        <div className={`rounded-lg p-3 border text-sm space-y-2 ${evaluation.isApproved ? 'bg-green-50 border-green-200' : 'bg-red-50 border-red-200'}`}>
          <div className="flex items-center justify-between">
            <span className={`font-semibold ${evaluation.isApproved ? 'text-green-700' : 'text-red-700'}`}>
              {evaluation.isApproved ? 'Onaylandı' : 'Reddedildi'}
            </span>
            <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${RISK_COLORS[evaluation.riskLevel]}`}>
              {RISK_CATEGORY_LABELS[evaluation.riskLevel]}
            </span>
          </div>
          {evaluation.isApproved && (
            <div className="grid grid-cols-3 gap-2">
              <div>
                <p className="text-[#64748B] text-xs">Faiz Oranı</p>
                <p className="font-semibold text-[#0F172A]">%{evaluation.approvedInterestRate}</p>
              </div>
              <div>
                <p className="text-[#64748B] text-xs">Aylık Taksit</p>
                <p className="font-semibold text-[#1B4FD8]">{formatCurrency(evaluation.monthlyInstallmentEstimate)}</p>
              </div>
              <div>
                <p className="text-[#64748B] text-xs">Kredi Skoru</p>
                <p className="font-semibold text-[#0F172A]">{evaluation.creditScore}</p>
              </div>
            </div>
          )}
          {!evaluation.isApproved && evaluation.rejectionReason && (
            <p className="text-xs text-red-600">{evaluation.rejectionReason}</p>
          )}
        </div>
      )}

      <div className="flex justify-end gap-2 pt-2">
        <Button type="button" variant="secondary" onClick={onClose}>İptal</Button>
        <Button
          type="button"
          variant="secondary"
          loading={evaluating}
          disabled={isOverMax}
          onClick={handleEvaluate}
        >
          Değerlendir
        </Button>
        <Button
          type="submit"
          loading={loading}
          disabled={!canCreate}
        >
          Kredi Oluştur
        </Button>
      </div>
    </form>
  );
}

// ── Ana sayfa ─────────────────────────────────────────────────────────────────

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
                {['#', 'Müşteri', 'Tür', 'Ana Para', 'Toplam Ödenecek', 'Faiz', 'Vade', 'Başlangıç', 'Kalan', 'Durum', ''].map(h => (
                  <th key={h} className="px-4 py-3 font-medium">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {loans.map(l => {
                const customer = customers.find(c => c.id === l.customerId);
                return (
                  <tr key={l.id} className="border-b border-[#E2E8F0] last:border-0 hover:bg-[#F8FAFC]">
                    <td className="px-4 py-3 text-[#64748B] font-mono text-xs">{l.id}</td>
                    <td className="px-4 py-3">
                      <button
                        onClick={() => navigate(`/customers/${l.customerId}`)}
                        className="text-left group"
                      >
                        <p className="font-medium text-[#0F172A] group-hover:text-[#1B4FD8] transition-colors">
                          {customer ? `${customer.firstName} ${customer.lastName}` : `#${l.customerId}`}
                        </p>
                        {customer && (
                          <p className="text-xs text-[#64748B]">{customer.identityNumber}</p>
                        )}
                      </button>
                    </td>
                    <td className="px-4 py-3">{LOAN_TYPE_LABELS[l.loanType]}</td>
                    <td className="px-4 py-3 font-medium">{formatCurrency(l.principalAmount)}</td>
                    <td className="px-4 py-3 font-semibold text-[#1B4FD8]">{formatCurrency(l.totalPayableAmount)}</td>
                    <td className="px-4 py-3 text-[#64748B]">%{l.interestRate}</td>
                    <td className="px-4 py-3">{l.term < 12 ? `${l.term} ay` : l.term % 12 === 0 ? `${l.term / 12} yıl` : `${l.term} ay`}</td>
                    <td className="px-4 py-3 text-[#64748B]">{formatDate(l.startDate)}</td>
                    <td className="px-4 py-3 font-medium text-[#1B4FD8]">{formatCurrency(l.remainingPrincipal)}</td>
                    <td className="px-4 py-3"><LoanStatusBadge status={l.status} /></td>
                    <td className="px-4 py-3">
                      <Button size="sm" variant="ghost" onClick={() => navigate(`/loans/${l.id}`)}>Taksitler →</Button>
                    </td>
                  </tr>
                );
              })}
              {loans.length === 0 && (
                <tr><td colSpan={11} className="px-4 py-8 text-center text-[#64748B]">Henüz kredi yok.</td></tr>
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
