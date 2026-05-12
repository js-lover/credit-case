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

### Silme (Soft Delete + Aktif Borç Kontrolü)

| # | Test Adı | Senaryo | Beklenen |
|---|---|---|---|
| 9 | `DeleteAsync_WithNonExistingId_ThrowsNotFoundException` | Olmayan ID silinmeye çalışılıyor | `NotFoundException` |
| 10 | `DeleteAsync_WithExistingId_CallsRepositoryDelete` | Var olan ID soft-delete ediliyor | `DeleteAsync` tam bir kez çağrılmalı |
| 11 | `DeleteAsync_WithActiveLoans_ThrowsBusinessRuleException` | Müşterinin aktif kredisi var | `BusinessRuleException` — borç tutarı mesajda yer almalı |

> **Not:** `DeleteAsync`, önce müşterinin aktif kredilerini kontrol eder. Aktif kredi varsa borç miktarını içeren Türkçe mesajla `422` döner. Aksi hâlde `IsDeleted = true` ve `DeletedAt = UtcNow` set ederek `Update()` çağırır (K-17).

---

## LoanService Testleri

**Dosya:** `CreditCase.Tests/Services/LoanServiceTests.cs`

### Taksit planı üretimi

| # | Test Adı | Senaryo | Beklenen |
|---|---|---|---|
| 11 | `CreateAsync_WithTwelveMonthTerm_GeneratesTwelveInstallments` | 12 aylık kredi | 12 taksit üretilmeli |
| 12 | `CreateAsync_CalculatesMonthlyAmountUsingServerRateAmount` | Amortisasyon formülü | Mock strateji beklenen tutarı üretmeli |
| 13 | `CreateAsync_AllGeneratedInstallments_HaveUnpaidStatus` | Yeni oluşturulan kredi | Tüm taksitler `Unpaid` başlamalı |
| 14 | `CreateAsync_GeneratedInstallments_DueDatesIncrementMonthly` | 3 aylık kredi, 1 Ocak başlangıç | Vadeler Şubat · Mart · Nisan |
| 15 | `CreateAsync_NewLoan_RemainingPrincipalEqualsFullPrincipal` | Yeni kredi oluşturuldu | `RemainingPrincipal = PrincipalAmount` |

**Amortisasyon formülü:**
```
r = rateAmount / 100 / 12
A = P × r(1+r)^n / [(1+r)^n − 1]

Örnek: 50.000 ₺, vade oranı 3.25, 24 ay
r = 3.25 / 100 / 12 ≈ 0.002708
A ≈ 2.198 ₺/ay
```

### Hata senaryoları

| # | Test Adı | Senaryo | Beklenen |
|---|---|---|---|
| 16 | `CreateAsync_WithNonExistingCustomer_ThrowsNotFoundException` | CustomerId veritabanında yok | `NotFoundException` |
| 17 | `CreateAsync_WithRejectedCreditScore_ThrowsBusinessRuleException` | Mock kredi skoru servisi Kritik kategori (red) döndürüyor | `BusinessRuleException` |
| 18 | `GetByIdAsync_WithNonExistingId_ThrowsNotFoundException` | Olmayan kredi ID'si sorgulanıyor | `NotFoundException` |

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
RemainingPrincipal = unpaid installments'ların Sum(Amount)

Örnek: 12 taksitli kredi, her taksit 1.120 TL
→ 1 taksit ödendi: RemainingPrincipal = 11 × 1.120 = 12.320 TL
```

> **Önceki formül:** `Round(PrincipalAmount / Term × unpaidCount, 2)` yalnızca anapara bileşenini hesaplıyordu ve faizi dışarıda bırakıyordu. Kullanıcı kalan borcu doğru okuyamıyordu. Düzeltme: `Sum(i.Amount)` gerçek kalan ödeme yükümlülüğünü yansıtır.

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

### Sıralı Ödeme Kuralı

| # | Test Adı | Senaryo | Beklenen |
|---|---|---|---|
| 28 | `CreateAsync_WithEarlierUnpaidInstallment_ThrowsBusinessRuleException` | Taksit #3 ödenmek isteniyor; #1 ve #2 hâlâ Unpaid | `BusinessRuleException` — "Önceki ödenmemiş taksitler önce ödenmelidir." |

> Sıralı ödeme kuralı hem API hem frontend düzeyinde uygulanır. Bu test backend guard'ını doğrular; frontend "Öde" butonu sadece en düşük numaralı Unpaid taksite görünür.

### Ödeme Geçmişi Bonusu (CreditScoreBonus)

| # | Test Adı | Senaryo | Beklenen |
|---|---|---|---|
| 29 | `CreateAsync_WithOnTimePayment_IncreasesCreditScoreBonus` | Vade tarihi bugün veya ileride, ödeme yapılıyor | `customer.CreditScoreBonus` +5 artmalı |
| 30 | `CreateAsync_WithLatePayment_DecreasesCreditScoreBonus` | Vade tarihi geçmiş, ödeme yapılıyor | `customer.CreditScoreBonus` −10 azalmalı |

> Test #29 ve #30, `PaymentService`'in `ICustomerRepository.UpdateAsync` çağırdığını ve `CreditScoreBonus` değerini doğru yönde değiştirdiğini doğrular.

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
Başarılı: 48   Başarısız: 0   Atlanan: 0   Toplam: 48
```

