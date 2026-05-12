import { useEffect, useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { customerService } from '../services/api/customerService';
import { loanService } from '../services/api/loanService';
import { loanEvaluationService } from '../services/api/loanEvaluationService';
import type {
  CustomerSummaryResponse, LoanResponse,
  LoanEvaluationResponse, LoanApplicationRequest,
} from '../types';
import {
  LoanStatus, LoanType, RiskCategory,
  LOAN_TYPE_LABELS, RISK_CATEGORY_LABELS,
  SCORE_CATEGORY_LABELS, SCORE_CATEGORY_COLORS,
} from '../types';

const TERM_OPTIONS = [6, 12, 18, 24, 36, 48, 60, 72];
import { PageLayout } from '../components/layout/PageLayout';
import { StatCard } from '../components/ui/Card';
import { LoanStatusBadge } from '../components/ui/Badge';
import { Button } from '../components/ui/Button';
import { Modal } from '../components/ui/Modal';
import { Spinner } from '../components/ui/Spinner';
import { formatCurrency, formatDate } from '../utils/formatters';

// ── Risk badge ────────────────────────────────────────────────────────────────

const RISK_COLORS: Record<RiskCategory, string> = {
  [RiskCategory.Low]:      'bg-green-100 text-green-700',
  [RiskCategory.Medium]:   'bg-amber-100 text-amber-700',
  [RiskCategory.High]:     'bg-orange-100 text-orange-700',
  [RiskCategory.VeryHigh]: 'bg-red-100 text-red-700',
};

function RiskBadge({ level }: { level: RiskCategory }) {
  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${RISK_COLORS[level]}`}>
      {RISK_CATEGORY_LABELS[level]}
    </span>
  );
}

// ── Evaluation form ───────────────────────────────────────────────────────────

function EvaluationForm({
  customerId,
  onResult,
  onClose,
}: {
  customerId: number;
  onResult: (r: LoanEvaluationResponse) => void;
  onClose: () => void;
}) {
  const [form, setForm] = useState<LoanApplicationRequest>({
    customerId,
    loanType: LoanType.Personal,
    requestedAmount: 10000,
    requestedTerm: 12,
  });
  const [loading, setLoading] = useState(false);

  const set = (field: keyof LoanApplicationRequest) =>
    (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) =>
      setForm(prev => ({ ...prev, [field]: Number(e.target.value) }));

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      const result = await loanEvaluationService.evaluate(form);
      onResult(result);
    } finally {
      setLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div>
        <label className="block text-sm font-medium text-[#0F172A] mb-1">Kredi Türü</label>
        <select value={form.loanType} onChange={set('loanType')}
          className="w-full border border-[#E2E8F0] rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-[#1B4FD8]/30">
          {Object.entries(LOAN_TYPE_LABELS).map(([k, v]) => (
            <option key={k} value={k}>{v}</option>
          ))}
        </select>
      </div>
      <div>
        <label className="block text-sm font-medium text-[#0F172A] mb-1">İstenen Tutar (₺)</label>
        <input type="number" min={1000} max={1000000} step={1000} value={form.requestedAmount}
          onChange={set('requestedAmount')} required
          className="w-full border border-[#E2E8F0] rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-[#1B4FD8]/30" />
        <p className="mt-1 text-xs text-[#64748B]">Min: 1.000 ₺ · Max: 1.000.000 ₺</p>
      </div>
      <div>
        <label className="block text-sm font-medium text-[#0F172A] mb-1">Vade</label>
        <div className="flex flex-wrap gap-2">
          {TERM_OPTIONS.map(t => (
            <button
              key={t}
              type="button"
              onClick={() => setForm(prev => ({ ...prev, requestedTerm: t }))}
              className={`px-3 py-1.5 rounded-lg text-sm font-medium border transition-colors ${
                form.requestedTerm === t
                  ? 'bg-[#1B4FD8] text-white border-[#1B4FD8]'
                  : 'bg-white text-[#64748B] border-[#E2E8F0] hover:border-[#1B4FD8]/50'
              }`}
            >
              {t < 12 ? `${t} ay` : t === 12 ? '1 yıl' : t % 12 === 0 ? `${t / 12} yıl` : `${t} ay`}
            </button>
          ))}
        </div>
      </div>
      <div className="flex justify-end gap-2 pt-2">
        <Button type="button" variant="secondary" onClick={onClose}>İptal</Button>
        <Button type="submit" loading={loading}>Değerlendir</Button>
      </div>
    </form>
  );
}

// ── Evaluation result panel ───────────────────────────────────────────────────

function EvaluationResult({ result, onClose }: { result: LoanEvaluationResponse; onClose: () => void }) {
  return (
    <div className="space-y-4">
      {/* Karar başlığı */}
      <div className={`rounded-lg px-4 py-3 flex items-center justify-between ${result.isApproved ? 'bg-green-50 border border-green-200' : 'bg-red-50 border border-red-200'}`}>
        <div>
          <p className={`font-semibold ${result.isApproved ? 'text-green-700' : 'text-red-700'}`}>
            {result.isApproved ? 'Kredi Onaylandı' : 'Kredi Reddedildi'}
          </p>
          {result.rejectionReason && (
            <p className="text-xs text-red-600 mt-0.5">{result.rejectionReason}</p>
          )}
        </div>
        <div className="flex items-center gap-2">
          <span
            className="text-xs font-semibold px-2 py-1 rounded-full text-white"
            style={{ backgroundColor: SCORE_CATEGORY_COLORS[result.creditScoreCategory] }}
          >
            {SCORE_CATEGORY_LABELS[result.creditScoreCategory]}
          </span>
          <RiskBadge level={result.riskLevel} />
        </div>
      </div>

      {/* Sayısal değerler */}
      <div className="grid grid-cols-2 gap-3 text-sm">
        <Row label="Kredi Skoru" value={result.creditScore.toString()} />
        <Row label="Borç/Gelir Oranı" value={`%${(result.debtToIncomeRatio * 100).toFixed(1)}`} />
        {result.isApproved && (
          <>
            <Row label="Onaylanan Tutar" value={formatCurrency(result.approvedAmount)} highlight />
            <Row label="Vade Oranı" value={`${result.approvedRateAmount}`} highlight />
            <Row label="Maks. Tutar" value={formatCurrency(result.maximumAmount)} />
            <Row label="Maks. Vade" value={`${result.maximumTerm} ay`} />
            <Row label="Tahmini Taksit" value={formatCurrency(result.monthlyInstallmentEstimate)} />
            <Row label="Geçerlilik" value={formatDate(result.expirationDate)} />
          </>
        )}
      </div>

      <div className="flex justify-end pt-2">
        <Button variant="secondary" onClick={onClose}>Kapat</Button>
      </div>
    </div>
  );
}

function Row({ label, value, highlight }: { label: string; value: string; highlight?: boolean }) {
  return (
    <div className="bg-[#F8FAFC] rounded-lg px-3 py-2">
      <p className="text-xs text-[#64748B]">{label}</p>
      <p className={`font-semibold mt-0.5 ${highlight ? 'text-[#1B4FD8]' : 'text-[#0F172A]'}`}>{value}</p>
    </div>
  );
}

// ── Ana sayfa ─────────────────────────────────────────────────────────────────

export function CustomerDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const customerId = Number(id);

  const [summary, setSummary] = useState<CustomerSummaryResponse | null>(null);
  const [loans, setLoans] = useState<LoanResponse[]>([]);
  const [evaluations, setEvaluations] = useState<LoanEvaluationResponse[]>([]);
  const [creditProfile, setCreditProfile] = useState<LoanEvaluationResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [evalModal, setEvalModal] = useState<'form' | 'result' | null>(null);
  const [evalResult, setEvalResult] = useState<LoanEvaluationResponse | null>(null);

  useEffect(() => {
    const load = async () => {
      try {
        const [s, all, evals] = await Promise.all([
          customerService.getSummary(customerId),
          loanService.getAll(),
          loanEvaluationService.getByCustomer(customerId),
        ]);
        setSummary(s);
        setLoans(all.filter(l => l.customerId === customerId));
        setEvaluations(evals);

        // Kredi notu ve risk kategorisi için maksimum uygunluk sorgusu
        loanEvaluationService.getMaximumEligibility(customerId)
          .then(setCreditProfile)
          .catch(() => {});
      } catch {
        navigate('/customers');
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [customerId, navigate]);

  const handleEvalResult = (result: LoanEvaluationResponse) => {
    setEvalResult(result);
    setEvalModal('result');
    setEvaluations(prev => [result, ...prev]);
  };

  if (loading) return <PageLayout title="Müşteri Detayı"><Spinner /></PageLayout>;
  if (!summary) return null;

  const activeLoans = loans.filter(l => l.status === LoanStatus.Active).length;

  return (
    <PageLayout
      title={summary.fullName}
      action={
        <div className="flex gap-2">
          <Button onClick={() => setEvalModal('form')}>+ Kredi Değerlendirmesi</Button>
          <Button variant="secondary" onClick={() => navigate('/customers')}>← Geri</Button>
        </div>
      }
    >
      {/* Borç özeti kartları */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-4">
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

      {/* Kredi notu ve risk profili */}
      <div className="bg-white rounded-xl border border-[#E2E8F0] shadow-sm px-5 py-4 mb-6">
        {creditProfile ? (
          <div className="flex flex-wrap items-center gap-6">
            {/* Skor göstergesi */}
            <div className="flex items-center gap-3">
              <div className="relative w-14 h-14 flex-shrink-0">
                <svg viewBox="0 0 36 36" className="w-14 h-14 -rotate-90">
                  <circle cx="18" cy="18" r="15.9" fill="none" stroke="#E2E8F0" strokeWidth="3" />
                  <circle
                    cx="18" cy="18" r="15.9" fill="none"
                    stroke={SCORE_CATEGORY_COLORS[creditProfile.creditScoreCategory]}
                    strokeWidth="3"
                    strokeDasharray={`${(creditProfile.creditScore / 1900) * 100} 100`}
                    strokeLinecap="round"
                  />
                </svg>
                <span className="absolute inset-0 flex items-center justify-center text-xs font-bold text-[#0F172A]">
                  {creditProfile.creditScore}
                </span>
              </div>
              <div>
                <p className="text-xs text-[#64748B]">Kredi Notu</p>
                <p className="text-lg font-bold text-[#0F172A]">{creditProfile.creditScore} <span className="text-xs font-normal text-[#64748B]">/ 1900</span></p>
              </div>
            </div>

            <div className="w-px h-10 bg-[#E2E8F0] hidden sm:block" />

            {/* Risk kategorisi */}
            <div>
              <p className="text-xs text-[#64748B] mb-1">Risk Kategorisi</p>
              <RiskBadge level={creditProfile.riskLevel} />
            </div>

            <div className="w-px h-10 bg-[#E2E8F0] hidden sm:block" />

            {/* Maks. uygun tutar */}
            <div>
              <p className="text-xs text-[#64748B]">Maks. Uygun Kredi</p>
              <p className="font-semibold text-[#1B4FD8]">{formatCurrency(creditProfile.maximumAmount)}</p>
            </div>

            {/* Skor açıklaması */}
            <div className="ml-auto text-xs text-[#64748B] hidden lg:block">
              <span className="inline-block w-2 h-2 rounded-full mr-1" style={{ backgroundColor: SCORE_CATEGORY_COLORS[4] }} />1720+ Prestijli &nbsp;
              <span className="inline-block w-2 h-2 rounded-full mr-1" style={{ backgroundColor: SCORE_CATEGORY_COLORS[3] }} />1470–1719 Güvenli &nbsp;
              <span className="inline-block w-2 h-2 rounded-full mr-1" style={{ backgroundColor: SCORE_CATEGORY_COLORS[2] }} />1150–1469 Dengeli &nbsp;
              <span className="inline-block w-2 h-2 rounded-full mr-1" style={{ backgroundColor: SCORE_CATEGORY_COLORS[1] }} />970–1149 Gelişime Açık &nbsp;
              <span className="inline-block w-2 h-2 rounded-full mr-1" style={{ backgroundColor: SCORE_CATEGORY_COLORS[0] }} />&lt;970 Kritik
            </div>
          </div>
        ) : (
          <div className="flex items-center gap-3 text-sm text-[#64748B]">
            <div className="w-4 h-4 border-2 border-[#1B4FD8] border-t-transparent rounded-full animate-spin" />
            Kredi notu hesaplanıyor…
          </div>
        )}
      </div>

      {/* Kredi listesi */}
      <h2 className="text-sm font-semibold text-[#64748B] uppercase tracking-wide mb-3">Krediler</h2>
      <div className="bg-white rounded-xl border border-[#E2E8F0] shadow-sm overflow-hidden mb-6">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-[#E2E8F0] text-left text-[#64748B]">
              {['Tür', 'Ana Para', 'Vade Oranı', 'Vade', 'Kalan', 'Durum', ''].map(h => (
                <th key={h} className="px-4 py-3 font-medium">{h}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {loans.map(l => (
              <tr key={l.id} className="border-b border-[#E2E8F0] last:border-0 hover:bg-[#F8FAFC]">
                <td className="px-4 py-3 font-medium">{LOAN_TYPE_LABELS[l.loanType]}</td>
                <td className="px-4 py-3">{formatCurrency(l.principalAmount)}</td>
                <td className="px-4 py-3">{l.rateAmount}</td>
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

      {/* Değerlendirme geçmişi */}
      {evaluations.length > 0 && (
        <>
          <h2 className="text-sm font-semibold text-[#64748B] uppercase tracking-wide mb-3">Kredi Değerlendirmeleri</h2>
          <div className="bg-white rounded-xl border border-[#E2E8F0] shadow-sm overflow-hidden">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-[#E2E8F0] text-left text-[#64748B]">
                  {['Tarih', 'Tür', 'İstenen', 'Onaylanan', 'Vade Oranı', 'Skor', 'Risk', 'Karar'].map(h => (
                    <th key={h} className="px-4 py-3 font-medium">{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {evaluations.map(ev => (
                  <tr key={ev.id} className="border-b border-[#E2E8F0] last:border-0 hover:bg-[#F8FAFC]">
                    <td className="px-4 py-3 text-[#64748B]">{formatDate(ev.evaluationDate)}</td>
                    <td className="px-4 py-3">{LOAN_TYPE_LABELS[ev.requestedLoanType]}</td>
                    <td className="px-4 py-3">{formatCurrency(ev.requestedAmount)}</td>
                    <td className="px-4 py-3 font-medium">{ev.isApproved ? formatCurrency(ev.approvedAmount) : '—'}</td>
                    <td className="px-4 py-3">{ev.isApproved ? ev.approvedRateAmount : '—'}</td>
                    <td className="px-4 py-3">{ev.creditScore}</td>
                    <td className="px-4 py-3"><RiskBadge level={ev.riskLevel} /></td>
                    <td className="px-4 py-3">
                      <span className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-semibold ${ev.isApproved ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
                        {ev.isApproved ? 'Onaylandı' : 'Reddedildi'}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}

      {/* Değerlendirme formu modal */}
      <Modal open={evalModal === 'form'} onClose={() => setEvalModal(null)} title="Kredi Değerlendirmesi">
        <EvaluationForm
          customerId={customerId}
          onResult={handleEvalResult}
          onClose={() => setEvalModal(null)}
        />
      </Modal>

      {/* Değerlendirme sonucu modal */}
      <Modal open={evalModal === 'result'} onClose={() => setEvalModal(null)} title="Değerlendirme Sonucu">
        {evalResult && (
          <EvaluationResult result={evalResult} onClose={() => setEvalModal(null)} />
        )}
      </Modal>
    </PageLayout>
  );
}
