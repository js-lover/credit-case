# Frontend Mimari Kararlar — CreditCase.UI

Bu belge, CreditCase.UI geliştirme sürecinde alınan önemli teknik ve tasarım kararlarını açıklar.

---

## K-01 — Build Aracı: Vite

**Karar:** CRA (Create React App) yerine Vite kullanıldı.

**Neden:** CRA bakımsız ve yavaş. Vite, dev server'da 10x daha hızlı başlangıç süresi sunar. Tailwind v4'ün `@tailwindcss/vite` plugin'i Vite ile birinci sınıf entegrasyon sağlar.

---

## K-02 — TypeScript `erasableSyntaxOnly`

**Karar:** Tüm enum'lar TypeScript `enum` syntax'ı yerine `const` nesnesi + type alias olarak tanımlandı.

**Problem:** Vite'ın varsayılan `tsconfig.app.json`'ı `verbatimModuleSyntax: true` ve `erasableSyntaxOnly: true` içerir. Bu ayarlarla TypeScript `enum` sözdizimi derleme hatası verir.

**Çözüm:**
```typescript
// ❌ Çalışmaz
export enum LoanType { Personal = 0, Education = 1, Vehicle = 2 }

// ✅ Çalışır
export const LoanType = { Personal: 0, Education: 1, Vehicle: 2 } as const;
export type LoanType = (typeof LoanType)[keyof typeof LoanType];
```

**Etki:** `const` nesneleri runtime'da tam değer gibi kullanılır; `switch`, `===` karşılaştırmaları çalışır.

---

## K-03 — CSS Yaklaşımı: Tailwind CSS v4 (Utility-First)

**Karar:** Tailwind CSS v4, `@tailwindcss/vite` plugin ile kuruldu. Özel tema renkleri `@theme` bloğuyla `index.css` içinde tanımlandı.

**Neden:** CLAUDE.md madde 18'de belirtilen architecht.com kurumsal renk paleti (`#1B4FD8`, `#0F172A`, `#F1F5F9` vb.) Tailwind tema değişkenleri olarak sabitlendi. Utility-first yaklaşım, bileşen başına CSS dosyası yönetimini ortadan kaldırır.

---

## K-04 — HTTP İstemci: Axios + Global Interceptor

**Karar:** Fetch API yerine Axios kullanıldı ve merkezi bir response interceptor yazıldı.

**Neden:** Backend'in standardize hata formatını (`ApiError`) tüm sayfalarda tekrar tekrar yakalamak yerine `src/services/api/client.ts` içindeki tek bir interceptor yönetir:

```typescript
switch (error.response?.status) {
  case 400:  toast.error(fieldErrors);       break;
  case 404:  toast.error('Kayıt bulunamadı.'); break;
  case 422:  toast.error(data.message);       break;
  default:   toast.error('Beklenmeyen hata'); break;
}
```

Sayfa bileşenleri yalnızca başarı durumunu yönetir; hata yönetimi servise devredilir.

---

## K-05 — Servis Katmanı Ayrımı

**Karar:** API çağrıları bileşen içine yazılmadı; `src/services/api/` altında her domain için ayrı servis modülleri oluşturuldu.

**Neden:** CLAUDE.md madde 18 açıkça belirtiyor: "API çağrıları UI bileşenlerinin içine gömülmemeli". Servis katmanı, endpoint URL'lerini tek noktada yönetir ve test edilebilirliği artırır.

---

## K-06 — State Yönetimi: React useState (Context Yok)

**Karar:** Redux veya Context kullanılmadı; her sayfanın kendi yerel state'i var.

**Neden:** Uygulama ölçeği Context'i haklı kılmıyor. Her sayfa bağımsız olarak veri çekiyor ve mevcut iş gereksinimi için global state paylaşımı gerekmiyor. Bu basitlik, Redux boilerplate maliyetinden ağır basıyor.

---

## K-07 — Ödeme Tutarı: Sabit (Serbest Giriş Kaldırıldı)

**Karar:** Ödeme modalında düzenlenebilir tutar alanı kaldırıldı; ödeme her zaman taksit tutarı üzerinden yapılır.

**Problem:** Serbest tutar girişine izin verildiğinde, kullanıcı 5.000 ₺ ödemesi yapsa da kalan anapara yalnızca taksit tutarı kadar azalıyordu. Bu tutarsızlık yanıltıcıydı.

**Neden:** Mevcut domain modeli kısmi/fazla ödemeyi desteklemiyor. Her taksit sabit bir tutara sahip ve tam olarak ödenince kapatılıyor. Serbest giriş bu modelle çeliştiği için kaldırıldı.

```typescript
// ❌ Önce: kullanıcının girdiği tutar gönderiliyordu
await paymentService.create({ installmentId: payTarget.id, paymentAmount: Number(payAmount) });

// ✅ Sonra: her zaman taksit tutarı gönderilir
await paymentService.create({ installmentId: payTarget.id, paymentAmount: payTarget.amount });
```

