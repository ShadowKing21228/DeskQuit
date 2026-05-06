# 📚 Документация DeskQuitServer

Выберите нужный вам сценарий:

## 🚀 Быстрый старт (5 минут)
- **[QUICK_START.md](./QUICK_START.md)** - Одностраничная инструкция для развёртывания на Ubuntu сервере
  - Для нетерпеливых
  - Команда за командой
  - Правильно скопипастить

## 📖 Полная документация

### Для системного администратора / DevOps
- **[DEPLOYMENT.md](./DEPLOYMENT.md)** - Полная инструкция развёртывания на Ubuntu сервере
  - Установка Docker и Docker Compose
  - Настройка переменных окружения
  - Автозапуск через systemd
  - Проксирование через Nginx
  - Резервные копии БД
  - Мониторинг
  - Обновление приложения

### Для разработчика
- **[LOCAL_DEV.md](./LOCAL_DEV.md)** - Локальная разработка
  - Запуск с Docker контейнером БД
  - Запуск с локальным PostgreSQL
  - Работа с миграциями
  - Hot reload при изменении кода
  - Тестирование API (curl, REST Client, Postman)
  - IDE-специфичные настройки

### Безопасность и HTTPS
- **[HTTPS.md](./HTTPS.md)** - Настройка HTTPS на продакшене
  - Nginx + Let's Encrypt (рекомендуется)
  - Самоподписанные сертификаты
  - Kestrel с SSL
  - Traefik автоматизация

### Основная информация
- **[README.md](./README.md)** - Основной README
  - Описание проекта
  - Структура проекта
  - API endpoints
  - Требования

## ⚡ Быстрые команды

### Для сервера (Ubuntu)

```bash
# Клонировать, настроить и запустить за одну команду
cd /opt && \
git clone https://github.com/your-username/DeskQuitServer.git && \
cd DeskQuitServer && \
cp .env.example .env && \
# Отредактировать пароль в .env
nano .env && \
docker-compose up -d --build
```

### Для локальной разработки

```bash
# Запустить БД в контейнере
docker run --name deskquit-postgres -e POSTGRES_DB=deskquit -e POSTGRES_PASSWORD=postgrespassword -p 5432:5432 -d postgres:15

# Подготовить приложение
cd DeskQuitServer && \
cp appsettings.Local.json.example appsettings.Local.json && \
# Отредактировать appsettings.Local.json если нужно
dotnet restore && \
dotnet ef database update && \
dotnet run
```

## 🔍 Найти нужную информацию

| Вопрос | Ответ |
|--------|-------|
| "Как развернуть на сервере?" | → [QUICK_START.md](./QUICK_START.md) или [DEPLOYMENT.md](./DEPLOYMENT.md) |
| "Как разворачивать локально?" | → [LOCAL_DEV.md](./LOCAL_DEV.md) |
| "Какие API endpoints?" | → [README.md](./README.md) |
| "Как работать с БД миграциями?" | → [LOCAL_DEV.md](./LOCAL_DEV.md) → Работа с миграциями |
| "Как настроить Nginx?" | → [DEPLOYMENT.md](./DEPLOYMENT.md) → Проксирование через Nginx |
| "Как настроить HTTPS?" | → [HTTPS.md](./HTTPS.md) → **Рекомендуется: Nginx + Let's Encrypt** |
| "Слишком много логов?" | → [LOGGING.md](./LOGGING.md) → Отключить verbose логирование |
| "Как сделать резервную копию?" | → [DEPLOYMENT.md](./DEPLOYMENT.md) → Резервная копия БД |
| "Docker команды?" | → [QUICK_START.md](./QUICK_START.md) → Частые команды |
| "Как отладить приложение?" | → [LOCAL_DEV.md](./LOCAL_DEV.md) → Debug режим в IDE |

## 📋 Чек-лист для развёртывания сервера

- [ ] Сервер на Ubuntu 20.04+
- [ ] Установлен Docker и Docker Compose
- [ ] Репозиторий клонирован в `/opt/DeskQuitServer`
- [ ] Создан и заполнен файл `.env` с безопасным паролем
- [ ] Запущено `docker-compose up -d --build`
- [ ] Проверено `docker-compose ps` - оба контейнера в статусе "Up"
- [ ] Приложение доступно на `http://localhost:8080`
- [ ] (Опционально) Настроен автозапуск через systemd
- [ ] (Опционально) Настроено проксирование через Nginx

## 🆘 Возникла проблема?

1. **Первым делом**: посмотри логи
   ```bash
   docker-compose logs -f
   ```

2. **Проверь стандартные проблемы** в соответствующем документе:
   - [DEPLOYMENT.md - Проблемы и решения](./DEPLOYMENT.md#проблемы-и-решения)
   - [QUICK_START.md - Проблемы и решения](./QUICK_START.md#проблемы-и-решения)
   - [LOCAL_DEV.md - Проблемы и решения](./LOCAL_DEV.md#проблемы-и-решения)

3. **Ничего не помогает?** Создай Issue в репозитории

## 📚 Структура документации

```
DeskQuitServer/
├── README.md           ← Основная информация о проекте
├── QUICK_START.md      ← Быстрый старт на сервере (5 мин)
├── DEPLOYMENT.md       ← Полная инструкция развёртывания
├── LOCAL_DEV.md        ← Локальная разработка
├── DOCS.md            ← Вы находитесь здесь
└── compose.yaml        ← Docker Compose конфигурация
```

## 🔗 Полезные ссылки

- [Docker документация](https://docs.docker.com/)
- [Docker Compose документация](https://docs.docker.com/compose/)
- [.NET документация](https://docs.microsoft.com/dotnet/)
- [PostgreSQL документация](https://www.postgresql.org/docs/)
- [Nginx документация](https://nginx.org/en/docs/)

---

**Последнее обновление**: 2026-05-06

