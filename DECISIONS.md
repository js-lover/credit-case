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

### K-06 · Düz (flat-rate) faiz yöntemi

**Karar:** Taksit tutarı `totalAmount = principal × (1 + rate/100 × termYears)` / `term` formülüyle hesaplanır. Her taksit eşit tutardadır.

**Gerekçe:** Azalan bakiye (annuity) yöntemi daha gerçekçidir; ancak bu projenin kapsamı faiz modelinin doğruluğu değil, bankacılık domain'inin doğru modellenmesidir. Düz faiz formülü; kolay doğrulanabilir, test edilebilir ve açıklanabilirdir. Gerçek bir üretim sisteminde faiz motoru ayrı bir servis olarak modüler şekilde eklenir.

---

### K-07 · Overdue güncellemesinin `GetAllAsync` içinde tetiklenmesi

**Karar:** `InstallmentService.GetAllAsync` çağrıldığında, dönüşten önce `UpdateOverdueAsync` çalıştırılır.

**Gerekçe:** Arka plan servisi (background service / hosted service) daha doğru bir çözümdür; ancak bu proje kapsamında Overdue kontrolü yalnızca liste sorgulandığında anlamlıdır. Bu yaklaşım; ek altyapı (IHostedService, timer) gerektirmeden, repository içinde tek bir `ExecuteUpdateAsync` SQL sorgusuyla toplu güncelleme sağlar.

---

### K-08 · Enum değerlerinin string olarak saklanması

**Karar:** `LoanType`, `LoanStatus`, `InstallmentStatus`, `PaymentStatus` enum'ları veritabanında sayısal değer yerine string olarak (`nvarchar`) saklanır. EF Core konfigürasyonunda `HasConversion<string>()` kullanılır.

**Gerekçe:** `0`, `1`, `2` gibi sayısal değerler veritabanında anlamını kaybeder; doğrudan SQL sorgusu ile veriyi yorumlamak güçleşir, migration sırasında enum sıralamasının değişmesi veri bozulmasına yol açar. `"Active"`, `"Closed"` gibi değerler self-documenting'dir ve yeni enum üyesi eklenmesi mevcut verileri etkilemez.

---

### K-09 · `decimal(18,2)` hassasiyeti

**Karar:** Tüm parasal alanlar (`PrincipalAmount`, `InterestRate`, `RemainingPrincipal`, `Amount`, `PaymentAmount`) `decimal` tipinde ve `HasPrecision(18, 2)` konfigürasyonuyla tanımlandı.

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

### K-12 · Mock CreditScoreService — interface arkasında

**Karar:** `ICreditScoreService` arayüzü `Application` katmanında tanımlandı; `MockCreditScoreService` implementasyonu `Infrastructure` katmanına yerleştirildi ve DI üzerinden bağlandı.

**Gerekçe:** Gerçek kredi skoru entegrasyonu hazır olduğunda yalnızca `Infrastructure` katmanına yeni bir implementasyon eklenir; `LoanService` hiçbir değişiklik gerektirmez. Bu yaklaşım Açık/Kapalı Prensibi'ni (OCP) doğrudan uygular.

---

### K-13 · Cascade delete

**Karar:** `Customer → Loan → Installment → Payment` zincirinde her seviye `OnDelete(DeleteBehavior.Cascade)` ile tanımlandı.

**Gerekçe:** Müşteri silindiğinde ait olduğu kredilerin, taksitlerin ve ödemelerin varlığını sürdürmesi anlamsızdır; yetim kayıt (orphan record) oluşturur. Bankacılık sistemlerinde müşteri silme işlemi zaten nadirdir ve bu davranış bilinçli bir iş kararıdır. Daha katı bir yaklaşımda soft-delete tercih edilebilir.

---

### K-15 · Email alanında benzersizlik (unique index + servis guard'ı)

**Karar:** `Customers.Email` kolonu hem veritabanı düzeyinde (`UNIQUE INDEX IX_Customers_Email`) hem uygulama katmanında (`CustomerService`) benzersiz olarak zorlanır. `IdentityNumber` için uygulanan örüntünün aynısı takip edildi.

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

### YK-06 · Soft Delete

**Neden uygulanmadı:**
Soft delete (kayıtların fiziksel olarak silinmemesi; `IsDeleted` flag'i ile işaretlenmesi) gerçek bankacılık sistemlerinde denetim (audit) gereksinimleri nedeniyle zorunludur. Bu projede scope dışı olduğu değerlendirildi; cascade delete basit ve tutarlı bir sonuç üretmektedir. Üretim sistemine taşınacaksa ilk eklenecek özellik soft delete olmalıdır.

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
