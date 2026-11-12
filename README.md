# I-Rieltor.uz-backend

ASP.NET Core (.NET 10) API: PostgreSQL 17, Redis (faqat kesh), E-IMZO, Nginx ortida.

## Ishga tushirish

**Lokal (`dotnet run`):** `.env.example`ni `.env`ga nusxalang va qiymatlarni to'ldiring. PostgreSQL va Redis kerak.

```bash
dotnet run --project WebApi
```

Ishga tushishda migratsiyalar qo'llanadi (`Database__MigrateOnStartup=false` bilan o'chiriladi) va SUPER_ADMIN yaratiladi.

**Docker compose:** `docker-compose.yml` boshidagi izohga qarang.

```bash
cp .env.example .env    # qiymatlarni to'ldiring
chmod 600 .env
docker compose up -d --build
```

Faqat Nginx tashqariga ochiq. API sog'lom (`/health/ready`) bo'lmaguncha Nginx ishga tushmaydi.

## HTTPS

TLS Nginx'da tugaydi, sertifikat Let's Encrypt'dan olinadi (`certbot` servisi).

1. Domenlarning DNS A yozuvlari shu serverga qarasin, 80 va 443 portlar ochiq bo'lsin.
2. `.env`da: `SERVER_NAME=auksion.i-rieltor.uz` (auksion sayti; API ham shu domenda `/api` ostida), ixtiyoriy
   `API_SERVER_NAME=api.i-rieltor.uz`, `ADMIN_SERVER_NAME=admin.i-rieltor.uz` va `PRESENTATION_SERVER_NAME=i-rieltor.uz`,
   hamda `LETSENCRYPT_EMAIL=...`.
   Hamma nomlarni bitta sertifikat qamraydi. Nom keyinroq qo'shilsa: `docker compose restart certbot`.
3. `docker compose up -d`. Sertifikat bir-ikki daqiqada olinadi, Nginx o'zi HTTPS'ga o'tadi:
   - 80-port endi faqat sertifikat tekshiruviga javob beradi, qolgan hamma so'rovni 301 bilan HTTPS'ga yo'naltiradi;
   - HSTS yoqiladi (1 yil).
4. Yangilash avtomatik: certbot har 12 soatda tekshiradi, Nginx yangi sertifikatni bir daqiqa ichida qayta yuklaydi.

Birinchi urinishda `LETSENCRYPT_STAGING=1` qo'yish mumkin. Bunda Let's Encrypt'ning test sertifikati olinadi: brauzer unga ishonmaydi, lekin limitlar yo'q. Ishlaganini ko'rgach, uni olib tashlang va staging sertifikatni o'chiring: `docker compose run --rm --entrypoint certbot certbot delete --cert-name auksion.i-rieltor.uz` (`SERVER_NAME` qiymati). Keyin `docker compose up -d`.

`SERVER_NAME` yoki `LETSENCRYPT_EMAIL` bo'sh bo'lsa, Nginx oddiy HTTP'da qoladi. Bu dasturchi kompyuteri uchun. Holatni ko'rish: `docker compose logs nginx certbot`.

`.env`dagi `FileStorage__PublicBaseUrl` va frontend manzillari `https://` bilan bo'lsin.

## Zaxira nusxa va tiklash

