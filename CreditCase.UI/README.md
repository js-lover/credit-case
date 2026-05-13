# CreditCase.UI

React tabanlı kredi yönetim sistemi arayüzü. ASP.NET Core backend ile çalışır.

---

## Teknoloji Stack'i

| Alan | Araç |
|---|---|
| Framework | React 18 |
| Dil | TypeScript (erasableSyntaxOnly) |
| Build | Vite 8 |
| CSS | Tailwind CSS v4 (`@tailwindcss/vite`) |
| Routing | React Router v6 |
| HTTP | Axios + global interceptor |
| Toast | react-hot-toast |

---

## Kurulum

### Gereksinimler

- Node.js ≥ 20
- Backend API çalışır durumda (bkz. `CreditCase.Api`)

### Adımlar

```bash
cd CreditCase.UI
npm install

# .env.example dosyasını kopyala ve API URL'ini ayarla
cp .env.example .env
# .env içindeki VITE_API_URL değerini backend portuna göre düzenle
```

### `.env` Örneği

```
VITE_API_URL=http://localhost:5285/api
```

> Backend portu farklıysa `VITE_API_URL` güncellenmeli.

---

## Geliştirme Sunucusu Başlatma

```bash
npm run dev
# → http://localhost:5173
```

Backend ve UI aynı anda çalışmalı:

```bash
# Terminal 1 — Backend
cd CreditCase.Api && dotnet run

# Terminal 2 — Frontend
cd CreditCase.UI && npm run dev
```

---

## Build

```bash
npm run build
# dist/ klasörü oluşturulur
```

---

## Klasör Yapısı

```
src/
├── services/api/          # Axios servisleri (customer, loan, installment, payment)
│   └── client.ts          # Axios instance + global error interceptor
├── types/
│   └── index.ts           # Tüm API DTO interface'leri + enum sabitleri
├── utils/
│   └── formatters.ts      # formatCurrency (₺) ve formatDate (tr-TR)
├── components/
│   ├── ui/                # Button, Badge, Card, Modal, Spinner
│   └── layout/            # Sidebar, PageLayout
└── pages/
    ├── Dashboard.tsx      # Sistem özeti (müşteri, kredi, borç, gecikme)
    ├── Customers.tsx      # Müşteri listesi + CRUD
    ├── CustomerDetail.tsx # Müşteri borç özeti + kredi listesi
    ├── Loans.tsx          # Kredi listesi + yeni kredi formu
    ├── LoanDetail.tsx     # Taksit planı + ödeme aksiyonu
    └── Payments.tsx       # Ödeme geçmişi
```

---

## Ekranlar

### Dashboard `/`
- 4 özet kart: Toplam Müşteri, Aktif Kredi, Bekleyen Borç, Gecikmiş Taksit
- Son 5 müşteri tablosu
- Son 5 kredi tablosu

### Müşteriler `/customers`
- Müşteri listesi (TC, e-posta, telefon, kayıt tarihi)
- Oluştur / Düzenle / Sil (modal akışlar)
- Satır tıklamasıyla müşteri detayına git
- **Silme işlemi soft delete'tir** — kayıt DB'den silinmez, `IsDeleted = true` set edilir; kredi/ödeme geçmişi korunur

### Müşteri Detayı `/customers/:id`
- Borç özet kartları (`GET /api/customers/{id}/summary` üzerinden)
- Müşteriye ait krediler tablosu

### Krediler `/loans`
- Tüm krediler listesi (müşteri adı, tür, anapara, vade farkı, vade, kalan)
- Yeni kredi oluşturma formu (müşteri dropdown, LoanType seçimi)

### Kredi Detayı `/loans/:id`
- Kredi bilgi kartları
- Taksit planı tablosu: her taksit için durum badge (yeşil/amber/kırmızı)
- Overdue taksit vade tarihi kırmızı vurgulu
- "Öde" butonu → onay modalı → taksit Paid olur, kalan anapara güncellenir

### Ödemeler `/payments`
- Tüm ödeme kayıtları (taksit ID, tutar, tarih, durum)

---

## Renk Paleti

| Değişken | Hex | Kullanım |
|---|---|---|
| `primary` | `#1B4FD8` | Butonlar, aktif nav, mavi vurgular |
| `primary-dark` | `#1640B0` | Buton hover |
| `text` | `#0F172A` | Ana metin, sidebar arka plan |
| `muted` | `#64748B` | İkincil metin, etiketler |
| `background` | `#F1F5F9` | Sayfa arka planı |
| `border` | `#E2E8F0` | Tablo çizgileri, kart kenarlıkları |
| `paid` | `#16A34A` | Paid taksit badge |
| `overdue` | `#DC2626` | Overdue taksit badge ve tarih |
| `unpaid` | `#D97706` | Unpaid taksit badge |

---

## Hata Yönetimi

Tüm API hataları `src/services/api/client.ts` içindeki Axios interceptor'ı tarafından yakalanır:

| HTTP Status | Toast Mesajı |
|---|---|
| 400 | Validation hata mesajları (birleştirilmiş) |
| 404 | "Kayıt bulunamadı." |
| 422 | Backend'den gelen iş kuralı mesajı |
| Network error | "Sunucuya ulaşılamıyor." |
| Diğer | "Beklenmeyen bir hata oluştu." |

Bileşenler yalnızca başarı durumunu yönetir; hata akışı servise devredilmiştir.

---

## Mimari Kararlar

Tüm önemli kararlar için bkz. [`DECISIONS.md`](./DECISIONS.md).

## Test Senaryoları

Manuel test rehberi için bkz. [`TEST-SCENARIOS.md`](./TEST-SCENARIOS.md).

---

## CORS

Backend'de `http://localhost:5173` için CORS yapılandırılmıştır (`CreditCase.Api/Program.cs`). Farklı bir port kullanılıyorsa `WithOrigins(...)` güncellenmeli.
