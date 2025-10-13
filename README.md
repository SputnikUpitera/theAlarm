Скачайте только CURSORTrayApp.exe, если вам не нужен исходный код

# theAlarm - приложение для быстрого и тихого сворачивания/закрытия определённых процессов, замаскированное под будильник(!!!)

## Русская версия

### Требования
- Windows 10 или новее
- .NET SDK 8.0 (или новее) для сборки

### Сборка (единый исполняемый файл)
Из Developer PowerShell или CMD:

```bash
cd TheAlarm
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
```

Исполняемый файл будет создан по пути:
```
TheAlarm\bin\Release\net8.0-windows\win-x64\publish\TheAlarm.exe
```

### Использование
- При запуске приложение сворачивается в системный трей
- Левый клик по иконке в трее: открывает окно будильника
- Правый клик по иконке в трее: меню с "Open Alarm Clock" и "Exit"

### Будильник (видимое окно)
- Установка времени с помощью выбора даты и времени
- Чекбокс "Daily" для ежедневных будильников
- Ввод опционального сообщения
- При срабатывании будильника появляется всплывающее окно
- Ежедневные будильники автоматически переносятся на следующий день
- Одноразовые будильники удаляются после срабатывания

### Скрытые настройки (горячие клавиши)
- Переключение окна настроек: Ctrl+Alt+F1
- **Горячие клавиши работают только когда открыто окно будильника или настроек**
- В полностью скрытом режиме (только иконка в трее) горячие клавиши отключены
- Добавление имен процессов (например, "notepad", "chrome") без ".exe"
- Флажки для защиты дочерних процессов (если отмечено, дочерние процессы не закрываются)
- Поведение в скрытом режиме:
  - Курсор в правом верхнем углу экрана → закрытие выбранных программ
  - Курсор в правом нижнем углу экрана → сворачивание выбранных программ

### Закрытие приложения
- Приложение полностью закрывается только через меню трея "Exit"
- Закрытие окон крестиком сворачивает приложение в системный трей
- Приложение продолжает работать в фоновом режиме для выполнения функций будильника и управления процессами

### Автозапуск с Windows
- Чекбокс "Start with Windows" для автоматического запуска
- Кнопка "Enable Autostart (Admin)" для принудительного включения с правами администратора

---

## English Version

### Requirements
- Windows 10 or later
- .NET SDK 8.0 (or later) for building

### Build (single-file executable)
From Developer PowerShell or CMD:

```bash
cd TheAlarm
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
```

The executable will be generated at:
```
TheAlarm\bin\Release\net8.0-windows\win-x64\publish\TheAlarm.exe
```

### Usage
- On launch, the app minimizes to system tray
- Left-click tray icon: opens Alarm Clock window
- Right-click tray icon: menu with "Open Alarm Clock" and "Exit"

### Alarm Clock (visible interface)
- Set time using date and time pickers
- "Daily" checkbox for recurring alarms
- Optional message input
- When alarm triggers, a small popup appears with your message
- Daily alarms automatically reschedule for next day
- One-time alarms are removed after triggering

### Hidden Settings (hotkey interface)
- Toggle settings window with: Ctrl+Alt+F1
- **Hotkeys work only when alarm window or settings window is open**
- In completely hidden mode (tray icon only) hotkeys are disabled
- Add process names (e.g., "notepad", "chrome") without ".exe"
- Checkboxes to protect child processes (if checked, child processes won't be closed)
- Behavior in hidden mode:
  - Move mouse to top-right corner of primary screen → close selected programs
  - Move mouse to bottom-right corner of primary screen → minimize selected programs

### Application Closing
- Application fully exits only via tray menu "Exit" item
- Closing windows with X button minimizes the application to system tray
- Application continues running in background for alarm and process control functions

### Windows Autostart
- "Start with Windows" checkbox for automatic startup
- "Enable Autostart (Admin)" button for forced enablement with admin rights


















