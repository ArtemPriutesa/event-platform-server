# Event Platform

Мікросервісна платформа для організаторів подій і відвідувачів. Event Catalog керує подіями, Ticketing — квитками й бронюваннями, Reviews — відгуками. Payments, Notifications і Discussions описані як цільові контексти, але їх окремі API поки не реалізовані.

## Зміст

- [Архітектура та дані](#архітектура-та-дані)
- [Швидкий запуск](#швидкий-запуск)
- [Ініціалізація баз даних](#ініціалізація-баз-даних)
- [Перевірка стану та тести](#перевірка-стану-та-тести)
- [Event Catalog API](#event-catalog-api)
- [Ticketing API](#ticketing-api)
- [Reviews API](#reviews-api)
- [Конфігурація та відновлення](#конфігурація-та-відновлення)
- [Структура репозиторію та ERD](#структура-репозиторію-та-erd)

## Архітектура та дані

| Сервіс / контекст | Агрегати | База даних та реалізація |
|---|---|---|
| Event Catalog | `Organizer`, `Category`, `Event` із `EventVenue` та `EventTag` | PostgreSQL; Domain, BLL, DAL, API; DAL використовує ADO.NET/Dapper, спільну сесію й Unit of Work |
| Ticketing | `TicketType`, `Reservation` із `ReservationDetails` і `ReservationItems`, `Ticket` | PostgreSQL; тришаровий ASP.NET Core та EF Core Code First |
| Reviews | `Review` із вкладеними автором, коментарями й фото | MongoDB; Api, Application, Domain, Infrastructure, MediatR |
| Цільові контексти | `Payment`, `Notification`, `Discussion` | Описані в моделі предметної області; реалізацій цих API немає |

Кожна база має одного власника даних. Спільних таблиць і SQL foreign keys між базами немає. UUID `PublicId` є міжсервісним ідентифікатором, а не зовнішнім ключем БД. Для інтеграційних подій потрібні ідемпотентні обробники; transactional outbox рекомендований для надійної доставки, але брокер повідомлень у рішенні не налаштований.

### Правила копіювання даних

| Дані | Джерело істини та копії | Правило |
|---|---|---|
| `Event.PublicId` | Event Catalog; посилання в Ticketing і Reviews | UUID незмінний; використовується без FK між базами. |
| Назва події | Event Catalog; знімок у `TicketType.EventTitleSnapshot`, бронюванні й відгуку | Назву в типі квитка можна оновлювати після інтеграційної події; історичні знімки не переписуються. |
| `TicketType.Name`, `TicketType.Price` | Ticketing; знімки в `ReservationItems` | Фіксуються під час бронювання й не змінюються після редагування типу квитка. |
| `Reservation.PublicId` і сума | Ticketing; запланована копія в Payments | Платіж зберігає знімок суми підтвердженого бронювання; статус платежу не є джерелом статусу бронювання. Payments API ще немає. |
| `Ticket.PublicId` | Ticketing; посилання у Reviews | Reviews зберігає UUID. Перевірка квитка через Ticketing потребує інтеграції. |
| `UserPublicId`, `author.displayName` | Зовнішній сервіс профілів; посилання/знімок у Ticketing і Reviews | Ім’я автора в опублікованому відгуку зберігається як історичний знімок. |
| Назва події у сповіщенні | Event Catalog; майбутній знімок у Notifications | Зберігати назву на момент надсилання; Notifications API ще немає. |

SQL-таблиці використовують локальні `BIGINT GENERATED ALWAYS AS IDENTITY` ключі та UUID для міжсервісного обміну. У SQL-сутностях також є поля аудиту, м’якого видалення й `version`; умовне оновлення має перевіряти очікувану версію та кількість змінених рядків. MongoDB використовує ObjectId для `_id` відгуку й UUID-рядки як зовнішні посилання.

## Швидкий запуск

Потрібні .NET SDK 9 або новіший і Docker Desktop із запущеним Docker Engine. Перед першим запуском згенеруйте паролі та збережіть їх у user secrets AppHost (виконати один раз):

```powershell
$postgresPassword = [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
$mongoPassword = [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
dotnet user-secrets set Parameters:postgres-password $postgresPassword --project Platform.AppHost\Platform.AppHost.csproj
dotnet user-secrets set Parameters:mongo-password $mongoPassword --project Platform.AppHost\Platform.AppHost.csproj
Remove-Variable postgresPassword, mongoPassword
```

Потім із кореня репозиторію запустіть:

```powershell
dotnet run --project Platform.AppHost\Platform.AppHost.csproj --launch-profile http
```

Aspire AppHost створює PostgreSQL, MongoDB і Redis з постійними volumes, передає connection strings API та очікує готовності ресурсів. Для Лабораторної №6 використовуються volumes `event-platform-laba6-postgres-data`, `event-platform-laba6-mongo-data` і `event-platform-laba6-redis-data`; volumes попередніх лабораторних не змінюються. Паролі стабільно зберігаються в user secrets AppHost; не змінюйте їх, доки використовуються відповідні volumes. Ticketing застосовує EF Core міграції при запуску. Для Event Catalog треба окремо застосувати SQL-скрипти з розділу [ініціалізації баз](#ініціалізація-баз-даних). URL API, журнали та фактичні порти доступні в Aspire Dashboard.

## Ініціалізація баз даних

### Event Catalog: перше створення схеми

Після запуску AppHost візьміть порт і параметри PostgreSQL з Aspire Dashboard. З кореня репозиторію виконайте:

```powershell
$connection = "Host=localhost;Port=<порт>;Database=event_catalog;Username=<користувач>;Password=<пароль>"
psql $connection -v ON_ERROR_STOP=1 -f db\p1\schema.sql
psql $connection -v ON_ERROR_STOP=1 -f db\p1\procedures.sql
psql $connection -v ON_ERROR_STOP=1 -f db\p1\seed.sql
```

`schema.sql` не призначений для повторного застосування. `db\p1\verify.sql` містить необов’язкову перевірку переходу статусу події. Для PostgreSQL, запущеного окремо від Aspire, можна створити базу через `createdb event_catalog` і використовувати `psql -d event_catalog`.

### Ticketing

AppHost створює базу `ticketing`, а Ticketing API застосовує EF Core міграції при старті. Не застосовуйте `db\p2\schema.sql` до цієї бази перед міграціями. `db\p2\seed.sql` — приклад окремого seed для самостійно створеної бази. У Development після міграцій додається ідемпотентний демонстраційний тип квитка.

### MongoDB / Reviews

Під час запуску Reviews створює індекси й додає два демонстраційні відгуки, лише якщо колекція порожня. Для окремої ручної ініціалізації з кореня репозиторію:

```powershell
mongosh "mongodb://localhost:27017/event_platform_payments" --file db\p3\collections.js
mongosh "mongodb://localhost:27017/event_platform_payments" --file db\p3\seed.js
```

Seed-скрипти MongoDB можна запускати повторно. Приклади документів наведені в `db\p3\examples.json`.

## Перевірка стану та тести

Знайдіть URL API в Aspire Dashboard і викличте `GET /health`. Успішна відповідь має `status: healthy` і `database: connected`.

| Endpoint | Перевірка |
|---|---|
| Event Catalog `GET /health` | PostgreSQL-з’єднання та запит `SELECT 1` |
| Ticketing `GET /health` | Доступність PostgreSQL через EF Core |
| Reviews `GET /health` | MongoDB-команда `ping` |

Swagger кожного API доступний за `/swagger` у середовищі Development.

Для локальної автоматизованої перевірки:

```powershell
dotnet build Platform.sln
dotnet test Platform.sln
```

У рішенні є тестовий проєкт `Services\Ticketing\Ticketing.Tests`. Перевірка API з базами потребує запущених контейнерів і підготовленої схеми Event Catalog.

## Event Catalog API

Сервіс керує подіями, категоріями, організаторами й тегами. Ідентифікатор `{id}` у маршрутах — локальний числовий ID сервісу; між сервісами використовується `PublicId`.

| Метод і шлях | Призначення |
|---|---|
| `GET /api/events?search=&status=&categoryId=` | Пошук і фільтрація подій |
| `GET /api/events/{id}` | Деталі події, місце проведення та теги |
| `POST /api/events` | Створити чернетку разом із місцем проведення |
| `PUT /api/events/{id}` | Оновити чернетку |
| `POST /api/events/{id}/publish` | Опублікувати подію |
| `POST /api/events/{id}/finish` | Завершити подію |
| `POST /api/events/{id}/cancel` | Скасувати подію |
| `DELETE /api/events/{id}` | М’яко видалити подію |
| `PUT` / `DELETE /api/events/{id}/tags/{tagId}` | Додати або прибрати тег |
| `GET /api/categories`, `/api/organizers`, `/api/tags` | Отримати довідники |

Дозволені переходи статусу перевіряються сервісом. SQL-схема, процедури та seed розміщені в `db\p1`.

Приклад тіла `POST /api/events`:

```json
{
  "organizerId": 1,
  "categoryId": 1,
  "title": "Demo event",
  "description": "Event description",
  "startsAt": "2099-06-15T18:00:00+03:00",
  "endsAt": "2099-06-15T21:00:00+03:00",
  "venue": {
    "name": "Central Hall",
    "address": "1 Main Street",
    "city": "Kyiv",
    "country": "Ukraine",
    "capacity": 500
  }
}
```

## Ticketing API

Ticketing керує типами квитків, бронюваннями й виданими квитками. Бронювання утримує кількість квитків у транзакції на 15 хвилин; підтвердження випускає квитки, скасування звільняє резерв.

| Метод і шлях | Призначення |
|---|---|
| `GET /api/ticket-types?page=1&pageSize=10&search=standard&sortBy=price&sortDir=asc` | Фільтрація та пагінація типів |
| `GET /api/ticket-types/{id}` | Отримати тип квитка |
| `POST /api/ticket-types` | Створити тип квитка |
| `PUT /api/ticket-types/{id}/price` | Змінити ціну |
| `POST /api/reservations` | Створити бронювання |
| `GET /api/reservations/{id}` | Отримати бронювання з eager loading |
| `GET /api/reservations/{id}/explicit` | Отримати бронювання з explicit loading |
| `GET /api/reservations/{id}/ticket-types` | LINQ-запит через `reservation_items` |
| `POST /api/reservations/{id}/confirm` | Підтвердити бронювання та випустити квитки |
| `POST /api/reservations/{id}/cancel` | Скасувати бронювання й звільнити резерв |
| `POST /api/tickets/{publicId}/use` | Позначити квиток використаним |

POST-запити створення повертають `201 Created`; помилка валідації — `400`, відсутній ресурс — `404`, конфлікт статусу чи залишку — `409`. Для створення ідемпотентного SQL-скрипту міграцій:

```powershell
dotnet ef migrations script --idempotent --project Services\Ticketing\Ticketing.Dal --startup-project Services\Ticketing\Ticketing.Api --output db\p2\migrations.sql
```

## Reviews API

Reviews використовує Clean Architecture: контролери передають команди й запити через MediatR, а Infrastructure працює з MongoDB. Відгук зберігає автора, фото й коментарі як вкладені дані.

| Метод і шлях | Призначення |
|---|---|
| `GET /api/reviews?eventPublicId=&search=&page=1&pageSize=20` | Список і пошук відгуків |
| `GET /api/reviews/{id}` | Отримати відгук за Mongo ObjectId |
| `POST /api/reviews` | Створити відгук |
| `PUT /api/reviews/{id}` | Оновити відгук із перевіркою `expectedVersion` у тілі |
| `DELETE /api/reviews/{id}?expectedVersion=1` | Видалити відгук із перевіркою версії |

Створення приймає `eventPublicId`, `eventTitleSnapshot`, `ticketPublicId`, `userPublicId`, `displayName`, необов’язкові `avatarUrl`/`authorEmail`, `rating` від 1 до 5, `text` і `photos`. Один користувач може залишити не більше одного відгуку на подію; квиток не може бути використаний для кількох відгуків. Перевірка квитка через Ticketing потребує міжсервісної інтеграції. Конфлікт конкурентної зміни повертає `409`; валідація — `400`, відсутність — `404`.

Для пошуку створюються індекси за подією/датою й текстом та унікальні індекси за парою подія-користувач і за квитком. Для AutoMapper 16 задайте ліцензійний ключ через `AutoMapper:LicenseKey` або `AUTOMAPPER_LICENSE_KEY`.

## Конфігурація та відновлення

- Не застосовуйте `db\p1\schema.sql` повторно до вже створеної схеми.
- Не застосовуйте `db\p2\schema.sql` перед EF Core міграціями.
- Для окремого запуску Event Catalog задайте `ConnectionStrings:EventCatalog`, Ticketing — `ConnectionStrings:Ticketing`, Reviews — `MongoDb:ConnectionString` і `MongoDb:DatabaseName`.
- Якщо PostgreSQL або MongoDB зі старого volume відхиляє пароль AppHost, використовуйте окремий названий volume для нової лабораторної або синхронізуйте пароль бази з параметром. Не видаляйте старі volumes: у них можуть бути потрібні дані.

## Структура репозиторію та ERD

- `Platform.AppHost` — Aspire AppHost.
- `Services\EventCatalog`, `Services\Ticketing`, `Services\Reviews` — сервіси.
- `Services\<Service>\<Service>.Api\Protos` — gRPC контракти серверів; `Platform.Aggregator\Protos` — клієнтські контракти.
- `Platform.ServiceDefaults\Caching` — спільний L1/L2 cache-aside та метрики кешу.
- `db\p1`, `db\p2`, `db\p3` — SQL/JavaScript схеми, seed і приклади документів.
- `docs\event-service-erd.drawio`, `docs\ticketing-service-erd.drawio`, `docs\payment-service-erd.drawio` — ERD-діаграми. Відкрийте їх у [diagrams.net](https://app.diagrams.net/). ERD Payments & Feedback показує цільову модель і не означає, що відповідні API вже реалізовані.

## Лабораторна №5: Aspire, Gateway та Aggregator

`Platform.AppHost` оркеструє PostgreSQL, MongoDB, Redis, три API, `Aggregator` і `ApiGateway`. Інфраструктурні ресурси запускаються перед API; Aggregator чекає на Orders і Reviews, Gateway — на всі сервіси. Gateway слухає фіксований порт `5000`.

### Запити через Gateway

| Метод і шлях | Призначення |
|---|---|
| `GET /api/catalog/events` | Проксі до Event Catalog (`/api/events`) |
| `GET /api/orders/reservations/{id}` | Проксі до Ticketing (`/api/reservations/{id}`) |
| `GET /api/reviews?eventPublicId=...` | Проксі до Reviews |
| `GET /api/aggregator/orders/{id}/full` | Бронювання, відповідні події та відгуки з частковою відповіддю |
| `GET /api/aggregator/grpc/{reservationId}/{eventId}/{eventPublicId}` | Паралельний запит до трьох gRPC сервісів із частковою відповіддю |

YARP використовує логічні імена Aspire для пошуку адрес сервісів. Gateway приймає або генерує `X-Correlation-Id`, повертає його у відповіді та передає далі. Трасування окремо зберігає W3C TraceId. Серверні логи містять метод, шлях, маршрут, статус, тривалість і IP клієнта; недоступний upstream повертає помилку Gateway і записується як попередження.

Aggregator паралельно отримує каталог і відгуки для подій у бронюванні. Недоступний сервіс додає елемент у `Warnings`, доступні дані повертаються зі статусом `200`. Поля з `null` не серіалізуються. Клієнтські HTTP виклики мають тайм-аут 4 секунди й автоматично передають correlation ID.

`Platform.ServiceDefaults` задає service discovery, спільний Redis distributed cache та in-memory кеш L1, OpenTelemetry для ASP.NET Core, HttpClient, Npgsql/SQL і MongoDB, а також компактні структуровані Serilog логи в консоль і Aspire Dashboard через OTLP. Cache-aside використовує 30-секундний L1 і 2-хвилинний Redis L2; зміни подій і квитків видаляють відповідні ключі після запису з журналюванням причини. Метрики `cache.hits` (з рівнем L1/L2) та `cache.misses` доступні через Meter `EventPlatform.Caching`. `/health` перевіряє доступність відповідної бази, `/alive` — liveness процесу.

Контракти `catalog.proto`, `ticketing.proto` і `reviews.proto` лежать у `Protos` відповідних API та згенеровані як серверні контракти; копії клієнтів розміщені в `Platform.Aggregator/Protos`. Кожен сервіс публікує відповідний gRPC adapter, а Aggregator реєструє типізовані клієнти через Aspire service discovery. Для перевірки інфраструктури кешу перегляньте Redis у Dashboard та повторіть `GET /api/ticket-types/{id}` або `GET /api/events/{id}`: другий запит має збільшити cache hit. Записи у відповідні API очищують кеш.

Для ручної перевірки запустіть AppHost командою з розділу «Швидкий запуск», переконайтеся в Dashboard, що всі ресурси `Running`, і викличте наведені вище маршрути через `http://localhost:5000`. Для трасування агрегованого запиту використовуйте `GET /api/aggregator/orders/{id}/full`; зупинка одного upstream має повертати доступну частину відповіді з попередженням.
