# Exception Handling

## Genel Bakış

Sistem, uygulama boyunca fırlatılan tüm hataları tek bir noktada yakalayıp tutarlı bir HTTP yanıtına dönüştürür. Bu sorumluluğu `ExceptionHandlingMiddleware` üstlenir.

```
İstek
  │
  ▼
ExceptionHandlingMiddleware.InvokeAsync()
  │
  ├─► try { await _next(context) }   ← Tüm pipeline burada çalışır
  │         │
  │         │  Hata oluşursa
  │         ▼
  └─► catch (ExceptionType ex)       ← Tip eşleştirme (en özelden genele)
        │
        ▼
      HTTP yanıtı (JSON)
```

---

## Katman İçindeki Konum

```
CreditCase.Api
└── Middleware
    └── ExceptionHandlingMiddleware.cs   ← burada

CreditCase.Application
└── Exceptions
    ├── NotFoundException.cs
    ├── BusinessRuleException.cs
    ├── LoanApplicationDeniedException.cs
    ├── InsufficientCreditScoreException.cs
    ├── ExcessiveDebtRatioException.cs
    └── InvalidCustomerProfileException.cs
```

Middleware, `Program.cs` içinde pipeline'ın **en önüne** kayıt edilir; böylece controller, service ve repository katmanlarından fırlatılan her exception buraya düşer:

```csharp
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseRouting();
app.UseCors();
// ...
app.MapControllers();
```

---

## HTTP Durum Kodu Eşlemesi

| Exception | HTTP Kodu | `type` Alanı |
|---|:---:|---|
| `NotFoundException` | 404 | `NotFound` |
| `LoanApplicationDeniedException` | 422 | `LoanDenied` |
| `InsufficientCreditScoreException` | 422 | `InsufficientCreditScore` |
| `ExcessiveDebtRatioException` | 422 | `ExcessiveDebtRatio` |
| `InvalidCustomerProfileException` | 422 | `InvalidCustomerProfile` |
| `BusinessRuleException` | 422 | `BusinessRuleViolation` |
| `FluentValidation.ValidationException` | 400 | `ValidationError` |
| `Exception` (beklenmeyen) | 500 | `InternalServerError` |

---

## Yanıt Formatları

### 404 — Kayıt Bulunamadı

```json
{
  "type": "NotFound",
  "message": "29 numaralı müşteri bulunamadı."
}
```

### 422 — İş Kuralı İhlali

```json
{
  "type": "BusinessRuleViolation",
  "message": "Bu taksit zaten ödenmiştir."
}
```

```json
{
  "type": "LoanDenied",
  "message": "Kredi başvurusu reddedildi: Borç/gelir oranı limit aşıyor."
}
```

### 400 — Validasyon Hatası

FluentValidation hataları alan bazında gruplandırılır:

```json
{
  "type": "ValidationError",
  "message": "One or more validation errors occurred.",
  "errors": {
    "FirstName": ["Ad alanı zorunludur."],
    "Email":     ["Geçersiz e-posta formatı."],
    "PhoneNumber": [
      "Telefon numarası zorunludur.",
      "Telefon numarası 10 veya 11 haneli rakamlardan oluşmalıdır."
    ]
  }
}
```

### 500 — Beklenmeyen Hata

```json
{
  "type": "InternalServerError",
  "message": "An unexpected error occurred."
}
```

> 500 yanıtında istemciye yalnızca genel mesaj iletilir; detay `ILogger` aracılığıyla sunucu loglarına yazılır.

---

## Exception Sınıfları

### `NotFoundException`

Veritabanında aranıp bulunamayan kayıtlar için. Tüm servisler tarafından kullanılır.

```csharp
// Fırlatma örnekleri
throw new NotFoundException($"{id} numaralı müşteri bulunamadı.");
throw new NotFoundException($"{id} numaralı kredi bulunamadı.");
throw new NotFoundException($"{id} numaralı taksit bulunamadı.");
throw new NotFoundException($"{evaluationId} numaralı kredi değerlendirmesi bulunamadı.");
```

---

### `BusinessRuleException`

Genel amaçlı iş kuralı ihlali. Daha spesifik bir exception sınıfı uygun değilse bu kullanılır.

