# SKFET Notification System 📢

<div align="center">
  
  ### Мобильное приложение для оповещения и общения студентов и сотрудников СКФЭТ
  
  [![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)](https://dotnet.microsoft.com/)
  [![.NET MAUI](https://img.shields.io/badge/.NET_MAUI-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/apps/maui)
  [![SignalR](https://img.shields.io/badge/SignalR-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/signalr)
  
  [![Platforms](https://img.shields.io/badge/Platform-Android%20%7C%20Windows-blue?style=flat-square)](https://dotnet.microsoft.com/apps/maui)

  <img src="Screenshots/demo.gif" width="250">
  
</div>

## О проекте

**SKFET Notification System** — официальное мобильное приложение **Северо-Кавказского финансово-энергетического техникума** для связи между администрацией, преподавателями, студентами и родителями. Помимо централизованной системы оповещения, приложение включает полноценный мессенджер с каналами и чатами в реальном времени.

## Ролевая модель

Система поддерживает четыре роли с разграничением прав:

| Роль | Возможности |
|------|-------------|
| **Администратор** | Полный доступ к системе, модерация, все каналы и чаты |
| **Преподаватель** | Отправка объявлений, создание каналов, приглашение участников |
| **Студент** | Просмотр объявлений, участие в каналах и чатах |
| **Родитель** | Просмотр объявлений, ограниченное участие в каналах |

## Ключевые возможности

### Уведомления и объявления

- Лента объявлений с фильтрацией и пагинацией
- Создание объявлений с текстом и изображениями
- Гибкий выбор аудитории: всем, по ролям, группам, персонально
- Push-уведомления через SignalR — приходят мгновенно
- Избранное, обсуждения в комментариях
- История отправленных объявлений

### Мессенджер и каналы

- **Каналы** — создавайте тематические каналы для группового общения
- **Чат в реальном времени** — сообщения доставляются мгновенно через SignalR
- **Отправка изображений** — с предпросмотром и возможностью масштабирования
- **Ответы на сообщения** — цитируйте конкретное сообщение
- **Редактирование** — изменяйте свои сообщения после отправки
- **Статусы сообщений** — ✓ доставлено, ✓✓ прочитано
- **Индикатор печати** — показывает, когда собеседник набирает текст
- **Умная группировка** — сообщения группируются по датам и отправителям
- **Пагинация** — подгрузка истории при скролле вверх

### Управление участниками

- Приглашение пользователей с фильтрацией по ролям и группам
- Роли в канале: **Владелец → Администратор → Модератор → Участник**
- Изменение ролей и удаление участников
- Передача прав владельца другому участнику
- Приоритетная сортировка в списке участников

### Приглашения

- Вкладки «Входящие» и «Исходящие»
- Принять / отклонить / отменить в один клик
- Удаление обработанных приглашений
- Баннер с количеством новых приглашений на главном экране

### Профиль

- Настройка аватара из галереи или предустановленных вариантов
- Редактирование личных данных
- Смена пароля

### Безопасность

- JWT + Refresh Tokens — безопасная аутентификация
- Автоматическое обновление токена
- Разграничение прав на основе роли

## Скриншоты

<div align="center">
<table>
  <tr>
    <td><img src="Screenshots/profile.jpg" width="200" alt="Профиль"/></td>
    <td><img src="Screenshots/avatars.jpg" width="200" alt="Аватарки"/></td>
    <td><img src="Screenshots/burger.jpg" width="200" alt="Аватарки"/></td>
  </tr>
  <tr>
    <td><img src="Screenshots/channels.jpg" width="200" alt="Каналы"/></td>
    <td><img src="Screenshots/chat.jpg" width="200" alt="Чат"/></td>
    <td><img src="Screenshots/members.jpg" width="200" alt="Участники канала"/></td>
  </tr>
  <tr>
    <td><img src="Screenshots/invite.jpg" width="200" alt="Пригласить"/></td>
    <td><img src="Screenshots/invites.jpg" width="200" alt="Исходящие приглашения"/></td>
    <td><img src="Screenshots/invites2.jpg" width="200" alt="Входящие приглашения"/></td>
  </tr>
  <tr>
    <td><img src="Screenshots/main.jpg" width="200" alt="Главная"/></td>
    <td><img src="Screenshots/main2.jpg" width="200" alt="Главная"/></td>
    <td><img src="Screenshots/login.jpg" width="200" alt="Логин"/></td>
  </tr>
</table>
</div>

## Архитектура приложения

### Паттерны и подходы

- **MVVM** — чёткое разделение логики и представления
- **Dependency Injection** — гибкая и тестируемая архитектура
- **Модульные API-сервисы** — разделение на `IAuthApiService`, `IChannelApiService`, `IChannelMessageApiService` и др.
- **Observer pattern** — реактивное обновление UI через SignalR события

### Ключевые компоненты

| Компонент | Назначение |
|-----------|-----------|
| `SignalRService` | Real-time подключение к NotificationHub и ChannelHub |
| `BaseApiService` | Общая логика HTTP-запросов и аутентификации |
| `AuthHandler` | Автоматическая подстановка JWT токена и обновление |
| `ChannelMessagesViewModel` | WhatsApp-подобный чат с группировкой и индикаторами |
| `PickImageService` | Кроссплатформенный выбор изображений|

## Технологический стек

### Мобильное приложение
- **.NET MAUI** — кросс-платформенный фреймворк
- **C# 12** — основной язык разработки
- **CommunityToolkit.Mvvm** — MVVM инструментарий (ObservableProperty, RelayCommand)
- **CommunityToolkit.Maui** — расширения MAUI (Popup, Behaviors)
- **Microsoft.Extensions.DependencyInjection** — DI контейнер
- **Microsoft.AspNetCore.SignalR.Client** — real-time коммуникация
- **System.Text.Json** — сериализация JSON
- **FFImageLoading** — оптимизированная загрузка изображений
- **Plugin.LocalNotification** — локальные уведомления

### Бэкенд (отдельный репозиторий)
- ASP.NET Core Web API
- Entity Framework Core (PostgreSQL)
- SignalR Hubs (NotificationHub, ChannelHub)
- JWT Authentication + Refresh Tokens
- Redis, Kafka, Docker
