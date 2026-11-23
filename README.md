# SENTINEL

**SENTINEL**, .NET 10 ve PostgreSQL ile geliştirilen, REST API üzerinden işlerin (job) yaşam döngüsünü yöneten bir backend projesidir.

Proje; katmanlı yapı, iş kurallarının servis katmanında uygulanması, Entity Framework Core ile kalıcı veri saklama ve otomatik testler üzerine kuruludur.

> **Mevcut sürüm:** `0.3.0`  
> **Durum:** Geliştirme aşamasında. Şu anda iş kayıtlarının yönetimi ve durum geçişleri desteklenmektedir; arka planda işleri otomatik çalıştıran bir worker henüz bulunmamaktadır.

## Kullanılan teknolojiler

- C# / .NET 10
- ASP.NET Core Minimal API
- PostgreSQL 17
- Entity Framework Core 10 ve Npgsql
- Docker Compose
- Birim ve entegrasyon testleri

## Proje yapısı

```text
Sentinel/
├── src/
│   ├── Sentinel.Api/             # HTTP API ve bağımlılık kayıtları
│   ├── Sentinel.Core/            # İş modeli, arayüzler ve iş kuralları
│   └── Sentinel.Infrastructure/  # PostgreSQL, EF Core ve repository uygulamaları
├── tests/
│   ├── Sentinel.UnitTests/
│   └── Sentinel.IntegrationTests/
├── compose.yaml
├── dotnet-tools.json
└── Sentinel.slnx
```

## İş yaşam döngüsü

Yeni oluşturulan bir iş `Pending` durumundadır. Desteklenen geçişler:

```text
Pending ──start──> Running ──complete──> Completed
   │
   └────cancel───────────────────────> Cancelled
```

Geçersiz durum geçişleri API tarafından `409 Conflict` yanıtıyla reddedilir.

## API uçları

| Metot | Adres | Açıklama |
| --- | --- | --- |
| `GET` | `/` | Uygulama adı, sürümü ve çalışma durumu |
| `POST` | `/api/jobs` | Yeni iş oluşturma |
| `GET` | `/api/jobs` | İşleri listeleme |
| `GET` | `/api/jobs/{id}` | Kimliğe göre iş sorgulama |
| `POST` | `/api/jobs/{id}/start` | Bekleyen işi başlatma |
| `POST` | `/api/jobs/{id}/complete` | Çalışan işi tamamlama |
| `POST` | `/api/jobs/{id}/cancel` | Bekleyen işi iptal etme |

Yeni iş oluşturma isteği örneği:

```http
POST /api/jobs
Content-Type: application/json

{
  "name": "Örnek iş"
}
```

Başarılı oluşturma `201 Created`, bulunamayan iş `404 Not Found`, geçersiz durum geçişi `409 Conflict` yanıtı üretir. Boş iş adı `400 Bad Request` ile reddedilir.

## Yerel kurulum (Windows PowerShell)

### Gereksinimler

- .NET 10 SDK
- Docker Desktop (Docker Compose desteğiyle)
- Git

Depoyu klonlayın ve proje dizinine geçin:

```powershell
git clone https://github.com/sdogukan603/Sentinel.git
cd Sentinel
```

**1. Yerel geliştirme parolasını belirleyin.** Aşağıdaki değer yalnızca örnektir; kendi yerel parolanızı kullanın.

```powershell
$env:SENTINEL_POSTGRES_PASSWORD = "yerel-gelistirme-parolaniz"
```

**2. PostgreSQL'i başlatın.**

```powershell
docker compose up -d
docker compose ps
```

**3. Uygulama bağlantısını tanımlayın.** Bağlantı dizesindeki parola, birinci adımda belirlediğiniz değerle aynı olmalıdır.

```powershell
$env:SENTINEL_DB_CONNECTION = "Host=127.0.0.1;Port=5432;Database=sentinel;Username=sentinel;Password=$env:SENTINEL_POSTGRES_PASSWORD"
```

**4. EF Core aracını yükleyin ve migration'ları uygulayın.**

```powershell
dotnet tool restore
dotnet ef database update --project src/Sentinel.Infrastructure --startup-project src/Sentinel.Api
```

**5. API'yi başlatın.**

```powershell
dotnet run --project src/Sentinel.Api
```

API'nin dinlediği adresi terminal çıktısından öğrenebilirsiniz. Örneğin kök adrese `GET /` isteği göndererek uygulama durumunu kontrol edebilirsiniz.

> **Not:** Bu örnek PowerShell oturumu için geçerlidir. Yeni bir terminal açarsanız ortam değişkenlerini yeniden tanımlamanız gerekebilir. Daha önce oluşturulmuş Docker veri hacminde PostgreSQL parolasını ortam değişkeniyle değiştirmek, mevcut veritabanı kullanıcısının parolasını otomatik değiştirmez.

## Testler

Projede birim ve PostgreSQL entegrasyon testleri bulunur. Geliştirme sırasında **40 test başarıyla çalıştırılmıştır**. Bu sayı, gelecekteki değişiklikler için sürekli başarı garantisi değildir.

Birim testlerini çalıştırmak için:

```powershell
dotnet test tests/Sentinel.UnitTests/Sentinel.UnitTests.csproj
```

PostgreSQL entegrasyon testleri için ayrı bir `sentinel_test` veritabanı ve ona işaret eden `SENTINEL_DB_CONNECTION` değişkeni gerekir. Test altyapısı, bağlantı dizesindeki veritabanı adının `sentinel_test` olmasını zorunlu kılar.

**Uyarı:** Entegrasyon testlerini üretim veritabanına karşı çalıştırmayın. Test veritabanını ayrıca oluşturup migration'ları bu veritabanına uygulayın.

## Geliştirme planı

- [x] İş modeli ve durum geçişleri
- [x] REST API uçları
- [x] PostgreSQL kalıcılığı ve EF Core migration'ları
- [x] Birim ve entegrasyon testleri
- [ ] Arka plan işleyicisi (Worker Engine)
- [ ] İş kuyruğu ve yürütme mekanizması
- [ ] Gözlemlenebilirlik ve operasyonel iyileştirmeler

## Lisans

Bu depoda henüz bir lisans dosyası bulunmamaktadır. Kaynak kodun herkese açık olması, otomatik olarak yeniden kullanım veya dağıtım izni vermez.
