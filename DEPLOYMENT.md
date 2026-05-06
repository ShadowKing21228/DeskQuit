# Инструкция развёртывания DeskQuitServer на Ubuntu сервере

## Требования

- Ubuntu 20.04 или выше
- Docker и Docker Compose
- Git

## Шаг 1: Установка Docker и Docker Compose

```bash
# Обновить пакеты
sudo apt update
sudo apt upgrade -y

# Установить Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Установить Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/download/v2.20.0/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose

# Проверить установку
docker --version
docker-compose --version

# Добавить текущего пользователя в группу docker (опционально, без sudo)
sudo usermod -aG docker $USER
newgrp docker
```

## Шаг 2: Клонирование репозитория

```bash
# Перейти в нужную директорию
cd /opt  # или любую другую директорию для приложений

# Клонировать репозиторий
git clone https://github.com/your-username/DeskQuitServer.git
cd DeskQuitServer
```

## Шаг 3: Настройка переменных окружения

```bash
# Скопировать файл примера
cp .env.example .env

# Отредактировать .env с нужными значениями
nano .env
```

Заполнить файл `.env` с безопасными значениями:

```env
# PostgreSQL Configuration
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your_very_strong_and_secure_password_here_min_20_chars
POSTGRES_DB=deskquit

# Рекомендация: генерируйте пароль так:
# openssl rand -base64 32
```

**Важно**: Используйте сильный пароль (минимум 20 символов, включая буквы, цифры, спецсимволы)

## Шаг 4: Запуск приложения

```bash
# Собрать образы и запустить контейнеры в фоне
docker-compose up -d --build

# Проверить статус контейнеров
docker-compose ps

# Проверить логи приложения
docker-compose logs deskquitserver -f

# Для выхода из логов: Ctrl+C
```

## Шаг 5: Проверка работоспособности

```bash
# Проверить доступность API
curl http://localhost:8080/openapi/v1.json

# Попробовать регистрацию (если endpoint доступен)
curl -X POST http://localhost:8080/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "TestPassword123!"
  }'
```

## Шаг 6: Остановка и перезапуск

```bash
# Остановить контейнеры
docker-compose down

# Перезапустить контейнеры
docker-compose restart

# Полностью удалить контейнеры и тома (внимание: удалит данные БД!)
docker-compose down -v
```

## Шаг 7: Резервная копия БД

```bash
# Создать резервную копию PostgreSQL
docker-compose exec db pg_dump -U postgres deskquit > backup_$(date +%Y%m%d_%H%M%S).sql

# Восстановить из резервной копии
docker-compose exec -T db psql -U postgres deskquit < backup_файл.sql
```

## Полезные команды

```bash
# Просмотр логов определённого сервиса
docker-compose logs db              # Логи БД
docker-compose logs deskquitserver  # Логи приложения

# Вход в контейнер
docker-compose exec deskquitserver /bin/bash
docker-compose exec db psql -U postgres deskquit

# Проверить переменные окружения в контейнере
docker-compose exec deskquitserver env | grep -i connection

# Перестроить конкретный сервис
docker-compose build --no-cache deskquitserver
```

## Развёртывание с использованием systemd (автозапуск)

Создать файл `/etc/systemd/system/deskquitserver.service`:

```bash
sudo nano /etc/systemd/system/deskquitserver.service
```

Вставить:

```ini
[Unit]
Description=DeskQuitServer Docker Compose
Requires=docker.service
After=docker.service

[Service]
Type=oneshot
WorkingDirectory=/opt/DeskQuitServer
RemainAfterExit=yes
ExecStart=/usr/local/bin/docker-compose up -d
ExecStop=/usr/local/bin/docker-compose down
User=your_username

[Install]
WantedBy=multi-user.target
```

Включить сервис:

```bash
sudo systemctl daemon-reload
sudo systemctl enable deskquitserver
sudo systemctl start deskquitserver
sudo systemctl status deskquitserver
```

## Проксирование через Nginx (для продакшена)

Установить Nginx:

```bash
sudo apt install nginx -y
```

Создать конфиг `/etc/nginx/sites-available/deskquitserver`:

```bash
sudo nano /etc/nginx/sites-available/deskquitserver
```

Вставить (базовый HTTP конфиг):

```nginx
server {
    listen 80;
    server_name your_domain.com;

    location / {
        proxy_pass http://localhost:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Активировать конфиг:

```bash
sudo ln -s /etc/nginx/sites-available/deskquitserver /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx
```

**Для HTTPS (рекомендуется):** см. [HTTPS.md](./HTTPS.md)

## Проблемы и решения

### Ошибка: "Connection refused"
```
Решение: Убедитесь, что оба контейнера запущены
docker-compose ps
```

### Ошибка: "Port 5432 already in use"
```
Решение: Измените порт в .env или остановите другие контейнеры
docker-compose down
```

### Ошибка: "Permission denied"
```
Решение: Добавьте пользователя в группу docker
sudo usermod -aG docker $USER
newgrp docker
```

### БД потеряла данные после перезагрузки
```
Убедитесь, что том pgdata не был удален:
docker volume ls  # Должен быть deskquitserver_pgdata
```

## Мониторинг

Установить простой мониторинг:

```bash
# Проверка каждые 5 минут
(crontab -l 2>/dev/null; echo "*/5 * * * * cd /opt/DeskQuitServer && docker-compose ps | grep -q 'Up' || docker-compose up -d") | crontab -
```

## Обновление приложения

```bash
# Получить последние изменения
cd /opt/DeskQuitServer
git pull

# Пересобрать образ и перезапустить
docker-compose up -d --build
```

## Контакты и поддержка

При возникновении проблем:
1. Проверьте логи: `docker-compose logs`
2. Убедитесь, что `.env` заполнен правильно
3. Проверьте свободное место на диске: `df -h`
4. Убедитесь, что порты 8080 и 5432 доступны

