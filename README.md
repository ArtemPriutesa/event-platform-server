# Event Platform

Мікросервісна платформа для організаторів, відвідувачів і контролерів подій. Організатори публікують заходи, відвідувачі бронюють і оплачують квитки, а після події залишають відгуки; контролери перевіряють квитки під час входу.


## Контексти та бази даних

| Проєкт | Bounded context | Власні сутності | Персистентність |
|---|---|---|---|
| №1 | Event Catalog | Organizer, Category, Event, EventVenue, Tag, EventTag | PostgreSQL; Npgsql + процедурний доступ для ADO.NET/Dapper |
| №2 | Ticketing | TicketType, Reservation, ReservationDetails, ReservationItem, Ticket | PostgreSQL; схема для EF Core Code First / Fluent API |
| №3 | Payments & Feedback | payments, notifications, reviews, discussions | MongoDB; агрегати документів |

Кожна БД має окремого власника даних. Спільних таблиць і foreign key між базами немає. UUID `PublicId` є міжсервісним ідентифікатором; споживачі зберігають його як зовнішнє посилання, а не як FK. Опис агрегатів і узгодженості даних наведено в [документації меж контекстів](docs/bounded-contexts.md).

## Політика копіювання даних

| Поле | Де зберігається | Джерело істини | Політика узгодженості |
|---|---|---|---|
| `Event.PublicId`, `Event.Title` | Event Catalog; копія назви у Ticketing і Payments/Feedback | Event Catalog | `PublicId` незмінний. Поточну назву в `TicketTypes.EventTitleSnapshot` оновлювати подією `EventUpdated`; назви в бронюванні, платежі й відгуку — незмінні знімки на момент операції. |
| `EventPublicId` | Ticketing `TicketTypes` та `ReservationItems`, MongoDB `reviews`/`payments` | Event Catalog | Тільки посилання UUID без FK; споживачі отримують факт публікації події асинхронною інтеграційною подією. |
| `TicketType.Name`, `TicketType.Price` | Ticketing; `ReservationItems.TicketTypeNameSnapshot`, `UnitPrice` | Ticketing | Знімок назви й ціни фіксується при створенні бронювання і не змінюється після зміни тарифу. |
| `Reservation.PublicId` | Ticketing і MongoDB `payments` | Ticketing | UUID передається при створенні платежу; унікальний індекс у MongoDB запобігає кільком платіжним агрегатам для одного бронювання. Статус платежу не є джерелом статусу бронювання. |
| `ReservationItems.UnitPrice × Quantity`, валюта | MongoDB `payments.amount`, `payments.currency` | Ticketing | Сума платежу — незмінний знімок суми підтвердженого бронювання; повернення записується окремим статусом/часом у платіжному агрегаті. |
| `Ticket.PublicId` | Ticketing і MongoDB `reviews` | Ticketing | Відгук містить UUID придбаного квитка як зовнішнє посилання; Ticketing підтверджує його валідність перед прийняттям відгуку. |
| `UserPublicId` | Ticketing і MongoDB | Сервіс ідентифікації/акаунтів (поза цими трьома БД) | Зберігається як зовнішній UUID без FK; ім’я автора в документах відгуку/обговорення — знімок для стабільного відображення. |
| `author.displayName` | MongoDB `reviews.author`, `discussions.author` | Профіль користувача / організатора | Знімок імені на момент публікації; зміна профілю не переписує історичні відгуки й повідомлення. |
| Назва події у сповіщенні | MongoDB `notifications.event.title` | Event Catalog | Вбудований знімок на момент надсилання сповіщення; історичні сповіщення не переписуються. |

### Ідентифікатори, аудит та конкурентність

SQL-таблиці використовують `BIGINT GENERATED ALWAYS AS IDENTITY` для локальних первинних ключів: вони компактні й ефективні для локальних вставок та індексів. Події, бронювання й квитки додатково мають UUID `PublicId` для безпечного обміну ідентифікаторами між незалежними базами; MongoDB використовує стабільні string `_id` у seed-прикладах та UUID-рядки для зовнішніх посилань. SQL-сутності містять `created_at`, `created_by`, `updated_at`, `updated_by`, `is_deleted` і `version` для аудиту, м’якого видалення та оптимістичної конкурентності. `version` є прикладним токеном: оновлення має збільшувати його умовою `WHERE version = :expected_version` та перевіряти кількість змінених рядків.

## Розгортання локально