```csharp
// CustomerService
throw new BusinessRuleException("Bu TC kimlik numarasına ait bir müşteri zaten mevcut.");
throw new BusinessRuleException("Bu e-posta adresine ait bir müşteri zaten mevcut.");
throw new BusinessRuleException("Aktif kredisi olan müşteri silinemez.");

// LoanService
throw new BusinessRuleException("Kredi başvurusu reddedildi. Kredi skoru Kritik kategorisinde.");
throw new BusinessRuleException("Balon ödeme yalnızca Araç kredileri için kullanılabilir.");
throw new BusinessRuleException($"Balon ödeme için en az {BalloonMinCreditScore} kredi skoru gereklidir.");
throw new BusinessRuleException($"İstenen tutar maksimum tutarı ({max} TL) aşmaktadır.");

// PaymentService
throw new BusinessRuleException("Bu taksit zaten ödenmiştir.");
throw new BusinessRuleException("Önceki ödenmemiş taksitler önce ödenmelidir.");

// BalloonPaymentStrategy
throw new BusinessRuleException("Balon ödeme planı en fazla 36 ay vade için oluşturulabilir.");
throw new BusinessRuleException("Balon taksit tutarı, anaparanın %90'ını aşmaktadır.");
```

---

### `LoanApplicationDeniedException`

Kredi başvurusu risk analizi veya skor kontrolü sonucu reddedildiğinde fırlatılır. `DenialReason` özelliğiyle red sebebi taşınır.

```csharp
public class LoanApplicationDeniedException : Exception
{
    public string DenialReason { get; }

    public LoanApplicationDeniedException(string reason)
        : base($"Kredi başvurusu reddedildi: {reason}")
    {
        DenialReason = reason;
    }
}
```

---

### `InsufficientCreditScoreException`

Kredi skoru minimum eşiğin altında kaldığında. Skoru ve gereken minimumu birlikte taşır.

```csharp
public class InsufficientCreditScoreException : Exception
{
    public int ActualScore    { get; }
    public int MinimumRequired { get; }
}

// Örnek mesaj:
// "Kredi skoru 650, gereken minimum skor olan 970'in altında."
```

---

### `ExcessiveDebtRatioException`

Borç/gelir oranı %70 sınırını aştığında. Hesaplanan oranı `ActualRatio` özelliğiyle tutar.

```csharp
public class ExcessiveDebtRatioException : Exception
{
    public decimal ActualRatio { get; }
}

// Örnek mesaj:
// "Borç/gelir oranı %75, izin verilen maksimum %70 sınırını aşıyor."
```

---

### `InvalidCustomerProfileException`

Müşteri verisi eksik veya geçersiz olduğunda; doğrudan bir domain kısıtlaması ihlalini ifade eder.

```csharp
// Örnek mesaj:
// "Müşteri profili geçersiz: İstihdam durumu 'Unemployed' olan müşteriye kredi verilemez."
```

---

## Catch Sırası

Middleware'deki `catch` blokları **en özelden en genele** doğru sıralanmıştır. Bu sıra önemlidir: C# ilk eşleşen bloğu çalıştırır.

```
1. NotFoundException               → 404
2. LoanApplicationDeniedException  → 422  ─┐
3. InsufficientCreditScoreException → 422  │ BusinessRuleException'ın
4. ExcessiveDebtRatioException     → 422  │ alt sınıfları değil; bağımsız
5. InvalidCustomerProfileException → 422  ─┘ ama aynı HTTP kodu
6. BusinessRuleException           → 422  ← genel iş kuralı
7. ValidationException (Fluent)    → 400  ← alan bazlı hata listesi
8. Exception                       → 500  ← beklenmeyen, loglanır
```

---

## Logging

`500` yanıtlarında hata ayrıntısı `ILogger<ExceptionHandlingMiddleware>` ile loglanır:

```csharp
_logger.LogError(ex, "An unexpected error occurred.");
```

`404` ve `422` hataları birer iş akışı çıktısı sayıldığından loglanmaz — zaten beklenen senaryolardır.

---

## ValidationException ile Diğerlerinin Farkı

| | Domain / İş Kuralı | Fluent Validation |
|---|---|---|
| **Nereden fırlatılır** | Service / Strategy katmanı | Controller action çağrılmadan önce |
| **Format** | `{ type, message }` | `{ type, message, errors: {...} }` |
| **HTTP kodu** | 422 | 400 |
| **Amaç** | İş mantığı ihlali | Girdi formatı / zorunlu alan kontrolü |
