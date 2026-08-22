# Sarılar Traffic Fine

Sarılar Group Junior .NET Developer case çalışması kapsamında geliştirilen **Trafik Cezası Yönetim ve Onay Modülü**.

Uygulama; şirket araçlarının ve trafik cezalarının kayıt altına alınmasını, trafik cezalarının rol bazlı ve çok aşamalı bir onay sürecinden geçirilmesini ve tüm onay/ret işlemlerinin izlenebilir şekilde saklanmasını sağlar.

---

## İçindekiler

- [Proje Kapsamı](#proje-kapsamı)
- [Teknoloji Yığını](#teknoloji-yığını)
- [Mimari](#mimari)
- [Solution Yapısı](#solution-yapısı)
- [Temel İş Akışı](#temel-iş-akışı)
- [Yetkilendirme](#yetkilendirme)
- [Dinamik Onay Akışı](#dinamik-onay-akışı)
- [Onay Geçmişi](#onay-geçmişi)
- [Concurrency Yaklaşımı](#concurrency-yaklaşımı)
- [Kurulum](#kurulum)
- [Demo Kullanıcıları](#demo-kullanıcıları)
- [Demo Senaryosu](#demo-senaryosu)
- [Testler](#testler)
- [Teknik Kararlar ve Trade-off'lar](#teknik-kararlar-ve-trade-offlar)
- [Bilinçli Olarak Kapsam Dışında Bırakılanlar](#bilinçli-olarak-kapsam-dışında-bırakılanlar)

---

## Proje Kapsamı

### Araç Yönetimi

Sistemde araçlar aşağıdaki temel bilgilerle tanımlanabilir ve listelenebilir:

- Plaka
- Araç tipi
- Marka
- Model

Desteklenen araç tipleri:

- Binek
- Çekici
- Dorse
- Kiralık Araç

Plaka bilgisi normalize edilir ve veritabanında benzersiz tutulur.

### Trafik Cezası Yönetimi

Bir trafik cezası:

- aktif bir araca bağlanır,
- ceza tarihi ve tutarı ile kaydedilir,
- açıklama içerebilir,
- ilk oluşturulduğunda `Yeni` durumunda saklanır,
- yalnız `Yeni` durumundayken düzenlenebilir,
- açık bir `Onaya Gönder` aksiyonu ile onay sürecine alınır.

### Onay Süreci

Varsayılan development workflow:

```text
Yeni
  |
  | Onaya Gönder
  v
Yönetici Onayı
  |
  | Onayla
  v
Finans Onayı
  |
  | Onayla
  v
Tamamlandı
```

Her onay aşamasında yetkili kullanıcı kaydı onaylayabilir veya gerekçe belirterek reddedebilir.

P0 kapsamında `Tamamlandı` ve `Reddedildi` terminal durumlardır.

---

## Teknoloji Yığını

- .NET 10
- ASP.NET Core MVC
- Entity Framework Core 10
- ASP.NET Core Identity
- SQL Server
- Bootstrap 5
- xUnit
- Git

---

## Mimari

Proje küçük ve odaklı bir **layered modular monolith** olarak tasarlanmıştır.

Amaç, case ölçeğinde gereksiz framework veya pattern maliyeti oluşturmadan katman sorumluluklarını net tutmaktır.

Bağımlılık yönü:

```text
Entities
   ^
   |
Business
   ^
   |
DataAccess

Web
 | \
 |  \-> DataAccess   (DI / Identity / persistence composition root)
 |
 \----> Business

Tests
 \----> Business + Entities
```

### Katman Sorumlulukları

**Entities**

- Domain entity'leri
- Enum'lar
- Domain veri yapıları

**Business**

- Use-case ve iş kuralları
- Yetkilendirme kontrolleri
- State transition kuralları
- Repository / Unit of Work abstraction'ları
- Business DTO ve result modelleri

**DataAccess**

- `AppDbContext`
- Entity Framework Core mapping'leri
- SQL Server repository implementasyonları
- ASP.NET Core Identity persistence
- Migration'lar
- Development seed
- Unit of Work implementasyonu

**Web**

- MVC controller'ları
- ViewModel'ler
- Razor view'lar
- Authentication UI
- DI composition
- HTTP / ClaimsPrincipal sınırı

MVC controller'ları domain işlemleri için doğrudan `DbContext` veya EF repository implementasyonlarına erişmez; Business servisleri üzerinden çalışır.

---

## Solution Yapısı

```text
SarilarTrafficFine.slnx

src/
├── SarilarTrafficFine.Entities
├── SarilarTrafficFine.Business
├── SarilarTrafficFine.DataAccess
└── SarilarTrafficFine.Web

tests/
└── SarilarTrafficFine.UnitTests
```

---

## Temel İş Akışı

### Trafik Cezası Lifecycle

```text
New
 |
 | Submit
 v
InApproval
 |
 +---- Approve ----> sonraki DB-defined workflow step
 |                       |
 |                       +---- son step ise ----> Completed
 |
 +---- Reject(reason) -------------------------> Rejected
```

Kurallar:

- Create her zaman `New` oluşturur.
- Submit yalnız `New` durumunda yapılabilir.
- Edit yalnız `New` durumunda yapılabilir.
- Approve/Reject yalnız mevcut workflow step'inin istediği role sahip kullanıcı tarafından yapılabilir.
- Reject reason zorunludur.
- Completed ve Rejected terminaldir.
- Her Submit/Approve/Reject işlemi ApprovalHistory kaydı üretir.

---

## Yetkilendirme

Development ortamında üç rol kullanılır:

| Rol | Sorumluluk |
|---|---|
| `Operator` | Araç ve trafik cezası oluşturur, `New` cezaları düzenler ve onaya gönderir |
| `Manager` | Development seed'indeki Yönetici Onayı aşamasında işlem yapar |
| `Finance` | Development seed'indeki Finans Onayı aşamasında işlem yapar |

Önemli nokta: Business katmanındaki approval engine, `Manager` veya `Finance` isimlerine göre geçiş yapmaz.

Yetki kontrolü mevcut workflow step'inin:

```text
RequiredRole
```

alanından yapılır.

Bu sayede workflow step sayısı veya rol zinciri değişse bile approval algoritmasının temel davranışı değişmez.

---

## Dinamik Onay Akışı

Onay akışı hard-code edilmiş status zinciri yerine veritabanında tanımlanan workflow ve step kayıtları üzerinden yürür.

Temel yapı:

```text
ApprovalWorkflow
├── Code
├── Name
├── IsActive
└── Steps

ApprovalWorkflowStep
├── StepOrder
├── Name
└── RequiredRole
```

Development seed'i:

```text
TRAFFIC_FINE

1. Yönetici Onayı  -> Manager
2. Finans Onayı    -> Finance
```

Submit sırasında aktif `TRAFFIC_FINE` workflow'u yüklenir ve kayıt ilk `StepOrder` değerine bağlanır.

Approve sırasında:

1. Kayıtlı workflow yüklenir.
2. Mevcut step bulunur.
3. Kullanıcının `RequiredRole` yetkisi kontrol edilir.
4. `StepOrder` değerine göre bir sonraki step aranır.
5. Sonraki step varsa kayıt o aşamaya ilerler.
6. Sonraki step yoksa kayıt `Completed` olur.

Bu nedenle geçiş algoritması belirli rol isimlerine veya yalnız iki aşamalı bir sürece bağımlı değildir.

---

## Onay Geçmişi

`ApprovalHistory` append-only bir audit trail olarak kullanılır.

Her işlemde en az aşağıdaki bilgiler saklanır:

- İşlemi gerçekleştiren kullanıcı
- İşlem tarihi
- İşlem tipi
- Açıklama / ret nedeni
- Önceki durum
- Yeni durum
- İlgili workflow step id
- Workflow step sırası
- Workflow step adı

Örnek:

```text
Operator
Yeni
-> Onayda · Yönetici Onayı

Manager
Onayda · Yönetici Onayı
-> Onayda · Finans Onayı

Finance
Onayda · Finans Onayı
-> Tamamlandı
```

Workflow step adı/sırası history kaydına snapshot olarak yazılır. Böylece daha sonra workflow tanımı değişse bile geçmiş işlem kendi bağlamını korur.

---

## Concurrency Yaklaşımı

`TrafficFine` kaydında SQL Server `rowversion` kullanılır.

Edit ve approval transition işlemlerinde istemcinin bildiği row version ile veritabanındaki güncel row version karşılaştırılır.

Aynı kayıt iki kullanıcı tarafından eş zamanlı değiştirilmişse EF Core concurrency exception'ı Business katmanında kontrollü bir conflict sonucuna çevrilir.

Amaç:

- sessiz veri ezilmesini engellemek,
- aynı approval step'ine iki farklı kullanıcının eş zamanlı işlem yapması riskini azaltmak,
- kullanıcıya kontrollü hata göstermek.

---

# Kurulum

## 1. Ön Koşullar

Aşağıdakiler kurulu olmalıdır:

- Git
- .NET 10 SDK
- SQL Server veya SQL Server Express

Bu proje local development sırasında SQL Server Express named instance ile doğrulanmıştır:

```text
.\SQLEXPRESS
```

Farklı bir SQL Server instance kullanılıyorsa aşağıdaki connection string kendi ortama göre değiştirilmelidir.

---

## 2. Repository'yi Klonla

```powershell
git clone https://github.com/denizumur/SarilarTrafficFine.git
cd SarilarTrafficFine
```

---

## 3. Bağımlılıkları ve Local Tool'ları Restore Et

```powershell
dotnet restore SarilarTrafficFine.slnx
dotnet tool restore
```

---

## 4. Development Connection String'i User Secrets'a Kaydet

Web project User Secrets kullanır.

SQL Server Express / Windows Authentication örneği:

```powershell
dotnet user-secrets set `
  "ConnectionStrings:DefaultConnection" `
  "Server=.\SQLEXPRESS;Database=SarilarTrafficFineDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True" `
  --project src/SarilarTrafficFine.Web
```

Başka bir SQL Server instance kullanılıyorsa yalnız connection string kendi ortama göre değiştirilmelidir.

> `TrustServerCertificate=True` local development kolaylığı için kullanılmaktadır.

---

## 5. Development Demo Parolasını User Secrets'a Kaydet

Development seeder kaynak kod içinde parola tutmaz.

Kendi local güçlü demo parolanızı tanımlayın:

```powershell
dotnet user-secrets set `
  "Seed:DemoPassword" `
  "<GÜÇLÜ_BİR_LOCAL_DEMO_PAROLASI>" `
  --project src/SarilarTrafficFine.Web
```

Parola ASP.NET Core Identity password kurallarını karşılamalıdır.

Örneğin kendi ortamınız için uppercase/lowercase/rakam/özel karakter içeren güçlü bir development parolası kullanabilirsiniz.

Gerçek parola:

- repository'ye commit edilmez,
- `appsettings.json` içine yazılmaz,
- README içinde paylaşılmaz.

User Secrets kontrolü:

```powershell
dotnet user-secrets list `
  --project src/SarilarTrafficFine.Web
```

---

## 6. Veritabanını Oluştur / Migration'ları Uygula

Önce local EF tool restore edilmiş olmalıdır:

```powershell
dotnet tool restore
```

Ardından:

```powershell
dotnet tool run dotnet-ef database update `
  --project src/SarilarTrafficFine.DataAccess `
  --startup-project src/SarilarTrafficFine.Web
```

Uygulama startup sırasında otomatik `Database.Migrate()` veya `EnsureCreated()` çalıştırmaz.

Migration uygulamak bilinçli ve açık bir deployment/development adımıdır.

---

## 7. Build

```powershell
dotnet build SarilarTrafficFine.slnx
```

---

## 8. Test

```powershell
dotnet test
```

---

## 9. Development Ortamında Çalıştır

PowerShell:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"

dotnet run `
  --project src/SarilarTrafficFine.Web
```

Terminalde gösterilen localhost adresini tarayıcıda açın.

Development startup sırasında seed işlemi idempotent şekilde:

- rolleri,
- demo kullanıcılarını,
- trafik cezası approval workflow'unu

eksikse oluşturur.

---

## Demo Kullanıcıları

Development seed aşağıdaki kullanıcıları oluşturur:

| Kullanıcı | Rol |
|---|---|
| `operator@demo.local` | Operator |
| `manager@demo.local` | Manager |
| `finance@demo.local` | Finance |

Üç demo kullanıcı da local ortamınızda:

```text
Seed:DemoPassword
```

için tanımladığınız parolayı kullanır.

Repository içinde demo parolası bulunmaz.

---

## Demo Senaryosu

Uygulamanın ana case akışını doğrulamak için aşağıdaki senaryo kullanılabilir.

### 1. Operator

`operator@demo.local` ile giriş yapın.

1. Gerekirse yeni bir araç oluşturun.
2. Yeni trafik cezası oluşturun.
3. Ceza `Yeni` durumunda görünmelidir.
4. Gerekirse bu aşamada düzenleyin.
5. `Onaya Gönder` aksiyonunu çalıştırın.

Beklenen:

```text
Yeni
-> Onayda · Yönetici Onayı
```

### 2. Manager

Çıkış yapıp `manager@demo.local` ile giriş yapın.

İlgili trafik cezasını açın ve `Onayla` seçin.

Beklenen:

```text
Onayda · Yönetici Onayı
-> Onayda · Finans Onayı
```

### 3. Finance

Çıkış yapıp `finance@demo.local` ile giriş yapın.

İlgili trafik cezasını açın ve `Onayla` seçin.

Beklenen:

```text
Onayda · Finans Onayı
-> Tamamlandı
```

Detay sayfasındaki workflow stepper'da iki approval step'i de tamamlanmış görünür.

ApprovalHistory üzerinde üç işlem görülür:

```text
Submitted
Approved - Yönetici Onayı
Approved - Finans Onayı
```

### Reject Senaryosu

Manager veya Finance kendi aktif approval step'inde `Reddet` seçebilir.

Ret nedeni zorunludur.

Beklenen:

```text
InApproval
-> Rejected
```

History üzerinde:

- reddeden kullanıcı,
- işlem zamanı,
- ilgili workflow step,
- önceki durum,
- yeni durum,
- ret nedeni

görüntülenir.

---

## Testler

Approval engine için 9 adet odaklı Business unit testi bulunmaktadır.

Test edilen ana davranışlar:

1. Submit ilk DB-defined step'e ilerler.
2. Yanlış role sahip kullanıcı approval yapamaz.
3. Approve bir sonraki dinamik step'e ilerler.
4. Son step approval kaydı `Completed` yapar.
5. Reject nedeni boş bırakılamaz.
6. Geçerli reject kaydı `Rejected` yapar.
7. `Completed` kayıt tekrar approve edilemez.
8. `Rejected` kayıt tekrar approve edilemez.
9. ApprovalHistory önceki/yeni durum ve workflow step snapshot'ını doğru saklar.

Dinamik workflow testi özellikle development seed'inden farklı bir zincir kullanır:

```text
Manager
-> Legal
-> Finance
```

Bu test, next-step algoritmasının:

```text
Manager -> Finance
```

şeklinde hard-code edilmediğini doğrular.

Testleri çalıştırmak için:

```powershell
dotnet test
```

EF modelinin migration ile senkron olduğunu kontrol etmek için:

```powershell
dotnet tool run dotnet-ef migrations has-pending-model-changes `
  --project src/SarilarTrafficFine.DataAccess `
  --startup-project src/SarilarTrafficFine.Web
```

Beklenen durumda yeni model değişikliği bulunmamalıdır.

---

## Teknik Kararlar ve Trade-off'lar

### 1. Katmanlı Monolith

Case ölçeğinde CQRS, MediatR veya microservice mimarisi kullanılmadı.

Bunun yerine:

```text
Entities
Business
DataAccess
Web
```

ayrımı ile küçük, okunabilir ve açıklanabilir bir yapı tercih edildi.

### 2. Repository + Unit of Work

EF Core `DbContext` zaten repository/unit-of-work benzeri davranışlara sahip olsa da, case/company sinyali ve katman sınırlarını görünür tutmak amacıyla küçük abstraction'lar kullanıldı.

Generic repository API bilinçli olarak dar tutuldu.

Domain'e özel query gerektiğinde specific repository metotları kullanılır.

### 3. DB-Driven Approval Workflow

Approval state'leri:

```text
PendingManagerApproval
PendingFinanceApproval
```

gibi ayrı enum değerleriyle hard-code edilmedi.

Bunun yerine:

```text
Status = InApproval
+
CurrentApprovalStepId
```

yaklaşımı tercih edildi.

Böylece step sırası ve gerekli rol veritabanındaki workflow tanımından okunur.

### 4. Explicit Submit

Yeni trafik cezası oluşturulduğu anda otomatik olarak approval'a girmez.

```text
Create
-> New
-> explicit Submit
```

yaklaşımı tercih edildi.

Bu sayede `New` gerçek bir düzenlenebilir draft state olarak anlam taşır.

### 5. Append-Only Approval History

Approval geçmişi güncellenmez veya overwrite edilmez.

Her state transition yeni bir history satırı üretir.

### 6. Terminal Rejected

P0 kapsamında `Rejected` terminal kabul edilmiştir.

Bu, case için bilinçli ve basit bir state-machine kararıdır.

Gerçek production senaryosunda veri düzeltme isteği ile kalıcı iş reddinin ayrılması değerlendirilebilir:

```text
ReturnForRevision
vs
FinalReject
```

Ancak bu ek lifecycle case tesliminin P0 kapsamına dahil edilmemiştir.

### 7. No Automatic Migration on Startup

Uygulama startup sırasında migration çalıştırmaz.

Database schema değişiklikleri açık EF migration komutlarıyla uygulanır.

### 8. Secret Management

Development SQL connection string ve demo kullanıcı parolası source control dışında tutulur.

Local development için .NET User Secrets kullanılır.

---

## Bilinçli Olarak Kapsam Dışında Bırakılanlar

Aşağıdaki özellikler mevcut case'in P0 çözümü için gerekli görülmemiştir:

- Kullanıcı kayıt / self-service account yönetimi
- Workflow definition admin UI
- Workflow versioning
- Rejected kayıt için resubmit / revision loop
- Trafik ihlal / ceza türü master kataloğu
- Trafik otoritesiyle dış itiraz/dispute entegrasyonu
- Dashboard / analytics
- Notification sistemi
- Docker
- Redis
- RabbitMQ
- Elasticsearch
- CQRS / MediatR
- Microservice mimarisi
- Genel amaçlı audit log

Bu kararların amacı daha fazla teknoloji göstermek yerine case'in ana problemini küçük, test edilebilir ve sürdürülebilir bir çözümle tamamlamaktır.

---

## Son Not

Projenin ana odağı:

```text
doğru state transition
+
rol bazlı yetkilendirme
+
izlenebilir approval history
+
temiz katman sınırları
+
tekrar üretilebilir local kurulum
```

olarak belirlenmiştir.
