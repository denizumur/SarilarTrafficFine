# Sarılar Traffic Fine

Sarılar Group Junior .NET Developer case çalışması kapsamında geliştirilen **Trafik Cezası Yönetim ve Onay Modülü**.

Uygulama; şirket araçlarının ve trafik cezalarının kayıt altına alınmasını, trafik cezalarının rol bazlı ve çok aşamalı bir onay sürecinden geçirilmesini ve onay/ret işlemlerinin izlenebilir şekilde saklanmasını sağlar.

---

## İçindekiler

- [Proje Kapsamı](#proje-kapsamı)
- [Teknoloji Yığını](#teknoloji-yığını)
- [Mimari](#mimari)
- [Solution Yapısı](#solution-yapısı)
- [Kullanıcı ve Rol Modeli](#kullanıcı-ve-rol-modeli)
- [Temel İş Akışı](#temel-iş-akışı)
- [Yetkilendirme](#yetkilendirme)
- [Dinamik Onay Akışı](#dinamik-onay-akışı)
- [Onay Veri Modeli](#onay-veri-modeli)
- [Onay Geçmişi](#onay-geçmişi)
- [Concurrency Yaklaşımı](#concurrency-yaklaşımı)
- [Validation ve Hata Yönetimi](#validation-ve-hata-yönetimi)
- [Kurulum](#kurulum)
- [Demo Kullanıcıları](#demo-kullanıcıları)
- [Demo Senaryosu](#demo-senaryosu)
- [Testler](#testler)
- [Teslim Doğrulaması](#teslim-doğrulaması)
- [Teknik Kararlar ve Trade-off'lar](#teknik-kararlar-ve-trade-offlar)
- [Bilinçli Olarak Kapsam Dışında Bırakılanlar](#bilinçli-olarak-kapsam-dışında-bırakılanlar)
- [PDF Çıktısı ve QuestPDF Lisans Notu](#pdf-çıktısı-ve-questpdf-lisans-notu)

---

# Proje Kapsamı

## Araç Yönetimi

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

Araç oluşturma işlemi P0 kapsamında `Operator` rolü ile sınırlandırılmıştır.

---

## Trafik Cezası Yönetimi

Bir trafik cezası:

- aktif bir araca bağlanır,
- ceza tarihi ve tutarı ile kaydedilir,
- açıklama içerebilir,
- ilk oluşturulduğunda `New / Yeni` durumunda saklanır,
- yalnız `New` durumundayken ve yalnız kaydı oluşturan kullanıcı tarafından düzenlenebilir,
- açık bir `Onaya Gönder / Submit` aksiyonu ile onay sürecine alınır.

Trafik cezası oluşturma işlemi tüm authenticated internal kullanıcılara açıktır.

Ceza tarihi gelecekte olamaz ve tutar pozitif olmalıdır.

---

## Onay Süreci

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

Her aktif onay aşamasında yetkili kullanıcı:

- kaydı onaylayabilir,
- gerekçe belirterek reddedebilir.

Herhangi bir approval aşamasında:

```text
Reject + Ret Nedeni
        |
        v
    Reddedildi
```

P0 kapsamında:

```text
Completed
Rejected
```

terminal durumlardır.

---

# Teknoloji Yığını

- .NET 9
- ASP.NET Core MVC
- Entity Framework Core 9
- ASP.NET Core Identity
- SQL Server
- Razor Views
- Bootstrap 5
- QuestPDF
- xUnit
- Git

Repository SDK sürümü `global.json` ile .NET 9'a sabitlenmiştir.

Local EF CLI aracı da repository içindeki `dotnet-tools.json` üzerinden EF Core 9 sürümüyle yönetilir.

---

# Mimari

Proje küçük ve odaklı bir **layered modular monolith** olarak tasarlanmıştır.

Amaç, case ölçeğinde gereksiz framework veya pattern maliyeti oluşturmadan katman sorumluluklarını açık tutmak, iş kurallarını UI ve persistence detaylarından ayırmak ve kritik business davranışlarını test edilebilir hale getirmektir.

Temel bağımlılık yönü:

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
 |  \----> DataAccess
 |
 \-------> Business

Tests
 \-------> Business + Entities
```

## Katman Sorumlulukları

### Entities

- Domain entity'leri
- Enum'lar
- Domain veri yapıları

Önemli entity'ler:

```text
Vehicle
TrafficFine
ApprovalWorkflow
ApprovalWorkflowStep
ApprovalHistory
```

### Business

- Use-case servisleri
- İş kuralları
- Yetkilendirme kontrolleri
- State transition kuralları
- Repository abstraction'ları
- Unit of Work abstraction'ı
- Business DTO'ları
- Command/result modelleri

Örnek servisler:

```text
TrafficFineService
TrafficFineApprovalQueryService
VehicleService
```

Business katmanı ASP.NET Core `HttpContext` veya MVC detaylarına doğrudan bağımlı değildir.

### DataAccess

- `AppDbContext`
- Entity Framework Core mapping'leri
- SQL Server repository implementasyonları
- ASP.NET Core Identity persistence
- Migration'lar
- Development seed
- Unit of Work implementasyonu

### Web

- MVC controller'ları
- ViewModel'ler
- Razor View'lar
- Authentication UI
- Authorization sınırı
- DI composition root
- HTTP / `ClaimsPrincipal` sınırı
- PDF üretimi

MVC controller'ları domain işlemleri için doğrudan `DbContext` veya concrete EF repository implementasyonlarına erişmez.

İş kuralları Business servisleri üzerinden yürütülür.

---

# Solution Yapısı

```text
SarilarTrafficFine.slnx

src/
├── SarilarTrafficFine.Entities/
│   └── Domain modelleri ve enum'lar
│
├── SarilarTrafficFine.Business/
│   └── İş kuralları, servisler, authorization ve abstraction'lar
│
├── SarilarTrafficFine.DataAccess/
│   └── EF Core, SQL Server, repository, Identity, migrations ve seed
│
└── SarilarTrafficFine.Web/
    └── ASP.NET Core MVC, Razor UI, authentication ve PDF

tests/
└── SarilarTrafficFine.UnitTests/
    └── Business davranış testleri
```

---

# Kullanıcı ve Rol Modeli

Kullanıcı yönetimi için ayrı ve tekrar eden bir custom `User` tablosu oluşturulmamıştır.

ASP.NET Core Identity doğrudan kullanıcı ve rol store'u olarak kullanılır.

Fiziksel SQL Server tarafında temel Identity tabloları:

```text
AspNetUsers
AspNetRoles
AspNetUserRoles
```

Anlamları:

```text
AspNetUsers
    -> kullanıcı kayıtları

AspNetRoles
    -> Operator / Manager / Finance gibi rol tanımları

AspNetUserRoles
    -> kullanıcı ile rol arasındaki eşleşmeler
```

Domain modeli Identity altyapısını yeniden üretmez.

Örneğin bir trafik cezasının sahibi:

```text
TrafficFine.CreatedByUserId
```

üzerinden Identity kullanıcı kimliğiyle ilişkilendirilir.

Approval işlemlerinde de işlemi gerçekleştiren kullanıcı bilgisi history kaydında tutulur.

Development ortamında kullanılan roller:

```text
Operator
Manager
Finance
```

---

# Temel İş Akışı

## Trafik Cezası Lifecycle

```text
Create
  |
  v
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

Temel kurallar:

- Create her zaman `New` oluşturur.
- TrafficFine create tüm authenticated internal kullanıcılar tarafından yapılabilir.
- Submit yalnız `New` durumundaki kayıtta çalışır.
- Submit yalnız kaydı oluşturan kullanıcı tarafından yapılabilir.
- Edit yalnız `New` durumundaki kayıtta çalışır.
- Edit yalnız kaydı oluşturan kullanıcı tarafından yapılabilir.
- Approval/Reject yetkisi aktif workflow step'indeki `RequiredRole` değerine göre belirlenir.
- Bir kullanıcı kendi oluşturduğu trafik cezasını approve/reject edemez.
- Reject reason zorunludur.
- `Completed` ve `Rejected` terminaldir.
- Her Submit / Approve / Reject işlemi `ApprovalHistory` kaydı üretir.

---

# Yetkilendirme

Development ortamında üç temel rol bulunmaktadır.

| Rol | Yetki |
|---|---|
| `Operator` | Araç oluşturabilir; trafik cezası oluşturabilir; kendi oluşturduğu `New` trafik cezalarını düzenleyip onaya gönderebilir |
| `Manager` | Trafik cezası oluşturabilir; kendi oluşturduğu `New` kayıtları düzenleyip onaya gönderebilir; başka kullanıcıların `Manager` rolü isteyen aktif workflow aşamalarında approve/reject yapabilir |
| `Finance` | Trafik cezası oluşturabilir; kendi oluşturduğu `New` kayıtları düzenleyip onaya gönderebilir; başka kullanıcıların `Finance` rolü isteyen aktif workflow aşamalarında approve/reject yapabilir |

Önemli nokta:

Business katmanındaki approval engine:

```text
if Manager -> Finance
```

gibi rol isimlerine göre transition yapmaz.

Yetki kontrolü aktif workflow step'inin:

```text
RequiredRole
```

değerinden yapılır.

Ayrıca approval/reject sırasında:

```text
current user
    !=
TrafficFine creator
```

kuralı Business katmanında doğrulanır.

Bu separation-of-duties kararı sayesinde kullanıcı kendi oluşturduğu cezayı kendi approval rolü eşleşse bile onaylayamaz veya reddedemez.

Controller/UI authorization ilk sınırdır.

Ancak kritik business authorization kuralları yalnız UI'a bırakılmaz; Business katmanında da doğrulanır.

---

# Dinamik Onay Akışı

Onay akışı hard-coded status zinciri yerine veritabanında tanımlanan workflow ve step kayıtları üzerinden yürür.

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

Submit sırasında:

1. Aktif `TRAFFIC_FINE` workflow'u yüklenir.
2. Workflow step'leri sıralanır.
3. İlk `StepOrder` bulunur.
4. `TrafficFine` bu workflow ve ilk step'e bağlanır.
5. Durum `InApproval` olur.
6. Transition `ApprovalHistory` içine kaydedilir.

Approve sırasında:

1. Trafik cezasının kayıtlı workflow'u yüklenir.
2. Mevcut approval step bulunur.
3. Kullanıcının aktif step'in `RequiredRole` değerine sahip olup olmadığı kontrol edilir.
4. Kullanıcının kaydın creator'ı olmadığı doğrulanır.
5. `StepOrder` değerine göre bir sonraki step aranır.
6. Sonraki step varsa kayıt o aşamaya ilerler.
7. Sonraki step yoksa kayıt `Completed` olur.
8. İşlem history kaydına eklenir.

Bu nedenle algoritma:

```text
Manager -> Finance
```

şeklinde hard-coded değildir.

Örneğin workflow verisi:

```text
Manager
  ->
Legal
  ->
Finance
```

olarak değiştirilirse approval engine'in temel next-step algoritmasının değişmesi gerekmez.

---

# Onay Veri Modeli

Case'deki approver-flow ihtiyacı tek bir `ApproverFlow` tablosuyla değil, iki seviyeli bir modelle karşılanmıştır:

```text
ApprovalWorkflow
        |
        v
ApprovalWorkflowStep
```

`ApprovalWorkflow` workflow'un üst tanımını tutar.

`ApprovalWorkflowStep`:

```text
StepOrder
Name
RequiredRole
```

bilgilerini taşır.

Aktif kayıt üzerinde:

```text
TrafficFine
├── CreatedByUserId
├── ApprovalWorkflowId
└── CurrentApprovalStepId
```

bilgileri bulunur.

Böylece sistem hem hangi workflow'un kullanıldığını hem de kaydın şu anda hangi approval step'inde olduğunu bilir.

Genel ilişki:

```text
AspNetUsers
     |
     | creator / actor identity
     v
TrafficFine
     |
     +----------> ApprovalWorkflow
     |                   |
     |                   v
     |           ApprovalWorkflowStep
     |
     +----------> ApprovalHistory
```

Approver belirleme kişi ID'sini workflow'a hard-code etmek yerine rol üzerinden yapılır:

```text
CurrentApprovalStep.RequiredRole
                +
CurrentUser.Roles
                +
CurrentUser != Creator
                =
Approve / Reject yetkisi
```

---

# Onay Geçmişi

`ApprovalHistory`, workflow aksiyonlarının append-only geçmişi olarak kullanılır.

Her Submit / Approve / Reject işleminde en az aşağıdaki bilgiler saklanır:

- işlemi gerçekleştiren kullanıcı,
- işlem tarihi,
- işlem tipi,
- açıklama / ret nedeni,
- önceki durum,
- yeni durum,
- ilgili workflow step id,
- workflow step sırası,
- workflow step adı.

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

Workflow step adı ve sıra bilgisi history kaydına snapshot olarak yazılır.

Böylece daha sonra workflow tanımı değişse bile geçmiş aksiyon kendi tarihsel bağlamını korur.

## ApprovalHistory ve genel Audit Log ayrımı

`ApprovalHistory` yalnız workflow aksiyonlarını temsil eder.

Örneğin `New` durumundaki bir trafik cezasının açıklamasının veya tutarının normal şekilde düzenlenmesi bir approval aksiyonu değildir ve `ApprovalHistory` içine yazılmaz.

Production seviyesinde tüm entity değişikliklerinin izlenmesi istenirse bunun için ayrı bir `AuditLog` mekanizması tasarlanabilir.

Bu ayrım iki farklı sorumluluğun birbirine karışmasını önler.

---

# Concurrency Yaklaşımı

`TrafficFine` kaydında SQL Server `rowversion` kullanılır.

Edit ve approval transition işlemlerinde istemcinin bildiği row version ile veritabanındaki güncel row version karşılaştırılır.

Aynı kayıt iki kullanıcı tarafından eş zamanlı değiştirilmişse EF Core concurrency problemi Business katmanında kontrollü bir conflict sonucuna çevrilir.

Amaç:

- sessiz veri ezilmesini engellemek,
- aynı approval step'ine eş zamanlı işlem yapılması riskini azaltmak,
- kullanıcıya kontrollü hata göstermek.

Bu yaklaşım optimistic concurrency modelidir.

---

# Validation ve Hata Yönetimi

Validation yalnız browser/client tarafına bırakılmaz.

Temel business kuralları Business katmanında da doğrulanır.

Örnekler:

```text
Amount > 0
FineDate <= Today
RejectReason zorunlu
Edit yalnız New
Submit yalnız creator + New
Approve/Reject yalnız RequiredRole
Creator kendi kaydını approve/reject edemez
Completed / Rejected üzerinde yeni approval yapılamaz
```

Özellikle gelecekteki ceza tarihi şu mesajla reddedilir:

```text
Ceza tarihi gelecek bir tarih olamaz.
```

Amaç:

```text
Client-side validation = kullanıcı deneyimi
Server / Business validation = authoritative kural
```

ayrımını korumaktır.

---

# Kurulum

## 1. Ön Koşullar

Aşağıdakiler kurulu olmalıdır:

- Git
- .NET 9 SDK
- SQL Server veya SQL Server Express

Repository `global.json` ile:

```text
9.0.301
```

SDK sürümüne pinlenmiştir.

`rollForward` politikası `latestPatch` olarak yapılandırılmıştır.

Proje local development sırasında SQL Server Express named instance ile doğrulanmıştır:

```text
.\SQLEXPRESS
```

Farklı SQL Server instance kullanılıyorsa connection string kendi ortama göre değiştirilmelidir.

---

## 2. Repository'yi Klonla

```powershell
git clone https://github.com/denizumur/SarilarTrafficFine.git
cd SarilarTrafficFine
```

---

## 3. SDK Sürümünü Kontrol Et

```powershell
dotnet --version
```

Beklenen:

```text
9.0.301
```

---

## 4. Bağımlılıkları ve Local Tool'ları Restore Et

```powershell
dotnet tool restore
dotnet restore .\SarilarTrafficFine.slnx
```

Repository-local EF CLI:

```text
dotnet-ef 9.0.19
```

sürümüyle tanımlanmıştır.

---

## 5. Development Connection String'i User Secrets'a Kaydet

Web project User Secrets kullanır.

SQL Server Express / Windows Authentication örneği:

```powershell
dotnet user-secrets set `
  "ConnectionStrings:DefaultConnection" `
  "Server=.\SQLEXPRESS;Database=SarilarTrafficFineDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True" `
  --project .\src\SarilarTrafficFine.Web
```

Başka bir SQL Server instance kullanılıyorsa yalnız connection string kendi ortama göre değiştirilmelidir.

> `TrustServerCertificate=True` local development kolaylığı için kullanılmıştır.

---

## 6. Development Demo Parolasını User Secrets'a Kaydet

Development seeder kaynak kod içinde parola tutmaz.

Kendi local güçlü demo parolanızı tanımlayın:

```powershell
dotnet user-secrets set `
  "Seed:DemoPassword" `
  "<GÜÇLÜ_BİR_LOCAL_DEMO_PAROLASI>" `
  --project .\src\SarilarTrafficFine.Web
```

Parola ASP.NET Core Identity password kurallarını karşılamalıdır.

Gerçek parola:

- repository'ye commit edilmez,
- `appsettings.json` içine yazılmaz,
- README içinde paylaşılmaz.

User Secrets kontrolü:

```powershell
dotnet user-secrets list `
  --project .\src\SarilarTrafficFine.Web
```

---

## 7. Veritabanını Oluştur / Migration'ları Uygula

Önce local tool restore edilmiş olmalıdır:

```powershell
dotnet tool restore
```

Ardından:

```powershell
dotnet tool run dotnet-ef database update `
  --project .\src\SarilarTrafficFine.DataAccess `
  --startup-project .\src\SarilarTrafficFine.Web
```

Uygulama startup sırasında otomatik `Database.Migrate()` veya `EnsureCreated()` çalıştırmaz.

Migration uygulamak bilinçli ve açık bir development/deployment adımıdır.

---

## 8. Build

```powershell
dotnet build .\SarilarTrafficFine.slnx
```

---

## 9. Test

```powershell
dotnet test .\SarilarTrafficFine.slnx
```

Final doğrulamada:

```text
20/20 PASS
```

sonucu alınmıştır.

---

## 10. EF Model / Migration Drift Kontrolü

```powershell
dotnet tool run dotnet-ef migrations has-pending-model-changes `
  --project .\src\SarilarTrafficFine.DataAccess `
  --startup-project .\src\SarilarTrafficFine.Web
```

Beklenen sonuç:

```text
No changes have been made to the model since the last migration.
```

---

## 11. Development Ortamında Çalıştır

PowerShell:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"

dotnet run `
  --project .\src\SarilarTrafficFine.Web
```

Terminalde gösterilen localhost adresini tarayıcıda açın.

Development startup sırasında seed işlemi idempotent şekilde eksik olan:

- rolleri,
- demo kullanıcılarını,
- trafik cezası approval workflow'unu

oluşturur.

---

# Demo Kullanıcıları

Development seed aşağıdaki kullanıcıları oluşturur:

| Kullanıcı | Rol |
|---|---|
| `operator@demo.local` | Operator |
| `manager@demo.local` | Manager |
| `finance@demo.local` | Finance |

Üç demo kullanıcı da local ortamınızda `Seed:DemoPassword` için tanımladığınız parolayı kullanır.

Repository içinde demo parolası bulunmaz.

---

# Demo Senaryosu

Uygulamanın ana case akışını doğrulamak için aşağıdaki senaryo kullanılabilir.

## 1. Operator

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

Bu senaryo Operator hesabı üzerinden örneklenmiştir.

TrafficFine oluşturma yalnız Operator'a özel değildir; authenticated internal kullanıcılar trafik cezası oluşturabilir.

`New` durumundaki kaydı edit ve submit etme yetkisi ise kaydın creator'ına aittir.

---

## 2. Manager

Çıkış yapıp `manager@demo.local` ile giriş yapın.

İlgili trafik cezasını açın ve `Onayla` seçin.

Manager kullanıcısı ilgili kaydın creator'ı olmamalıdır.

Beklenen:

```text
Onayda · Yönetici Onayı
-> Onayda · Finans Onayı
```

---

## 3. Finance

Çıkış yapıp `finance@demo.local` ile giriş yapın.

İlgili trafik cezasını açın ve `Onayla` seçin.

Finance kullanıcısı ilgili kaydın creator'ı olmamalıdır.

Beklenen:

```text
Onayda · Finans Onayı
-> Tamamlandı
```

Detay sayfasındaki workflow stepper'da iki approval step'i de tamamlanmış görünür.

ApprovalHistory üzerinde:

```text
Submitted
Approved - Yönetici Onayı
Approved - Finans Onayı
```

işlemleri görülür.

---

## Reject Senaryosu

Manager veya Finance kendi aktif approval step'inde `Reddet` aksiyonunu kullanabilir.

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

`Rejected` P0 kapsamında terminal state'tir.

---

# Testler

Business katmanında **20 adet odaklı unit test** bulunmaktadır.

Final test suite:

```text
Toplam   : 20
Başarılı : 20
Başarısız: 0
Atlandı  : 0
```

## Test Edilen Davranışlar

1. `SubmitAsync_NewFine_MovesToFirstDatabaseDefinedStep`
   - New kayıt ilk DB-defined workflow step'ine ilerler.

2. `ApproveAsync_WrongRole_ReturnsForbidden`
   - Yanlış role sahip kullanıcı approval yapamaz.

3. `ApproveAsync_CreatorWithMatchingRole_ReturnsForbidden`
   - Creator'ın rolü eşleşse bile kendi kaydını approve etmesi engellenir.

4. `RejectAsync_CreatorWithMatchingRole_ReturnsForbidden`
   - Creator'ın kendi kaydını reject etmesi engellenir.

5. `ApproveAsync_CurrentStepApproved_MovesToNextDatabaseDefinedStep`
   - Approval bir sonraki DB-defined step'e ilerler.

6. `ApproveAsync_FinalStepApproved_CompletesTrafficFine`
   - Son workflow step'i onaylandığında kayıt `Completed` olur.

7. `RejectAsync_WithoutReason_ReturnsValidationError`
   - Ret nedeni boş bırakılamaz.

8. `RejectAsync_ValidReason_MovesFineToRejectedTerminalState`
   - Geçerli reject kaydı `Rejected` terminal state'ine taşır.

9. `ApproveAsync_CompletedFine_ReturnsInvalidState`
   - Completed kayıt tekrar approve edilemez.

10. `ApproveAsync_RejectedFine_ReturnsInvalidState`
    - Rejected kayıt tekrar approve edilemez.

11. `ApproveAsync_WritesPreviousNewAndStepSnapshotsToHistory`
    - ApprovalHistory previous/new state ve workflow step snapshot bilgilerini doğru saklar.

12. `CreateAsync_FutureFineDate_ReturnsValidationError`
    - Gelecek tarihli trafik cezası Business validation ile reddedilir.

13. `GetPendingApprovalsAsync_PassesCurrentUserRolesToRepository`
    - Bekleyen approval sorgusu mevcut kullanıcı rollerini repository katmanına aktarır.

14. `CreateAsync_ManagerUser_Succeeds`
    - Manager rolündeki authenticated kullanıcı trafik cezası oluşturabilir.

15. `CreateAsync_FinanceUser_Succeeds`
    - Finance rolündeki authenticated kullanıcı trafik cezası oluşturabilir.

16. `EditAsync_Creator_Succeeds`
    - Creator kendi `New` kaydını düzenleyebilir.

17. `EditAsync_NonCreator_ReturnsForbidden`
    - Başka kullanıcı creator'ın `New` kaydını düzenleyemez.

18. `SubmitAsync_NonCreator_ReturnsForbidden`
    - Başka kullanıcı creator'ın kaydını onaya gönderemez.

19. `ApproveAsync_CreatorWithMatchingFinanceRole_ReturnsForbidden`
    - Finance rolü eşleşen creator kendi kaydını approve edemez.

20. `RejectAsync_CreatorWithMatchingFinanceRole_ReturnsForbidden`
    - Finance rolü eşleşen creator kendi kaydını reject edemez.

Dinamik workflow testi development seed'inden farklı bir zincir de kullanır:

```text
Manager
  ->
Legal
  ->
Finance
```

Bu test yaklaşımı next-step algoritmasının:

```text
Manager -> Finance
```

şeklinde hard-coded olmadığını doğrular.

Testleri çalıştırmak için:

```powershell
dotnet test .\SarilarTrafficFine.slnx
```

---

# Teslim Doğrulaması

Final teslim öncesinde repository yalnız mevcut local working tree üzerinde değil, `origin/main` üzerinden sıfır klasöre clone edilerek de doğrulanmıştır.

Doğrulanan durum:

```text
.NET SDK             9.0.301
Target Framework     net9.0
EF Core              9.x
dotnet-ef             9.0.19
Restore               PASS
Build                 PASS
Tests                 20/20 PASS
Migration Drift       NONE
Fresh Clone           PASS
Working Tree          CLEAN
UI Regression Smoke   PASS
PDF Regression Smoke  PASS
```

Final fresh-clone doğrulamasında eski `.NET 10` referansları için yapılan scoped kontrolde eşleşme bulunmamıştır.

Bu doğrulama repository'nin temiz bir ortamdan tekrar üretilebilir olduğunu göstermek için yapılmıştır.

---

# Teknik Kararlar ve Trade-off'lar

## 1. Katmanlı Modular Monolith

Case ölçeğinde:

- CQRS,
- MediatR,
- microservice mimarisi

kullanılmadı.

Bunun yerine:

```text
Entities
Business
DataAccess
Web
```

ayrımı ile küçük, okunabilir ve açıklanabilir bir yapı tercih edildi.

Bu proje tam kapsamlı bir Clean Architecture iddiası taşımaz.

Case ölçeğine uygun bir layered modular monolith'tir.

---

## 2. Repository + Unit of Work

EF Core `DbContext` zaten repository ve unit-of-work benzeri davranışlar sağlar.

Buna rağmen bu projede:

- persistence sınırını Business katmanından ayırmak,
- test doubles kullanabilmek,
- veri erişim kontratlarını görünür tutmak

amacıyla küçük repository ve Unit of Work abstraction'ları kullanılmıştır.

Bu tercihin abstraction maliyeti olduğu bilinmektedir.

Generic repository API bilinçli olarak dar tutulmuştur.

Domain'e özel query gerektiğinde specific repository metotları kullanılır.

---

## 3. DB-Driven Approval Workflow

Approval state'leri:

```text
PendingManagerApproval
PendingFinanceApproval
```

gibi ayrı enum değerleriyle hard-code edilmemiştir.

Bunun yerine:

```text
Status = InApproval
+
CurrentApprovalStepId
```

yaklaşımı tercih edilmiştir.

Step sırası ve gerekli rol veritabanındaki workflow tanımından okunur.

Bu tasarım case'in approval mekanizmasının teknik tasarımını geliştiriciye bırakan alanında seçilmiş bir implementation kararıdır.

---

## 4. Role-Based Approver Model

Workflow tanımına belirli kullanıcı ID'leri hard-code edilmemiştir.

Her step:

```text
RequiredRole
```

tanımlar.

Örneğin:

```text
1. Yönetici Onayı -> Manager
2. Finans Onayı   -> Finance
```

Aktif step'in istediği role sahip ve kaydın creator'ı olmayan kullanıcı ilgili approval işlemini gerçekleştirebilir.

Bu nedenle ayrı bir `ApproverFlow` isimli tablo zorunlu değildir.

`ApprovalWorkflow + ApprovalWorkflowStep` modeli aynı iş ihtiyacını daha açık şekilde karşılar.

---

## 5. ASP.NET Core Identity Kullanımı

Ayrı bir custom User domain tablosu oluşturmak yerine ASP.NET Core Identity'nin mevcut kullanıcı ve rol modeli kullanılmıştır.

```text
AspNetUsers
AspNetRoles
AspNetUserRoles
```

kullanıcı yönetimi için yeterli olduğundan aynı veriyi tekrar edecek ikinci bir `User` tablosu eklenmemiştir.

Domain'e özel ek kullanıcı alanları gerekseydi custom `ApplicationUser` ile Identity genişletilebilirdi.

P0 kapsamında buna ihtiyaç bulunmamaktadır.

---

## 6. Explicit Submit

Yeni trafik cezası oluşturulduğu anda otomatik olarak approval'a girmez.

```text
Create
-> New
-> explicit Submit
```

yaklaşımı tercih edilmiştir.

Bu sayede `New` gerçek bir düzenlenebilir draft state olarak anlam taşır.

---

## 7. Creator Ownership

P0 kararı:

```text
New edit   -> yalnız creator
New submit -> yalnız creator
```

Bu nedenle başka authenticated kullanıcılar başka kullanıcının draft kaydını değiştiremez veya approval'a gönderemez.

---

## 8. Separation of Duties

Bir kullanıcının workflow rolü uygun olsa dahi:

```text
Creator == CurrentUser
```

ise kendi oluşturduğu trafik cezasını approve veya reject etmesine izin verilmez.

Bu, kayıt oluşturma ve onaylama sorumluluklarını ayırmak için bilinçli bir P0 kuralıdır.

---

## 9. Append-Only Approval History

Approval geçmişi güncellenmez veya overwrite edilmez.

Her workflow transition yeni bir history satırı üretir.

Normal draft edit işlemleri `ApprovalHistory` kapsamına dahil değildir.

Genel veri değişikliklerinin takibi ayrı bir Audit Log sorumluluğudur.

---

## 10. Terminal Rejected

P0 kapsamında `Rejected` terminal kabul edilmiştir.

Bu, case için bilinçli ve sade bir state-machine kararıdır.

Gerçek production senaryosunda:

```text
ReturnForRevision
vs
FinalReject
```

gibi ayrımlar değerlendirilebilir.

Ancak bu ek lifecycle case tesliminin P0 kapsamına dahil edilmemiştir.

---

## 11. Completed Immutable Davranışı

P0 kapsamında `Completed` kayıt yeni approval işlemine girmez ve normal draft edit akışına geri dönmez.

Production ihtiyacında kontrollü revision/version mekanizması ayrıca tasarlanabilir.

---

## 12. Optimistic Concurrency

`TrafficFine.RowVersion` kullanılarak SQL Server optimistic concurrency yaklaşımı uygulanmıştır.

Amaç aynı kaydın eş zamanlı işlemler sonucunda sessizce ezilmesini engellemektir.

---

## 13. No Automatic Migration on Startup

Uygulama startup sırasında migration çalıştırmaz.

Database schema değişiklikleri açık EF migration komutlarıyla uygulanır.

Bu, schema mutation işlemini uygulamanın normal startup davranışından ayırır.

---

## 14. Secret Management

Development SQL connection string'i ve demo kullanıcı parolası source control dışında tutulur.

Local development için .NET User Secrets kullanılır.

Repository içinde gerçek parola bulunmaz.

---

## 15. PDF Sorumluluğu Web Katmanında

QuestPDF bağımlılığı yalnız Web katmanında tutulmuştur.

Business, DataAccess ve Entities katmanlarının PDF renderer'a bağımlı olması engellenmiştir.

---

# Bilinçli Olarak Kapsam Dışında Bırakılanlar

Aşağıdaki özellikler mevcut case'in P0 çözümü için gerekli görülmemiştir:

- Kullanıcı kayıt / self-service account yönetimi
- Workflow definition admin UI
- Workflow versioning
- Rejected kayıt için resubmit / revision loop
- Genel amaçlı audit log
- Trafik ihlal / ceza türü master kataloğu
- Trafik otoritesiyle dış itiraz/dispute entegrasyonu
- Dashboard / analytics
- Notification sistemi
- Approval delegation
- Multi-approver / quorum workflow
- Docker
- Redis
- RabbitMQ
- Elasticsearch
- CQRS / MediatR
- Microservice mimarisi

Bu kararların amacı daha fazla teknoloji göstermek yerine case'in ana problemini küçük, test edilebilir ve sürdürülebilir bir çözümle tamamlamaktır.

---

# PDF Çıktısı ve QuestPDF Lisans Notu

Trafik cezası detay ekranından kayıtların PDF çıktısı alınabilir.

PDF çıktısında aşağıdaki bilgiler yer alır:

- Trafik cezası bilgileri
- Araç bilgileri
- Güncel kayıt durumu
- Dinamik onay akışı
- Onay geçmişi
- İşlemi yapan kullanıcılar
- Ret nedeni / açıklamalar

PDF üretimi Web katmanında:

```text
QuestPDF 2026.7.3
```

kullanılarak gerçekleştirilmiştir.

Business, DataAccess ve Entities katmanları PDF kütüphanesine bağımlı değildir.

Bu case çalışmasında QuestPDF şu şekilde yapılandırılmıştır:

```csharp
QuestPDF.Settings.License = LicenseType.Community;
```

Bu ayar case / bireysel değerlendirme bağlamında kullanılmıştır.

QuestPDF'in lisans koşulları ayrı bir ürün lisansıdır. Uygulamanın gerçek production veya ticari kullanımında ilgili kurumun güncel QuestPDF lisans uygunluğunu ayrıca değerlendirmesi gerekir.

---

# Son Not

Projenin ana odağı:

```text
doğru state transition
+
rol bazlı yetkilendirme
+
creator ownership
+
separation of duties
+
DB-driven approval workflow
+
izlenebilir approval history
+
optimistic concurrency
+
temiz katman sınırları
+
test edilebilir business kuralları
+
tekrar üretilebilir local kurulum
```

olarak belirlenmiştir.

Case'in P0 gereksinimleri tamamlanmış; proje .NET 9 üzerinde build, test, migration drift, fresh-clone ve temel UI/PDF regression kontrollerinden geçirilmiştir.