Потрібні PostgreSQL 14+ з `psql`/`createdb` і MongoDB з `mongosh`. Наведені команди — для PowerShell з кореня репозиторію. PostgreSQL-сервер має бути запущений, а поточний системний користувач — мати право створювати БД та підключатися до них. Налаштуйте пароль через стандартний механізм PostgreSQL (`.pgpass` або запит `psql`), а не зберігайте його в репозиторії.

### Короткий порядок запуску всіх БД

Виконуйте команди з кореня репозиторію саме в цьому порядку. Спочатку створюється Event Catalog, адже Ticketing використовує UUID демонстраційної події з першої БД.

```powershell
# 1. Event Catalog: створити схему, процедури та початкові дані
createdb event_catalog
psql -v ON_ERROR_STOP=1 -d event_catalog -f db\p1\schema.sql
psql -v ON_ERROR_STOP=1 -d event_catalog -f db\p1\procedures.sql
psql -v ON_ERROR_STOP=1 -d event_catalog -f db\p1\seed.sql

# Необов’язкова перевірка переходу статусу події
psql -v ON_ERROR_STOP=1 -d event_catalog -f db\p1\verify.sql

# 2. Ticketing
createdb ticketing
psql -v ON_ERROR_STOP=1 -d ticketing -f db\p2\schema.sql
psql -v ON_ERROR_STOP=1 -d ticketing -f db\p2\seed.sql

# 3. Payments & Feedback: спочатку колекції та індекси, потім документи
mongosh "mongodb://localhost:27017/event_platform_payments" --file db\p3\collections.js
mongosh "mongodb://localhost:27017/event_platform_payments" --file db\p3\seed.js
```

Не запускайте повторно `schema.sql` у вже створених БД: ці файли створюють таблиці й не призначені для повторного застосування. Seed-скрипти можна запускати повторно.

## Примітки

