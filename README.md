# Digital Loan & Repayment Management System

Bireysel müşterilerin kredi başvurularını, kredi bakiyelerini ve geri ödeme planlarını yönetebildiği full-stack dijital bankacılık uygulaması.

---

## İçindekiler

1. [Teknoloji Stack'i](#teknoloji-stacki)
2. [Mimari](#mimari)
3. [Proje Yapısı](#proje-yapısı)
4. [Domain Modeli](#domain-modeli)
5. [ER Diyagramı](#er-diyagramı)
6. [İş Kuralları](#iş-kuralları)
7. [Kredi Değerlendirme Motoru](#kredi-değerlendirme-motoru)
8. [Faiz Oranı Belirleme](#faiz-oranı-belirleme)
9. [Taksit Hesaplama](#taksit-hesaplama)
10. [Sıralı Ödeme Kuralı](#sıralı-ödeme-kuralı)
11. [Balon Ödeme](#balon-ödeme)
12. [Kredi Skoru Dinamiği](#kredi-skoru-dinamiği)
13. [Kredi Oluşturma Akışı](#kredi-oluşturma-akışı)
14. [API Endpoint Listesi](#api-endpoint-listesi)
15. [Hata Yönetimi](#hata-yönetimi)
16. [Üçüncü Parti Servis Entegrasyonu](#üçüncü-parti-servis-entegrasyonu)
17. [Kurulum ve Çalıştırma](#kurulum-ve-çalıştırma)
18. [Geliştirme Adımları](#geliştirme-adımları)
19. [Yapay Zeka Kullanımı](#yapay-zeka-kullanımı)

---

## Teknoloji Stack'i

### Backend

| Katman | Teknoloji |
|---|---|
| Framework | .NET 10 / ASP.NET Core Web API |
| Dil | C# |
| ORM | Entity Framework Core 10 |
| Veritabanı | Microsoft SQL Server 2022 |
| Validasyon | FluentValidation 11 |
| API Dokümantasyonu | Swagger / OpenAPI (Swashbuckle) |
| Container | Docker |
| Test | xUnit · Moq · FluentAssertions |
| Versiyon Kontrolü | Git — Conventional Commits |

### Frontend

| Katman | Teknoloji |
|---|---|
| Framework | React 18 |
| Dil | TypeScript (`erasableSyntaxOnly`) |
| Build | Vite |
| CSS | Tailwind CSS v4 (`@tailwindcss/vite`) |
| Routing | React Router v6 |
| HTTP | Axios + global interceptor |
| Toast | react-hot-toast |

---

## Mimari

Projede **Clean Architecture** yaklaşımı uygulanmıştır. Katmanlar yalnızca içe doğru bağımlılık kurar; dış katmanlar iç katmanları referans alır, tersi geçerli değildir.

```
┌─────────────────────────────────────────────────────┐
│                   CreditCase.Api                    │
│         Controllers │ Middleware │ Program.cs       │
│                                                     │
│  ┌───────────────────────────────────────────────┐  │
│  │           CreditCase.Application              │  │
│  │   Services │ DTOs │ Interfaces │ Validators   │  │
│  │                                               │  │
│  │  ┌─────────────────────────────────────────┐  │  │
│  │  │          CreditCase.Domain              │  │  │
│  │  │      Entities │ Enums │ Business Rules  │  │  │
│  │  └─────────────────────────────────────────┘  │  │
│  └───────────────────────────────────────────────┘  │
│                                                     │
│  ┌───────────────────────────────────────────────┐  │
│  │         CreditCase.Infrastructure             │  │
│  │  AppDbContext │ Repositories │ Mock Services  │  │
│  └───────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────┘
```

### Bağımlılık Kuralı

```
Api  →  Application  →  Domain
Infrastructure  →  Application  →  Domain
```

`Domain` hiçbir dış katmana bağımlı değildir. `Application`, yalnızca `Domain`'i tanır. `Infrastructure`, `Application` interface'lerini implement eder; bu sayede gerçek EF Core implementasyonu uygulama katmanından gizlenir.

---

## Proje Yapısı

```
CreditCase.sln
│
├── CreditCase.Domain/
│   ├── Entities/
│   │   ├── Customer.cs          # IsDeleted / DeletedAt (soft delete) + CreditScoreBonus
│   │   ├── Loan.cs
│   │   ├── Installment.cs       # IsBalloon bayrağı
│   │   ├── Payment.cs
│   │   └── LoanEvaluationResult.cs
│   ├── Enums/
│   │   ├── LoanType.cs
│   │   ├── LoanStatus.cs
│   │   ├── InstallmentStatus.cs
│   │   ├── PaymentStatus.cs
│   │   ├── RiskCategory.cs
│   │   ├── ProfessionCategory.cs
│   │   └── EmploymentStatus.cs
│   └── Interfaces/
│       └── IRiskAnalysisRule.cs
│
├── CreditCase.Application/
│   ├── DTOs/
│   │   ├── Customers/   (CreateCustomerRequest, UpdateCustomerRequest, CustomerResponse, CustomerSummaryResponse)
│   │   ├── Loans/       (CreateLoanRequest, LoanResponse, LoanApplicationRequest, LoanEvaluationResponse)
│   │   ├── Installments/(UpdateInstallmentRequest, InstallmentResponse)
│   │   └── Payments/    (CreatePaymentRequest, PaymentResponse)
│   ├── Interfaces/
│   │   ├── Repositories/(ICustomerRepository, ILoanRepository, ...)
│   │   ├── Services/    (ICustomerService, ILoanService, IRiskAnalysisService,
│   │   │                 IInterestCalculationService, IMaximumLoanCalculatorService,
│   │   │                 IInstallmentPlanStrategy, ILoanEvaluationService)
│   │   └── External/    (ICreditScoreService)
│   ├── Services/
│   │   ├── CustomerService.cs
│   │   ├── LoanService.cs
│   │   ├── LoanEvaluationService.cs
│   │   ├── InstallmentService.cs
│   │   └── PaymentService.cs
│   ├── Validators/
│   │   ├── CreateCustomerRequestValidator.cs
│   │   ├── UpdateCustomerRequestValidator.cs
│   │   ├── CreateLoanRequestValidator.cs   # tutar-vade çapraz kural dahil
│   │   └── CreatePaymentRequestValidator.cs
│   ├── Exceptions/
│   │   ├── NotFoundException.cs
│   │   └── BusinessRuleException.cs
│   └── DependencyInjection.cs
│
├── CreditCase.Infrastructure/
│   ├── Persistence/
│   │   ├── AppDbContext.cs      # Global query filter, filtered unique indexes
│   │   └── Repositories/
│   │       ├── CustomerRepository.cs
│   │       ├── LoanRepository.cs
│   │       ├── InstallmentRepository.cs
│   │       ├── PaymentRepository.cs
│   │       └── LoanEvaluationRepository.cs
│   ├── Services/
│   │   ├── MockCreditScoreService.cs        # Profil tabanlı skor hesaplama
│   │   ├── RiskAnalysisEngine.cs            # Ağırlıklı kural motoru
│   │   ├── InterestCalculationEngine.cs     # Dinamik faiz oranı
│   │   ├── MaximumLoanCalculator.cs         # Borç kapasitesi hesabı
│   │   ├── StandardInstallmentStrategy.cs   # Düz faizli eşit taksit
│   │   ├── BalloonPaymentStrategy.cs        # Balon ödeme planı
│   │   └── Rules/
│   │       ├── CreditScoreRule.cs           # Ağırlık: 0.30
│   │       ├── DebtToIncomeRule.cs          # Ağırlık: 0.25
│   │       ├── ProfessionStabilityRule.cs   # Ağırlık: 0.20
│   │       ├── AgeRule.cs                   # Ağırlık: 0.15
│   │       └── EmploymentStatusRule.cs      # Ağırlık: 0.10
│   ├── Migrations/
│   │   ├── 20260511000000_InitialCreate.cs
│   │   ├── 20260511000002_AddSoftDeleteToCustomer.cs
│   │   └── 20260512000001_AddCreditScoreBonusToCustomer.cs
│   └── DependencyInjection.cs
│
├── CreditCase.Api/
│   ├── Controllers/
│   │   ├── CustomersController.cs
│   │   ├── LoansController.cs
│   │   ├── LoanEvaluationController.cs
│   │   ├── InstallmentsController.cs
│   │   └── PaymentsController.cs
│   ├── Middleware/
│   │   └── ExceptionHandlingMiddleware.cs
│   └── Program.cs
│
├── CreditCase.Tests/
│   ├── Services/
│   │   ├── CustomerServiceTests.cs   # 10 test (soft delete + borç kontrolü dahil)
│   │   ├── LoanServiceTests.cs       # 9 test
│   │   ├── InstallmentServiceTests.cs# 6 test
│   │   └── PaymentServiceTests.cs    # 8 test
│   └── Validators/
│       └── CustomerValidatorTests.cs # 15 test (TC + telefon validasyonu)
│
└── CreditCase.UI/                    # React frontend (bkz. CreditCase.UI/README.md)
    ├── src/
    │   ├── pages/     (Dashboard, Customers, Loans, Payments, ...)
    │   ├── services/  (Axios API katmanı)
    │   ├── components/(UI bileşenleri + layout)
    │   └── types/     (API DTO interface'leri)
    └── README.md
```

---

## Domain Modeli

### Customer

Bankanın bireysel müşterisini temsil eder.

| Alan | Tip | Açıklama |
|---|---|---|
| Id | int | Birincil anahtar |
| FirstName | string (100) | Ad |
| LastName | string (100) | Soyad |
| IdentityNumber | string (11) | TC Kimlik No — aktif kayıtlar arasında benzersiz |
| Email | string (200) | E-posta — aktif kayıtlar arasında benzersiz |
| PhoneNumber | string (20) | Telefon (10–11 hane, yalnızca rakam) |
| DateOfBirth | DateTime | Doğum tarihi — yaş puanı hesabında kullanılır |
| MonthlyIncome | decimal(18,2) | Aylık net gelir — risk ve maksimum tutar hesabında kullanılır |
| ProfessionCategory | enum | Kamu / Sağlık / Finans / Teknoloji / Eğitim / Ticaret / Hizmetler / İnşaat / Mevsimlik / Diğer |
| EmploymentStatus | enum | Tam Zamanlı / Yarı Zamanlı / Serbest / Emekli / İşsiz |
| CreditScoreBonus | int | Ödeme davranışından biriken bonus (−200 / +200 aralığı) |
| CreatedAt | DateTime | Kayıt tarihi |
| IsDeleted | bool | Soft delete bayrağı (varsayılan: false) |
| DeletedAt | DateTime? | Silinme tarihi (null = aktif kayıt) |

### Loan

Müşteriye verilen krediyi temsil eder.

| Alan | Tip | Açıklama |
|---|---|---|
| Id | int | Birincil anahtar |
| CustomerId | int | Bağlı müşteri (FK) |
| LoanType | enum | Bireysel / Eğitim / Taşıt |
| PrincipalAmount | decimal(18,2) | Ana para tutarı |
| InterestRate | decimal(5,2) | Yıllık faiz oranı (%) — InterestCalculationEngine tarafından belirlenir |
| Term | int | Vade (ay) |
| StartDate | DateTime | Kredi başlangıç tarihi |
| Status | enum | Aktif / Kapalı |
| RemainingPrincipal | decimal(18,2) | Ödenmemiş taksit tutarlarının toplamı |

### Installment

Krediye bağlı aylık ödeme planı kalemini temsil eder.

| Alan | Tip | Açıklama |
|---|---|---|
| Id | int | Birincil anahtar |
| LoanId | int | Bağlı kredi (FK) |
| InstallmentNumber | int | Taksit sıra numarası |
| Amount | decimal(18,2) | Taksit tutarı |
| DueDate | DateTime | Son ödeme tarihi |
| Status | enum | Unpaid / Paid / Overdue |
| IsBalloon | bool | Balon ödeme taksiti mi? |

### Payment

Gerçekleştirilen ödeme işlemini temsil eder.

| Alan | Tip | Açıklama |
|---|---|---|
| Id | int | Birincil anahtar |
| InstallmentId | int | Bağlı taksit (FK) — unique |
| PaymentAmount | decimal(18,2) | Ödeme tutarı |
| PaymentDate | DateTime | Ödeme tarihi |
| Status | enum | Başarılı / Başarısız |

### LoanEvaluationResult

Kredi başvurusu değerlendirme kaydını temsil eder.

| Alan | Tip | Açıklama |
|---|---|---|
| Id | int | Birincil anahtar |
| CustomerId | int | Bağlı müşteri (FK) |
| RequestedAmount | decimal(18,2) | İstenen kredi tutarı |
| RequestedTerm | int | İstenen vade (ay) |
| IsApproved | bool | Onay kararı |
| ApprovedAmount | decimal(18,2) | Onaylanan tutar |
| MaximumAmount | decimal(18,2) | Hesaplanan maksimum uygun tutar |
| ApprovedInterestRate | decimal(5,2) | Hesaplanan faiz oranı |
| RiskLevel | enum | Low / Medium / High / VeryHigh |
| CreditScore | int | Değerlendirme anındaki kredi skoru |
| DebtToIncomeRatio | decimal | Borç / Gelir oranı |
| MonthlyInstallmentEstimate | decimal(18,2) | Tahmini aylık taksit |
| RejectionReason | string? | Red sebebi (yalnızca reddedilen başvurularda) |
| EvaluationDate | DateTime | Değerlendirme tarihi |
| ExpirationDate | DateTime | Onay geçerlilik tarihi (30 gün) |

---

## ER Diyagramı

```mermaid
erDiagram
    CUSTOMER {
        int Id PK
        string FirstName
        string LastName
        string IdentityNumber UK
        string Email UK
        string PhoneNumber
        datetime DateOfBirth
        decimal MonthlyIncome
        string ProfessionCategory
        string EmploymentStatus
        int CreditScoreBonus
        datetime CreatedAt
        bool IsDeleted
        datetime DeletedAt
    }
    LOAN {
        int Id PK
        int CustomerId FK
        string LoanType
        decimal PrincipalAmount
        decimal InterestRate
        int Term
        datetime StartDate
        string Status
        decimal RemainingPrincipal
    }
    INSTALLMENT {
        int Id PK
        int LoanId FK
        int InstallmentNumber
        decimal Amount
        datetime DueDate
        string Status
        bool IsBalloon
    }
    PAYMENT {
        int Id PK
        int InstallmentId FK
        decimal PaymentAmount
        datetime PaymentDate
        string Status
    }
    LOAN_EVALUATION {
        int Id PK
        int CustomerId FK
        decimal RequestedAmount
        int RequestedTerm
        bool IsApproved
        decimal ApprovedAmount
        decimal MaximumAmount
        decimal ApprovedInterestRate
        string RiskLevel
        int CreditScore
        decimal DebtToIncomeRatio
        decimal MonthlyInstallmentEstimate
        string RejectionReason
        datetime EvaluationDate
        datetime ExpirationDate
    }

    CUSTOMER ||--o{ LOAN : "sahip"
    CUSTOMER ||--o{ LOAN_EVALUATION : "başvurur"
    LOAN ||--|{ INSTALLMENT : "içerir"
    INSTALLMENT ||--o| PAYMENT : "alır"
```

**İlişkiler:**
- `Customer` → `Loan` : 1'e N (bir müşterinin birden fazla kredisi olabilir)
- `Customer` → `LoanEvaluation` : 1'e N (değerlendirme geçmişi korunur)
- `Loan` → `Installment` : 1'e N (bir kredi birden fazla taksit içerir)
- `Installment` → `Payment` : 1'e 0..1 (her taksit en fazla bir ödemeye sahip olabilir)

---

## İş Kuralları

### Müşteri Yönetimi
- TC Kimlik Numarası ve e-posta **aktif kayıtlar** arasında benzersiz olmalıdır.
- Silme işlemi **soft delete** ile yapılır: kayıt fiziksel olarak silinmez, `IsDeleted = true` ve `DeletedAt` set edilir. Kredi ve ödeme geçmişi korunur.
- EF Core global query filter (`HasQueryFilter`) sayesinde soft-deleted kayıtlar tüm sorgularda otomatik olarak hariç tutulur.
- **Aktif borcu olan müşteri silinemez.** Tüm kredilerin kapanması şarttır; aksi hâlde `422` hatası döner.

### Kredi Oluşturma
- Kredi oluşturulmadan önce `LoanEvaluationService` üzerinden risk ve uygunluk analizi yapılır.
- Değerlendirme `Approved` değilse kredi reddedilir.
- Kredi kaydedilirken taksit planı **otomatik olarak** üretilir; ayrı bir endpoint çağrısı gerekmez.
- **Tutar-Vade Kısıtı:** Küçük tutarlı kredilere uzun vadeler açık değildir.

| Kredi Tutarı | Maks. Vade |
|---|---|
| ≤ 10.000 ₺ | 24 ay |
| ≤ 50.000 ₺ | 60 ay |
| ≤ 150.000 ₺ | 84 ay |
| > 150.000 ₺ | 120 ay |

### Taksit Yönetimi
- Her taksit, vade tarihi (`DueDate`) taşır.
- `GET /api/installments` çağrıldığında sistem, `DueDate` geçmiş ve `Unpaid` durumundaki taksitleri otomatik olarak `Overdue` olarak günceller.

### Ödeme Yönetimi
- Aynı taksit için ikinci ödeme oluşturulamaz (çift koruma — bkz. [Sıralı Ödeme Kuralı](#sıralı-ödeme-kuralı)).
- `Paid` statüsündeki bir taksit için ödeme yapılamaz.
- **Sıralı ödeme zorunludur:** önceki ödenmemiş taksit varken ileri taksit ödenemez.
- Başarılı ödeme sonrası:
  1. Taksit durumu `Paid` olarak güncellenir.
  2. Kredinin `RemainingPrincipal` değeri yeniden hesaplanır.
  3. Tüm taksitler ödenmiş ise kredi durumu `Closed` olarak işaretlenir.
  4. Müşterinin `CreditScoreBonus` değeri güncellenir (+5 zamanında, −10 gecikmiş).

---

## Kredi Değerlendirme Motoru

Kredi başvurusu `POST /api/loans/evaluate` ile değerlendirilir. Onaylanan değerlendirme ID'si ile `POST /api/loans` çağrısında kredi oluşturulur.

### Risk Analizi — Ağırlıklı Kural Motoru

`RiskAnalysisEngine`, `IRiskAnalysisRule` interface'ini implement eden 5 bağımsız kural sınıfını toplayan bir motor çalıştırır. Her kural 0–100 arası puan üretir ve kendi ağırlığıyla çarpılır:

```
Toplam Puan = Σ (Kural[i].Evaluate() × Kural[i].Weight)
```

| Kural | Ağırlık | Puan Kaynağı |
|---|---|---|
| `CreditScoreRule` | 0.30 | Kredi skoru 0-1000 → 0-100 normalize |
| `DebtToIncomeRule` | 0.25 | Borç/Gelir oranı bantları |
| `ProfessionStabilityRule` | 0.20 | Meslek kategorisine göre stabilite |
| `AgeRule` | 0.15 | Yaş bantları (36-50 = 100 puan) |
| `EmploymentStatusRule` | 0.10 | İstihdam durumu |

Risk kategorisi toplam puana göre belirlenir:

| Toplam Puan | Risk Kategorisi | Faiz Etkisi |
|---|---|---|
| ≥ 75 | Düşük (Low) | Risk primi: +0% |
| ≥ 55 | Orta (Medium) | Risk primi: +5% |
| ≥ 35 | Yüksek (High) | Risk primi: +12% |
| < 35 | Çok Yüksek (VeryHigh) | Başvuru reddedilir |

```mermaid
flowchart LR
    subgraph RiskAnalysisEngine
        CS["CreditScoreRule\n×0.30"]
        DTI["DebtToIncomeRule\n×0.25"]
        PS["ProfessionStabilityRule\n×0.20"]
        AR["AgeRule\n×0.15"]
        ES["EmploymentStatusRule\n×0.10"]
        SUM["Σ Toplam Puan\n0–100"]
    end

    CS --> SUM
    DTI --> SUM
    PS --> SUM
    AR --> SUM
    ES --> SUM

    SUM -->|"≥ 75"| LOW["Low Risk\nFaiz +0%"]
    SUM -->|"55-74"| MED["Medium Risk\nFaiz +5%"]
    SUM -->|"35-54"| HIGH["High Risk\nFaiz +12%"]
    SUM -->|"< 35"| VHIGH["VeryHigh\nRed"]

    style LOW fill:#86efac,color:#14532d
    style MED fill:#fde68a,color:#78350f
    style HIGH fill:#fed7aa,color:#9a3412
    style VHIGH fill:#fecaca,color:#991b1b
```

### Maksimum Kredi Tutarı

```
BorçKapasitesi   = (AylıkGelir × 0.70) − MevcutAylıkBorçÖdemesi
KapasiteBazlıMax = BorçKapasitesi × VadeAy
GelirBazlıMax    = AylıkGelir × RiskKatsayısı   (Low=5.0x, Medium=3.5x, High=2.0x)
MaksimumTutar    = Min(GelirBazlıMax, KapasiteBazlıMax, 1.000.000 ₺)
```

---

## Faiz Oranı Belirleme

Faiz oranı, `InterestCalculationEngine` tarafından 4 değişkenden dinamik olarak hesaplanır. Bu rate, daha sonra taksit planı üretiminde kullanılır.

### Formül

```
Son Faiz Oranı = Temel (%5) + Risk Primi + Vade Primi + Tutar Primi − Meslek Bonusu
```

### Bileşenler

**Risk Primi:**

| Risk Kategorisi | Prim |
|---|---|
| Düşük | +%0 |
| Orta | +%5 |
| Yüksek | +%12 |

**Vade Primi** (uzun vadede belirsizlik artar):

| Vade | Prim |
|---|---|
| 1–6 ay | +%0 |
| 7–12 ay | +%3 |
| 13–24 ay | +%7 |
| 25–36 ay | +%12 |
| 37–60 ay | +%18 |
| 61–84 ay | +%25 |
| 85–120 ay | +%35 |

**Tutar Primi** (gelire oranla istenen tutar):

| İstenen Tutar / Aylık Gelir | Prim |
|---|---|
| ≤ 2× | +%0 |
| 2×–3× | +%1 |
| > 3× | +%2 |

**Meslek Bonusu:**

| Meslek | Bonus |
|---|---|
| Kamu (Government) | −%1 |
| Diğer | %0 |

### Örnek Hesaplamalar

**Senaryo A — Düşük Riskli, Kısa Vadeli:**
```
Yazılımcı, 38 yaş, 12.000 ₺/ay gelir | İstek: 20.000 ₺, 12 ay
Risk Puanı: 78 → Low  →  Risk Primi: +%0
Vade Primi (12 ay): +%3
Tutar Primi (20.000 / 12.000 = 1.67×): +%0
Meslek Bonusu: %0
Son Faiz: %5 + 0 + 3 + 0 = %8
```

**Senaryo B — Orta Riskli, Uzun Vadeli:**
```
Satış Temsilcisi, 28 yaş, 5.000 ₺/ay gelir | İstek: 18.000 ₺, 36 ay
Risk Puanı: 61 → Medium  →  Risk Primi: +%5
Vade Primi (36 ay): +%12
Tutar Primi (18.000 / 5.000 = 3.6×): +%2
Meslek Bonusu: %0
Son Faiz: %5 + 5 + 12 + 2 = %24
```

**Senaryo C — Kamu Çalışanı Bonusu:**
```
Devlet memuru, 45 yaş, 8.000 ₺/ay gelir | İstek: 30.000 ₺, 24 ay
Risk Puanı: 82 → Low  →  Risk Primi: +%0
Vade Primi (24 ay): +%7
Tutar Primi (30.000 / 8.000 = 3.75×): +%2
Meslek Bonusu: −%1
Son Faiz: %5 + 0 + 7 + 2 − 1 = %13
```

```mermaid
flowchart TD
    START([Kredi Başvurusu]) --> RISK[Risk Analizi\nRiskAnalysisEngine]
    RISK --> BASE["Temel Faiz: %5"]
    BASE --> RP["+ Risk Primi\nLow=0% · Med=+5% · High=+12%"]
    RP --> VP["+ Vade Primi\n1-6ay: 0% ... 85+ay: +35%"]
    VP --> TP["+ Tutar Primi\n≤2x: 0% · 2-3x: +1% · >3x: +2%"]
    TP --> MB["− Meslek Bonusu\nKamu: −1% · Diğer: 0%"]
    MB --> RATE(["Son Faiz Oranı\n%5 – %52 aralığı"])

    style START fill:#dbeafe,color:#1e3a8a
    style RATE fill:#d1fae5,color:#064e3b
```

---

## Taksit Hesaplama

Faiz oranı belirlendikten sonra taksit planı iki strateji sınıfından biri ile üretilir.

### Standart Plan — `StandardInstallmentStrategy`

Düz faiz (flat-rate) yöntemiyle eşit tutarlı taksit planı üretir:

```
termYears     = Term / 12
totalAmount   = PrincipalAmount × (1 + InterestRate / 100 × termYears)
monthlyAmount = ROUND(totalAmount / Term, 2)
```

**Örnek:**
```
Ana Para: 20.000 ₺ · Faiz: %8 · Vade: 12 ay

termYears    = 12 / 12 = 1
totalAmount  = 20.000 × (1 + 0.08 × 1) = 21.600 ₺
monthlyAmount = 21.600 / 12 = 1.800 ₺/ay  (tüm taksitler eşit)
```

`RemainingPrincipal`, her başarılı ödemede ödenmemiş taksit tutarlarının toplamıyla güncellenir:
```
RemainingPrincipal = SUM(ödenmemiş taksitlerin Amount değerleri)
```

### Balon Ödeme Planı — `BalloonPaymentStrategy`

İlk `n-1` taksit normal tutarın **%60**'ı kadar düşük tutulur; son taksit (balon) kalan borcun tamamını kapsar:

```
normalMonthly  = totalAmount / term
regularAmount  = ROUND(normalMonthly × 0.60, 2)
balloonAmount  = ROUND(totalAmount − regularAmount × (term − 1), 2)
```

**Kısıt:** `balloonAmount ≤ principalAmount × 0.50` (anaparanın %50'sini aşamaz).

---

## Sıralı Ödeme Kuralı

Finansal tutarlılığı korumak amacıyla taksitler yalnızca küçükten büyüğe sırayla ödenebilir. Önceki ödenmemiş taksit varken ileri bir taksit ödenemez.

```mermaid
sequenceDiagram
    participant K as Kullanıcı
    participant A as API
    participant P as PaymentService
    participant DB as Veritabanı

    K->>A: POST /api/payments {installmentId: 4}
    A->>P: CreateAsync(request)

    P->>DB: GetByIdAsync(installmentId=4)
    DB-->>P: Installment #4 (Unpaid)

    P->>DB: GetByInstallmentIdAsync(4)
    DB-->>P: null (ödeme kaydı yok)

    P->>DB: GetByIdWithInstallmentsAsync(loanId)
    DB-->>P: Loan + tüm taksitler

    P->>P: Taksit #1 Unpaid mı?
    Note over P: #1 ve #2 hâlâ ödenmemiş!

    P-->>A: BusinessRuleException\n422
    A-->>K: "Önceki ödenmemiş taksitler\nönce ödenmelidir."
```

**Frontend davranışı:** Taksit planı ekranında yalnızca en düşük numaralı ödenmemiş taksitin yanında "Öde" butonu görünür; diğerleri "Önceki bekliyor" olarak gösterilir.

---

## Balon Ödeme

Balon ödeme, kredinin bir kısmını son aya erteleyerek ilk taksitlerde yük azaltan özel bir geri ödeme modelidir.

```mermaid
gantt
    title Standart vs Balon Ödeme Karşılaştırması (12 Ay, 20.000 ₺)
    dateFormat MM
    axisFormat Ay %m

    section Standart
    1.800 ₺/ay (×12) :01, 12M

    section Balon
    1.080 ₺/ay (×11) :01, 11M
    13.200 ₺ balon   :12, 1M
```

**Standart Kredi (20.000 ₺, %8, 12 ay):**

| Taksit | Tutar |
|---|---|
| 1–12 | 1.800 ₺ / ay |
| **Toplam** | **21.600 ₺** |

**Balon Ödemeli Kredi (aynı parametreler):**

| Taksit | Tutar |
|---|---|
| 1–11 | ~1.080 ₺ / ay (normalin %60'ı) |
| 12 (BALON) | ~13.720 ₺ |
| **Toplam** | **21.600 ₺** |

**Kısıtlamalar:** Yalnızca Taşıt (`Vehicle`) kredilerinde seçilebilir. Balon tutar anaparanın %50'sini aşarsa başvuru reddedilir.

---

## Kredi Skoru Dinamiği

Müşteri profili ve ödeme geçmişi, kredi skorunu birlikte belirler.

```mermaid
flowchart TD
    subgraph "MockCreditScoreService — Baz Skor (maks. 800)"
        AG["Yaş Puanı\nmaks. 200"]
        INC["Gelir Puanı\nmaks. 250"]
        EMP["İstihdam Puanı\nmaks. 200"]
        PROF["Meslek Puanı\nmaks. 150"]
        BASE_SUM["Baz Skor"]
    end

    subgraph "Ödeme Davranışı"
        PAY_ON["Zamanında Ödeme\n+5 bonus"]
        PAY_LATE["Gecikmeli Ödeme\n−10 bonus"]
        BONUS["CreditScoreBonus\n−200 / +200 arası"]
    end

    AG --> BASE_SUM
    INC --> BASE_SUM
    EMP --> BASE_SUM
    PROF --> BASE_SUM

    PAY_ON --> BONUS
    PAY_LATE --> BONUS

    BASE_SUM --> CLAMP["Nihai Skor = Clamp(Baz + Bonus, 0, 1000)"]
    BONUS --> CLAMP

    CLAMP -->|"≥ 750"| LOW_IND["Low Risk\nNegative Records: yok"]
    CLAMP -->|"600–749"| MED_IND["Medium Risk\nNegative Records: yok"]
    CLAMP -->|"< 600"| HIGH_IND["High Risk\nNegative Records: ödeme gecikmesi"]

    style LOW_IND fill:#86efac,color:#14532d
    style MED_IND fill:#fde68a,color:#78350f
    style HIGH_IND fill:#fecaca,color:#991b1b
```

### Baz Skor Bileşenleri

**Yaş Puanı (maks. 200):**

| Yaş | Puan |
|---|---|
| < 21 | 40 |
| 21–25 | 100 |
| 26–35 | 160 |
| 36–50 | 200 (pik) |
| 51–60 | 175 |
| 61–65 | 120 |
| > 65 | 55 |

**Gelir Puanı (maks. 250):**

| Aylık Gelir | Puan |
|---|---|
| < 3.000 ₺ | 30 |
| 3.000–5.999 ₺ | 85 |
| 6.000–9.999 ₺ | 145 |
| 10.000–19.999 ₺ | 205 |
| 20.000–49.999 ₺ | 240 |
| ≥ 50.000 ₺ | 250 |

**İstihdam Puanı (maks. 200):**

| Durum | Puan |
|---|---|
| Tam Zamanlı | 200 |
| Emekli | 170 |
| Serbest Meslek | 130 |
| Yarı Zamanlı | 100 |
| İşsiz | 20 |

**Meslek Puanı (maks. 150):**

| Meslek | Puan |
|---|---|
| Kamu | 150 |
| Sağlık | 140 |
| Finans | 135 |
| Eğitim / Teknoloji | 130 |
| Ticaret / Hizmetler | 90–100 |
| Diğer | 80 |
| İnşaat | 70 |
| Mevsimlik | 45 |

### Ödeme Geçmişi Bonusu

Her başarılı ödeme sonrası `CreditScoreBonus` güncellenir ve `Customer` tablosunda kalıcı olarak saklanır:

```csharp
bool isOnTime = installment.DueDate.Date >= DateTime.UtcNow.Date;
int delta = isOnTime ? +5 : -10;
customer.CreditScoreBonus = Math.Clamp(customer.CreditScoreBonus + delta, -200, +200);
```

Düzenli ödeme yapan bir müşteri zaman içinde kredi skorunu artırabilir; bu da sonraki başvurularda daha iyi faiz oranına yol açar.

---

## Kredi Oluşturma Akışı

```mermaid
flowchart TD
    A([POST /api/loans/evaluate]) --> B["FluentValidation\nTutar · Vade · MüşteriId"]
    B -->|Geçersiz| ERR1([400 Bad Request])
    B -->|Geçerli| C[Müşteri Kontrolü]
    C -->|Bulunamadı| ERR2([404 Not Found])
    C -->|Mevcut| D["MockCreditScoreService\nGetCreditScoreAsync(customerId)"]
    D --> E["RiskAnalysisEngine\nΣ (kural × ağırlık)"]
    E --> F{Risk Kategorisi?}
    F -->|VeryHigh| ERR3([422 Reddedildi])
    F -->|Low/Medium/High| G["InterestCalculationEngine\nBaz + Risk + Vade + Tutar − Meslek"]
    G --> H["MaximumLoanCalculator\nBorç kapasitesi hesabı"]
    H --> I["LoanEvaluationResult kaydedilir\n(30 gün geçerli)"]
    I --> J([200 LoanEvaluationResponse])

    J -.->|Onaylı değerlendirme ile| K

    K([POST /api/loans]) --> L["FluentValidation\nTutar-Vade kısıtı kontrolü"]
    L -->|Geçersiz| ERR4([400 Bad Request])
    L -->|Geçerli| M{isBalloonPayment?}
    M -->|true| N[BalloonPaymentStrategy\nDüşük taksit + yüksek son]
    M -->|false| O[StandardInstallmentStrategy\nEşit taksitler]
    N --> P[Taksit tutarları hesaplanır]
    O --> P
    P -->|Balon > %50 anapara| ERR5([422 İş Kuralı İhlali])
    P --> Q["Loan + Installments\nEF Core cascade insert"]
    Q --> R([201 Created\nLoanResponse + taksit listesi])

    style ERR1 fill:#fecaca,color:#991b1b
    style ERR2 fill:#fecaca,color:#991b1b
    style ERR3 fill:#fecaca,color:#991b1b
    style ERR4 fill:#fecaca,color:#991b1b
    style ERR5 fill:#fecaca,color:#991b1b
    style J fill:#d1fae5,color:#064e3b
    style R fill:#d1fae5,color:#064e3b
```

---

## API Endpoint Listesi

### Customers

| Method | Endpoint | Açıklama | Başarı Kodu |
|---|---|---|---|
| GET | `/api/customers` | Tüm müşterileri listele | 200 |
| GET | `/api/customers/{id}` | Müşteriyi ID ile getir | 200 |
| GET | `/api/customers/{id}/summary` | Müşterinin borç özetini getir | 200 |
| POST | `/api/customers` | Yeni müşteri oluştur | 201 |
| PUT | `/api/customers/{id}` | Müşteri bilgilerini güncelle | 200 |
| DELETE | `/api/customers/{id}` | Müşteriyi soft-delete et (aktif borç varsa 422) | 204 |

### Loan Evaluation

| Method | Endpoint | Açıklama | Başarı Kodu |
|---|---|---|---|
| POST | `/api/loans/evaluate` | Kredi başvurusunu değerlendir (risk + faiz hesabı) | 200 |
| GET | `/api/loans/maximum-eligibility/{customerId}` | Müşterinin maksimum uygun kredi tutarını getir | 200 |
| GET | `/api/loans/evaluation/{evaluationId}` | Değerlendirme kaydını getir | 200 |
| GET | `/api/customers/{customerId}/evaluations` | Müşteriye ait değerlendirme geçmişini getir | 200 |

### Loans

| Method | Endpoint | Açıklama | Başarı Kodu |
|---|---|---|---|
| GET | `/api/loans` | Tüm kredileri listele | 200 |
| GET | `/api/loans/{id}` | Krediyi taksit detaylarıyla getir | 200 |
| POST | `/api/loans` | Kredi oluştur (taksit planı otomatik üretilir) | 201 |

### Installments

| Method | Endpoint | Açıklama | Başarı Kodu |
|---|---|---|---|
| GET | `/api/installments` | Tüm taksitleri listele (overdue günceller) | 200 |
| GET | `/api/installments/{id}` | Taksiti getir | 200 |
| PUT | `/api/installments/{id}` | Taksit durumunu güncelle | 200 |

### Payments

| Method | Endpoint | Açıklama | Başarı Kodu |
|---|---|---|---|
| GET | `/api/payments` | Tüm ödemeleri listele | 200 |
| POST | `/api/payments` | Ödeme gerçekleştir | 201 |

---

## Hata Yönetimi

Tüm hata senaryoları `ExceptionHandlingMiddleware` tarafından yakalanır ve tutarlı bir JSON formatında döner.

### Hata Tipleri ve HTTP Kodları

| Durum | HTTP Kodu | `type` Değeri |
|---|---|---|
| Kayıt bulunamadı | 404 | `NotFound` |
| İş kuralı ihlali | 422 | `BusinessRuleViolation` |
| Validasyon hatası | 400 | `ValidationError` |
| Sunucu hatası | 500 | `InternalServerError` |

### Örnek Yanıtlar

**404 Not Found:**
```json
{
  "type": "NotFound",
  "message": "99 numaralı müşteri bulunamadı."
}
```

**422 Business Rule Violation:**
```json
{
  "type": "BusinessRuleViolation",
  "message": "Bu taksit zaten ödenmiştir."
}
```

**422 Sıralı Ödeme İhlali:**
```json
{
  "type": "BusinessRuleViolation",
  "message": "Önceki ödenmemiş taksitler önce ödenmelidir."
}
```

**422 Aktif Borçlu Müşteri Silme:**
```json
{
  "type": "BusinessRuleViolation",
  "message": "Müşterinin 2 aktif kredisi ve toplam 45.000,00 TL borcu bulunmaktadır. Tüm borçlar kapatılmadan müşteri silinemez."
}
```

**400 Validation Error:**
```json
{
  "type": "ValidationError",
  "message": "Bir veya daha fazla validasyon hatası oluştu.",
  "errors": {
    "PrincipalAmount": ["Kredi tutarı 0'dan büyük olmalıdır."],
    "Term": ["Seçilen vade, 5000 TL kredi için uygun değil. Bu tutar için maksimum vade: 24 ay."]
  }
}
```

---

## Üçüncü Parti Servis Entegrasyonu

Kredi başvurusunda `ICreditScoreService` arayüzü üzerinden müşterinin kredi skoru sorgulanır. Arayüz `Application` katmanında tanımlıdır; implementasyon `Infrastructure` katmanındadır. Gerçek servis entegrasyonu gerektiğinde yalnızca `MockCreditScoreService` sınıfı değiştirilir — `LoanEvaluationService` hiçbir değişiklik gerektirmez (Açık/Kapalı Prensibi).

### Interface Tanımı

```csharp
// CreditCase.Application/Interfaces/External/ICreditScoreService.cs
public interface ICreditScoreService
{
    Task<CreditScoreResult> GetCreditScoreAsync(int customerId);
}

public record CreditScoreResult(
    int CustomerId,
    int CreditScore,              // 0-1000
    string RiskIndicator,         // "Low" | "Medium" | "High"
    IReadOnlyList<NegativeRecord> NegativeRecords,
    decimal DefaultProbability,   // 0.0-1.0
    DateTime QueryDate
);
```

### Mock Implementasyon — Profil Tabanlı Skor

`MockCreditScoreService`, rastgele değer üretmek yerine müşterinin gerçek profilinden skor hesaplar. Bu sayede aynı profil her zaman aynı skoru üretir (deterministik) ve test edilebilir sonuçlar elde edilir.

```mermaid
flowchart LR
    CUST[("Customer\n(DB)")]
    AGE["ScoreAge\nDoğum tarihi → yaş\nmaks. 200"]
    INC["ScoreIncome\nAylık gelir bantları\nmaks. 250"]
    EMP["ScoreEmployment\nİstihdam durumu\nmaks. 200"]
    PROF["ScoreProfession\nMeslek kategorisi\nmaks. 150"]
    BONUS["CreditScoreBonus\nÖdeme geçmişi\n−200 / +200"]

    SUM["Baz Skor\n(maks. 800)"]
    FINAL["Clamp(Baz + Bonus, 0, 1000)\nNihai Skor"]

    CUST --> AGE
    CUST --> INC
    CUST --> EMP
    CUST --> PROF
    CUST --> BONUS

    AGE --> SUM
    INC --> SUM
    EMP --> SUM
    PROF --> SUM

    SUM --> FINAL
    BONUS --> FINAL
```

**Mock servis yanıtı (örnek — 780 skorlu müşteri):**
```json
{
  "customerId": 3,
  "creditScore": 780,
  "riskIndicator": "Low",
  "negativeRecords": [],
  "defaultProbability": 0.05,
  "queryDate": "2026-05-12T10:00:00Z"
}
```

**Mock servis yanıtı (örnek — 520 skorlu müşteri):**
```json
{
  "customerId": 7,
  "creditScore": 520,
  "riskIndicator": "High",
  "negativeRecords": [
    {
      "recordType": "Payment Late",
      "recordDate": "2025-11-12T00:00:00Z",
      "amount": 1200.00
    }
  ],
  "defaultProbability": 0.35,
  "queryDate": "2026-05-12T10:00:00Z"
}
```

---

## Kurulum ve Çalıştırma

### Gereksinimler

- .NET 10 SDK
- Docker
- Node.js ≥ 20

### 1. SQL Server'ı başlat

```bash
docker run -e "ACCEPT_EULA=Y" \
           -e "SA_PASSWORD=StrongPass123!" \
           -p 1433:1433 \
           --name sqlserver \
           -d mcr.microsoft.com/mssql/server:2022-latest
```

### 2. Veritabanı migration'ını uygula

```bash
dotnet ef database update \
  --project CreditCase.Infrastructure \
  --startup-project CreditCase.Api
```

### 3. Backend'i başlat

```bash
dotnet run --project CreditCase.Api
```

Backend varsayılan olarak `http://localhost:5285` adresinde çalışır.  
Swagger arayüzü: `http://localhost:5285/swagger`

### 4. Frontend'i başlat

```bash
cd CreditCase.UI
npm install
cp .env.example .env   # VITE_API_URL değerini backend portuna göre düzenle
npm run dev
```

Frontend `http://localhost:5173` adresinde çalışır. Ayrıntılı kurulum için bkz. [`CreditCase.UI/README.md`](./CreditCase.UI/README.md).

### Birim testleri

```bash
dotnet test CreditCase.Tests
```

### Bağlantı Dizesi

`CreditCase.Api/appsettings.json` dosyasında tanımlıdır:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=CreditCaseDb;User Id=sa;Password=StrongPass123!;TrustServerCertificate=True;"
  }
}
```

---

## Geliştirme Adımları

Proje, Clean Architecture katman sırasına uygun ve bağımsız geliştirilebilir parçalar hâlinde ele alınmıştır.

### 1. Domain Katmanı — Temel Modelleme

`Customer`, `Loan`, `Installment`, `Payment`, `LoanEvaluationResult` entity'leri ile tüm enum'lar tanımlanmıştır. Entity ilişkileri navigation property'ler aracılığıyla kurulmuş; finansal alanlar için `decimal` tipi tercih edilmiştir.

### 2. Application Katmanı — İş Mantığı

Her entity için giriş (Request) ve çıkış (Response) DTO'ları oluşturulmuş; servis ve repository sözleşmeleri `Interfaces/` altında tanımlanmıştır. `FluentValidation` ile her request DTO için ayrı validator tanımlanmış; tutar-vade çapraz kural kontrolü de bu katmanda yer almaktadır.

### 3. Infrastructure Katmanı — Veri Erişimi ve Servisler

Beş adet risk kuralı (`CreditScoreRule`, `DebtToIncomeRule`, `ProfessionStabilityRule`, `AgeRule`, `EmploymentStatusRule`), `IRiskAnalysisRule` interface'i aracılığıyla `RiskAnalysisEngine`'e DI ile inject edilir. Yeni kural eklemek yalnızca yeni bir sınıf oluşturup DI'a kaydetmeyi gerektirir; engine kodu değişmez.

İki adet taksit planı stratejisi (`StandardInstallmentStrategy`, `BalloonPaymentStrategy`) `IInstallmentPlanStrategy` interface'ini implement eder.

### 4. API Katmanı — Sunum

Controller'lar enjekte edilen servis interface'lerini çağırır; herhangi bir iş mantığı içermez. `ExceptionHandlingMiddleware`, tüm exception türlerini yakalayarak tutarlı JSON hata formatına dönüştürür.

---

## Yapay Zeka Kullanımı

Bu projede yapay zeka destekli geliştirme yaklaşımı, belirli bir disiplin çerçevesinde uygulanmıştır.

| Alan | Kullanım Biçimi |
|---|---|
| Mimari tasarım | Clean Architecture katman sorumluluklarının belirlenmesinde referans olarak kullanıldı |
| Risk motoru tasarımı | Ağırlıklı kural motoru yaklaşımı tartışıldı; kuralların bağımsız sınıflara bölünmesi kararı AI önerisiyle şekillendi |
| Faiz hesaplama | Dinamik faiz bileşenleri (risk/vade/tutar primleri) ve gerçekçi Türk bankacılığı bantları AI ile belirlendi |
| Kredi skoru modelleme | 4 bileşenli profil bazlı skor algoritması AI ile tasarlandı; bantlar gerçek bankacılık referanslarına göre uyarlandı |
| EF Core konfigürasyonu | `HasPrecision`, `HasConversion<string>`, filtered unique index gibi konfigürasyon detayları AI yardımıyla hızlıca oluşturuldu |
| FluentValidation kuralları | Tutar-vade çapraz kural, TC/telefon format kuralları AI önerisiyle yazıldı ve domain'e uyarlandı |

### Kullanım Prensibi

AI çıktıları projeye doğrudan kopyalanmamıştır. Her çıktı şu süreçten geçirilmiştir:

1. **Analiz:** Üretilen kod veya yapı incelenerek ne yaptığı anlaşıldı.
2. **Domain uygunluğu kontrolü:** Bankacılık iş kurallarına ve projenin gereksinimlerine uyup uymadığı değerlendirildi.
3. **Uyarlama:** Gerektiğinde değiştirildi, eksik kısımlar tamamlandı, fazlalıklar çıkarıldı.
4. **Mimari tutarlılık:** Katmanlar arası bağımlılık kuralına ve Clean Architecture prensiplerine aykırılık oluşturup oluşturmadığı kontrol edildi.

Bu yaklaşımın temel amacı AI'ı bir üretkenlik aracı olarak kullanmak, çıktıyı kör bir şekilde kabul etmek değil; **AI çıktısını yönetme ve değerlendirme yetkinliğini** ortaya koymaktır.
