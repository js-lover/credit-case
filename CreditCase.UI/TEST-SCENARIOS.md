# Frontend Test Senaryoları — CreditCase.UI

Bu belge, CreditCase.UI React uygulamasının manuel test senaryolarını kapsar.
Her senaryo; **Hazırlık**, **Adımlar** ve **Beklenen Sonuç** bölümlerinden oluşur.

---

## İçindekiler

1. [Genel Altyapı](#1-genel-altyapı)
2. [Dashboard](#2-dashboard)
3. [Müşteri Listesi (Customers)](#3-müşteri-listesi-customers)
4. [Müşteri Detayı (CustomerDetail)](#4-müşteri-detayı-customerdetail)
5. [Kredi Listesi (Loans)](#5-kredi-listesi-loans)
6. [Kredi Detayı ve Ödeme (LoanDetail)](#6-kredi-detayı-ve-ödeme-loandetail)
7. [Ödeme Geçmişi (Payments)](#7-ödeme-geçmişi-payments)
8. [Hata Yönetimi (Error Handling)](#8-hata-yönetimi-error-handling)
9. [Veri Formatlama](#9-veri-formatlama)
10. [Giriş Validasyonu](#10-giriş-validasyonu)
11. [Soft Delete](#11-soft-delete)

---

## 1. Genel Altyapı

### TS-GEN-01 — Uygulama İlk Yükleme

**Hazırlık:** Backend çalışıyor (`http://localhost:5285`), `VITE_API_URL` doğru.

**Adımlar:**
1. `http://localhost:5173` adresine git.

**Beklenen Sonuç:**
- Sidebar solda görünür (koyu arka plan, beyaz metin).
- Dashboard sayfası yüklenir.
- Boş veri için sıfır değerleri gösterilir; hata ekranı çıkmaz.

---

### TS-GEN-02 — Sidebar Navigasyon

**Adımlar:**
1. Sidebar'daki "Müşteriler" linkine tıkla.
2. "Krediler" linkine tıkla.
3. "Ödemeler" linkine tıkla.
4. "Dashboard" linkine tıkla.

**Beklenen Sonuç:**
- Her tıklamada doğru sayfa yüklenir.
- Aktif link `#1B4FD8` mavi arka planla vurgulanır; diğerleri beyaz/opak kalır.
- URL çubuğu `/`, `/customers`, `/loans`, `/payments` olarak değişir.

---

### TS-GEN-03 — Backend Kapalıyken Yükleme

**Hazırlık:** Backend durdurulmuş.

**Adımlar:**
1. Herhangi bir sayfaya git veya yenile.

**Beklenen Sonuç:**
- `"Sunucuya ulaşılamıyor."` toast mesajı sağ üstte görünür.
- Sayfa çökmez; loading durumu kalkar.

---

## 2. Dashboard

### TS-DASH-01 — Özet Kartların Doğruluğu

**Hazırlık:** Sistemde en az 2 müşteri, 3 kredi ve birkaç taksit olsun.

**Adımlar:**
1. Dashboard'a git.

**Beklenen Sonuç:**
- **Toplam Müşteri:** `GET /api/customers` sonucundaki kayıt sayısıyla eşleşir.
- **Aktif Kredi:** Status = Active olan kredilerin sayısıyla eşleşir.
- **Bekleyen Borç:** Tüm Unpaid + Overdue taksitlerin toplam tutarı — `₺` formatında.
- **Gecikmiş Taksit:** Overdue taksitlerin sayısıyla eşleşir; varsa kırmızı, yoksa yeşil renkte.

> **Not:** "Bekleyen Borç" ve "Gecikmiş Taksit" `GET /api/installments` üzerinden hesaplanır çünkü `GET /api/loans` installment detaylarını döndürmez.

---

### TS-DASH-02 — Son Müşteriler Tablosu

**Adımlar:**
1. Dashboard'a git.
2. "Son Müşteriler" tablosuna bak.

**Beklenen Sonuç:**
- En fazla 5 kayıt gösterilir.
- Sıralama `createdAt` azalan (en yeni en üstte).
- "Detay" butonuna tıklayınca `/customers/:id` sayfasına gidilir.

---

### TS-DASH-03 — Son Krediler Tablosu

**Adımlar:**
1. Dashboard'a git.
2. "Son Krediler" tablosuna bak.

**Beklenen Sonuç:**
- En fazla 5 kredi gösterilir.
- "Kalan" sütunu `₺` formatında ve mavi renkte.
- LoanStatusBadge: Aktif → mavi, Kapalı → gri.
- "Taksitler →" butonuna tıklayınca `/loans/:id` sayfasına gidilir.

---

## 3. Müşteri Listesi (Customers)

### TS-CUST-01 — Boş Liste Durumu

**Hazırlık:** Hiç müşteri yok.

**Adımlar:**
1. `/customers` sayfasına git.

**Beklenen Sonuç:**
- Tablo başlıkları görünür.
- "Henüz müşteri yok." mesajı tüm sütun genişliğinde ortalanmış.
- "+ Yeni Müşteri" butonu görünür.

---

### TS-CUST-02 — Müşteri Oluşturma (Başarılı)

**Adımlar:**
1. "+ Yeni Müşteri" butonuna tıkla.
2. Formu doldur: Ad, Soyad, TC Kimlik No (11 hane), E-posta, Telefon.
3. "Oluştur" butonuna tıkla.

**Beklenen Sonuç:**
- Modal kapanır.
- `"Müşteri oluşturuldu."` toast görünür.
- Yeni müşteri tabloda görünür.

---

### TS-CUST-03 — Müşteri Oluşturma (Zorunlu Alan Boş)

**Adımlar:**
1. "+ Yeni Müşteri" butonuna tıkla.
2. "Ad" alanını boş bırak.
3. "Oluştur" butonuna tıkla.

**Beklenen Sonuç:**
- Form HTML5 required validasyonuyla submit edilmez.
- Modal açık kalır; toast çıkmaz.

---

### TS-CUST-04 — Müşteri Oluşturma (Duplicate E-posta)

**Hazırlık:** `ahmet@example.com` e-postasıyla bir müşteri mevcut.

**Adımlar:**
1. Yeni müşteri formunu aç; aynı e-postayı yaz.
2. "Oluştur" butonuna tıkla.

**Beklenen Sonuç:**
- Backend 422 döner.
- `"A customer with this email already exists."` (ya da lokalize mesaj) toast görünür.
- Modal açık kalır; liste değişmez.

---

### TS-CUST-05 — Müşteri Düzenleme (Pre-fill)

**Adımlar:**
1. Bir müşteri satırında "Düzenle" butonuna tıkla.

**Beklenen Sonuç:**
- Modal açılır.
- Ad, Soyad, E-posta, Telefon alanları mevcut değerlerle dolu.
- "TC Kimlik No" alanı **görünmez** (düzenleme modunda disabled).

---

### TS-CUST-06 — Müşteri Düzenleme (Başarılı)

**Adımlar:**
1. Bir müşteriyi düzenle; telefon numarasını değiştir.
2. "Güncelle" butonuna tıkla.

**Beklenen Sonuç:**
- `"Müşteri güncellendi."` toast.
- Tabloda güncel telefon numarası.

---

### TS-CUST-07 — Müşteri Silme (Onay Akışı)

**Adımlar:**
1. Bir müşteri satırında "Sil" butonuna tıkla.

**Beklenen Sonuç:**
- Onay modalı açılır: `"[Ad Soyad] adlı müşteri ve bağlı tüm kredi/taksit kayıtları kalıcı olarak silinecek."` mesajı görünür.
- "İptal" butonuna tıklayınca modal kapanır; kayıt silinmez.

> **Not:** UI mesajı "kalıcı silme" ifadesi içerse de arka planda **soft delete** uygulanır; veriler DB'de `IsDeleted = true` olarak korunur (bkz. DECISIONS.md K-13).

---

### TS-CUST-08 — Müşteri Silme (Başarılı)

**Adımlar:**
1. Sil modalını aç; "Sil" butonuna tıkla.

**Beklenen Sonuç:**
- `"Müşteri silindi."` toast.
- Müşteri listeden kaybolur.
- Müşteriye ait kredi ve ödeme geçmişi DB'de korunur (soft delete).

---

### TS-CUST-09 — Müşteri Detayına Git

**Adımlar:**
1. Bir müşteri satırında "Detay" butonuna tıkla.

**Beklenen Sonuç:**
- `/customers/:id` sayfasına yönlendirilir.
- Sayfa başlığında müşterinin tam adı görünür.

---

## 4. Müşteri Detayı (CustomerDetail)

### TS-CD-01 — Özet Kartlar

**Hazırlık:** Müşterinin 1 aktif kredisi, birkaç ödenmemiş ve 1 gecikmiş taksiti var.

**Adımlar:**
1. `/customers/:id` sayfasına git.

**Beklenen Sonuç:**
- **Toplam Kredi:** Kredi sayısıyla eşleşir.
- **Kalan Anapara:** `GET /api/customers/{id}/summary` → `totalRemainingPrincipal` — `₺` formatında, mavi.
- **Bekleyen Borç:** `totalOutstandingDebt` — amber renkte.
- **Gecikmiş Taksit:** `overdueInstallments` — varsa kırmızı, yoksa yeşil. Alt metin `"X ödendi · Y bekliyor"`.

---

### TS-CD-02 — Kredi Tablosu ve Detay Linki

**Adımlar:**
1. Müşteri detay sayfasını aç.
2. Kredi tablosundaki "Taksitler →" butonuna tıkla.

**Beklenen Sonuç:**
- Kredi tablosu müşteriye ait tüm kredileri listeler.
- Buton tıklamasıyla `/loans/:id` sayfasına gidilir.

---

### TS-CD-03 — Geri Navigasyon

**Adımlar:**
1. Müşteri detay sayfasında "← Geri" butonuna tıkla.

**Beklenen Sonuç:**
- `/customers` listesine dönülür.

---

### TS-CD-04 — Geçersiz Müşteri ID

**Adımlar:**
1. `/customers/99999` adresine git.

**Beklenen Sonuç:**
- Backend 404 döner.
- `"Kayıt bulunamadı."` toast görünür.
- `/customers` sayfasına yönlendirilir.

---

## 5. Kredi Listesi (Loans)

### TS-LOAN-01 — Müşteri Adı Görünümü

**Hazırlık:** Sistemde müşteriler ve krediler var.

**Adımlar:**
1. `/loans` sayfasına git.

**Beklenen Sonuç:**
- Her kredi satırında müşteri adı soyadı (`customerId` üzerinden lookup) görünür.
- Bilinmeyen müşteri için `#id` formatı gösterilir.

---

### TS-LOAN-02 — Yeni Kredi Oluşturma (Başarılı)

**Hazırlık:** En az bir müşteri kayıtlı.

**Adımlar:**
1. "+ Yeni Kredi" butonuna tıkla.
2. Müşteri seç (dropdown), Kredi Türü seç, Ana Para, vade farkı Oranı, Vade, Başlangıç Tarihi gir.
3. "Kredi Oluştur" butonuna tıkla.

**Beklenen Sonuç:**
- `"Kredi oluşturuldu. Taksit planı hazır."` toast.
- Otomatik olarak `/loans/:id` sayfasına yönlendirilir.
- Taksit planı tablosu dolu görünür.

---

### TS-LOAN-03 — LoanType Integer Gönderimi

**Hazırlık:** Önceki hata: select değeri string gönderiliyordu.

**Adımlar:**
1. Kredi oluşturma formunda "Eğitim" türünü seç.
2. "Kredi Oluştur"a tıkla.

**Beklenen Sonuç:**
- Backend 400 hatası almaz (`LoanType` enum dönüşüm hatası yok).
- Kredi başarıyla oluşturulur.

---

### TS-LOAN-04 — Müşteri Yokken Buton Gizlenir

**Hazırlık:** Hiç müşteri yok.

**Adımlar:**
1. `/loans` sayfasına git.

**Beklenen Sonuç:**
- "+ Yeni Kredi" butonu görünmez.

---

### TS-LOAN-05 — Geçersiz Kredi ID

**Adımlar:**
1. `/loans/99999` adresine git.

**Beklenen Sonuç:**
- Backend 404 döner.
- `"Kayıt bulunamadı."` toast.
- `/loans` listesine yönlendirilir.

---

## 6. Kredi Detayı ve Ödeme (LoanDetail)

### TS-LD-01 — Taksit Planı Görünümü

**Hazırlık:** 12 taksitli bir kredi, birkaç Paid, birkaç Unpaid, birkaç Overdue.

**Adımlar:**
1. `/loans/:id` sayfasına git.

**Beklenen Sonuç:**
- Tablo 12 satır içerir.
- Taksit numarası, tutar (`₺` formatı), vade tarihi, ödeme tarihi, durum badge, aksiyon sütunu.
- Overdue taksitin vade tarihi **kırmızı** ve bold.
- Paid: yeşil badge, "Öde" butonu yok.
- Unpaid / Overdue: amber/kırmızı badge, "Öde" butonu var.

---

### TS-LD-02 — Özet Stat Kartlar

**Adımlar:**
1. Kredi detay sayfasını aç.

**Beklenen Sonuç:**
- **Ana Para:** Kredinin `principalAmount` değeri.
- **Kalan Anapara:** Ödenmemiş taksitlerin toplam tutarı (taksit ödemelerinden sonra güncellenir).
- **Ödenen / Toplam:** `X / term` formatında; gecikmiş varsa kırmızı alt metin.
- **Durum:** "Aktif" veya "Kapalı".

---

### TS-LD-03 — Ödeme Modalı (Sabit Tutar)

**Adımlar:**
1. Unpaid bir taksitte "Öde" butonuna tıkla.

**Beklenen Sonuç:**
- Modal açılır.
- Taksit numarası, ödenecek tutar (`₺` formatı), son ödeme tarihi gösterilir.
- Düzenlenebilir tutar alanı **yoktur** — tutar sabittir.
- "İptal" butonuyla modal kapanır, hiçbir şey değişmez.

---

### TS-LD-04 — Taksit Ödeme (Başarılı)

**Adımlar:**
1. Unpaid bir taksitte "Öde" butonuna tıkla.
2. "Ödemeyi Onayla" butonuna tıkla.

**Beklenen Sonuç:**
- `"X. taksit ödendi."` toast görünür.
- Modal kapanır.
- İlgili satırdaki badge **yeşil "Ödendi"** olur.
- "Öde" butonu o satırdan kaybolur.
- **Kalan Anapara** stat kartı azalır (taksit tutarı kadar).
- **Ödenen / Toplam** sayacı artar.

---

### TS-LD-05 — Tüm Taksitler Ödendi → Kredi Kapanır

**Hazırlık:** Kredinin yalnızca 1 taksiti kaldı (Unpaid).

**Adımlar:**
1. Son taksidi öde.

**Beklenen Sonuç:**
- **Kalan Anapara:** `₺0,00`.
- **Durum** stat kartı: "Kapalı".
- LoanStatusBadge: **gri "Kapalı"**.

---

### TS-LD-06 — Overdue Taksit Ödemesi

**Hazırlık:** Vade tarihi geçmiş, Overdue statüsünde taksit.

**Adımlar:**
1. Overdue taksitte "Öde" butonuna tıkla.
2. Ödemeyi onayla.

**Beklenen Sonuç:**
- Taksit "Ödendi" (yeşil) olur.
- Vade tarihi artık kırmızı gösterilmez (satır rengi normale döner).

---

### TS-LD-07 — İkinci Ödeme Denemesi (İdempotency)

**Hazırlık:** Taksit zaten ödendi.

**Adımlar:**
1. API üzerinden aynı `installmentId` ile tekrar ödeme gönder.

**Beklenen Sonuç:**
- Backend 422 döner.
- `"A payment already exists for this installment."` toast.
- UI'da herhangi bir değişiklik olmaz.

---

## 7. Ödeme Geçmişi (Payments)

### TS-PAY-01 — Liste Görünümü

**Adımlar:**
1. `/payments` sayfasına git.

**Beklenen Sonuç:**
- Tablo: `#`, Taksit ID, Ödeme Tutarı (`₺`), Ödeme Tarihi, Durum.
- Başarılı ödemeler yeşil "Başarılı" badge.
- Başarısız ödemeler kırmızı "Başarısız" badge.

---

### TS-PAY-02 — Boş Liste Durumu

**Hazırlık:** Hiç ödeme yapılmamış.

**Adımlar:**
1. `/payments` sayfasına git.

**Beklenen Sonuç:**
- "Henüz ödeme yok." mesajı görünür.

---

### TS-PAY-03 — Ödeme Sonrası Liste Güncelleme

**Adımlar:**
1. LoanDetail sayfasından bir taksit öde.
2. `/payments` sayfasına git.

**Beklenen Sonuç:**
- Yeni ödeme kayıtları listede görünür.
- Tutar ve tarih doğru formatlı (`₺`, `GG.AA.YYYY`).

---

## 8. Hata Yönetimi (Error Handling)

### TS-ERR-01 — 400 Validation Hatası

**Hazırlık:** Geçersiz form verisi (backend validasyon tetiklenmeli).

**Adımlar:**
1. Müşteri oluşturma formunda TC 11 haneden az.

**Beklenen Sonuç:**
- Backend 400 döner.
- Field hata mesajları birleştirilip toast olarak gösterilir.

---

### TS-ERR-02 — 404 Not Found

**Adımlar:**
1. `/loans/99999` ya da `/customers/99999` adresine git.

**Beklenen Sonuç:**
- `"Kayıt bulunamadı."` toast.
- İlgili liste sayfasına yönlendirilir.

---

### TS-ERR-03 — 422 Business Rule İhlali

**Adımlar:**
1. Zaten ödenmiş taksit için ödeme yap (API düzeyinde).

**Beklenen Sonuç:**
- `"This installment has already been paid."` toast.
- UI durumu değişmez.

---

### TS-ERR-04 — Modal ESC ile Kapanma

**Adımlar:**
1. Herhangi bir modal aç.
2. ESC tuşuna bas.

**Beklenen Sonuç:**
- Modal kapanır. Form state sıfırlanmaz (yeniden açınca aynı değerler).

---

### TS-ERR-05 — Modal Overlay ile Kapanma

**Adımlar:**
1. Herhangi bir modal aç.
2. Modal dışına (karanlık overlay alanına) tıkla.

**Beklenen Sonuç:**
- Modal kapanır.

---

## 9. Veri Formatlama

### TS-FMT-01 — Para Birimi Formatı

**Beklenen Sonuç:**
| Ham Değer | Beklenen Görünüm |
|---|---|
| `15000` | `15.000,00 ₺` |
| `1100.5` | `1.100,50 ₺` |
| `0` | `0,00 ₺` |

---

### TS-FMT-02 — Tarih Formatı

**Beklenen Sonuç:**
| Ham Değer | Beklenen Görünüm |
|---|---|
| `"2026-06-15T00:00:00"` | `15.06.2026` |
| `"2026-01-01T00:00:00"` | `1.01.2026` |

---

### TS-FMT-03 — LoanType Etiketleri

**Beklenen Sonuç:**
| Enum Değeri | Görünen Metin |
|---|---|
| `0` | `Bireysel` |
| `1` | `Eğitim` |
| `2` | `Taşıt` |

---

## 10. Giriş Validasyonu

### TS-VAL-01 — TC Kimlik No: Harf Girişi Engellenir

**Adımlar:**
1. Yeni Müşteri formunu aç.
2. "TC Kimlik No" alanına `"abc12345678"` yazmaya çalış.

**Beklenen Sonuç:**
- Harfler alanda görünmez; yalnızca rakamlar yazılır.
- State'e harf geçmez (`onChange` filtresi `replace(/\D/g, '')` ile temizler).

---

### TS-VAL-02 — TC Kimlik No: 11 Haneden Az/Fazla

**Adımlar:**
1. "TC Kimlik No" alanına `"1234567"` (7 hane) gir.
2. "Oluştur" butonuna tıkla.

**Beklenen Sonuç:**
- HTML5 `minLength={11}` + `pattern="\d{11}"` devreye girer; form submit edilmez.
- Tarayıcı native hata balonu: `"11 haneli rakamlardan oluşmalıdır"` gösterilir.

---

### TS-VAL-03 — TC Kimlik No: 12+ Hane Girilemez

**Adımlar:**
1. "TC Kimlik No" alanına 12 haneli bir sayı yapıştır.

**Beklenen Sonuç:**
- `maxLength={11}` sayesinde alan 11 karakterden fazla kabul etmez; fazla karakterler kesilir.

---

### TS-VAL-04 — Telefon: Harf Girişi Engellenir

**Adımlar:**
1. "Telefon" alanına `"05xx555"` yazmaya çalış.

**Beklenen Sonuç:**
- Harfler görünmez; yalnızca rakamlar state'e geçer.

---

### TS-VAL-05 — Telefon: 10 Haneden Az

**Adımlar:**
1. "Telefon" alanına `"555"` gir.
2. "Oluştur" butonuna tıkla.

**Beklenen Sonuç:**
- HTML5 `minLength={10}` + `pattern="\d{10,11}"` devreye girer; form submit edilmez.
- Tarayıcı native hata balonu: `"10 veya 11 haneli rakamlardan oluşmalıdır"` gösterilir.

---

### TS-VAL-06 — Telefon: 12 Haneden Uzun Girilemez

**Adımlar:**
1. "Telefon" alanına 12 haneli bir numara yapıştır.

**Beklenen Sonuç:**
- `maxLength={11}` aşılmaz; 11. karakterden sonrası kesilir.

---

### TS-VAL-07 — Backend Validasyonu (API Direkt Çağrı)

**Adımlar:**
1. API'ye doğrudan `POST /api/customers` isteği gönder; `phoneNumber: "abc"`.

**Beklenen Sonuç:**
- Backend 400 döner.
- `"Phone number must be 10 or 11 digits."` mesajı response body'de.

---

### TS-VAL-08 — Düzenleme Formunda Telefon Validasyonu

**Hazırlık:** Mevcut bir müşteri var.

**Adımlar:**
1. Müşteri "Düzenle" modalını aç.
2. Telefon alanını `"555"` olarak değiştir.
3. "Güncelle" butonuna tıkla.

**Beklenen Sonuç:**
- HTML5 `minLength` devreye girer; submit edilmez.
- Backend'e ulaşılsa bile `UpdateCustomerRequestValidator` 400 döner.

---

## 11. Soft Delete

### TS-SD-01 — Silinen Müşteri Listede Görünmez

**Adımlar:**
1. Bir müşteriyi "Sil" → onay → "Sil" ile sil.
2. `/customers` sayfasını yenile.

**Beklenen Sonuç:**
- Müşteri listede artık görünmez.
- DB'de kayıt mevcuttur; `IsDeleted = 1`, `DeletedAt` dolu.

---

### TS-SD-02 — Silinen Müşterinin Kredileri API'den Erişilebilir Kalır

**Hazırlık:** Müşterinin en az 1 kredisi var.

**Adımlar:**
1. Müşteriyi sil (UI'da listeden kaybolur).
2. Tarayıcı adres çubuğuna `/loans` yaz; ya da bilinen `/loans/:id` adresine git.

**Beklenen Sonuç:**
- Kredi bilgileri `/loans/:id` sayfasında görünür; soft delete yalnızca müşteriyi etkiler.
- Taksit ve ödeme geçmişi korunur; veri kaybı yoktur.

> Veri korumasının DB düzeyinde doğrulanması için bkz. backend `TEST-SCENARIOS.md` — Soft Delete bölümü.

---

### TS-SD-03 — Silinen Müşterinin E-postasıyla Yeni Kayıt

**Hazırlık:** `ahmet@example.com` e-postasıyla bir müşteri soft-deleted.

**Adımlar:**
1. Yeni müşteri formunu aç; `ahmet@example.com` e-postasını gir.
2. "Oluştur" butonuna tıkla.

**Beklenen Sonuç:**
- Kayıt başarıyla oluşturulur.
- Filtered unique index (`WHERE IsDeleted = 0`) sayesinde çakışma olmaz.

---

### TS-SD-04 — Silinen Müşteri TC'siyle Yeni Kayıt

**Hazırlık:** `12345678901` TC numarasıyla bir müşteri soft-deleted.

**Adımlar:**
1. Aynı TC ile yeni müşteri oluştur.

**Beklenen Sonuç:**
- Kayıt başarıyla oluşturulur; 422 hatası gelmez.

---

### TS-SD-05 — Aktif Müşteriyle Duplicate E-posta Hâlâ Reddedilir

**Hazırlık:** `ahmet@example.com` e-postasıyla aktif (silinmemiş) bir müşteri var.

**Adımlar:**
1. Aynı e-posta ile yeni müşteri oluşturmaya çalış.

**Beklenen Sonuç:**
- Backend 422 döner: `"A customer with this email already exists."` toast.
- Filtered index yalnızca aktif kayıtlardaki tekiliği korur.

---

*Son güncelleme: 2026-05-11*
