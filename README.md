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