---

## K-08 — `RemainingPrincipal` Hesaplama Düzeltmesi

**Karar:** Backend'de `RemainingPrincipal`, formül bazlı hesaplamadan kalan taksit tutarlarının toplamına değiştirildi.

**Problem (önceki):**
```csharp
// Kalan taksit sayısı × (anapara / vade) — ödeme tutarını yok sayıyordu
loan.RemainingPrincipal = Math.Round(loan.PrincipalAmount / loan.Term * unpaidCount, 2);
```

Bu formül, taksit tutarına (faiz dahil) değil yalnızca anapara bileşenine bakıyordu. Kullanıcı bakiyesini doğru okuyamıyordu.

**Çözüm (sonraki):**
```csharp
// Ödenmemiş taksitlerin gerçek toplamı
loan.RemainingPrincipal = loan.Installments
    .Where(i => i.Status != InstallmentStatus.Paid)
    .Sum(i => i.Amount);
```

---

## K-09 — Select Değerlerinin Integer'a Dönüştürülmesi

**Karar:** Form `set()` fonksiyonu, `<select>` elemanlarını da numeric olarak değerlendirecek şekilde güncellendi.

**Problem:** `e.target.type`, `<select>` için `'select-one'` döner; sayı kontrolü sadece `type === 'number'` bakıyordu. Bu yüzden `loanType` ve `customerId` backend'e string olarak gidiyordu ve backend JSON deserialization hatası veriyordu.

```typescript
// ❌ Önce
const val = e.target.type === 'number' ? Number(e.target.value) : e.target.value;

// ✅ Sonra
const val = (e.target.type === 'number' || e.target.tagName === 'SELECT')
    ? Number(e.target.value)
    : e.target.value;
```

---

## K-10 — Dashboard Metriklerinin Installment Servisinden Alınması

**Karar:** Dashboard'daki "Bekleyen Borç" ve "Gecikmiş Taksit" metrikleri `GET /api/loans` yerine `GET /api/installments` üzerinden hesaplanır.

**Problem:** `LoanRepository.GetAllAsync()`, `_context.Loans.ToListAsync()` ile çalışır — taksitleri eager load etmez. Kredi listesi sayfası için bu tercih edilir (performans). Ancak Dashboard, taksit düzeyinde agregasyon yapar.

```typescript
// ❌ Önce: her zaman 0 dönerdi (installments: [] boş array)
const totalOutstanding = loans.flatMap(l => l.installments)...

// ✅ Sonra: ayrı endpoint
Promise.all([customerService.getAll(), loanService.getAll(), installmentService.getAll()])
```

**Alternatif düşünülen:** `LoanRepository.GetAllAsync()` içine `.Include(l => l.Installments)` eklemek. Ancak kredi listesi sayfası installment verisi kullanmadığı için gereksiz veri yükü olurdu.

---

## K-11 — Routing: React Router v6 BrowserRouter

**Karar:** Hash routing yerine BrowserRouter kullanıldı.

**Neden:** Temiz URL'ler (`/customers/1` yerine `/#/customers/1`). Geliştirme ortamında Vite, tüm rotaları `index.html`'e yönlendiriyor; bu nedenle production'da da aynı konfigürasyon gerekir.

---

## K-12 — Toast Bildirimleri: react-hot-toast

**Karar:** Custom toast bileşeni yerine `react-hot-toast` kullanıldı.

**Neden:** Sağ üst köşe, 3 saniyelik otomatik kapanma, loading state'e gerek olmadan tek satır kullanım. `Toaster` bileşeni `App.tsx` kök seviyesine alındı; bütün sayfalardan `toast.success()` / `toast.error()` direkt çağrılabilir.

---

## K-13 — Giriş Validasyonu: TC Kimlik No ve Telefon (Frontend Katmanı)

**Karar:** `identityNumber` ve `phoneNumber` alanları için frontend'de iki katmanlı doğrulama uygulandı. Backend FluentValidation kuralları için bkz. backend `DECISIONS.md` K-18.

**Çözüm:**

| Katman | Mekanizma | Ne yapar |
|---|---|---|
| **Giriş filtresi** | `onChange` → `replace(/\D/g, '')` | Harf state'e geçmez, kutuda görünmez |
| **HTML5 kısıt** | `pattern`, `minLength`, `maxLength`, `inputMode` | Submit engellenir, tarayıcı hata balonu gösterir |

```typescript
const setDigits = (field: string, maxLen: number) =>
  (e: React.ChangeEvent<HTMLInputElement>) => {
    const digits = e.target.value.replace(/\D/g, '').slice(0, maxLen);
    setForm(prev => ({ ...prev, [field]: digits }));
  };
```

---

*Son güncelleme: 2026-05-11*