`backup` servisi har kuni `BACKUP_AT_UTC` vaqtida (standart 22:00 UTC, ya'ni Toshkentda 03:00) va har ishga tushganda zaxira nusxa oladi. Nusxalar `BACKUP_DIR` papkasiga (standart `./backups`) yoziladi:
- `db/irieltor-<vaqt>.dump`: bazaning `pg_dump -Fc` nusxasi; yozilgach bir marta o'qib tekshiriladi;
- `files/files-<vaqt>.tar.gz`: yuklangan fayllar (`uploads/` va `deleted/`);
- `BACKUP_KEEP_DAYS` (standart 14) kundan eski nusxalar o'chiriladi.

Oxirgi to'liq nusxa 26 soatdan eski bo'lsa, `docker compose ps` servisni `unhealthy` deb ko'rsatadi. Qo'lda nusxa olish: `docker compose exec backup sh /opt/backup/backup.sh now`.

**Muhim:** nusxa shu serverning o'zida turadi, server yo'qolsa nusxa ham yo'qoladi. `BACKUP_DIR`ni boshqa joyga ham ko'chiring (rsync yoki restic bilan boshqa serverga).

**Tiklash:**

```bash
docker compose stop api
# baza: --clean bilan mavjud jadvallar nusxadagi holatga almashtiriladi
docker compose exec -T postgres pg_restore -U irieltor -d IRieltor --clean --if-exists --no-owner < backups/db/irieltor-YYYYMMDD-HHMMSS.dump
# fayllar (arxiv faqat root uchun o'qiladi; egalar arxivdagidek tiklanadi)
docker compose run --rm --no-deps --user root -v ./backups:/restore:ro --entrypoint sh api -c \
  "tar -xzf /restore/files/files-YYYYMMDD-HHMMSS.tar.gz -C /data/files"
docker compose start api
```

Tiklashni oyiga bir marta alohida bazada sinab ko'ring:

```bash
docker compose exec postgres createdb -U irieltor restore_test
docker compose exec -T postgres pg_restore -U irieltor -d restore_test --no-owner < backups/db/<fayl>.dump
docker compose exec postgres psql -U irieltor -d restore_test -c 'select count(*) from "Users"'
docker compose exec postgres dropdb -U irieltor restore_test
```

## Monitoring

Eng sodda variant tanlangan:
- **Xatolar:** `Alerts__Email` berilsa, API Error darajasidagi har xatoni shu manzilga mavjud SMTP orqali yuboradi. 15 daqiqada ko'pi bilan bitta xat, bir xatda 20 tagacha xato, har birida `CorrelationId` bilan. Interval `Alerts__MinIntervalMinutes` bilan o'zgaradi.
- **Ishlayaptimi:** tekin tashqi uptime xizmati, masalan UptimeRobot. Unda `https://api.i-rieltor.uz/health/ready` uchun 5 daqiqalik HTTP monitor oching va emailga xabar sozlang. Server o'chsa yoki baza ishlamasa (503), xabar keladi.

## Testlar

```bash
dotnet test
```

- `Tests/IRieltor.Tests/Unit`: validatsiya, filtrlar, vaqt, email tasdiqlash tokeni.
- `Tests/IRieltor.Tests/Integration`: butun API real PostgreSQL va Redis bilan (Testcontainers). Docker ishlab turishi shart.

Testlar Microsoft.Testing.Platform orqali ishlaydi (`global.json`). Bitta guruhni tanlash: `dotnet test -- --filter-namespace IRieltor.Tests.Unit`.

CI (`.github/workflows/ci.yml`) har push'da quyidagilarni bajaradi:
- `--locked-mode` bilan restore;
- build va testlar;
- model va migratsiyalar mosligini tekshirish (`has-pending-model-changes`);
- Docker image build.

## Paketlar

`Directory.Build.props` har loyihada `packages.lock.json` yaratadi. Paket qo'shsangiz yoki yangilasangiz, o'zgargan lock faylni ham commit qiling. Aks holda CI va Docker build to'xtaydi.

## Loglar

Serilog ishlatiladi, darajalar `appsettings.json`dagi `Serilog` bo'limida.
- Development'da log oddiy matn, SQL so'rovlar ham ko'rinadi.
- Boshqa muhitlarda har qator alohida JSON (`docker logs`, Loki yoki ELK uchun).
- Har so'rov uchun bitta qator yoziladi: metod, yo'l, status va vaqt.

Har qatorda `CorrelationId` bor. U javobdagi `X-Request-ID` header'i va Nginx access log'idagi `id=` bilan bir xil. Foydalanuvchi xato haqida xabar berganda, shu id bo'yicha log qidiriladi.

## Health check

| Yo'l | Tekshiradi | Javob |
|---|---|---|
| `/health/live` | jarayon ishlayaptimi | `Healthy` |
| `/health/ready` | baza va Redis | baza yo'q → 503 `Unhealthy`; Redis yo'q → 200 `Degraded` (API bazadan ishlayveradi) |

Docker healthcheck: `dotnet WebApi.dll --healthcheck`. Runtime image'da curl yo'q, shuning uchun ilova o'zini o'zi tekshiradi. U `AllowedHosts` ro'yxatidagi birinchi host nomidan foydalanadi.

## Vaqt

- Bazada va API'da hamma vaqt UTC (`timestamptz`). Javoblarda vaqt `Z` bilan qaytadi: `2026-10-21T04:00:00Z`.
- Kiritilgan vaqtda `Z` yoki offset bo'lishi shart: `2026-10-21T09:00:00+05:00`. Offsetsiz qiymat (`2026-10-21T09:00:00`) 400 bilan rad etiladi, chunki u Toshkent vaqtimi yoki UTC'mi, aniq emas. Bu JSON uchun ham, form/query uchun ham amal qiladi (`WebApi/Converters/UtcDateTime.cs`).
- Ro'yxat filtrlarida (`Filters[createdAt.greaterthan]=2026-10-01`) offsetsiz sana UTC deb olinadi.
- Asia/Tashkent'ga o'girish frontendning ishi.
- Email tasdiqlash xatlarining kunlik limiti 00:00 UTC'da, ya'ni Toshkent vaqti bilan 05:00 da yangilanadi.

## Ma'lumotlarni saqlash muddati

`DataRetentionService` kuniga bir marta ishlaydi. Muddatlar `Retention` bo'limida, kunlarda; `0` qadamni o'chiradi.

| Sozlama | Standart | Nima qiladi |
|---|---|---|
| `RefreshTokenDays` | 30 | muddati o'tgan yoki bekor qilingan refresh tokenlarni o'chiradi |
| `DeletedUserDays` | 365 | o'chirilgan foydalanuvchining shaxsiy ma'lumotlarini anonimlashtiradi (ism, login, email, telefon, manzil, PINFL/INN, parol) |
| `DeletedFileDays` | 90 | `deleted/` papkasidagi fayllarni o'chiradi |

Muddatlarni yurist bilan kelishib oling (ZRU-547).
