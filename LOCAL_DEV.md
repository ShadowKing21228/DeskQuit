# Локальная разработка DeskQuitServer

## Требования

- .NET 10.0 SDK (скачать с [dotnet.microsoft.com](https://dotnet.microsoft.com))
- PostgreSQL 15 (или Docker контейнер)
- IDE: Visual Studio, Rider или VS Code

## Вариант 1: С использованием Docker для БД (рекомендуется)

### Запуск только БД в контейнере

```bash
# Запустить только PostgreSQL
docker run --name deskquit-postgres \
  -e POSTGRES_DB=deskquit \
  -e POSTGRES_PASSWORD=postgrespassword \
  -p 5432:5432 \
  -v pgdata:/var/lib/postgresql/data \
  -d postgres:15
```

### Подготовка локального конфига

```bash
cd DeskQuitServer

# Скопировать пример конфига
cp appsettings.Local.json.example appsettings.Local.json
```

**Отредактировать `appsettings.Local.json`:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=deskquit;Username=postgres;Password=postgrespassword"
  },
  "JwtSettings": {
    "Key": "YourSuperSecureKeyForLocalDevelopmentAtLeast256BitsLong"
  }
}
```

### Инициализация БД

```bash
# Восстановить зависимости
dotnet restore

# Применить миграции (создать таблицы)
dotnet ef database update
```

### Запуск приложения

```bash
cd DeskQuitServer
dotnet run
```

Приложение доступно на `http://localhost:5000` или `http://localhost:8080`

### Остановка БД

```bash
# Остановить контейнер
docker stop deskquit-postgres

# Удалить контейнер (данные сохранятся в томе)
docker rm deskquit-postgres

# Запустить снова с теми же данными
docker run --name deskquit-postgres \
  -e POSTGRES_DB=deskquit \
  -e POSTGRES_PASSWORD=postgrespassword \
  -p 5432:5432 \
  -v pgdata:/var/lib/postgresql/data \
  -d postgres:15

# Полностью удалить контейнер и все данные
docker rm -f deskquit-postgres
docker volume rm pgdata
```

## Вариант 2: С локально установленным PostgreSQL

### Установка PostgreSQL (Ubuntu/Debian)

```bash
sudo apt install postgresql postgresql-contrib -y
sudo systemctl start postgresql
```

### Создание базы данных и пользователя

```bash
# Подключиться к PostgreSQL
sudo -u postgres psql

# Выполнить в psql:
CREATE USER deskquit_user WITH PASSWORD 'your_password';
CREATE DATABASE deskquit OWNER deskquit_user;
ALTER USER deskquit_user CREATEDB;
```

### Конфигурация приложения

**`appsettings.Local.json`:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=deskquit;Username=deskquit_user;Password=your_password"
  },
  "JwtSettings": {
    "Key": "YourSuperSecureKeyForLocalDevelopmentAtLeast256BitsLong"
  }
}
```

### Инициализация и запуск

```bash
cd DeskQuitServer
dotnet restore
dotnet ef database update
dotnet run
```

## Работа с миграциями

### Создание новой миграции

```bash
cd DeskQuitServer

# Например, добавляли новое поле в модель
dotnet ef migrations add AddNewFieldToUser

# Это создаст файл в папке Migrations/
```

### Применение миграций

```bash
# Применить все неприменённые миграции
dotnet ef database update

# Откатить миграцию
dotnet ef database update <previous-migration-name>
```

### Удаление последней миграции

```bash
# Если миграция ещё не была применена к БД
dotnet ef migrations remove
```

## Режим разработки

### Hot reload при изменении кода

```bash
dotnet watch run
```

Приложение будет перезагружаться при сохранении файлов.

### Debug режим в IDE

**Visual Studio:**
1. Установить точку останова (F9)
2. Нажать F5 для запуска в debug режиме
3. Навести на переменную для просмотра её значения

**Rider:**
1. Установить точку останова
2. Нажать Shift+F9 для запуска debug
3. Использовать Debug консоль

**VS Code:**
1. Установить расширение C#
2. Нажать F5 для запуска debug
3. Использовать Debug панель

## Тестирование API

### Использование curl

```bash
# Регистрация
curl -X POST http://localhost:8080/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "TestPassword123!"
  }'

# Вход
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "TestPassword123!"
  }'
```

### Использование REST Client (VS Code)

Установить расширение "REST Client" и создать файл `test.http`:

```http
### Регистрация
POST http://localhost:8080/api/auth/register
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "TestPassword123!"
}

### Вход
POST http://localhost:8080/api/auth/login
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "TestPassword123!"
}
```

Нажать "Send Request" над каждым блоком.

### Использование Postman

1. Скачать [Postman](https://www.postman.com/downloads/)
2. Создать новый Collection
3. Добавить запросы:
   - POST `http://localhost:8080/api/auth/register`
   - POST `http://localhost:8080/api/auth/login`
   - И другие endpoints

## Просмотр логов БД

```bash
# Если используется Docker контейнер
docker logs deskquit-postgres -f

# Если локальный PostgreSQL
sudo tail -f /var/log/postgresql/postgresql.log
```

## Очистка локального окружения

### Полная очистка (начать с нуля)

```bash
# Если используется Docker
docker stop deskquit-postgres
docker rm deskquit-postgres
docker volume rm pgdata

# Если локальный PostgreSQL
sudo -u postgres psql -c "DROP DATABASE deskquit;"
sudo -u postgres psql -c "DROP USER deskquit_user;"

# Удалить локальный конфиг
rm DeskQuitServer/appsettings.Local.json

# Очистить build артефакты
dotnet clean

# Восстановить и заново
dotnet restore
dotnet ef database update
```

## Полезные команды

```bash
# Информация о проекте
dotnet --version
dotnet sln list

# Список всех пакетов
dotnet list package

# Проверка кода на ошибки
dotnet build

# Запуск тестов
dotnet test

# Публикация
dotnet publish -c Release -o ./publish
```

## Проблемы и решения

| Проблема | Решение |
|----------|---------|
| "Cannot connect to database" | Проверить, что PostgreSQL запущен и пароль в конфиге правильный |
| Migration failed | Проверить структуру таблиц: `dotnet ef database update` снова |
| Port 5432 already in use | `lsof -i :5432` для поиска процесса |
| "Migrations folder not found" | Создать папку: `mkdir Migrations` |
| "appsettings.Local.json not found" | Скопировать из примера: `cp appsettings.Local.json.example appsettings.Local.json` |

## IDE-специфичные настройки

### Rider
- Встроенный Database tool для работы с БД
- Built-in REST Client для тестирования API

### Visual Studio
- SQL Server Object Explorer для работы с БД
- Встроенный HTTP Request Editor

### VS Code
- Расширение "SQLTools" для работы с БД
- Расширение "REST Client" для тестирования API
- Расширение "Thunder Client" как альтернатива Postman

## Документация

- [.NET документация](https://docs.microsoft.com/dotnet/)
- [Entity Framework Core](https://docs.microsoft.com/ef/core/)
- [PostgreSQL](https://www.postgresql.org/docs/)
- [ASP.NET Core](https://docs.microsoft.com/aspnet/core/)

