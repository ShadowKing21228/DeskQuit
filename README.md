# DeskQuitServer

ASP.NET Core приложение для управления напоминаниями о перерывах и отслеживания времени работы.

## Быстрый старт

### Локальная разработка

1. **Требования**:
   - .NET 10.0 SDK
   - PostgreSQL 15
   - (Опционально) Docker и Docker Compose для контейнеризации

2. **Установка**:
   ```bash
   git clone https://github.com/your-username/DeskQuitServer.git
   cd DeskQuitServer
   
   # Скопировать локальный конфиг примера
   cp DeskQuitServer/appsettings.Local.json.example DeskQuitServer/appsettings.Local.json
   
   # Отредактировать с нужными учётными данными БД
   nano DeskQuitServer/appsettings.Local.json
   ```

3. **Запуск**:
   ```bash
   # Восстановить зависимости
   dotnet restore
   
   # Запустить приложение
   cd DeskQuitServer
   dotnet run
   ```

   Приложение будет доступно на `http://localhost:8080`

### Docker (рекомендуется для сервера)

```bash
# Скопировать и заполнить переменные окружения
cp .env.example .env
nano .env

# Запустить контейнеры
docker-compose up -d --build
```

Подробные инструкции для развёртывания на Ubuntu серверу см. в [DEPLOYMENT.md](./DEPLOYMENT.md)

## Структура проекта

```
DeskQuitServer/
├── Controllers/          # API контроллеры
├── Models/              # Модели данных
├── Data/                # Контекст и миграции БД
├── DTOs/                # Data Transfer Objects
├── Services/            # Бизнес-логика
├── appsettings.json     # Конфигурация по умолчанию
└── Program.cs           # Точка входа
```

## API Endpoints

### Аутентификация
- `POST /api/auth/register` - Регистрация пользователя
- `POST /api/auth/login` - Вход в систему

### Конфигурация пользователя
- `GET /api/userconfig` - Получить конфигурацию
- `PUT /api/userconfig` - Обновить конфигурацию

### Напоминания
- `GET /api/userreminders` - Список напоминаний
- `POST /api/userreminders` - Создать напоминание
- `PUT /api/userreminders/{id}` - Обновить напоминание
- `DELETE /api/userreminders/{id}` - Удалить напоминание

### Статистика
- `GET /api/userstats` - Получить статистику

## Конфигурация

### Переменные окружения

- `ConnectionStrings__DefaultConnection` - Строка подключения к БД
- `POSTGRES_USER` - Пользователь PostgreSQL
- `POSTGRES_PASSWORD` - Пароль PostgreSQL
- `POSTGRES_DB` - Имя базы данных

### Файлы конфигурации

- `appsettings.json` - Базовая конфигурация (не изменяйте для GitHub)
- `appsettings.Local.json` - Локальная конфигурация (в .gitignore)
- `.env` - Переменные окружения для Docker (в .gitignore)

## Требования

- .NET 10.0 или выше
- PostgreSQL 15
- Docker и Docker Compose (опционально)

## Лицензия

MIT

## Разработка

### Создание миграций

```bash
cd DeskQuitServer
dotnet ef migrations add YourMigrationName
dotnet ef database update
```

### Запуск тестов

```bash
dotnet test
```

## Поддержка

За вопросами и проблемами создавайте Issue в репозитории.