---

## Soft Delete ve Validasyon Test Senaryoları

Aşağıdaki senaryolar birim test paketine dahildir ve 48 testin içinde yer almaktadır.

### Soft Delete — CustomerService

| # | Test Adı | Senaryo | Beklenen |
|---|---|---|---|
| 34 | `DeleteAsync_WithExistingId_SetsIsDeletedTrue` | Var olan müşteri soft-delete ediliyor | `customer.IsDeleted == true` |
| 35 | `DeleteAsync_WithExistingId_SetsDeletedAt` | Soft-delete sonrası | `customer.DeletedAt != null` |
| 36 | `GetAllAsync_DoesNotReturnSoftDeletedCustomers` | `IsDeleted = true` olan kayıt mevcut | Listede görünmemeli (global query filter) |
| 37 | `GetByIdAsync_ForSoftDeletedCustomer_ThrowsNotFoundException` | Soft-deleted müşteri ID'si ile sorgu | `NotFoundException` |

### Giriş Validasyonu — CreateCustomerRequestValidator

| # | Test Adı | Senaryo | Beklenen |
|---|---|---|---|
| 38 | `IdentityNumber_WithLetters_FailsValidation` | `IdentityNumber = "ABCDEFGHIJK"` | Validation hatası |
| 39 | `IdentityNumber_WithLessThan11Digits_FailsValidation` | `IdentityNumber = "1234567"` | Validation hatası |
| 40 | `IdentityNumber_WithExactly11Digits_PassesValidation` | `IdentityNumber = "12345678901"` | Geçerli |
| 41 | `PhoneNumber_WithLetters_FailsValidation` | `PhoneNumber = "abc"` | Validation hatası |
| 42 | `PhoneNumber_WithLessThan10Digits_FailsValidation` | `PhoneNumber = "555"` | Validation hatası |
| 43 | `PhoneNumber_With10Digits_PassesValidation` | `PhoneNumber = "5551234567"` | Geçerli |
| 44 | `PhoneNumber_With11Digits_PassesValidation` | `PhoneNumber = "05551234567"` | Geçerli |

### Giriş Validasyonu — UpdateCustomerRequestValidator

| # | Test Adı | Senaryo | Beklenen |
|---|---|---|---|
| 45 | `PhoneNumber_WithInvalidFormat_FailsValidation` | `PhoneNumber = "123"` | Validation hatası |
| 46 | `UpdateAsync_WithInvalidPhone_ThrowsValidationException` | Servis katmanında geçersiz telefon | `ValidationException` |

---

## Test Sonuçları

```
Mevcut (birim test): 48   Başarısız: 0   Atlanan: 0
```

---

## Kapsam Dışı

| Alan | Neden kapsam dışı |
|---|---|
| Validator testleri (kısmi) | `CreateCustomerRequestValidator` ve `UpdateCustomerRequestValidator` senaryoları (38-46) planlanmış; bu iterasyonda servis iş kurallarına odaklanıldı |
| Repository testleri | EF Core sorgularını test etmek gerçek veritabanı veya `InMemory` provider gerektirir; entegrasyon test kapsamında ele alınabilir |
| Controller testleri | HTTP pipeline testi `WebApplicationFactory` gerektiren entegrasyon testidir; birim test kapsamında değil |
| Entegrasyon testleri | Gerçek veritabanına karşı uçtan uca akış testi ayrı bir test projesiyle yapılabilir |
