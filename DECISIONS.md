# Tasarım Kararları

Bu belge, projede alınan teknik kararları ve bilinçli olarak uygulanmayan yaklaşımları gerekçeleriyle birlikte belgeler.

Her karar için şu soruyu yanıtlar: **"Bu neden böyle yapıldı / yapılmadı?"**

---

## İçindekiler

- [Yapılan Kararlar](#yapılan-kararlar)
- [Yapılmayan Kararlar](#yapılmayan-kararlar)

---

## Yapılan Kararlar

---

### K-01 · Clean Architecture katman ayrımı

**Karar:** Proje `Domain → Application → Infrastructure → Api` katmanlarına bölündü. Her katman yalnızca kendisinden içeride olan katmanı referans alır.

**Gerekçe:** Bankacılık sistemlerinde iş kuralları sık değişmez; ancak persistence teknolojisi veya API protokolü değişebilir. Bu ayrım sayesinde, örneğin EF Core'dan Dapper'a geçilmesi durumunda yalnızca `Infrastructure` katmanı değişir; `Application` ve `Domain` etkilenmez. Servis sınıfları test ortamında sahte (in-memory) repository'lerle çalıştırılabilir.

---

### K-02 · ID alanı için `int` tipi

**Karar:** Tüm entity'lerin birincil anahtarı `int` (SQL Server `IDENTITY`) olarak tanımlandı.

**Gerekçe:** `Guid` daha "modern" görünse de SQL Server'da clustered index üzerinde rastgele `Guid` ciddi `page split` sorununa yol açar. Bu riski ortadan kaldırmak için `newsequentialid()` veya .NET'in `Guid.CreateVersion7()` API'si kullanılması gerekir. `Installment` ve `Payment` gibi hiçbir zaman URL'e çıkmayan iç entity'ler için bu ek karmaşıklık karşılık vermiyor. `Customer` ve `Loan` için `Guid` düşünülebilirdi; ancak projenin tutarlılığı ve migration maliyeti gözetilerek tek tipte `int` tercih edildi. Detaylı analiz için bkz. [Yapılmayan Kararlar — YK-01](#yk-01--guid-id).

---

### K-03 · Validasyonun Application katmanında tutulması

**Karar:** `FluentValidation` validator'ları `CreditCase.Application` içinde tanımlandı. Servisler, `ValidateAndThrowAsync` ile doğrulamayı kendi içinde tetikler.

**Gerekçe:** Controller'da `[ApiController]` attribute'ü ile yapılan model doğrulama yalnızca HTTP katmanına özeldir; servisler başka transport'lardan (gRPC, message queue) çağrıldığında devre dışı kalır. Validasyonu Application'a taşımak, kuralları HTTP bağımsız kılar ve test edilebilirliği artırır.

---

### K-04 · Global Exception Handling Middleware

**Karar:** `ExceptionHandlingMiddleware`, `NotFoundException`, `BusinessRuleException` ve `ValidationException`'ı yakalayarak tutarlı bir JSON formatına dönüştürür.

**Gerekçe:** Her controller action'ında `try/catch` bloğu yazmak kod tekrarı yaratır ve istisna formatının tutarsızlaşma riskini artırır. Merkezi middleware ile tüm hata yanıtları aynı yapıya (`type`, `message`, `errors`) sahip olur; API tüketicisi her endpoint için ayrı hata formatı öğrenmek zorunda kalmaz.

---

### K-05 · Kredi oluşturmada taksit planının otomatik üretilmesi

**Karar:** `POST /api/loans` çağrısında taksit listesi `LoanService` içinde hesaplanıp `Loan` entity'sine eklenir. Ayrı bir "taksit oluştur" endpoint'i yoktur.

**Gerekçe:** Taksitsiz kredi iş kuralına aykırıdır; bu iki işlemi ayrı endpoint'lere bölmek API tüketicisini zorunlu bir sıra takip etmeye iter ve tutarsız veri durumlarına (kredisi var, taksiti yok) kapı açar. EF Core'un navigation property cascade insert mekanizması sayesinde `Loan + Installments` tek `SaveChanges` içinde atomik olarak kaydedilir.

---

### K-06 · Vade oranı sistemi: ratio formatı + amortisasyon taksit hesaplama

Bu sistem iki bağımsız bileşenden oluşur.

**Bileşen 1 — Vade Oranı Belirleme (`InterestCalculationEngine`):**

Vade oranı **ratio formatındadır** (örn. `3.25`, `4.48`) — yüzde değildir. Her başvuru için 3 aşamada hesaplanır:

```
Son Vade Oranı = TemelOran[LoanType][ScoreCategory] × (1 + VadeFactörü) ± MeslekBonusu

TemelOran    : claude.md §6A tablosu (12 ay referans)
               Bireysel / Kritik=6.8 ... Eğitim / Prestijli=0.9
VadeFactörü  : ≤6ay=−0.25, 12ay=0.00, 24ay=+0.15, 36ay=+0.28, 72ay=+0.75
MeslekBonusu : Kamu=−0.30, Sağlık/Teknoloji=−0.20, Mevsimlik=+0.30
```

**Bileşen 2 — Taksit Hesaplama (`StandardInstallmentStrategy`):**

Vade oranı belirlendikten sonra taksit planı **amortisasyon (azalan bakiye)** formülüyle üretilir. Hesaplamada KKDF (%15) ve BSMV (%5) vergiler brüt orana dahil edilir (bkz. K-22):

```
grossRate = rateAmount × (1 + 0.15 + 0.05)   // KKDF + BSMV dahil brüt oran
r = grossRate / 100                            // aylık faiz oranı (ondalık)
A = P × r(1+r)^n / [(1+r)^n − 1]
```

**Gerekçe:** Düz faiz (flat-rate) başlangıçta kullanılmıştı; ancak projenin spec'inde (claude.md §6A) amortisasyon formülü açıkça belirtilmiş ve örnek hesaplamalar bu yöntemle örtüşmektedir. Amortisasyon, azalan anaparayı daha gerçekçi modeller ve bankacılık endüstrisinin fiilen kullandığı yöntemdir. `rateAmount` aylık net oran olduğundan `/12` bölme işlemi gerekmez; yıllık bir oranı aylığa çevirmek söz konusu değildir.

---

### K-07 · Overdue güncellemesinin `GetAllAsync` içinde tetiklenmesi

**Karar:** `InstallmentService.GetAllAsync` çağrıldığında, dönüşten önce `UpdateOverdueAsync` çalıştırılır.

**Gerekçe:** Arka plan servisi (background service / hosted service) daha doğru bir çözümdür; ancak bu proje kapsamında Overdue kontrolü yalnızca liste sorgulandığında anlamlıdır. Bu yaklaşım; ek altyapı (IHostedService, timer) gerektirmeden, repository içinde tek bir `ExecuteUpdateAsync` SQL sorgusuyla toplu güncelleme sağlar.

---

### K-08 · Enum değerlerinin string olarak saklanması

**Karar:** `LoanType`, `LoanStatus`, `InstallmentStatus`, `PaymentStatus` enum'ları veritabanında sayısal değer yerine string olarak (`nvarchar`) saklanır. EF Core konfigürasyonunda `HasConversion<string>()` kullanılır.

**Gerekçe:** `0`, `1`, `2` gibi sayısal değerler veritabanında anlamını kaybeder; doğrudan SQL sorgusu ile veriyi yorumlamak güçleşir, migration sırasında enum sıralamasının değişmesi veri bozulmasına yol açar. `"Active"`, `"Closed"` gibi değerler self-documenting'dir ve yeni enum üyesi eklenmesi mevcut verileri etkilemez.

---

### K-09 · `decimal` hassasiyeti — parasal vs. oran alanları

**Karar:** Parasal alanlar (`PrincipalAmount`, `RemainingPrincipal`, `Amount`, `PaymentAmount`) `decimal(18, 2)` olarak tanımlandı. Oran alanları (`RateAmount`, `ApprovedRateAmount`) ise dört ondalık basamak hassasiyeti gerektirdiğinden `decimal(7, 4)` olarak tanımlandı.

**Gerekçe:** `float` ve `double` IEEE 754 kayan noktalı aritmetik kullanır; bu aritmetik para hesaplamalarında yuvarlama hataları üretir (`0.1 + 0.2 ≠ 0.3`). `decimal` tipi ondalık aritmetiği tam olarak temsil eder. Bu, finansal sistemlerde endüstri standardıdır.

---

### K-10 · Repository pattern — Unit of Work olmadan

**Karar:** Her repository (`CustomerRepository`, `LoanRepository`, ...) `AppDbContext`'i doğrudan enjekte alır ve kendi `SaveChangesAsync` çağrısını yapar. Ayrı bir `IUnitOfWork` arayüzü tanımlanmadı.

**Gerekçe:** EF Core'un `DbContext`'i zaten Unit of Work ve Identity Map pattern'larını içerir. Bu proje kapsamında birden fazla repository'nin aynı transaction içinde koordineli çalışması yalnızca `PaymentService` içinde gerekir ve bu servis, iki repository'yi ardışık olarak çağırarak yönetir. `IUnitOfWork` eklemek bu ölçekte soyutlama maliyetini karşılamaz.

---

### K-11 · `/// <summary>` — yalnızca interface ve controller'lara

**Karar:** XML dokümantasyon yorumları yalnızca servis interface'lerine ve controller action'larına eklendi. Entity'ler, repository implementasyonları ve servis implementasyonları belgelenmedi.

**Gerekçe:** `public string FirstName` veya `GetByIdAsync(int id)` gibi adların yorum gerektirmediği durumlarda `/// <summary>` eklemek gürültü yaratır ve bakım yükü doğurur. Belgelemenin gerçek değer ürettiği yerler şunlardır: (1) iş kuralı taşıyan servis sözleşmeleri — önemli kısıtları belgelemek interface tüketicisini bilinçlendirir; (2) controller action'ları — XML yorumları Swagger UI'a doğrudan yansır ve API dokümantasyonunu zenginleştirir.

---

### K-12 · Mock CreditScoreService — interface arkasında, profil tabanlı

**Karar:** `ICreditScoreService` arayüzü `Application` katmanında tanımlandı; `MockCreditScoreService` implementasyonu `Infrastructure` katmanına yerleştirildi ve DI üzerinden bağlandı. Başlangıçta ID'ye dayalı deterministik bir değer üretilirken, sonradan **gerçek müşteri profili üzerinden** hesaplama yapacak şekilde yeniden yazıldı.

**Mock Servis Algoritması:**

```csharp
int baseScore = ScoreAge(dob)          // maks. 400 — 36-50 yaş pik
              + ScoreIncome(income)    // maks. 550 — Türk bankacılığı gelir bantları
              + ScoreEmployment(status)// maks. 400 — Aktif=400, İşsiz=40
              + ScoreProfession(cat);  // maks. 350 — Kamu=350, Mevsimlik=90

int finalScore = Math.Clamp(baseScore + customer.CreditScoreBonus, 0, 1900);
// Toplam baz maks. = 1700; bonus [−200, +200] ile nihai maks. = 1900
```

**Neden profil tabanlı yapıldı:**

- ID'ye dayalı belirleyici değer üretmek test ortamında mantıklı görünse de demo ve sunum senaryolarında aynı profil farklı ID'lerle farklı sonuç verebilir; bu tutarsızlık açıklaması güçtür.
- Gerçek profil bazlı hesap, `DateOfBirth`, `MonthlyIncome`, `EmploymentStatus`, `ProfessionCategory` alanlarının Customer entity'sinde taşınması için somut bir motivasyon yaratır.
- Sunum sırasında "bu müşteri neden bu faiz oranını aldı?" sorusuna cevap verilebilir.

**Gerekçe (OCP):** Gerçek kredi skoru entegrasyonu hazır olduğunda yalnızca `Infrastructure` katmanına yeni bir implementasyon eklenir; `LoanEvaluationService` hiçbir değişiklik gerektirmez.

---

### K-13 · Cascade delete (FK konfigürasyonu)

**Karar:** `Customer → Loan → Installment → Payment` zincirinde her seviye `OnDelete(DeleteBehavior.Cascade)` ile tanımlandı.

**Gerekçe:** FK kısıtının tutarlı kalması için cascade tanımı gereklidir. Soft delete uygulandığından (bkz. K-17) `Customer` kaydı veritabanından hiçbir zaman fiziksel olarak silinmez; dolayısıyla cascade zinciri artık tetiklenmez. Tanım konfigürasyonda kalmaya devam eder ancak pasif durumdadır.

---

### K-15 · Email ve IdentityNumber alanlarında benzersizlik (filtered unique index + servis guard'ı)

**Karar:** `Customers.Email` ve `Customers.IdentityNumber` kolonları hem veritabanı düzeyinde hem uygulama katmanında (`CustomerService`) benzersiz olarak zorlanır. Soft delete (K-17) eklendikten sonra index'ler `WHERE IsDeleted = 0` filtreli hale getirildi.

**Gerekçe:** E-posta adresi bankacılık sistemlerinde bildirim, şifre sıfırlama ve kimlik doğrulama kanalı olarak kullanılır. İki müşterinin aynı e-postaya sahip olması hem işlevsel (hangi müşteriye bildirim gönderilecek?) hem güvenlik (farklı kişinin hesabına erişim riski) açısından sorunludur. `PhoneNumber` ise bireysel bankacılıkta unique beklense de gerçek hayatta istisnalar mevcuttur — aynı hane halkı, vasi-veli ilişkisi, kurumsal hesaplar. Bu sınır durumları gözetilerek telefon unique yapılmadı; email için bu gerekçeler geçerli değildir.

**UpdateAsync'te özel durum:** Müşteri mevcut e-postasını "güncellemeden" kaydedebilmeli; yalnızca başka bir müşteriye ait e-posta reddedilmelidir.

```csharp
// CreateAsync — yeni kayıt: başka müşteride aynı email var mı?
var existingEmail = await _customerRepository.GetByEmailAsync(request.Email);
if (existingEmail is not null)
    throw new BusinessRuleException("A customer with this email already exists.");

// UpdateAsync — güncelleme: email değiştiriliyor mu? Değiştiriliyor ise çakışma var mı?
if (!string.Equals(customer.Email, request.Email, StringComparison.OrdinalIgnoreCase))
{
    var existingEmail = await _customerRepository.GetByEmailAsync(request.Email);
    if (existingEmail is not null)
        throw new BusinessRuleException("A customer with this email already exists.");
}
```

Veritabanı index'i, uygulama katmanı kontrolünü atlatabilecek eş zamanlı yazma senaryolarına karşı son güvence hattı işlevi görür.

**Soft delete sonrası filtered index:** Soft-deleted bir müşterinin TC/e-posta değeriyle yeni kayıt açılabilmesi için standart unique index yeterli değildir — silinmiş kayıt hâlâ tabloda bulunduğundan DB constraint ihlali oluşurdu. Bu nedenle index'ler `WHERE IsDeleted = 0` filtreli olarak yeniden oluşturuldu:

```sql
CREATE UNIQUE INDEX IX_Customers_Email ON Customers(Email) WHERE IsDeleted = 0;
CREATE UNIQUE INDEX IX_Customers_IdentityNumber ON Customers(IdentityNumber) WHERE IsDeleted = 0;
```

EF Core `AppDbContext`'teki karşılığı:

```csharp
entity.HasIndex(e => e.Email).IsUnique().HasFilter("[IsDeleted] = 0");
entity.HasIndex(e => e.IdentityNumber).IsUnique().HasFilter("[IsDeleted] = 0");
```

---

### K-16 · İdempotency — iş kuralı düzeyinde çift koruma katmanı

**Karar:** Aynı takside ikinci ödeme yapılmasını engellemek için `PaymentService.CreateAsync` içinde iki bağımsız guard tanımlandı. Ayrıca `UpdateOverdueAsync`, filtre koşulu gereği doğal olarak idempotent'tir.

**Gerekçe:** Bankacılık sistemlerinde en kritik idempotency ihlali ödeme tekrarıdır. Yalnızca taksit durumunu kontrol etmek teorik olarak yeterliymiş gibi görünse de şu senaryo tehlikelidir: ödeme kaydı yazıldı ancak `installment.Status = Paid` satırına ulaşılmadan servis çöktü. Bu durumda durum hâlâ `Unpaid` kalır; birinci kontrol geçilir. İkinci guard bu boşluğu kapatır.

**Uygulanan örüntü — `PaymentService.cs`:**

```csharp
// Katman 1: entity state kontrolü (taksit zaten Paid mi?)
if (installment.Status == InstallmentStatus.Paid)
    throw new BusinessRuleException("This installment has already been paid.");

// Katman 2: ilişkili tabloda kayıt var mı? (kısmi yazım sonrası koruma)
var existingPayment = await _paymentRepository.GetByInstallmentIdAsync(request.InstallmentId);
if (existingPayment is not null)
    throw new BusinessRuleException("A payment already exists for this installment.");
```

İki guard birbirini tamamlar: birincisi bellek üzerindeki entity state'e, ikincisi veritabanı tablosuna bakar. Herhangi bir sırada başarısız olunsa dahi ikinci istek **422** alır; yinelenen kayıt oluşmaz.

**Doğal idempotency — `InstallmentRepository.cs`:**

```csharp
// Filtre: yalnızca Unpaid olan ve vadesi geçmiş taksitler hedeflenir
await _context.Installments
    .Where(i => i.Status == InstallmentStatus.Unpaid && i.DueDate < now)
    .ExecuteUpdateAsync(s => s.SetProperty(i => i.Status, InstallmentStatus.Overdue));
```

`Status == Unpaid` koşulu sayesinde zaten `Overdue` olan taksitler tekrar hedeflenmez. Aynı sorgu kaç kez çalıştırılırsa çalıştırılsın sonuç değişmez — set semantiği üzerine kurulu idempotency.

**Sınır:** Her iki teknik de _uygulama düzeyinde_ koruma sağlar. Eş zamanlı iki isteğin her iki guard'ı da geçmesi (read-modify-write yarışı) teorik olarak mümkündür. Bu sınırın nasıl giderilebileceği için bkz. [YK-11](#yk-11--http-düzeyi-idempotency-key).

---

### K-17 · Soft Delete — müşteri kaydı

**Karar:** `DELETE /api/customers/{id}` endpoint'i kayıtları fiziksel olarak silmez; `Customer.IsDeleted = true` ve `Customer.DeletedAt = UtcNow` set eder. EF Core global query filter (`HasQueryFilter(e => !e.IsDeleted)`) silinmiş kayıtları tüm sorgulardan otomatik olarak dışlar.

**Gerekçe:** Finansal sistemlerde müşteri kaydının silinmesi denetim izini (audit trail) yok eder:
- Silinmiş müşterinin kredi, taksit ve ödeme geçmişi `CASCADE DELETE` ile birlikte kalıcı kaybolur.
- BDDK gibi düzenleyici kurumlar finansal verilerin yıllarca saklanmasını zorunlu kılar.
- Hard delete geri alınamaz; soft delete her zaman geri döndürülebilir.

**Uygulama:**

```csharp
// Domain/Entities/Customer.cs
public bool IsDeleted { get; set; } = false;
public DateTime? DeletedAt { get; set; }

// Infrastructure/Persistence/AppDbContext.cs
entity.HasQueryFilter(e => !e.IsDeleted); // tüm sorgular otomatik filtreli

// Infrastructure/Persistence/Repositories/CustomerRepository.cs
public async Task DeleteAsync(Customer customer)
{
    customer.IsDeleted = true;
    customer.DeletedAt = DateTime.UtcNow;
    _context.Customers.Update(customer);
    await _context.SaveChangesAsync();
}
```

**Migration:** `20260511000002_AddSoftDeleteToCustomer` — `IsDeleted` ve `DeletedAt` kolonları, filtered unique index'ler.

**UI etkisi:** Kullanıcı için davranış değişmez; silinen müşteri listede görünmez. Veri DB'de korunur.

---

### K-18 · Giriş Validasyonu — TC Kimlik No ve Telefon Numarası

**Karar:** `CreateCustomerRequestValidator` ve yeni eklenen `UpdateCustomerRequestValidator`'a format kuralları eklendi.

**Problem:** Önceden `IdentityNumber` yalnızca 11 karakter uzunluğunu kontrol ediyordu; harfli giriş (örn. `"ABCDEFGHIJK"`) kabul ediliyordu. `PhoneNumber` için hiçbir format kuralı yoktu; `"123"` veya `"abc"` geçerli sayılıyordu. Ayrıca `UpdateAsync` metodu hiç validator çağırmıyordu — güncelleme isteklerinde bu kurallar atlanabiliyordu.

**Çözüm:**

```csharp
// CreateCustomerRequestValidator
RuleFor(x => x.IdentityNumber)
    .Matches(@"^\d{11}$").WithMessage("Identity number must be exactly 11 digits.");

RuleFor(x => x.PhoneNumber)
    .Matches(@"^\d{10,11}$").WithMessage("Phone number must be 10 or 11 digits.");

// UpdateCustomerRequestValidator (yeni dosya)
// FirstName, LastName, Email kurallarına ek olarak:
RuleFor(x => x.PhoneNumber)
    .Matches(@"^\d{10,11}$").WithMessage("Phone number must be 10 or 11 digits.");

// CustomerService.UpdateAsync — ilk satır olarak eklendi
await _updateValidator.ValidateAndThrowAsync(request);
```

**Not:** `UpdateCustomerRequest`'te `IdentityNumber` alanı bulunmaz; TC düzenleme iş kuralı gereği mümkün değildir.

---

### K-14 · `--no-ff` merge stratejisi

**Karar:** Feature branch'leri `develop`'a ve `develop`, `main`'e `--no-ff` (no fast-forward) ile merge edildi.

**Gerekçe:** Fast-forward merge, feature branch'inin varlığını geçmişten siler; `git log --graph` ile hangi işin hangi branch'te yapıldığı görülemez. `--no-ff` her merge için açık bir merge commit oluşturur; bu sayede iş geçmişi okunabilir kalır ve hangi özelliklerin bir arada geliştirildiği izlenebilir.

---

## Yapılmayan Kararlar

---

### YK-01 · Guid ID

**Tercih edilen alternatif:** `int` (bkz. K-02)

**Neden uygulanmadı:**
SQL Server'da `Guid` birincil anahtar, clustered index'te rastgele konumlara yazılır. Bu durum sürekli page split'e ve disk I/O artışına yol açar. Doğru uygulanması için `newsequentialid()` SQL Server fonksiyonu veya .NET'te `Guid.CreateVersion7()` (monoton artan, zaman bileşenli) kullanılması gerekir. Bu detayın gözetilmeden `Guid.NewGuid()` ile yapılması, görünürde profesyonel ama pratikte performansı düşüren bir karara dönüşür. Ek olarak: `Installment` ve `Payment` ID'leri hiçbir zaman dış dünyaya açılmaz; bu entity'ler için Guid'in güvenlik avantajı (sequential leakage önleme) anlamsızdır.

---

### YK-02 · AutoMapper

**Neden uygulanmadı:**
AutoMapper, çok sayıda DTO-Entity dönüşümü olan büyük projelerde tekrarı azaltır. Bu projede 4 entity ve 4 DTO ailesi bulunmaktadır; manuel mapping metotları (`MapToResponse`) her sınıfta tek bir private static metot olarak yer alır. AutoMapper eklemek: (1) ek bağımlılık, (2) profil konfigürasyonu, (3) çalışma zamanına ertelenen hata tespiti maliyetini getirir. Açık ve izlenebilir manuel mapping, bu ölçekte AutoMapper'dan daha az karmaşıktır.

---

### YK-03 · Generic Repository (`IRepository<T>`)

**Neden uygulanmadı:**
`IRepository<T>` ile `GetAll`, `GetById`, `Add`, `Update`, `Delete` metodlarını tek bir arayüzde toplamak cazip görünür. Ancak bankacılık entity'leri domain'e özgü sorgular gerektirir: `GetByIdentityNumberAsync`, `GetByIdWithInstallmentsAsync`, `GetByInstallmentIdAsync`, `UpdateOverdueAsync`. Bu metodlar `IRepository<T>`'ye sığmaz; generic interface'in yanına entity'e özel interface eklemek ise generic'in sağladığı soyutlamayı anlamsız kılar. Domain'e özgü interface'ler (`ILoanRepository`, `ICustomerRepository`) niyeti daha açık ifade eder.

---

### YK-04 · CQRS (Command Query Responsibility Segregation)

**Neden uygulanmadı:**
CQRS, okuma ve yazma modellerinin birbirinden önemli ölçüde farklılaştığı, yüksek ölçekli sistemlerde anlamlıdır. Bu projede okuma ve yazma modelleri örtüşmekte; MediatR gibi bir kütüphanenin eklenmesi handler, command ve query sınıfları çoğaltır. Dört servis sınıfı, hem okuma hem yazma sorumluluklarını bu ölçek için yeterli düzeyde yönetmektedir.

---

### YK-05 · Result&lt;T&gt; pattern (exception yerine)

**Neden uygulanmadı:**
`Result<T>` veya `OneOf<T, Error>` gibi pattern'lar exception'ları dönüş değerine taşıyarak iş akışını daha öngörülebilir kılar. Bu projede `NotFoundException` ve `BusinessRuleException` belirli HTTP kodlarına karşılık gelmekte ve `ExceptionHandlingMiddleware` tarafından tutarlı biçimde yakalanmaktadır. Exception tabanlı hata yönetimi bu mimaride yeterlidir; Result pattern eklemenin sağlayacağı kazanım, getirdiği tip karmaşıklığını karşılamamaktadır.

---

### YK-06 · Soft Delete ~~uygulanmadı~~ → **K-17 olarak uygulandı**

**Durum güncellendi:** Bu karar başlangıçta "yapılmayan" olarak sınıflandırılmıştı. Finansal sistemlerde müşteri verisinin kalıcı silinmesinin denetim ve yasal uyum açısından yarattığı riskler değerlendirilerek soft delete uygulamaya alındı. Detaylar için bkz. **K-17**.

---

### YK-07 · `ProducesResponseType` attribute'leri

**Neden uygulanmadı:**
`[ProducesResponseType(StatusCodes.Status200OK)]` gibi attribute'ler Swagger'ın olası HTTP yanıtlarını belgelemesine olanak tanır. Bu projede `ExceptionHandlingMiddleware` tüm hata yanıtlarını merkezi olarak üretmektedir; olası hata kodları `/// <summary>` yorumlarında açıklanmıştır. Her action'a tüm olası yanıt kodlarını attribute olarak eklemek — özellikle middleware katmanından gelen 404, 422, 400'ler için — tekrarlı ve bakımı güç bir yapı doğurur.

---

### YK-08 · Arka plan servisi ile Overdue güncelleme

**Neden uygulanmadı:**
`IHostedService` veya `BackgroundService` ile periyodik çalışan bir görev daha doğru bir Overdue yönetimi sağlar; `GET /api/installments` çağrılmadan da vadesi geçmiş taksitler güncellenir. Ancak bu yaklaşım: scheduler konfigürasyonu, eş zamanlılık (concurrency) yönetimi ve çoklu instance çalışma durumunda distributed lock gerektirir. Mevcut GetAll tetiklemeli yaklaşım bu kapsam için yeterlidir; arka plan servisi gerçek üretim ihtiyacı ortaya çıktığında eklenebilir.

---

### YK-09 · Authentication / Authorization

**Neden uygulanmadı:**
JWT tabanlı kimlik doğrulama ve rol bazlı yetkilendirme (`[Authorize]`) gerçek bir bankacılık API'sinde zorunludur. Bu proje değerlendirme kapsamında domain mantığını, mimari tasarımı ve kod kalitesini ön plana çıkarmayı hedeflemektedir. Auth katmanı eklemek, temel gereksinimleri gölgeleyen ek karmaşıklık yaratırdı.

---

### YK-11 · HTTP düzeyi `Idempotency-Key`

**Tercih edilen alternatif:** İş kuralı guard'ları (bkz. K-16)

**Neden uygulanmadı:**
Gerçek anlamda HTTP-level idempotency, istemcinin her `POST` isteğiyle benzersiz bir `Idempotency-Key` header'ı göndermesini; sunucunun bu anahtarı cache'e almasını (Redis, veritabanı) ve tekrar gelen aynı anahtarlı istekte cached yanıtı döndürmesini gerektirir:

```
POST /api/payments
Idempotency-Key: a3f9c2d1-84b0-4e7f-9c12-3a5e7d8f0b1c

→ İlk istek: işlem gerçekleşir, yanıt cache'e alınır.
→ Aynı key ile ikinci istek: işlem tekrar çalıştırılmaz, cache'den aynı yanıt döner.
```

Bu yaklaşım iki temel sorunu çözer:
1. **Ağ kesilmesi (retry):** İstemci yanıt alamadığında aynı isteği tekrar gönderir; idempotency anahtarı sayesinde işlem ikinci kez çalışmaz.
2. **Race condition:** Mevcut `K-16` guard'larında eş zamanlı iki istek, read-modify-write penceresi nedeniyle her iki kontrolü de geçebilir. Veritabanı düzeyi unique constraint (`UNIQUE (InstallmentId)` on `Payments` tablosu) veya distributed lock bu yarışı kırabilir.

**Bu projede uygulanmama nedenleri:**

- Idempotency-Key cache'i için Redis veya ayrı bir veritabanı tablosu gerektirir.
- Anahtarın TTL yönetimi, cache miss senaryoları ve istemci implementasyonu ek altyapı maliyeti yaratır.
- Tek kullanıcılı / değerlendirme kapsamındaki bu projede eş zamanlı istek senaryosu gerçekçi değildir.
- Mevcut iş kuralı guard'ları, bu kapsam için yeterli koruma sağlamaktadır.

**Üretim sistemine taşınacaksa:** `Payments` tablosuna `UNIQUE (InstallmentId)` database constraint'i eklenmesi, uygulama düzeyi kontrollerinden bağımsız olarak en güçlü güvenceyi sağlar ve `Idempotency-Key` altyapısı kurulana kadar geçici bir köprü işlevi görür.

---

### YK-10 · Docker Compose

**Neden uygulanmadı:**
`docker-compose.yml` ile SQL Server container'ı ve uygulama tek komutla ayağa kaldırılabilir. SQL Server container'ı zaten bağımsız olarak çalışmaktadır; uygulama `dotnet run` ile başlatılmaktadır. Bu ölçekte Docker Compose'un sağladığı kolaylık, ek konfigürasyon yükünü karşılamamaktadır. Gerçek bir CI/CD hattına bağlanacak projede Docker Compose veya Kubernetes manifest'i ilk eklenecek altyapı öğesi olurdu.

---

### K-19 · Sıralı Ödeme Kuralı (Sequential Payment)

**Karar:** Taksitler yalnızca `InstallmentNumber` sırasına göre ödenebilir. Önceki ödenmemiş taksit varken ileri bir taksit için `POST /api/payments` isteği `422` ile reddedilir.

**Gerekçe:** Bankacılık sistemlerinde sıralı ödeme zorunludur çünkü:

1. Gecikmiş borcun öncelikli tahsil edilmesi yasal gerekliliktir.
2. Faiz muhasebesi "en eski borcun ilk ödenmesi" varsayımıyla çalışır.
3. Overdue takip mekanizması sıradan bağımsız düşünülemez — hangi taksitin geciktiği bilgisi ancak önceki tüm taksitler ödenmiş ise anlamlıdır.

**Uygulama:**
```csharp
var hasEarlierUnpaid = loan.Installments.Any(i =>
    i.InstallmentNumber < installment.InstallmentNumber &&
    i.Status != InstallmentStatus.Paid);

if (hasEarlierUnpaid)
    throw new BusinessRuleException("Önceki ödenmemiş taksitler önce ödenmelidir.");
```

**Frontend yansıması:** Taksit planı tablosunda yalnızca en düşük numaralı ödenmemiş taksit için "Öde" butonu görünür; diğerleri "Önceki bekliyor" mesajıyla gösterilir. Bu UX kararı, backend validasyonunu kullanıcıya önceden hissettirmek için alınmıştır.

---

### K-20 · Ödeme Geçmişi Bonusu (CreditScoreBonus)

**Karar:** Her başarılı ödeme sonrası müşterinin `CreditScoreBonus` değeri güncellenir. Zamanında ödeme +5, gecikmeli ödeme −10 bonus verir. Değer [−200, +200] aralığında kısıtlanır. Bu bonus, bir sonraki kredi değerlendirmesinde `MockCreditScoreService`'in hesapladığı baz skora eklenir.

**Gerekçe:** Statik profil verisine (yaş, gelir, meslek) dayalı skor zamanla değişmez; bu da tüm müşterileri kendi profil bantlarında sabit tutar. Ödeme bonusu sayesinde:

- Düzenli ödeme yapan müşteri zamanla daha iyi faiz oranı kazanabilir.
- Ödemeleri aksatan müşterinin skoru düşer, sonraki başvurularda daha yüksek faiz veya red ile karşılaşır.
- Bu mekanizma gerçek bankacılık sistemlerindeki kredi davranış skorlamasını (behavioral scoring) basit bir şekilde modeller.

**Neden ±200 ile sınırlandı:** Baz skor maksimumu 800'dür. Bonus [−200, +200] aralığı, ödeme geçmişinin etkisini belirgin (%20) ama dominant olmayan bir seviyede tutmak için seçilmiştir. Tek başına ödeme davranışı müşteriyi Low'dan VeryHigh'a taşıyamaz; profil belirleyici olmaya devam eder.

```csharp
bool isOnTime = installment.DueDate.Date >= DateTime.UtcNow.Date;
int delta = isOnTime ? +5 : -10;
customer.CreditScoreBonus = Math.Clamp(customer.CreditScoreBonus + delta, -200, +200);
```

---

### K-22 · KKDF ve BSMV'nin Brüt Orana Dahil Edilmesi

**Karar:** Tüm taksit hesaplamalarında (`LoanEvaluationService`, `StandardInstallmentStrategy`, `LoanService.ComputeTotalPayable`) aylık net vade oranına KKDF (%15) ve BSMV (%5) vergiler eklenerek brüt oran kullanılır.

```
brüt oran = net oran × (1 + 0.15 + 0.05) = net oran × 1.20
```

Her taksit satırı aşağıdaki bileşenlere ayrıştırılır:

```
Brüt Faiz   = Kalan Bakiye × (brüt oran / 100)
Net Faiz    = Brüt Faiz / 1.20
KKDF        = Net Faiz × 0.15
BSMV        = Net Faiz × 0.05
Anapara     = Taksit - Brüt Faiz
```

UI'de hem `LoanEvaluationService`'in ürettiği amortisman önizleme tablosu hem de `LoanDetail` sayfasındaki gerçek taksit tablosu bu dökümü gösterir.

YMO (Yıllık Maliyet Oranı) brüt aylık oranın bileşik yıllık karşılığı olarak hesaplanır:

```
YMO = ((1 + brüt oran / 100)^12 − 1) × 100
```

**Gerekçe:** BDDK düzenlemelerine göre ihtiyaç ve taşıt kredilerinde KKDF (%15) ve BSMV (%5) müşterinin ödediği taksit içinde yer alır. Net oranla hesaplanmış taksit bu vergileri dışarıda bırakır ve müşteriye yanlış (düşük) taksit tutarı gösterir. Brüt oran kullanımı değerlendirme tahminini (evaluation) ile gerçek taksit miktarını (installment) tutarlı kılar; UI'deki vergi dökümü şeffaflık sağlar.

**Etkilenen dosyalar:**
- `CreditCase.Infrastructure/Services/StandardInstallmentStrategy.cs`
- `CreditCase.Application/Services/LoanEvaluationService.cs`
- `CreditCase.Application/Services/LoanService.cs`
- `CreditCase.UI/src/pages/LoanDetail.tsx` (frontend hesaplama)
- `CreditCase.UI/src/pages/Loans.tsx` (değerlendirme paneli)

---

### K-21 · Strategy Pattern — Taksit Planı Üretimi

**Karar:** Taksit planı üretimi `IInstallmentPlanStrategy` interface'i arkasında iki ayrı strateji sınıfına bölündü: `StandardInstallmentStrategy` (eşit taksit) ve `BalloonPaymentStrategy` (balon ödeme).

**Gerekçe:** `LoanService`, hangi stratejinin kullanılacağını `isBalloonPayment` bayrağına göre seçer; strateji implementasyonu hakkında hiçbir bilgiye sahip değildir:

```csharp
IInstallmentPlanStrategy strategy = request.IsBalloonPayment
    ? _strategies.First(s => s.SupportsBalloon)
    : _strategies.First(s => !s.SupportsBalloon);

var installments = strategy.Generate(principal, rate, term, startDate);
```

Bu yaklaşımın avantajları:
- Yeni bir ödeme modeli (örn. anapara ertelemeli, mevsimlik ödeme planı) eklenmesi mevcut kodu değiştirmez; yeni bir strateji sınıfı yazılır ve DI'a kaydedilir.
- Her strateji izole birim testlerle doğrulanabilir.
- `LoanService` sınıfı tek sorumlulukla kalır: orkestrasyonu yönetmek, hesaplamayı yapmamak.

**Balon kısıtı:** `BalloonPaymentStrategy`, balon taksit tutarı anaparanın %50'sini aştığında `BusinessRuleException` fırlatır. Bu kontrol strateji içindedir, `LoanService` bilmez — Tek Sorumluluk Prensibi (SRP) bu kısıtı da içerir.
