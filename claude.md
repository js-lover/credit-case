# CLAUDE.md

## Proje Adı

Digital Loan & Repayment Management System

---

## 1. Proje Tanımı

Bu proje, bireysel müşterilerin kredi başvurularını, kredi bakiyelerini ve geri ödeme planlarını dijital ortamda yönetebildiği full-stack bir bankacılık uygulamasıdır.

Sistem aşağıdaki temel bankacılık senaryolarını kapsamaktadır:

- Müşteri yönetimi
- Kredi tanımlama
- Taksit oluşturma
- Geri ödeme yönetimi
- Borç ve bakiye görüntüleme
- Üçüncü parti servis entegrasyonu (mock)

Bu proje, bankacılık domain mantığını doğru modelleme, para hareketlerini güvenli şekilde yönetme ve sürdürülebilir backend mimarisi kurma amacıyla geliştirilmektedir.

---

## 2. Proje Amaçları

Bu proje geliştirilirken aşağıdaki hedefler esas alınmalıdır:

- Finansal verilerin tutarlı şekilde yönetilmesi
- Katmanlı ve sürdürülebilir mimari kurulması
- RESTful API standartlarının uygulanması
- Domain odaklı tasarım yaklaşımı
- Kredi ve ödeme süreçlerinin doğru modellenmesi
- AI destekli geliştirme yaklaşımının bilinçli kullanılması

---

## 3. Mimari Yaklaşım

Projede **Clean Architecture** yaklaşımı kullanılmaktadır.

### Katmanlar

```
CreditCase.Api
│
├── Presentation Layer
│   ├── Controllers
│   ├── Middleware
│   └── Swagger

CreditCase.Application
│
├── Business Logic
├── DTOs
├── Interfaces
├── Services
└── Validation

CreditCase.Domain
│
├── Entities
├── Enums
├── Value Objects
└── Business Rules

CreditCase.Infrastructure
│
├── Persistence
├── Entity Framework Core
├── DbContext
├── Repository Implementations
├── Third Party Services
└── External Integrations
```

---

## 4. Teknoloji Stack'i

### Backend

- .NET / ASP.NET Core Web API
- Entity Framework Core
- C#

### Database

- Microsoft SQL Server
- Docker Container

### API Documentation

- Swagger / OpenAPI

### Development Environment

- Visual Studio Code
- Docker
- Git

---

## 5. Domain Modeli

Sistemde aşağıdaki temel entity'ler bulunmaktadır:

### Customer

Bankanın bireysel müşterisini temsil eder.

**Alanlar:**

| Alan | Açıklama |
|---|---|
| Id | Benzersiz tanımlayıcı |
| FirstName | Ad |
| LastName | Soyad |
| IdentityNumber | TC Kimlik Numarası |
| Email | E-posta adresi |
| PhoneNumber | Telefon numarası |
| CreatedAt | Kayıt tarihi |

---

### Loan

Müşteriye ait kredi bilgisini temsil eder.

**Alanlar:**

| Alan | Açıklama |
|---|---|
| Id | Benzersiz tanımlayıcı |
| CustomerId | Bağlı müşteri |
| LoanType | Kredi türü |
| PrincipalAmount | Ana para tutarı |
| RateAmount | Vade oranı (ratio) |
| Term | Vade (ay) |
| StartDate | Başlangıç tarihi |
| Status | Kredi durumu |
| RemainingPrincipal | Kalan ana para |

**LoanType:**

- `Personal`
- `Education`
- `Vehicle`

**LoanStatus:**

- `Active`
- `Closed`

---

### Installment

Krediye ait aylık ödeme planını temsil eder.

**Alanlar:**

| Alan | Açıklama |
|---|---|
| Id | Benzersiz tanımlayıcı |
| LoanId | Bağlı kredi |
| InstallmentNumber | Taksit sırası |
| Amount | Taksit tutarı |
| DueDate | Son ödeme tarihi |
| Status | Taksit durumu |

**InstallmentStatus:**

- `Paid`
- `Unpaid`
- `Overdue`

---

### Payment

Gerçekleştirilen ödeme işlemini temsil eder.

**Alanlar:**

| Alan | Açıklama |
|---|---|
| Id | Benzersiz tanımlayıcı |
| InstallmentId | Bağlı taksit |
| PaymentAmount | Ödeme tutarı |
| PaymentDate | Ödeme tarihi |
| Status | Ödeme durumu |

**PaymentStatus:**

- `Successful`
- `Failed`

---

### LoanEvaluationResult

Kredi başvurusu değerlendirmesi ve onay/red kararını temsil eder. Bu entity, müşterinin kredi başvurusunun risk analizi, uygunluk kontrolü ve sonuç değerlendirmesinin kayıtlarını içerir.

**Alanlar:**

| Alan | Açıklama |
|---|---|
| Id | Benzersiz tanımlayıcı |
| CustomerId | Bağlı müşteri |
| RequestedAmount | İstenen kredi tutarı |
| RequestedTerm | İstenen vade (ay) |
| IsApproved | Onay kararı (true/false) |
| ApprovedAmount | Onaylanan kredi tutarı |
| MaximumAmount | Müşteri için elde edilebilir maksimum tutar |
| MaximumTerm | Müşteri için elde edilebilir maksimum vade (ay) |
| ApprovedRateAmount | Onaylanan vade oranı (ratio) |
| RiskLevel | Müşteri risk seviyesi |
| CreditScore | Kredi değerlendirme skoru |
| DebtToIncomeRatio | Borç/Gelir oranı |
| MonthlyInstallmentEstimate | Tahmini aylık taksit tutarı |
| RejectionReason | Red sebebi (reddedildiyse) |
| EvaluationDate | Değerlendirme tarihi |
| ExpirationDate | Onay geçerlilik tarihi |

**RiskLevel Enum:**

- `Low` (Düşük Risk)
- `Medium` (Orta Risk)
- `High` (Yüksek Risk)
- `VeryHigh` (Çok Yüksek Risk)

