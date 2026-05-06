# Развёртывание DeskQuitServer на Ubuntu - Краткая инструкция

## Для нетерпеливых (5 минут)

```bash
# 1. Установить Docker и Docker Compose
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo curl -L "https://github.com/docker/compose/releases/download/v2.20.0/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose
sudo usermod -aG docker $USER

# 2. Клонировать проект
cd /opt
git clone https://github.com/your-username/DeskQuitServer.git
cd DeskQuitServer

# 3. Настроить переменные окружения
cp .env.example .env
nano .env  # Установить безопасный пароль

# 4. Запустить
docker-compose up -d --build

# 5. Проверить
docker-compose ps
curl http://localhost:8080/openapi/v1.json
```

## Пошагово для сервера Ubuntu 20.04+

### Шаг 1: Установка Docker (как root или с sudo)

```bash
sudo apt update
sudo apt upgrade -y
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo curl -L "https://github.com/docker/compose/releases/download/v2.20.0/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose
sudo usermod -aG docker $USER
# После этого: logout и login обратно
```

### Шаг 2: Подготовка проекта

```bash
# Клонировать репозиторий
cd /opt
git clone https://github.com/your-username/DeskQuitServer.git
cd DeskQuitServer

# Скопировать и отредактировать переменные окружения
cp .env.example .env
nano .env
```

**Отредактировать `.env` файл:**
```
POSTGRES_USER=postgres
POSTGRES_PASSWORD=GenerateStrongPasswordHere_Min20Chars
POSTGRES_DB=deskquit
```

Сгенерировать надёжный пароль:
```bash
openssl rand -base64 32
```

### Шаг 3: Запуск приложения

```bash
# Запустить в фоне
docker-compose up -d --build

# Проверить статус (должны быть оба контейнера в статусе "Up")
docker-compose ps

# Посмотреть логи (Ctrl+C для выхода)
docker-compose logs -f
```

### Шаг 4: Проверка доступности

```bash
# Должно вернуть OpenAPI схему
curl http://localhost:8080/openapi/v1.json

# Приложение должно быть доступно на http://localhost:8080
```

## Частые команды

```bash
# Остановить приложение
docker-compose down

# Перезапустить
docker-compose restart

# Посмотреть логи конкретного контейнера
docker-compose logs deskquitserver
docker-compose logs db

# Вход в БД
docker-compose exec db psql -U postgres deskquit

# Резервная копия БД
docker-compose exec db pg_dump -U postgres deskquit > backup.sql

# Удалить всё включая данные (внимание!)
docker-compose down -v
```

## Автозапуск после перезагрузки сервера

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
User=username_here

[Install]
WantedBy=multi-user.target
```

Активировать:
```bash
sudo systemctl daemon-reload
sudo systemctl enable deskquitserver
sudo systemctl start deskquitserver
sudo systemctl status deskquitserver
```

## Проксирование через Nginx (опционально)

```bash
sudo apt install nginx -y
sudo nano /etc/nginx/sites-available/deskquitserver
```

Вставить:
```nginx
server {
    listen 80;
    server_name your-domain.com;
    
    location / {
        proxy_pass http://localhost:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

```bash
sudo ln -s /etc/nginx/sites-available/deskquitserver /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx
```

## Проблемы и решения

| Проблема | Решение |
|----------|---------|
| "Connection refused" | `docker-compose ps` - проверить все ли контейнеры запущены |
| "Port 5432 already in use" | `docker-compose down` и запустить заново |
| "Permission denied" | `sudo usermod -aG docker $USER` и переlogin |
| Нет доступа к БД | Проверить пароль в `.env` файле |
| Контейнеры не стартуют | `docker-compose logs` для просмотра ошибок |

## Отладка

```bash
# Полные логи приложения
docker-compose logs deskquitserver -f

# Проверить переменные окружения в контейнере
docker-compose exec deskquitserver env | grep Connection

# Вход в контейнер для отладки
docker-compose exec deskquitserver /bin/bash

# Проверить подключение к БД
docker-compose exec deskquitserver curl localhost:8080/api/auth/login
```

## Обновление приложения

```bash
cd /opt/DeskQuitServer
git pull
docker-compose up -d --build
```

---

Полная документация: [DEPLOYMENT.md](./DEPLOYMENT.md)

