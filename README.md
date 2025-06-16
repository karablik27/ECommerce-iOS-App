# 🛍️ ECommerceApp

**ECommerceApp** — проект по разработке микросервисной архитектуры с iOS-приложением для управления заказами и балансом пользователя. Проект реализован на SwiftUI (клиент) и .NET 7 (сервер), использует асинхронную коммуникацию между сервисами через RabbitMQ и хранение данных в PostgreSQL.

## 📦 Содержание

- 🧩 Архитектура
- 📱 iOS-клиент (SwiftUI)
- 🔧 Backend (Microservices)
  - OrdersService
  - PaymentsService
- 🚀 Запуск через Docker
- 📁 Структура проекта
- 📄 Лицензия

## 🧩 Архитектура

```
[iOS App] ⇄ [API Gateway] ⇄ OrdersService
                             ↓
                          (RabbitMQ)
                             ↓
                       PaymentsService
                             ↓
                        PostgreSQL DB
```

## 📱 iOS-клиент (SwiftUI)

Возможности:
- Регистрация нового счёта
- Просмотр списка счетов и баланса
- Пополнение баланса (только положительная сумма)
- Создание заказов
- Просмотр заказов
- Темная/светлая тема

Технологии:
- SwiftUI, MVVM, Combine
- @AppStorage для сохранения темы
- async/await для сетевых вызовов

## 🔧 Backend (Microservices)

### OrdersService
- ASP.NET Core Web API
- PostgreSQL (хранение заказов)
- MassTransit + RabbitMQ
- Transactional Outbox Pattern

### PaymentsService
- ASP.NET Core Web API
- Проверка баланса и списание средств
- Inbox/Outbox Patterns
- Масштабируемая фоновая обработка платежей

## 🚀 Запуск через Docker

```bash
docker compose down -v        # удалить контейнеры и volume-ы
docker compose up --build     # пересобрать и запустить
```


