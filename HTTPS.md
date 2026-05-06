# HTTPS на продакшене

## Рекомендуемый вариант: Nginx + Let's Encrypt (бесплатно и безопасно)

Это лучший вариант для продакшена:
- ✅ Автоматическое продление сертификата
- ✅ Бесплатные сертификаты Let's Encrypt
- ✅ Высокая безопасность
- ✅ Простое управление

### Шаг 1: Установить Nginx и Certbot

```bash
sudo apt update
sudo apt install nginx certbot python3-certbot-nginx -y
sudo systemctl start nginx
```

### Шаг 2: Создать Nginx конфиг

```bash
sudo nano /etc/nginx/sites-available/deskquitserver
```

Вставить (замените `your-domain.com` на реальный домен):

```nginx
server {
    listen 80;
    server_name your-domain.com www.your-domain.com;

    # Redirect HTTP to HTTPS
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name your-domain.com www.your-domain.com;

    # SSL certificates (будут созданы Certbot)
    ssl_certificate /etc/letsencrypt/live/your-domain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/your-domain.com/privkey.pem;

    # SSL configuration (рекомендуемые настройки)
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;

    # HSTS header (заставляет браузер использовать HTTPS)
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;

    # Proxy к приложению
    location / {
        proxy_pass http://localhost:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_http_version 1.1;
        proxy_set_header Connection "";
        
        # WebSocket support (если нужен)
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
    }
}
```

### Шаг 3: Активировать конфиг и получить сертификат

```bash
# Активировать конфиг
sudo ln -s /etc/nginx/sites-available/deskquitserver /etc/nginx/sites-enabled/

# Протестировать конфиг
sudo nginx -t

# Перезагрузить Nginx
sudo systemctl restart nginx

# Получить сертификат Let's Encrypt (интерактивно)
sudo certbot --nginx -d your-domain.com -d www.your-domain.com
```

Certbot автоматически:
- Создаст сертификат
- Обновит Nginx конфиг
- Настроит автоматическое продление (каждые 90 дней)

### Шаг 4: Проверить автоматическое продление

```bash
# Проверить статус сертификата
sudo certbot certificates

# Тест автоматического продления
sudo certbot renew --dry-run

# Просмотр логов
sudo systemctl status certbot.timer
```

## Вариант 2: Nginx с самоподписанным сертификатом (для тестирования)

**Только для тестирования, не для продакшена!**

```bash
# Создать самоподписанный сертификат (на 365 дней)
sudo openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout /etc/ssl/private/deskquitserver.key \
  -out /etc/ssl/certs/deskquitserver.crt

# Использовать в Nginx конфиге выше, но с другими путями:
# ssl_certificate /etc/ssl/certs/deskquitserver.crt;
# ssl_certificate_key /etc/ssl/private/deskquitserver.key;
```

## Вариант 3: Kestrel с HTTPS (внутри контейнера)

Можно настроить ASP.NET Kestrel слушать HTTPS напрямую:

### Шаг 1: Обновить Program.cs

```csharp
// После app.Build(), перед app.Run()
if (!app.Environment.IsDevelopment())
{
    app.Urls.Add("https://0.0.0.0:8443");
    app.Urls.Add("http://0.0.0.0:8080");
}
```

### Шаг 2: Обновить Dockerfile

Добавить копирование сертификата:

```dockerfile
FROM base AS final
WORKDIR /app

# Скопировать сертификат (должен быть в проекте)
COPY DeskQuitServer/https/certificate.pfx /app/certificate.pfx

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DeskQuitServer.dll"]
```

### Шаг 3: Обновить compose.yaml

```yaml
services:
  deskquitserver:
    ports:
      - "8080:8080"    # HTTP
      - "8443:8443"    # HTTPS
    environment:
      # ... существующие...
      - ASPNETCORE_Urls=https://+:8443;http://+:8080
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/app/certificate.pfx
      - ASPNETCORE_Kestrel__Certificates__Default__Password=your_cert_password
```

