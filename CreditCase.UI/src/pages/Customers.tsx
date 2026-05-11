import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import { customerService } from '../services/api/customerService';
import type { CustomerResponse, CreateCustomerRequest, UpdateCustomerRequest } from '../types';
import { PageLayout } from '../components/layout/PageLayout';
import { Button } from '../components/ui/Button';
import { Card } from '../components/ui/Card';
import { Modal } from '../components/ui/Modal';
import { Spinner } from '../components/ui/Spinner';
import { formatDate } from '../utils/formatters';

// ── Form bileşeni ─────────────────────────────────────────────────────────────

interface CustomerFormProps {
  initial?: UpdateCustomerRequest & { identityNumber?: string };
  isEdit?: boolean;
  onSubmit: (data: CreateCustomerRequest | UpdateCustomerRequest) => Promise<void>;
  onClose: () => void;
  loading: boolean;
}

function CustomerForm({ initial, isEdit, onSubmit, onClose, loading }: CustomerFormProps) {
  const [form, setForm] = useState({
    firstName: initial?.firstName ?? '',
    lastName: initial?.lastName ?? '',
    identityNumber: initial?.identityNumber ?? '',
    email: initial?.email ?? '',
    phoneNumber: initial?.phoneNumber ?? '',
  });

  const set = (field: string) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setForm(prev => ({ ...prev, [field]: e.target.value }));

  // Sadece rakam kabul eden handler (TC ve telefon için)
  const setDigits = (field: string, maxLen: number) => (e: React.ChangeEvent<HTMLInputElement>) => {
    const digits = e.target.value.replace(/\D/g, '').slice(0, maxLen);
    setForm(prev => ({ ...prev, [field]: digits }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await onSubmit(isEdit
      ? { firstName: form.firstName, lastName: form.lastName, email: form.email, phoneNumber: form.phoneNumber }
      : form
    );
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="grid grid-cols-2 gap-3">
        <Field label="Ad" value={form.firstName} onChange={set('firstName')} required />
        <Field label="Soyad" value={form.lastName} onChange={set('lastName')} required />
      </div>
      {!isEdit && (
        <Field
          label="TC Kimlik No"
          value={form.identityNumber}
          onChange={setDigits('identityNumber', 11)}
          inputMode="numeric"
          minLength={11}
          maxLength={11}
          pattern="\d{11}"
          title="11 haneli rakamlardan oluşmalıdır"
          placeholder="xxxxxxxxxxx"
          required
        />
      )}
      <Field label="E-posta" type="email" value={form.email} onChange={set('email')} required />
      <Field
        label="Telefon"
        value={form.phoneNumber}
        onChange={setDigits('phoneNumber', 11)}
        inputMode="tel"
        minLength={10}
        maxLength={11}
        pattern="\d{10,11}"
        title="10 veya 11 haneli rakamlardan oluşmalıdır"
        placeholder="5xxxxxxxxx"
        required
      />
      <div className="flex justify-end gap-2 pt-2">
        <Button type="button" variant="secondary" onClick={onClose}>İptal</Button>
        <Button type="submit" loading={loading}>{isEdit ? 'Güncelle' : 'Oluştur'}</Button>
      </div>
    </form>
  );
}

function Field({ label, ...props }: { label: string } & React.InputHTMLAttributes<HTMLInputElement>) {
  return (
    <div>
      <label className="block text-sm font-medium text-[#0F172A] mb-1">{label}</label>
      <input {...props} className="w-full border border-[#E2E8F0] rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-[#1B4FD8]/30" />
    </div>
  );
}

// ── Ana sayfa ─────────────────────────────────────────────────────────────────

export function Customers() {
  const [customers, setCustomers] = useState<CustomerResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [modal, setModal] = useState<'create' | 'edit' | 'delete' | null>(null);
  const [selected, setSelected] = useState<CustomerResponse | null>(null);
  const [saving, setSaving] = useState(false);
  const navigate = useNavigate();

  const load = async () => {
    try {
      setLoading(true);
      setCustomers(await customerService.getAll());
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  const handleCreate = async (data: CreateCustomerRequest | UpdateCustomerRequest) => {
    setSaving(true);
    try {
      await customerService.create(data as CreateCustomerRequest);
      toast.success('Müşteri oluşturuldu.');
      setModal(null);
      load();
    } finally { setSaving(false); }
  };

  const handleEdit = async (data: CreateCustomerRequest | UpdateCustomerRequest) => {
    if (!selected) return;
    setSaving(true);
    try {
      await customerService.update(selected.id, data as UpdateCustomerRequest);
      toast.success('Müşteri güncellendi.');
      setModal(null);
      load();
    } finally { setSaving(false); }
  };

  const handleDelete = async () => {
    if (!selected) return;
    setSaving(true);
    try {
      await customerService.delete(selected.id);
      toast.success('Müşteri silindi.');
      setModal(null);
      load();
    } finally { setSaving(false); }
  };

  return (
    <PageLayout
      title="Müşteriler"
      action={<Button onClick={() => setModal('create')}>+ Yeni Müşteri</Button>}
    >
      <Card>
        {loading ? <Spinner /> : (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-[#E2E8F0] text-left text-[#64748B]">
                {['Ad Soyad', 'TC Kimlik', 'E-posta', 'Telefon', 'Kayıt Tarihi', ''].map(h => (
                  <th key={h} className="px-4 py-3 font-medium">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {customers.map(c => (
                <tr key={c.id} className="border-b border-[#E2E8F0] last:border-0 hover:bg-[#F8FAFC]">
                  <td className="px-4 py-3 font-medium text-[#0F172A]">{c.firstName} {c.lastName}</td>
                  <td className="px-4 py-3 text-[#64748B] font-mono text-xs">{c.identityNumber}</td>
                  <td className="px-4 py-3 text-[#64748B]">{c.email}</td>
                  <td className="px-4 py-3 text-[#64748B]">{c.phoneNumber}</td>
                  <td className="px-4 py-3 text-[#64748B]">{formatDate(c.createdAt)}</td>
                  <td className="px-4 py-3">
                    <div className="flex gap-2 justify-end">
                      <Button size="sm" variant="ghost" onClick={() => navigate(`/customers/${c.id}`)}>Detay</Button>
                      <Button size="sm" variant="secondary" onClick={() => { setSelected(c); setModal('edit'); }}>Düzenle</Button>
                      <Button size="sm" variant="danger" onClick={() => { setSelected(c); setModal('delete'); }}>Sil</Button>
                    </div>
                  </td>
                </tr>
              ))}
              {customers.length === 0 && (
                <tr><td colSpan={6} className="px-4 py-8 text-center text-[#64748B]">Henüz müşteri yok.</td></tr>
              )}
            </tbody>
          </table>
        )}
      </Card>

      {/* Oluştur */}
      <Modal open={modal === 'create'} onClose={() => setModal(null)} title="Yeni Müşteri">
        <CustomerForm onSubmit={handleCreate} onClose={() => setModal(null)} loading={saving} />
      </Modal>

      {/* Düzenle */}
      <Modal open={modal === 'edit'} onClose={() => setModal(null)} title="Müşteriyi Düzenle">
        {selected && (
          <CustomerForm
            isEdit
            initial={selected}
            onSubmit={handleEdit}
            onClose={() => setModal(null)}
            loading={saving}
          />
        )}
      </Modal>

      {/* Sil onayı */}
      <Modal
        open={modal === 'delete'}
        onClose={() => setModal(null)}
        title="Müşteriyi Sil"
        footer={
          <>
            <Button variant="secondary" onClick={() => setModal(null)}>İptal</Button>
            <Button variant="danger" loading={saving} onClick={handleDelete}>Sil</Button>
          </>
        }
      >
        <p className="text-sm text-[#64748B]">
          <strong className="text-[#0F172A]">{selected?.firstName} {selected?.lastName}</strong> adlı müşteri
          ve bağlı tüm kredi/taksit kayıtları kalıcı olarak silinecek. Devam etmek istiyor musunuz?
        </p>
      </Modal>
    </PageLayout>
  );
}