- Порядок запуску всіх трьох баз наведено вище. Seed-файли можна запускати повторно; `schema.sql` — ні.
- Event Catalog має процедури CRUD і перевірений перехід статусів; перевірка: `psql -v ON_ERROR_STOP=1 -d event_catalog -f db\p1\verify.sql`.
- Ticketing містить п’ять сутностей для EF Core; використовуйте провайдер `Npgsql.EntityFrameworkCore.PostgreSQL`.
- MongoDB має колекції `payments`, `notifications`, `reviews` і `discussions`. Приклади документів: [examples.json](db/p3/examples.json).
- ERD: [Event Catalog](docs/event-service-erd.drawio) · [Ticketing](docs/ticketing-service-erd.drawio) · [Payments & Feedback](docs/payment-service-erd.drawio). Відкрийте файли на [draw.io](https://app.diagrams.net/).
- Для перевірки індексів PostgreSQL виконайте `SELECT tablename, indexname, indexdef FROM pg_indexes WHERE schemaname = 'public';`. FK між базами відсутні.

## Лабораторна 2: ASP.NET Core / тришаровий сервіс Event Catalog

Єдиний сервіс рішення — Event Catalog, поділений на Domain, BLL, DAL та API. DAL надає generic repository contracts, ADO.NET і Dapper-репозиторії; усі scoped репозиторії в одному запиті використовують спільне з’єднання й транзакцію через Unit of Work. Для подій застосовано параметризований Dapper із базовим `BaseDapperRepository<T>`, а мапінг API DTO налаштовано профілем AutoMapper. Схема та процедури PostgreSQL знаходяться в `db\p1`.

### Aspire, база даних і конфігурація підключення

AppHost запускає PostgreSQL із persistent data volume та EventCatalog API; Aspire передає API connection string через `WithReference`. Потрібен запущений Docker. Після першого старту AppHost застосуйте таблиці, процедури й початкові дані до БД (порт і параметри підключення подивіться в Aspire Dashboard):

```powershell
psql "Host=localhost;Port=<порт з Aspire Dashboard>;Database=event_catalog;Username=<користувач>;Password=<пароль>" -v ON_ERROR_STOP=1 -f db\p1\schema.sql
psql "Host=localhost;Port=<порт з Aspire Dashboard>;Database=event_catalog;Username=<користувач>;Password=<пароль>" -v ON_ERROR_STOP=1 -f db\p1\procedures.sql
psql "Host=localhost;Port=<порт з Aspire Dashboard>;Database=event_catalog;Username=<користувач>;Password=<пароль>" -v ON_ERROR_STOP=1 -f db\p1\seed.sql
```

```powershell
dotnet build Platform.sln
dotnet run --project Platform.AppHost\Platform.AppHost.csproj
```

Для запуску API поза AppHost збережіть connection string через User Secrets (не в `appsettings.json`):

```powershell
dotnet user-secrets set "ConnectionStrings:EventCatalog" "Host=localhost;Port=5432;Database=event_catalog;Username=<користувач>;Password=<пароль>" --project Services\EventCatalog\EventCatalog.Api\EventCatalog.Api.csproj
dotnet run --project Services\EventCatalog\EventCatalog.Api\EventCatalog.Api.csproj --urls http://localhost:5031
```

Не додавайте паролі у файли репозиторію. Swagger доступний на `/swagger`; `/health` перевіряє з’єднання з PostgreSQL. Якщо connection string відсутній, застосунок завершує запуск із явним повідомленням, без демо-даних.

### API

- `GET /api/events?search=&status=&categoryId=` — пошук і фільтрація подій.
- `GET /api/events/{id}` — деталі події, місце проведення та теги.
- `POST /api/events` — створити чернетку разом із місцем проведення.
- `PUT /api/events/{id}` — оновити чернетку.
- `POST /api/events/{id}/publish`, `/finish`, `/cancel` — змінити статус із перевіркою дозволеного переходу.
- `DELETE /api/events/{id}` — м’яко видалити подію.
- `PUT`/`DELETE /api/events/{id}/tags/{tagId}` — додати/прибрати тег.
- `GET /api/categories`, `/api/organizers`, `/api/tags` — отримати довідники.

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

PowerShell приклад запитів:

```powershell
$event = @{
  organizerId = 1
  categoryId = 1
  title = "Demo event"
  startsAt = "2099-06-15T18:00:00+03:00"
  endsAt = "2099-06-15T21:00:00+03:00"
  venue = @{ name = "Central Hall"; address = "1 Main Street"; city = "Kyiv"; country = "Ukraine"; capacity = 500 }
} | ConvertTo-Json -Depth 4
Invoke-RestMethod -Method Post -Uri http://localhost:5031/api/events -ContentType "application/json" -Body $event
Invoke-RestMethod -Method Get -Uri "http://localhost:5031/api/events?status=PUBLISHED"
```

Помилки валідації, відсутніх ресурсів і конфліктів повертаються як ProblemDetails.

## Лабораторна 4: Reviews API / Clean Architecture та MongoDB

`Services/Reviews` містить окремий мікросервіс із чотирма проєктами та напрямком залежностей `Api → Application → Domain`, `Api → Infrastructure → Application/Domain`. Контролери передають кожну команду й запит через MediatR. Pipeline behaviors виконують логування, вимірювання часу, валідацію та обробку винятків; Application визначає репозиторні інтерфейси, а Infrastructure реалізує їх через MongoDB.

Домен зберігає відгук із вкладеними `author`, `comments` і `photos`, Value Objects `Author` та `Email`, інкапсульованим станом і версією для оптимістичної конкурентності. BSON-правила налаштовуються в Infrastructure через `BsonClassMap`; створюються індекси `{ eventPublicId: 1, createdAt: -1 }`, текстовий індекс відгуків і унікальні індекси `{ eventPublicId, userPublicId }` та `ticketPublicId`. Під час старту сервіс створює індекси й додає два демонстраційні відгуки лише коли колекція порожня.

Запустіть `Platform.AppHost` за інструкціями вище: Aspire підійме PostgreSQL і MongoDB, автоматично передасть connection strings, а Reviews API застосує власні індекси й seed. Для окремого запуску задайте `MongoDb:ConnectionString` та `MongoDb:DatabaseName` у конфігурації. MongoDB health check: `GET /health`; Swagger доступний на `/swagger` у Development.

Основні маршрути: `GET /api/reviews?eventPublicId=&search=&page=1&pageSize=20`, `GET /api/reviews/{objectId}`, `POST /api/reviews`, `PUT /api/reviews/{objectId}`, `DELETE /api/reviews/{objectId}?expectedVersion=1`. Створення приймає `eventPublicId`, `eventTitleSnapshot`, `ticketPublicId`, `userPublicId`, `displayName`, необов'язкові `avatarUrl`/`authorEmail`, `rating` (1–5), `text` і `photos`. Оновлення та видалення вимагають `expectedVersion`; конкурентна зміна повертає 409. Один користувач може створити лише один відгук на подію. Помилки валідації повертаються як `400 ValidationProblemDetails`, відсутність — `404 ProblemDetails`, конфлікт — `409 ProblemDetails`, недоступна MongoDB — `503 ProblemDetails`.

Для AutoMapper 16 передайте Community license key через `AutoMapper:LicenseKey` або змінну середовища `AUTOMAPPER_LICENSE_KEY`.