## Вариант 4: Docker с Let's Encrypt (автоматизированный)

Использовать `docker-compose` с `traefik` для автоматического HTTPS:

**compose-https.yaml:**

```yaml
services:
  traefik:
    image: traefik:v2.10
    command:
      - "--api.insecure=false"
      - "--providers.docker=true"
      - "--entrypoints.web.address=:80"
      - "--entrypoints.websecure.address=:443"
      - "--certificatesresolvers.letsencrypt.acme.httpchallenge=true"
      - "--certificatesresolvers.letsencrypt.acme.httpchallenge.entrypoint=web"
      - "--certificatesresolvers.letsencrypt.acme.email=your-email@example.com"
      - "--certificatesresolvers.letsencrypt.acme.storage=/letsencrypt/acme.json"
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
      - ./letsencrypt:/letsencrypt

  deskquitserver:
    image: deskquitserver
    build:
      context: .
      dockerfile: DeskQuitServer/Dockerfile
    environment:
      - ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.deskquitserver.rule=Host(`your-domain.com`)"
      - "traefik.http.routers.deskquitserver.entrypoints=web,websecure"
      - "traefik.http.routers.deskquitserver.tls.certresolver=letsencrypt"
      - "traefik.http.services.deskquitserver.loadbalancer.server.port=8080"
    depends_on:
      - db
      - traefik

  db:
    image: postgres:15
    environment:
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_DB: ${POSTGRES_DB}
    volumes:
      - ./DeskQuitServer/Data/init.sql:/docker-entrypoint-initdb.d/init.sql
      - pgdata:/var/lib/postgresql/data

volumes:
  pgdata:
```

Запуск:
```bash
docker-compose -f compose-https.yaml up -d
```

## Сравнение вариантов

| Вариант | Сложность | Стоимость | Безопасность | Рекомендация |
|---------|-----------|----------|-------------|-------------|
| Nginx + Let's Encrypt | ⭐⭐ | ✅ Бесплатно | ✅✅✅ | **🏆 Лучше всего** |
| Kestrel + сертификат | ⭐⭐⭐ | 💰 Платно | ✅✅ | Для специфичных случаев |
| Traefik | ⭐⭐ | ✅ Бесплатно | ✅✅✅ | Хорошая автоматизация |
| Самоподписанный | ⭐ | ✅ Бесплатно | ❌ | Только для тестирования |

## Проверка HTTPS

```bash
# Проверить работает ли https
curl -k https://your-domain.com  # -k игнорирует самоподписанные сертификаты

# Проверить сертификат
openssl s_client -connect your-domain.com:443

# Проверить через браузер
https://your-domain.com
```

## Проблемы и решения

| Проблема | Решение |
|----------|---------|
| "Certificate renewal failed" | Проверить `sudo certbot renew --dry-run` |
| "Port 80 already in use" | Остановить другой веб-сервер или изменить порт |
| "Mixed content error" | Убедиться, что все ресурсы загружаются по HTTPS |
| "HSTS error" | Использовать разный домен для тестирования или очистить HSTS кэш |
| "Redirect loop" | Проверить конфиг Nginx, убедиться что нет дублей |

## Финальный чек-лист для HTTPS

- [ ] Доменное имя зарегистрировано
- [ ] DNS указывает на IP сервера
- [ ] Установлен Nginx
- [ ] Установлен Certbot
- [ ] Создан конфиг Nginx
- [ ] Получен сертификат Let's Encrypt
- [ ] HTTP редирект на HTTPS работает
- [ ] Сертификат автоматически продлевается
- [ ] Проверено в браузере: https://your-domain.com
- [ ] Проверено через curl без ошибок
- [ ] HSTS header установлен
- [ ] SSL Labs score: A+ 🎉

## Рекомендация

**Используй Nginx + Let's Encrypt** - это:
- ✅ Самый простой способ
- ✅ Самый безопасный
- ✅ Бесплатно
- ✅ Автоматическое продление
- ✅ Используется во всем мире