---

## 6. Entity İlişkileri

```
Customer
    ├── Loans (1:N)
    └── LoanEvaluationResults (1:N)

Loan
    └── Installments (1:N)

Installment
    └── Payment (1:1)
```

---

## 6A. Kredi Değerlendirme & Onay Mekanizması

Sistem, müşteri kredi başvurusunu otomatik olarak değerlendirmeli ve onay/red kararı vermelidir. Bu mekanizma gerçekçi bankacılık standartlarını takip etmelidir.

### Değerlendirme Parametreleri

Kredi değerlendirmesi aşağıdaki müşteri parametrelerini dikkate almalıdır:

| Parametre | Açıklama | Örnek |
|-----------|----------|--------|
| **Yaş** | Müşterinin yaşı | 28 |
| **Aylık Gelir** | Müşterinin aylık net geliri | 5.000 TL |
| **Meslek** | Müşterinin meslek kategorisi | Yazılımcı, İnşaat, Öğretmen vb. |
| **Kredi Skoru** | Dış kredi değerlendirme servisi skoru | 0-1900 |
| **Mevcut Borç** | Müşterinin diğer krediler toplamı | 50.000 TL |
| **İstenen Tutar** | Başvurulan kredi miktarı | 20.000 TL |
| **İstenen Vade** | Başvurulan taksit dönemi (ay) | 24 ay |
| **İstihdam Durumu** | İşte olma/olmama durumu | Aktif, İşsiz, Serbest meslek |
| **Kredi Türü** | Kredi kategorisi | İhtiyaç, Araç, Eğitim |
| **Ödeme Geçmişi** | Geçmiş ödeme davranışı | Zamanında/Geç |

### Vade Oranı Sistemi (Rate System)

Sistemde kullanılan oranlar yüzde (%) değil, decimal oran (ratio) formatında hesaplanır ve gösterilir. Bu, uluslararası fintech standartlarına uygun, profesyonel bir gösterimdir.

#### Oran Formatı Açıklaması

