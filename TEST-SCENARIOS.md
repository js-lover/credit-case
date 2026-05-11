# Test Senaryoları

Proje: **Digital Loan & Repayment Management System**

---

## Yaklaşım

Testler **birim testi (unit test)** olarak yazılmıştır. Her servis sınıfı izole biçimde test edilir; veritabanı bağlantısı yoktur. Bağımlılıklar (repository, validator, dış servis) **Moq** ile mock'lanır.

### Kullanılan araçlar

| Araç | Amaç |
|---|---|
| xUnit | Test çerçevesi |
| Moq | Bağımlılık mock'lama |
| FluentAssertions | Okunabilir assertion ifadeleri |

### Kural

Her test **AAA** (Arrange · Act · Assert) desenini izler:
- **Arrange** — test verisi ve mock kurulumu
- **Act** — test edilen metodun çağrılması
- **Assert** — beklenen sonucun doğrulanması

---

## CustomerService Testleri

**Dosya:** `CreditCase.Tests/Services/CustomerServiceTests.cs`

### Sorgulama

| # | Test Adı | Senaryo | Beklenen |
|---|---|---|---|
| 1 | `GetByIdAsync_WithExistingId_ReturnsMatchingCustomerResponse` | Var olan ID sorgulanıyor | DTO alanları entity ile eşleşmeli |
| 2 | `GetByIdAsync_WithNonExistingId_ThrowsNotFoundException` | Olmayan ID sorgulanıyor | `NotFoundException` fırlatılmalı, mesajda ID yer almalı |

### Oluşturma

| # | Test Adı | Senaryo | Beklenen |
|---|---|---|---|
| 3 | `CreateAsync_WithValidRequest_ReturnsCreatedCustomerResponse` | Geçerli istek, benzersiz TC ve email | Yeni müşteri DTO'su dönmeli |
| 4 | `CreateAsync_WithDuplicateIdentityNumber_ThrowsBusinessRuleException` | TC kimlik numarası başka müşteride kayıtlı | `BusinessRuleException` (422) |
| 5 | `CreateAsync_WithDuplicateEmail_ThrowsBusinessRuleException` | Email başka müşteride kayıtlı | `BusinessRuleException` (422) |

### Güncelleme

| # | Test Adı | Senaryo | Beklenen |
|---|---|---|---|
| 6 | `UpdateAsync_WithNonExistingId_ThrowsNotFoundException` | Olmayan ID güncellenmeye çalışılıyor | `NotFoundException` |
| 7 | `UpdateAsync_WhenEmailChangedToExistingEmail_ThrowsBusinessRuleException` | Başka müşteride kayıtlı email'e geçmek isteniyor | `BusinessRuleException` |
| 8 | `UpdateAsync_WhenEmailUnchanged_DoesNotQueryEmailUniqueness` | Kendi email'i değiştirilmeden güncelleme | `GetByEmailAsync` hiç çağrılmamalı |

> **Neden 8. test önemli?** `UpdateAsync` email değiştirilmiyorsa veritabanı sorgusu yapmaz. Bu; hem gereksiz sorgudan kaçınır, hem de "kendi emailine güncelleme" senaryosunun hatalı reddedilmesini engeller.

### Silme

| # | Test Adı | Senaryo | Beklenen |
|---|---|---|---|
| 9 | `DeleteAsync_WithNonExistingId_ThrowsNotFoundException` | Olmayan ID silinmeye çalışılıyor | `NotFoundException` |
| 10 | `DeleteAsync_WithExistingId_CallsRepositoryDelete` | Var olan ID siliniyor | `DeleteAsync` tam bir kez çağrılmalı |

---

## LoanService Testleri

**Dosya:** `CreditCase.Tests/Services/LoanServiceTests.cs`

### Taksit planı üretimi

| # | Test Adı | Senaryo | Beklenen |
|---|---|---|---|
| 11 | `CreateAsync_WithTwelveMonthTerm_GeneratesTwelveInstallments` | 12 aylık kredi | 12 taksit üretilmeli |
| 12 | `CreateAsync_FlatRateInterest_CalculatesCorrectMonthlyAmount` | 12.000 TL · %12 · 12 ay | Aylık taksit = **1.120,00 TL** |
| 13 | `CreateAsync_FlatRateInterest_SixMonthTerm_CalculatesCorrectMonthlyAmount` | 6.000 TL · %10 · 6 ay | Aylık taksit = **1.050,00 TL** |
| 14 | `CreateAsync_AllGeneratedInstallments_HaveUnpaidStatus` | Yeni oluşturulan kredi | Tüm taksitler `Unpaid` başlamalı |
| 15 | `CreateAsync_GeneratedInstallments_DueDatesIncrementMonthly` | 3 aylık kredi, 1 Ocak başlangıç | Vadeler Şubat · Mart · Nisan |
| 16 | `CreateAsync_NewLoan_RemainingPrincipalEqualsFullPrincipal` | Yeni kredi oluşturuldu | `RemainingPrincipal = PrincipalAmount` |

**Faiz formülü doğrulama:**
```
totalAmount = principal × (1 + rate/100 × termYears)
monthly     = Round(totalAmount / term, 2)

Örnek 1: 12.000 × (1 + 0,12 × 1,0) / 12 = 1.120,00
Örnek 2:  6.000 × (1 + 0,10 × 0,5) /  6 = 1.050,00
```

### Hata senaryoları

