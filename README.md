# ViloreHome

Навчальний проєкт каталогу товарів для дому. Бекенд побудований на ASP.NET Core, EF Core та SQLite; фронтенд — на React.

## API документація

Контракт каталогу товарів: [OpenAPI 3.0.3](docs/api/openapi.yaml). Він описує CRUD-операції для `/api/items`, перевірку доступності `/health`, DTO та формат бізнес-помилок.

Перевірка контракту у Swagger Editor:

![Контракт ViloreHome у Swagger Editor](docs/api/swagger_screenshot.png)

Архітектурні рішення:

- [ADR 0001 — архітектурний стиль](docs/adr/0001-architecture-style.md)
- [ADR 0002 — шарова архітектура](docs/adr/0002-layered-architecture.md)
- [ADR 0003 — REST API та модель помилок](docs/adr/0003-api-style-and-error-model.md)