**Yanlış (UI'de gösterilmemeli):**
- Faiz Yüzdesi: %4.48
- Faiz Oranı: 4.48%

**Doğru (Profesyonel Format):**
- Vade Oranı: 4.48 (veya 4.48x olarak gösterilebilir)
- Açıklaması: Aylık 0.186 oran ile 24 ayda toplam vade oranı 4.48
- Örnek: 100.000 TL kredi, 4.48 vade oranı, 24 ay = ~108.000 TL geri ödeme

Kullanıcı arayüzünde vade oranı, basit ve profesyonel şekilde şu formatta gösterilmeli:
```
Vade Oranı: 4.48
Aylık Oran: 0.186
```

---

### Kredi Skoru Kategorileri (Türkiye Bankacılık Standardı)

Sistem, kredi skorunu 5 kategoriye böler. Her kategori farklı risk profili ve vade oranı aralığını temsil eder:

| Kategori | Skor Aralığı | Risk Seviyesi | Vade Oranı Aralığı | Maksimum Kredi |
|----------|---------|--------|---------|--------|
| Kritik | 0 - 969 | Çok Yüksek | 5.5 - 7.2 | İnceleme Gerekli |
| Gelişime Açık | 970 - 1149 | Yüksek | 4.5 - 5.5 | Gelir × 3 |
| Dengeli | 1150 - 1469 | Orta | 3.5 - 4.5 | Gelir × 10 |
| Güvenli | 1470 - 1719 | Düşük | 2.5 - 3.5 | Gelir × 15 |
| Prestijli | 1720 - 1900 | Çok Düşük | 1.5 - 2.5 | Gelir × 20 |

#### Kategori Özellikleri

**Kritik (0-969):**
- Temerrüt riski yüksek
- Ödeme geçmişi sorunlu
- Borç/Gelir oranı > 0.70
- Kredi verme kararı manuel inceleme gerektirir
- Vade Oranı: 5.5 - 7.2 aralığında dinamik belirlenir

**Gelişime Açık (970-1149):**
- Yüksek risk profili
- Ödeme davranışında düzensizlikler
- Borç/Gelir oranı: 0.51 - 0.70
- Vade Oranı: 4.5 - 5.5 aralığında
- Maksimum kredi: Aylık gelir × 3

**Dengeli (1150-1469):**
- Orta risk profili
- Stabil ödeme davranışı
- Borç/Gelir oranı: 0.31 - 0.50
- Vade Oranı: 3.5 - 4.5 aralığında
- Maksimum kredi: Aylık gelir × 10

**Güvenli (1470-1719):**
- Düşük risk profili
- İyi ödeme geçmişi
- Borç/Gelir oranı: 0.10 - 0.30
- Vade Oranı: 2.5 - 3.5 aralığında
- Maksimum kredi: Aylık gelir × 15

**Prestijli (1720-1900):**
- Çok düşük risk profili
- Mükemmel ödeme geçmişi
- Borç/Gelir oranı: ≤ 0.10
- Vade Oranı: 1.5 - 2.5 aralığında (en avantajlı)
- Maksimum kredi: Aylık gelir × 20

---

### Dinamik Vade Oranı Hesaplama (Detaylı Mekanizma)

#### İlke ve Kısıtlamalar

1. **Hardcoded Oran Yasağı**: UI tarafından vade oranı manuel olarak girilemez
2. **Otomatik Hesaplama**: Sistem, verilen parametrelerden vade oranını hesaplar
3. **Parametreler**: Kredi skoru kategorisi, kredi türü, vade süresi, müşteri profili
4. **Dinamiklik**: Vade süresi arttıkça vade oranı da artar

#### Kredi Türüne Göre Temel Vade Oranı (Türkiye Bankacılık Standarları)

Bu oranlar, 12 ay vade için referans oranlar olup, vade süresine göre dinamik olarak değişir:

| Kredi Türü | Kritik | Gelişime Açık | Dengeli | Güvenli | Prestijli |
|-----------|--------|--------|---------|---------|---------|
| İhtiyaç Kredisi | 6.8 | 5.2 | 4.0 | 3.0 | 2.0 |
| Araç Kredisi | 5.8 | 4.2 | 3.0 | 2.0 | 1.2 |
| Eğitim Kredisi | 5.2 | 3.8 | 2.7 | 1.7 | 0.9 |

Burada 12 ay süresi için yazılan oranlar, temel vade oranlarıdır.

#### Vade Süresi Dinamik Aralıkları (Sistem Tarafından Belirlenen Sınırlar)

Her kredi skoru kategorisinin kendi vade süresi sınırları vardır. UI tarafından istenen vade, bu sınırlar içinde olmalıdır:

| Kategori | Minimum Vade | Maksimum Vade | Minimum Taksit |
|----------|---------|---------|---------|
| Kritik | 6 ay | 24 ay | 50.000 TL |
| Gelişime Açık | 6 ay | 36 ay | 25.000 TL |
| Dengeli | 6 ay | 48 ay | 15.000 TL |
| Güvenli | 6 ay | 60 ay | 10.000 TL |
| Prestijli | 6 ay | 72 ay | 5.000 TL |

**Açıklamalar:**
- Müşteri talep ettiği vade bu aralığın dışındaysa, sistem tarafından en yakın sınıra çekilir
- Örnek: Kritik kategorideki müşteri 48 ay isterse, sistem otomatik olarak 24 ay ata
- Örnek: Prestijli kategorideki müşteri 36 ay isterse, kabul edilir (sınırlar: 6-72)

#### Vade Süresine Göre Vade Oranı Dinamik Artışı

Temel vade oranı (12 ay için), vade süresine göre şu dinamik faktörleri alır:

```
Son Vade Oranı = Temel Vade Oranı × (1 + Vade Faktörü)

Vade Faktörü Tablosu:
- 6 ay: -0.25 (indirimli oran)
- 12 ay: 0.00 (temel oran)
- 18 ay: +0.08
- 24 ay: +0.15
- 36 ay: +0.28
- 48 ay: +0.42
- 60 ay: +0.58
- 72 ay: +0.75 (en yüksek)
```

**Örneğin:**
- Temel Oran (12 ay, Dengeli): 4.0
- İstenilen Vade: 24 ay
- Vade Faktörü: +0.15
- Son Vade Oranı = 4.0 × (1 + 0.15) = 4.0 × 1.15 = 4.60

#### Meslek Bonusu/Penaltısı (Kategoriye Uygulanır)

Vade oranının son hali, meslek kategorisine göre ayarlanabilir:

| Meslek | Bonus/Penalti | Açıklama |
|--------|--------|----------|
| Kamu Personeli | -0.3 | Sabit gelir, istikrardan indirim |
| Yazılım Mühendisi | -0.2 | Yüksek gelir sektörü |
| Doktor/Avukat | -0.2 | Profesyonel sektör |
| Öğretmen | -0.15 | Kamu sektörü avantajı |
| Satış Danışmanı | +0.2 | Değişken gelir |
| Freelancer | +0.3 | Değişken gelir, risk yükselişi |
| İnşaat Sektörü | +0.2 | Mevsimsel risk |

**Hesaplama Sırası:**
1. Kredi kategorisine göre temel vade oranı seç
2. Vade süresine göre faktörü uygula
3. Meslek bonusu/penaltısını ekle/çıkar

---

### Örnek Hesaplama 1: Güvenli Kategorisi

#### Senaryo

- **Müşteri**: Yazılım Mühendisi
- **Yaş**: 28
- **Aylık Gelir**: 8.000 TL
- **Kredi Skoru**: 1550 → **Güvenli Kategorisi**
- **İstenen Kredi Tutarı**: 25.000 TL
- **İstenen Vade**: 24 ay
- **Kredi Türü**: İhtiyaç Kredisi
- **Mevcut Borç**: 30.000 TL

#### Adım 1: Kredi Skoru Kategorisini Belirle

- Skor: 1550 → **Güvenli (1470-1719)**
- Bu kategoride:
  - Vade Oranı Aralığı: 2.5 - 3.5
  - Maksimum Kredi: Gelir × 15 = 120.000 TL
  - Maksimum Vade: 60 ay
  - Minimum Taksit: 10.000 TL

#### Adım 2: Vade Süresini Doğrula

- İstenen vade: 24 ay
- Güvenli kategorisi sınırları: 6-60 ay
- **24 ay → Geçerli ✓**

#### Adım 3: Temel Vade Oranını Seç

- Kredi Türü: İhtiyaç Kredisi
- Kategori: Güvenli
- **Temel Vade Oranı (12 ay): 3.0**

#### Adım 4: Vade Faktörünü Uygula

- İstenen Vade: 24 ay
- Vade Faktörü: +0.15
- **Vade Uygulanmış Oran = 3.0 × (1 + 0.15) = 3.0 × 1.15 = 3.45**

#### Adım 5: Meslek Bonusu Uygula

- Meslek: Yazılım Mühendisi
- Bonus: -0.2
- **Son Vade Oranı = 3.45 - 0.2 = 3.25**

#### Adım 6: Doğrulamalar

**Borç/Gelir Oranı:**
- Mevcut Borç: 30.000 TL
- Aylık Gelir: 8.000 TL
- Borç/Gelir: 30.000 / (8.000 × 12) = 0.31
- **Sınır: ≤ 0.30 → UYARI (Sınıra çok yakın)**

**İstenen Kredi Kontrolleri:**
- İstenen Tutar: 25.000 TL
- Maksimum Kredi: 120.000 TL
- **25.000 ≤ 120.000 → Geçerli ✓**

**Tahmini Aylık Taksit:**
- Kredi: 25.000 TL
- Vade Oranı: 3.25
- Vade: 24 ay
- Aylık Oran: 3.25 / 24 = 0.135
- **Tahmini Taksit: ~1.135 TL**
- Minimum Taksit Sınırı: 10.000 TL
- **1.135 < 10.000 → UYARI (Taksit çok düşük)**

#### Final Sonuç

```
Kredi Türü: İhtiyaç Kredisi
Kategori: Güvenli (Skor: 1550)
Vade Oranı: 3.25
Aylık Oran: 0.135
Vade Süresi: 24 ay
Maksimum Kredi: 120.000 TL
Tahmini Aylık Taksit: 1.135 TL
Onay Durumu: RED (Taksit çok düşük / Borç oranı yüksek)
```

**Not:** Sistem bu müşteri için vadeyi 48 aya çıkarmayı veya kredi tutarını arttırmayı önerebilir.

---

### Örnek Hesaplama 2: Dengeli Kategorisi

#### Senaryo

- **Müşteri**: Satış Danışmanı
- **Yaş**: 32
- **Aylık Gelir**: 5.000 TL
- **Kredi Skoru**: 1200 → **Dengeli Kategorisi**
- **İstenen Kredi Tutarı**: 15.000 TL
- **İstenen Vade**: 36 ay
- **Kredi Türü**: İhtiyaç Kredisi
- **Mevcut Borç**: 15.000 TL

#### Adım 1: Kredi Skoru Kategorisini Belirle

- Skor: 1200 → **Dengeli (1150-1469)**
- Bu kategoride:
  - Vade Oranı Aralığı: 3.5 - 4.5
  - Maksimum Kredi: Gelir × 10 = 50.000 TL
  - Maksimum Vade: 48 ay
  - Minimum Taksit: 15.000 TL

#### Adım 2: Vade Süresini Doğrula

- İstenen vade: 36 ay
- Dengeli kategorisi sınırları: 6-48 ay
- **36 ay → Geçerli ✓**

#### Adım 3: Temel Vade Oranını Seç

- Kredi Türü: İhtiyaç Kredisi
- Kategori: Dengeli
- **Temel Vade Oranı (12 ay): 4.0**

#### Adım 4: Vade Faktörünü Uygula

- İstenen Vade: 36 ay
- Vade Faktörü: +0.28
- **Vade Uygulanmış Oran = 4.0 × (1 + 0.28) = 4.0 × 1.28 = 5.12**

#### Adım 5: Meslek Bonusu Uygula

- Meslek: Satış Danışmanı
- Penalti: +0.2
- **Son Vade Oranı = 5.12 + 0.2 = 5.32**

#### Adım 6: Doğrulamalar

**Borç/Gelir Oranı:**
- Mevcut Borç: 15.000 TL
- Aylık Gelir: 5.000 TL
- Borç/Gelir: 15.000 / (5.000 × 12) = 0.25
- **Sınır: ≤ 0.30 → Geçerli ✓**

**İstenen Kredi Kontrolleri:**
- İstenen Tutar: 15.000 TL
- Maksimum Kredi: 50.000 TL
- **15.000 ≤ 50.000 → Geçerli ✓**

**Tahmini Aylık Taksit:**
- Kredi: 15.000 TL
- Vade Oranı: 5.32
- Vade: 36 ay
- Aylık Oran: 5.32 / 36 = 0.148
- **Tahmini Taksit: ~555 TL**
- Minimum Taksit Sınırü: 15.000 TL
- **555 < 15.000 → UYARI (Taksit minimum sınırı altında)**

#### Final Sonuç

```
Kredi Türü: İhtiyaç Kredisi
Kategori: Dengeli (Skor: 1200)
Vade Oranı: 5.32
Aylık Oran: 0.148
Vade Süresi: 36 ay
Maksimum Kredi: 50.000 TL
Tahmini Aylık Taksit: 555 TL
Onay Durumu: RED (Taksit minimum sınırı altında)
```

**Not:** Sistem bu müşteri için kredi tutarını 30.000+ TL'ye çıkarmayı önerebilir.

---

### Örnek Hesaplama 3: Güvenli & Avantajlı

#### Senaryo

- **Müşteri**: Öğretmen (Kamu)
- **Yaş**: 35
- **Aylık Gelir**: 6.500 TL
- **Kredi Skoru**: 1650 → **Güvenli Kategorisi**
- **İstenen Kredi Tutarı**: 50.000 TL
- **İstenen Vade**: 24 ay
- **Kredi Türü**: İhtiyaç Kredisi
- **Mevcut Borç**: 20.000 TL

#### Adım 1: Kredi Skoru Kategorisini Belirle

- Skor: 1650 → **Güvenli (1470-1719)**
- Maksimum Kredi: 97.500 TL
- Maksimum Vade: 60 ay

#### Adım 2: Vade Süresini Doğrula

- İstenen vade: 24 ay
- **24 ay → Geçerli ✓**

#### Adım 3: Temel Vade Oranını Seç

- Kredi Türü: İhtiyaç Kredisi
- Kategori: Güvenli
- **Temel Vade Oranı (12 ay): 3.0**

#### Adım 4: Vade Faktörünü Uygula

- İstenen Vade: 24 ay
- Vade Faktörü: +0.15
- **Vade Uygulanmış Oran = 3.0 × 1.15 = 3.45**

#### Adım 5: Meslek Bonusu Uygula

- Meslek: Öğretmen (Kamu)
- Bonus: -0.15
- **Son Vade Oranı = 3.45 - 0.15 = 3.30**

#### Adım 6: Doğrulamalar

**Borç/Gelir Oranı:**
- Mevcut Borç: 20.000 TL
- Aylık Gelir: 6.500 TL
- Borç/Gelir: 20.000 / (6.500 × 12) = 0.256
- **Sınır: ≤ 0.30 → Geçerli ✓**

**İstenen Kredi:**
- 50.000 ≤ 97.500 → Geçerli ✓

**Tahmini Aylık Taksit:**
- Vade Oranı: 3.30
- Vade: 24 ay
- **Tahmini Taksit: ~2.150 TL**
- Minimum: 10.000 TL
- **2.150 < 10.000 → UYARI**

#### Final Sonuç

```
Kredi Türü: İhtiyaç Kredisi
Kategori: Güvenli (Skor: 1650)
Vade Oranı: 3.30
Aylık Oran: 0.138
Vade Süresi: 24 ay
Tahmini Aylık Taksit: 2.150 TL
Onap Durumu: RED (Taksit çok düşük)
```

---

### Reddetme Senaryoları (Sistem Tarafından Otomatik Red)

Aşağıdaki durumlardan herhangi biri gerçekleşirse, sistem krediyi otomatik olarak reddeder:

| Koşul | Neden | Açıklama |
|-------|------|----------|
| Kredi Skoru < 400 | Veri Hatası | Sistem değil, harici servisten hata |
| Borç/Gelir > 0.70 | Ödeme Gücü Yetersiz | Mevcut borçlar çok yüksek |
| İstenen Tutar > Maksimum | Limit Aşılması | Kategorisine göre kredi limiti aşılmış |
| İstenen Vade > Maksimum | Vade Sınırı Aşılması | Kategorisine göre maksimum vade aşılmış |
| İstenen Vade < Minimum | Vade Çok Kısa | Taksit tutarı minimum sınırını aşmış |
| Tahmini Taksit < Minimum | Taksit Çok Düşük | Kategorideki minimum taksit sınırı altında |
| İstihdam Durumu = İşsiz | İstihdama Sahip Değil | Gelir kaynağı belirsiz |

**Red Durumunda:**
- Sistem otomatik olarak REJECTED döner
- Red Sebebi: Yukarıdaki tablonun Açıklama sütundan seçilir
- Kullanıcı Bildirimi: Profesyonel, açık bir şekilde bildirilir

---

### Sistem Tarafından Belirlenen Optimum Vade

Eğer müşteri vade belirtmezse veya sistem tarafından otomatik önerilecekse:

| Kategori | Önerilen Vade | Neden |
|----------|--------|---------|
| Kritik | 12 ay | Minimum risk taşıması |
| Gelişime Açık | 18 ay | Dengeli taksit yükü |
| Dengeli | 24 ay | Standard seçim |
| Güvenli | 36 ay | Düşük taksit yükü |
| Prestijli | 48 ay | Maksimum esneklik |

Bu öneriler, müşterinin aylık gelir ve kredi tutarı göz önüne alınarak uyarlanabilir.

---

### UI Gösterimi (Mock-up Örneği)

Sistem tarafından hesaplanan sonuçlar, kullanıcıya şu şekilde gösterilmeli:

```
Kredi Değerlendirme Sonucu
━━━━━━━━━━━━━━━━━━━━━━━━━━

Kredi Skoru: 1650 (Güvenli)
Risk Seviyesi: Düşük

Kredi Türü: İhtiyaç Kredisi
İstenen Tutar: 50.000 TL
Vade Süresi: 24 ay

Vade Oranı: 3.30
Aylık Oran: 0.138

Tahmini Aylık Taksit: 2.150 TL
Toplam Ödeme: 51.600 TL
Toplam Ek Ödeme: 1.600 TL

Maksimum Kredi Limiti: 97.500 TL
Kalan Kredilendirme Kapasitesi: 47.500 TL

Geçerlilik Süresi: 7 Gün

┌────────────────────────────┐
│   KREDİ ONAYLANDI ✓        │
└────────────────────────────┘

[ Kabul Et ]  [ Reddet ]  [ Detayları Gör ]
```

---

### Önemli Notlar

1. **Hardcoded Yasağı**: Hiçbir şekilde vade oranı UI tarafından manuel girilmemeli
2. **Sistem Kontrolü**: Tüm parametreler sistem tarafından doğrulanmalı
3. **Sınırlar Net**: Kategorilere göre vade ve kredi sınırları kesin olmalı
4. **Dinamiklik**: Vade arttıkça vade oranı da dinamik olarak artsın
5. **Profesyonellik**: Oran formatı (4.48, 3.25) yüzde değil, ratio olmalı
6. **Geçerlilik**: Onaylanan kredi 7 gün boyunca geçerli olmalı

---

### Risk Analizi Motoru (Risk Analysis Engine)

Sistem, aşağıdaki parametreleri göz önüne alarak risk seviyesini belirlemeli:

#### Risk Parametreleri ve Ağırlıkları

```
Toplam Risk Puanı = 
  (Kredi Skoru Puanı × 0.30) +
  (Borç/Gelir Oranı Puanı × 0.25) +
  (Yaş Puanı × 0.15) +
  (Meslek Stabilite Puanı × 0.20) +
  (İstihdam Durumu Puanı × 0.10)

Risk Puanı: 0-100
```

Sisteme göre:
- 0-25: Prestijli Kategorisi
- 25-40: Güvenli Kategorisi
- 40-60: Dengeli Kategorisi
- 60-80: Gelişime Açık Kategorisi
- 80-100: Kritik Kategorisi

---

### Taksit Planı Üretimi (Installment Generation)

Kredi onaylanıp oluşturulduğunda, sistem otomatik olarak taksit planı üretmelidir.

#### Aylık Taksit Hesaplama (Amortisasyon Metodu)

```
A = P × [r(1+r)^n] / [(1+r)^n - 1]

Burada:
- A = Aylık Taksit Tutarı
- P = Ana Para (Kredi Tutarı)
- r = Aylık Oran (Vade Oranı / Ay Sayısı)
- n = Toplam Taksit Sayısı (Ay)
```

#### Örnek Taksit Planı

```
Kredi: 50.000 TL
Vade: 24 ay
Vade Oranı: 3.30
Aylık Oran: 3.30 / 24 = 0.1375

Aylık Taksit = 50.000 × 0.1375 / 24 ≈ 2.260 TL

Taksit 1: 2.260 TL (Ana para: 2.083 TL, Ek Ödeme: 177 TL)
Kalan Bakiye: 47.917 TL

Taksit 2: 2.260 TL (Ana para: 2.095 TL, Ek Ödeme: 165 TL)
Kalan Bakiye: 45.822 TL

...

Taksit 24: 2.260 TL (Ana para: 2.248 TL, Ek Ödeme: 12 TL)
Kalan Bakiye: 0 TL
```

#### Taksit Planı Oluşturma Kuralları

- Her taksit için son ödeme tarihi belirlenmelidir (örn: her ayın 25'i)
- İlk taksit tarihi kredi başlangıç tarihinden 1 ay sonra
- Geçmiş tarihli taksitin durumu otomatik olarak `Overdue` kontrol edilmeli
- Kalan bakiye hesaplaması her ödemede güncellenmeli
- Ödeme yapıldıktan sonra ana para ve ek ödeme kısmı ayrı tutulmalı

### Mock Credit Score Service

Sistem, müşteri risk değerlendirmesi için dış bir kredi skoru servisi ile entegre olmalıdır (mock).

#### Servis Tanımı

Dış servis, müşteri kimlik numarasını alıp aşağıdaki cevabı döner:

```json
{
  "customerId": 1,
  "creditScore": 1650,
  "riskLevel": "Low",
  "negativeRecords": [],
  "defaultProbability": 0.05,
  "queryDate": "2024-01-15"
}
```

- **creditScore**: 0-1900 arasında değişen kredi skoru
- **riskLevel**: Low / Medium / High / VeryHigh
- **negativeRecords**: Geçmiş ödeme gecikmesi, temerrüt vb. kayıtlar
- **defaultProbability**: Temerrüt olasılığı (0.0-1.0)

---

## 7. İş Kuralları

### Kredi Oluşturma

- Bir müşteri birden fazla krediye sahip olabilir.
- Kredi oluşturulduğunda sistem otomatik olarak taksit planı üretmelidir.
- Vade oranı sistem tarafından dinamik olarak hesaplanmalıdır.

### Taksit Yönetimi

- Her taksit yalnızca bir krediye ait olabilir.
- Her taksidin son ödeme tarihi bulunmalıdır.
- Ödenmeyen ve tarihi geçmiş taksitler `Overdue` olarak işaretlenmelidir.

### Ödeme Yönetimi

- Bir ödeme yalnızca tek bir takside ait olabilir.
- Başarılı ödeme sonrası ilgili taksidin durumu `Paid` olmalıdır.
- Ödeme tarihi ve ödeme tutarı saklanmalıdır.

---

## 8. API Tasarım Kuralları

RESTful API standartları kullanılmalıdır.

### Customers

```http
GET     /api/customers
GET     /api/customers/{id}
POST    /api/customers
PUT     /api/customers/{id}
DELETE  /api/customers/{id}
```

### Loans

```http
GET     /api/loans
GET     /api/loans/{id}
POST    /api/loans
```

### Installments

```http
GET     /api/installments
GET     /api/installments/{id}
PUT     /api/installments/{id}
```

### Payments

```http
GET     /api/payments
POST    /api/payments
```

---

## 9. Validation Kuralları

### Customer

- `FirstName` boş olamaz
- `LastName` boş olamaz
- `IdentityNumber` benzersiz olmalıdır
- `Email` formatı doğrulanmalıdır

### Loan

- `PrincipalAmount` > 0 olmalıdır
- `RateAmount` 0-10 aralığında olmalıdır
- `Term` minimum 6 ay, maksimum 72 ay olmalıdır

### Payment

- `PaymentAmount` > 0 olmalıdır
- Aynı taksit için ikinci ödeme oluşturulamamalıdır

---

## 10. Exception Handling

Global exception handling mekanizması kullanılmalıdır.

Beklenen hata senaryoları:

- Resource not found
- Validation errors
- Business rule violations
- Database exceptions
- Third-party service failures

API response formatı tutarlı olmalıdır.

---

## 11. Veritabanı Yaklaşımı

### ORM

Entity Framework Core kullanılmalıdır.

### Migration

- Code First yaklaşımı kullanılmalıdır.
- Migration'lar Git içerisinde tutulmalıdır.

### Precision

Finansal işlemler için `decimal` tipi kullanılmalıdır. `float` veya `double` kullanılmamalıdır.

```csharp
decimal PrincipalAmount { get; set; }
decimal RateAmount { get; set; }
```

---

## 12. Üçüncü Parti Servis Entegrasyonu

En az 1 adet mock dış servis entegrasyonu yapılmalıdır.

### Credit Score Service (Mock)

Mock servis aşağıdaki formatta yanıt döner:

```json
{
  "customerId": 1,
  "creditScore": 1650,
  "riskLevel": "Low",
  "status": "Approved"
}
```

---

## 13. AI Kullanımı

Bu projede AI destekli geliştirme yaklaşımı bilinçli şekilde kullanılmaktadır.

AI aşağıdaki alanlarda destek amacıyla kullanılmıştır:

- Mimari tasarım önerileri
- Refactoring desteği
- Validation önerileri
- API tasarımı
- Error handling yaklaşımı
- Entity ilişkilerinin modellenmesi
- Docker & SQL Server yapılandırması
- EF Core migration süreçleri

### AI Kullanım Prensibi

AI tarafından üretilen çıktılar doğrudan kullanılmamalıdır.

Beklenen yaklaşım:

1. AI çıktısını analiz etmek
2. Business kurallarına uygunluğunu kontrol etmek
3. Gerektiğinde düzenlemek
4. Mimari tutarlılığı korumak

Amaç yalnızca AI kullanmak değil, **AI çıktısını yönetebilme yeteneğini** göstermektir.

---

## 14. Git & Branch Stratejisi

### Branch Yapısı

```
main
develop
feature/*
```

### Commit Kuralları

Conventional Commit standardı kullanılmalıdır.

**Örnekler:**

```
feat: add customer management endpoints
feat: implement installment generation service
fix: resolve payment transaction issue
chore: configure EF Core migrations
```

Commit geçmişi okunabilir ve anlamlı olmalıdır.

---

## 15. Teknik Beklentiler

Projede aşağıdaki teknik beklentiler dikkate alınmalıdır:

- Clean Architecture veya katmanlı mimari
- RESTful API tasarımı
- DTO / Entity ayrımı
- Validation mekanizması
- Exception handling
- Git versiyon kontrolü
- Dokümantasyon kalitesi

---

## 16. Dokümantasyon Beklentileri

Projede aşağıdaki dokümantasyonların bulunması beklenmektedir:

- `README.md`
- ER Diagram
- API endpoint listesi
- Kredi oluşturma → taksit üretme akış diyagramı

---

## 17. Değerlendirme Kriterleri

Projede özellikle aşağıdaki alanlar değerlendirilecektir:

- Bankacılık domain mantığı
- Para & bakiye tutarlılığı
- Veri modelleme kalitesi
- Kod okunabilirliği
- API tasarımı
- Dokümantasyon kalitesi
- AI kullanım yaklaşımı

---

## 18. Clean Architecture Yerleşimi

Kredi değerlendirme mekanizması aşağıdaki katmanlara dağıtılmalıdır:

### Domain Layer (CreditCase.Domain)

```
CreditCase.Domain
│
├── Entities
│   ├── LoanEvaluationResult.cs
│   ├── Customer.cs
│   ├── Loan.cs
│   ├── Installment.cs
│   └── Payment.cs
│
├── ValueObjects
│   ├── CreditScore.cs
│   ├── RiskLevel.cs
│   ├── RateAmount.cs
│   └── DebtToIncomeRatio.cs
│
├── Enums
│   ├── RiskCategory.cs
│   ├── ProfessionCategory.cs
│   ├── EmploymentStatus.cs
│   ├── LoanStatus.cs
│   └── LoanType.cs
│
└── Interfaces
    ├── IRiskAnalysisRule.cs
    ├── ILoanEligibilityRule.cs
    └── IRateCalculationRule.cs
```

### Application Layer (CreditCase.Application)

```
CreditCase.Application
│
├── Services
│   ├── ILoanEvaluationService.cs
│   ├── IRiskAnalysisService.cs
│   ├── IRateCalculationService.cs
│   ├── IMaximumLoanCalculator.cs
│   ├── IInstallmentGenerationService.cs
│   └── Implementations
│
├── DTOs
│   ├── LoanApplicationRequest.cs
│   ├── LoanEvaluationResponse.cs
│   ├── InstallmentPlanDto.cs
│   └── RiskAnalysisResultDto.cs
│
└── Validators
    ├── LoanApplicationValidator.cs
    └── CustomerFinancialProfileValidator.cs
```

### Infrastructure Layer (CreditCase.Infrastructure)

```
CreditCase.Infrastructure
│
├── Services
│   ├── CreditScoreService.cs (Mock)
│   ├── RiskAnalysisEngine.cs
│   ├── RateCalculationEngine.cs
│   ├── MaximumLoanCalculator.cs
│   ├── InstallmentGenerator.cs
│   └── LoanEvaluationProcessor.cs
│
├── Repositories
│   ├── ILoanEvaluationRepository.cs
│   └── LoanEvaluationRepository.cs
│
└── Persistence
    ├── DbContext
    └── Migrations
```

### API Layer (CreditCase.Api)

```
CreditCase.Api
│
├── Controllers
│   ├── LoanEvaluationController.cs
│   │   POST   /api/loans/evaluate
│   │   GET    /api/loans/evaluation/{id}
│   │   GET    /api/loans/maximum-eligibility/{customerId}
│   │
│   └── LoansController.cs
│       POST   /api/loans
│
└── Middleware
    └── LoanEvaluationExceptionMiddleware.cs
```

---

## 19. SOLID Prensipleri & Ölçeklenebilirlik

### S - Single Responsibility Prensibi

Her sınıf ve servis sadece bir işten sorumlu olmalıdır:

- **RiskAnalysisService**: Yalnızca risk seviyesini belirler
- **RateCalculationService**: Yalnızca vade oranını hesaplar
- **MaximumLoanCalculator**: Yalnızca maksimum kredi tutarını bulur
- **InstallmentGenerationService**: Yalnızca taksit planını oluşturur
- **LoanEvaluationService**: Tüm bu servisleri koordine eder

Bunun tersi olan "God Service" anti-pattern'ından kaçınılmalıdır.

### O - Open/Closed Prensibi

Sisteminiz yeni kurallar eklenmeye açık, değiştirilmeye kapalı olmalıdır. Risk analizi için bunu şu şekilde sağlanır:

Her risk kuralı ayrı bir interface'i implement eder. Yeni bir risk kuralı eklemek gerektiğinde:
1. Mevcut kodu değiştirmek yerine
2. Yeni bir rule sınıfı oluşturulur
3. Dependency Injection container'ında sisteme eklenir

Bu sayede yeni kurallar kolayca eklenebilir. Mevcut koddaki risk engine değiştirilmez.

### L - Liskov Substitution Prensibi

Tüm rule implementasyonları birbirinin yerine kullanılabilir olmalıdır.

Kredi uygunluğu kuralları örneğinde, tüm kurallar:
- Aynı parametreleri alır (müşteri, istenen tutar)
- Aynı çıktıyı üretir (uygun/uygun değil, nedeni)
- Sistem içinde birbirinin yerine geçebilir

### I - Interface Segregation Prensibi

Büyük, monolitik interface'ler yerine, küçük ve spesifik interface'ler tasarlayın. Her servis sadece kendisine gerekli interface'i inject alır.

### D - Dependency Inversion Prensibi

Servisler concrete sınıflara değil, abstraction'lara (interface'lere) bağlı olmalıdır. Startup'ta hangi interface'in hangi implementasyonu kullanacağını tanımlarsınız, sonrası otomatik olur.

---

## 20. Validation & Exception Handling

### Custom Domain Exceptions

Kredi değerlendirme sistemi kendi özel exception'larını tanımlamalıdır:

- **LoanApplicationDeniedException**: Kredi başvurusu reddedildiğinde
- **InsufficientCreditScoreException**: Kredi skoru minimum limitin altında
- **ExcessiveDebtRatioException**: Borç/gelir oranı çok yüksek
- **InvalidCustomerProfileException**: Müşteri verisi eksik veya geçersiz
- **CreditScoreServiceUnavailableException**: Dış servis yanıt vermiyor

### Business Rule Validation

Kredi başvurusu kabul edilmeden önce bir dizi doğrulama yapılmalıdır:

**Müşteri Seviyesi Validasyonlar:**
- Yaşı 21-70 arasında mı?
- Aylık gelir pozitif mi?
- Kimlik numarası geçerli mi?
- İstihdam durumu "işsiz" değil mi?

**Kredi Başvurusu Seviyesi Validasyonlar:**
- İstenen tutar sistem limitini aşmıyor mu?
- İstenen vade minimum/maksimum sınırlar içinde mi?
- Kredi türü tanınmış bir tür mü?

### Global Exception Middleware

Tüm hataları yakalamak ve HTTP cevaplarına çevirmek için ASP.NET Core'un Middleware yapısı kullanılmalıdır.

Exception türüne göre farklı HTTP status kodları döndürülmelidir:
- **400 Bad Request**: Validasyon hataları
- **422 Unprocessable Entity**: Business rule ihlali
- **503 Service Unavailable**: Dış servis çağrısı başarısız
- **500 Internal Server Error**: Beklenmeyen sistem hatası

### API Response Standardı

Tüm hata cevapları tutarlı bir formatı takip etmelidir:

```json
{
  "statusCode": 422,
  "message": "Kredi başvurusu değerlendirilmiştir",
  "details": {
    "reason": "Kredi skoru yetersiz",
    "errorCode": "LOAN_DENIED"
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

---

## 21. Üretim Seviyesi Geliştirmeler

### Audit Logging

Tüm kredi değerlendirme kararları kaydedilmelidir. Her değerlendirme için müşteri ID, talep edilen/onaylanan tutar, risk seviyesi, kredi skoru, karar ve sebebi, değerlendirme zamanı, sistem versiyonu ve IP adresi saklanmalıdır.

### Soft Delete

Veritabanından kredi değerlendirmesi kaydı tamamen silinmemelidir. Bunun yerine "soft delete" yöntemi kullanılmalıdır: `DeletedAt` alanı güncel zamanla doldurulur.

### Transaction Management

Kredi değerlendirmesi yapıldıktan sonra, onay durumunda Kredi ve Taksit kayıtları oluşturulmalıdır. Bu işlemlerin hepsi birlikte başarılı olmalı veya hepsi beraber başarısız olmalıdır. Veritabanı transaction'ları kullanılmalıdır.

### Retry Policy

Dış servislere yapılan çağrılar başarısız olabilir. Bu durumlar için retry stratejisi uygulanmalıdır: İlk başarısız çağrıdan sonra bekle, tekrar dene (3-5 kez), her denemede artan süre bekle.

### Caching Strategy

Kredi skoru servisi sonuçları cache'lenebilir. Aynı müşteri için birkaç saat içinde tekrar sorgulanırsa, cache'deki sonucu dön. Cache süresi 24 saat olmalıdır.

---

## 22. Frontend Mimarisi ve Beklentiler

### Mimari Yaklaşım ve Teknoloji

**Teknoloji:** Web arayüzü React kullanılarak geliştirilecektir.

**Mimari Dizayn:** Component-Based mimari uygulanacaktır. API çağrıları UI bileşenlerinin içine gömülmemeli; ayrı bir servis katmanında soyutlanarak yönetilmelidir.

### Temel Ekranlar (UI/UX)

**Borç ve Özet Görünümü (Dashboard):** Müşterinin toplam kredi borcu, kalan anaparası, gecikmiş taksit sayısı ve taksit dökümleri açıkça gösterilmelidir.

**Müşteri ve Kredi Yönetimi:** Müşteri CRUD işlemleri ve kredi tanımlama ekranları geliştirilmelidir.

**Ödeme Ekranı:** Müşterinin taksit listesini görebildiği ve tek bir taksit için ödeme işlemini yapabildiği arayüz.

### Tasarım Dili ve Kurumsal Kimlik

Frontend arayüzünün renk paleti, tipografik vurguları ve genel tasarım dili "https://architecht.com/" web sitesi ile benzer bir kurumsal yapıda olmalıdır.

Kullanıcı arayüzü sade tutulabilir. Asıl öncelik finansal mantığın (bakiye, tutar, ödendi/gecikti statüleri) doğru ve tutarlı şekilde yansıtılmasıdır. Gecikmiş taksitler ve ödenmiş taksitler, durumlarına uygun renk kodlarıyla net bir şekilde ayrıştırılmalıdır.

### API Entegrasyonu ve Veri Yönetimi

**Servis Entegrasyonu:** Backend tarafında tasarlanan RESTful `/api/customers`, `/api/loans`, `/api/installments` ve `/api/payments` endpoint'leri üzerinden veri alışverişi yapılacaktır.

**Hata Yönetimi:** Backend'in döneceği HTTP hata kodları React tarafında global bir interceptor ile yakalanmalıdır. Bu hatalar kullanıcıya anlaşılır UI bildirimleri olarak yansıtılmalıdır.

**Veri Formatlama:** Finansal tutarlar arayüzde ham haliyle gösterilmemeli; yerel para birimi formatında, binlik ayıraçları kullanılarak gösterilmelidir.

---

> **Not:** UI görselliği ikincil önemdedir. Öncelik; doğru çalışan sistem, tutarlı business logic ve sürdürülebilir backend mimarisi olmalıdır.