| # | Test Adı | Senaryo | Beklenen |
|---|---|---|---|
| 17 | `CreateAsync_WithNonExistingCustomer_ThrowsNotFoundException` | CustomerId veritabanında yok | `NotFoundException` |
| 18 | `CreateAsync_WithRejectedCreditScore_ThrowsBusinessRuleException` | Mock kredi skoru servisi "Rejected" döndürüyor | `BusinessRuleException` |
| 19 | `GetByIdAsync_WithNonExistingId_ThrowsNotFoundException` | Olmayan kredi ID'si sorgulanıyor | `NotFoundException` |

---

## PaymentService Testleri

**Dosya:** `CreditCase.Tests/Services/PaymentServiceTests.cs`

### Başarılı ödeme sonrası durum güncellemeleri

| # | Test Adı | Senaryo | Beklenen |
|---|---|---|---|
| 20 | `CreateAsync_WithValidPayment_SetsInstallmentStatusToPaid` | İlk ödeme yapılıyor | Taksit `Paid` olarak güncellenmeli |
| 21 | `CreateAsync_WithValidPayment_RecalculatesRemainingPrincipal` | 12 taksitli kredi; ilk taksit ödeniyor | `RemainingPrincipal` = 11.000 TL |
| 22 | `CreateAsync_WhenLastInstallmentPaid_SetsLoanStatusToClosed` | 1 taksitli kredi; o taksit ödeniyor | Kredi `Closed` olmalı |
| 23 | `CreateAsync_WhenLastInstallmentPaid_SetsRemainingPrincipalToZero` | Tüm taksitler ödendi | `RemainingPrincipal` = 0 |
| 24 | `CreateAsync_WithValidPayment_ReturnsPaymentResponse` | Geçerli ödeme | `InstallmentId`, `PaymentAmount`, `Status = Successful` doğru dönmeli |

**RemainingPrincipal formülü:**
```
RemainingPrincipal = Round(PrincipalAmount / Term × unpaidCount, 2)

Örnek: 12.000 / 12 × 11 = 11.000
```

### İdempotency — çift koruma katmanı (K-16)

| # | Test Adı | Senaryo | Beklenen |
|---|---|---|---|
| 25 | `CreateAsync_WithAlreadyPaidInstallment_ThrowsBusinessRuleException` | Taksit zaten `Paid` durumunda | `BusinessRuleException` — Katman 1 devreye girmeli |
| 26 | `CreateAsync_WithExistingPaymentRecord_ThrowsBusinessRuleException` | Taksit `Unpaid` ama önceki ödeme kaydı var | `BusinessRuleException` — Katman 2 devreye girmeli |

> **25 vs 26 neden ikidir?** 25. test taksit durum kontrolünü (entity state), 26. test ödeme tablosu kontrolünü (veritabanı kaydı) doğrular. Birincisini geçen ancak ikincisini geçemeyen senaryo gerçek hayatta olabilir: ödeme kaydı yazıldı fakat taksit durumu güncellenemeden servis çöktü.

### Bulunamama senaryosu

| # | Test Adı | Senaryo | Beklenen |
|---|---|---|---|
| 27 | `CreateAsync_WithNonExistingInstallment_ThrowsNotFoundException` | InstallmentId veritabanında yok | `NotFoundException` |

---

## InstallmentService Testleri

**Dosya:** `CreditCase.Tests/Services/InstallmentServiceTests.cs`

### Overdue güncelleme davranışı

| # | Test Adı | Senaryo | Beklenen |
|---|---|---|---|
| 28 | `GetAllAsync_AlwaysCallsUpdateOverdueFirst` | Taksitler listeleniyor | `UpdateOverdueAsync` mutlaka çağrılmalı |
| 29 | `GetAllAsync_ReturnsAllInstallments` | 2 taksit var | 2 elemanlı liste dönmeli |

> **Neden 28. test önemli?** Overdue güncelleme bir arka plan servisiyle değil GetAll tetiklemesiyle yapılıyor (K-07 kararı). Bu davranışın korunması kritik; aksi hâlde vadesi geçmiş taksitler hiçbir zaman `Overdue` olmaz.

### Sorgulama

| # | Test Adı | Senaryo | Beklenen |
|---|---|---|---|
| 30 | `GetByIdAsync_WithExistingId_ReturnsInstallmentResponse` | Var olan ID sorgulanıyor | DTO doğru dönmeli |
| 31 | `GetByIdAsync_WithNonExistingId_ThrowsNotFoundException` | Olmayan ID sorgulanıyor | `NotFoundException` |

### Güncelleme

| # | Test Adı | Senaryo | Beklenen |
|---|---|---|---|
| 32 | `UpdateAsync_WithValidRequest_ChangesInstallmentStatus` | `Unpaid → Overdue` güncelleme | Dönen DTO `Overdue` içermeli |
| 33 | `UpdateAsync_WithNonExistingId_ThrowsNotFoundException` | Olmayan ID güncellenmeye çalışılıyor | `NotFoundException` |

---

## Test Sonuçları

```
Başarılı: 33   Başarısız: 0   Atlanan: 0   Toplam: 33
```

---

## Kapsam Dışı

| Alan | Neden kapsam dışı |
|---|---|
| Validator testleri | `CreateCustomerRequestValidator`, `CreateLoanRequestValidator` gibi sınıflar bağımsız test edilebilir; bu iterasyonda servis iş kurallarına odaklanıldı |
| Repository testleri | EF Core sorgularını test etmek gerçek veritabanı veya `InMemory` provider gerektirir; entegrasyon test kapsamında ele alınabilir |
| Controller testleri | HTTP pipeline testi `WebApplicationFactory` gerektiren entegrasyon testidir; birim test kapsamında değil |
| Entegrasyon testleri | Gerçek veritabanına karşı uçtan uca akış testi ayrı bir test projesiyle yapılabilir |
