# TheAlarm: Context Window

Этот документ нужен как быстрое контекстное окно для дальнейшей разработки.
Он описывает фактическое устройство проекта по коду, а не только пользовательское поведение из `README.md`.

## 1. Назначение проекта

`TheAlarm` это небольшое WinForms-приложение под Windows, которое совмещает два сценария:

1. Видимый сценарий: простой будильник с окном, списком alarm-ов и popup-уведомлением.
2. Скрытый сценарий: tray utility для закрытия или сворачивания указанных процессов через наведение мыши в углы экрана.

Идея продукта:

- приложение живет в системном трее;
- обычный пользователь видит "будильник";
- скрытый функционал управляет выбранными программами;
- persisted state хранится рядом с `exe`, чтобы сборка оставалась portable.

## 2. Технологический профиль

- Язык: `C#`
- UI: `Windows Forms`
- Target framework: `net8.0-windows`
- Формат запуска: `WinExe`
- Модель приложения: `ApplicationContext` без постоянной главной формы
- Зависимости: только BCL и Win32 API, внешних пакетов нет

См. [TheAlarm.csproj](/W:/Projects/CURSOR/CURSORTrayApp/TheAlarm.csproj:1).

## 3. Принципы реализации

### 3.1. Приложение живет через tray context

Точка входа запускает не форму, а `TrayAppContext`.
Это значит, что реальный центр управления приложением не в UI-форме, а в оркестраторе.

См. [Program.cs](/W:/Projects/CURSOR/CURSORTrayApp/Program.cs:11) и [TrayAppContext.cs](/W:/Projects/CURSOR/CURSORTrayApp/TrayAppContext.cs:16).

### 3.2. UI отделен по ролям

- `AlarmForm` отвечает за постановку и отображение будильников.
- `SettingsForm` отвечает за список процессов и автозапуск.
- `PopupForm` показывает сообщение при срабатывании.
- `TrayAppContext` связывает формы, трей, таймеры, хуки, Win32-операции и persisted state.

### 3.3. Поведение строится на Win32, а не только на WinForms

Проект использует:

- глобальный mouse hook;
- глобальную hotkey;
- перечисление окон потоков;
- `taskkill` для принудительного завершения процессов;
- `Toolhelp32Snapshot` для обхода дочерних процессов;
- registry для автозапуска.

Это означает, что основные риски проекта связаны не с UI, а с платформенными edge-case-ами Windows.

### 3.4. Portable-first конфигурация

Persisted state хранится рядом с исполняемым файлом, но больше не лежит в plaintext JSON.
Текущее состояние сериализуется в единый `AppState`, шифруется через DPAPI `CurrentUser` и сохраняется в `config.dat`.

Если рядом лежит legacy `config.json`, а `config.dat` еще нет, при старте выполняется автоматическая миграция.
Такой подход сохраняет portable-сценарий, но остается чувствительным к правам записи в каталог запуска и к Windows user context.

### 3.5. Versioned state как точка расширения

В проекте введена единая persistable-модель `AppState`.
Сейчас в ней живут:

- process rules;
- alarm-ы;
- version/schema для будущих миграций.

Это важно для последующих этапов, потому что hotkey и Pomodoro должны встраиваться в тот же storage contract, а не создавать альтернативные файлы.

## 4. Карта структуры проекта

### 4.1. Точки входа и orchestration

- [Program.cs](/W:/Projects/CURSOR/CURSORTrayApp/Program.cs:11)
  Запуск WinForms-приложения через `Application.Run(new TrayAppContext())`.

- [TrayAppContext.cs](/W:/Projects/CURSOR/CURSORTrayApp/TrayAppContext.cs:16)
  Главный runtime-координатор.

Ключевые зоны в `TrayAppContext`:

- [TrayAppContext.cs](/W:/Projects/CURSOR/CURSORTrayApp/TrayAppContext.cs:44)
  Конструктор: создаются формы, иконка, таймеры, mouse hook, hotkey, repository и загрузка persisted state.
- [TrayAppContext.cs](/W:/Projects/CURSOR/CURSORTrayApp/TrayAppContext.cs:276)
  Проверка углов экрана.
- [TrayAppContext.cs](/W:/Projects/CURSOR/CURSORTrayApp/TrayAppContext.cs:308)
  Выполнение действий над процессами.
- [TrayAppContext.cs](/W:/Projects/CURSOR/CURSORTrayApp/TrayAppContext.cs:601)
  Корректный выход из приложения.
- [TrayAppContext.cs](/W:/Projects/CURSOR/CURSORTrayApp/TrayAppContext.cs:16)
  Применение state к формам и сохранение state при изменениях process rules/alarm-ов.

### 4.2. Формы

- [AlarmForm.cs](/W:/Projects/CURSOR/CURSORTrayApp/AlarmForm.cs:9)
  Будильник: дата, время, daily, текст, список alarm-ов, удаление.

- [SettingsForm.cs](/W:/Projects/CURSOR/CURSORTrayApp/SettingsForm.cs:12)
  Скрытые настройки: 2 списка процессов, флаг защиты дочерних процессов, автозапуск, debug-кнопки.

- [PopupForm.cs](/W:/Projects/CURSOR/CURSORTrayApp/PopupForm.cs:8)
  Простое popup-окно с текстом и кнопкой закрытия.

### 4.3. Интеграция с системой

- [GlobalHotkeyWindow.cs](/W:/Projects/CURSOR/CURSORTrayApp/GlobalHotkeyWindow.cs:7)
  Регистрация глобального `Ctrl+Alt+F1`.

- [LowLevelMouseHook.cs](/W:/Projects/CURSOR/CURSORTrayApp/LowLevelMouseHook.cs:6)
  Low-level hook для перемещения мыши по системе.

### 4.4. State, storage и migration

- [AppState.cs](/W:/Projects/CURSOR/CURSORTrayApp/AppState.cs)
  Единая модель persisted state: schema version, process rules, alarm-ы, encrypted envelope.

- [AppStateRepository.cs](/W:/Projects/CURSOR/CURSORTrayApp/AppStateRepository.cs)
  Чтение/запись `config.dat`, atomic save, warning fallback и orchestration миграции.

- [EncryptionService.cs](/W:/Projects/CURSOR/CURSORTrayApp/EncryptionService.cs)
  DPAPI `CurrentUser` protect/unprotect без собственного ключа в коде.

- [MigrationService.cs](/W:/Projects/CURSOR/CURSORTrayApp/MigrationService.cs)
  Миграция legacy plaintext `config.json` в новый `AppState`.

- [AppLog.cs](/W:/Projects/CURSOR/CURSORTrayApp/AppLog.cs)
  Минимальное файловое логирование для load/save/migration ошибок storage-контура.

## 5. Как проект реально работает

### 5.1. Жизненный цикл

При старте:

1. Создаются все формы.
2. Создается tray icon и контекстное меню.
3. Загружается persisted state через `AppStateRepository`.
4. Если найден только legacy `config.json`, выполняется миграция в encrypted `config.dat`.
5. Process rules и alarm-ы применяются к `SettingsForm` и `AlarmForm`.
6. При проблеме расшифровки или чтения показывается warning, а приложение стартует с пустым state.
7. Запускается таймер проверки alarm-ов.
8. Запускается таймер проверки углов как fallback.
9. Запускается глобальный mouse hook.
10. Регистрируется глобальная hotkey.

### 5.2. Открытие окон

- Левый клик по tray icon открывает `AlarmForm`.
- Hotkey переключает видимость `SettingsForm`.
- Закрытие форм по `X` не завершает приложение, а прячет окно обратно в tray mode.

### 5.3. Будильники

`AlarmForm` держит рабочий список alarm-ов в памяти, но persisted source of truth теперь находится в `AppState`.

Поведение:

- пользователь выбирает дату, время, daily и сообщение;
- alarm сохраняется как `TimeUtc`;
- при добавлении или удалении `AlarmForm` подает сигнал, и `TrayAppContext` сохраняет обновленный state через repository;
- `TrayAppContext` каждую секунду вызывает `ConsumeDueAlarms()`;
- одноразовые alarm-ы удаляются;
- daily alarm-ы пересоздаются на следующий день;
- после consume/rollover alarm-ов state снова сохраняется;
- для каждого сработавшего alarm-а показывается `PopupForm`.

Следствие:

- alarm-ы переживают перезапуск приложения;
- daily alarm после срабатывания сохраняется уже в новом времени.

### 5.4. Управление процессами

Два канала действий:

- `Close`
- `Minimize`

Для каждого процесса хранится:

- имя процесса;
- флаг `ProtectChildren`.

Эти правила теперь живут не в ad-hoc JSON внутри `TrayAppContext`, а в `AppState.ProcessRules`.
`SettingsForm` остается UI-источником и UI-приемником, а `TrayAppContext` сохраняет изменения через repository.

Логика:

- если курсор в правом верхнем углу, вызывается `Close`;
- если курсор в правом нижнем углу, вызывается `Minimize`;
- при `Close` используется `taskkill`;
- при `Minimize` перечисляются окна процессов и вызывается Win32 minimize;
- если `ProtectChildren = false`, для minimize дополнительно обходятся дочерние процессы;
- если `ProtectChildren = true`, действие применяется только к корневому процессу.

См. [TrayAppContext.cs](/W:/Projects/CURSOR/CURSORTrayApp/TrayAppContext.cs:308).

### 5.5. Автозапуск

Автозапуск включается через `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`.

Есть два пути:

- обычный checkbox: запись/удаление значения через `Registry.CurrentUser`;
- кнопка admin: запуск `reg add` с `Verb = "runas"`.

См. [SettingsForm.cs](/W:/Projects/CURSOR/CURSORTrayApp/SettingsForm.cs:468) и [SettingsForm.cs](/W:/Projects/CURSOR/CURSORTrayApp/SettingsForm.cs:526).

## 6. Что где менять

### 6.1. Если нужно менять поведение tray и навигацию

Идти в [TrayAppContext.cs](/W:/Projects/CURSOR/CURSORTrayApp/TrayAppContext.cs:16).

Там находятся:

- tray icon;
- меню;
- режимы приложения;
- показ форм;
- таймеры;
- mouse hook;
- hotkey;
- orchestration persisted state и сохранение `config.dat`.

### 6.2. Если нужно менять UX будильника

Идти в [AlarmForm.cs](/W:/Projects/CURSOR/CURSORTrayApp/AlarmForm.cs:9).

Там находятся:

- layout окна;
- логика создания/удаления alarm-ов;
- таблица со списком;
- daily logic.

### 6.3. Если нужно менять скрытый функционал и списки процессов

Идти в [SettingsForm.cs](/W:/Projects/CURSOR/CURSORTrayApp/SettingsForm.cs:12).

Там находятся:

- два списка процессов;
- управление `ProtectChildren`;
- Apply;
- debug-кнопки;
- логика автозапуска.

### 6.4. Если нужно менять low-level интеграцию

- mouse hook: [LowLevelMouseHook.cs](/W:/Projects/CURSOR/CURSORTrayApp/LowLevelMouseHook.cs:6)
- hotkey: [GlobalHotkeyWindow.cs](/W:/Projects/CURSOR/CURSORTrayApp/GlobalHotkeyWindow.cs:7)
- Win32 process/window actions: [TrayAppContext.cs](/W:/Projects/CURSOR/CURSORTrayApp/TrayAppContext.cs:308)

## 7. Известные ошибки и проблемные места

Ниже перечислены не просто "TODO", а реальные текущие расхождения или слабые места по коду.

### 7.1. Encrypted storage привязан к Windows user context

Используется DPAPI `CurrentUser`.
Это правильно для локального шифрования без хранения ключа в коде, но означает:

- `config.dat` не переносится прозрачно между Windows-пользователями;
- перенос portable-сборки на другой профиль не гарантирует чтение уже сохраненного state;
- сценарии backup/recovery пока не реализованы.

### 7.2. Глобальная hotkey работает не так, как описано

По README ожидается, что hotkey ограничена состоянием окон, но в коде:

- `Ctrl+Alt+F1` регистрируется глобально всегда;
- `CanToggleEvaluator` присваивается, но вообще не используется;
- значит hotkey активна независимо от реального режима приложения.

См. [TrayAppContext.cs](/W:/Projects/CURSOR/CURSORTrayApp/TrayAppContext.cs:120) и [GlobalHotkeyWindow.cs](/W:/Projects/CURSOR/CURSORTrayApp/GlobalHotkeyWindow.cs:11).

### 7.3. В `GlobalHotkeyWindow` есть мертвое поле

- поле `_closeId` не используется;
- сборка уже дает предупреждение `CS0169`.

См. [GlobalHotkeyWindow.cs](/W:/Projects/CURSOR/CURSORTrayApp/GlobalHotkeyWindow.cs:10).

### 7.4. В `AlarmForm` есть мертвое событие

Проблема:

- событие `RequestPopup` объявлено;
- `TrayAppContext` на него подписывается;
- но `AlarmForm` никогда его не вызывает;
- popup фактически инициируется обходным путем через возврат списка сообщений из `ConsumeDueAlarms()`.

См. [AlarmForm.cs](/W:/Projects/CURSOR/CURSORTrayApp/AlarmForm.cs:12) и [TrayAppContext.cs](/W:/Projects/CURSOR/CURSORTrayApp/TrayAppContext.cs:245).

Следствие:

- есть лишняя абстракция;
- код вводит в заблуждение при чтении.

### 7.5. Проверка углов экрана реализована двумя механизмами сразу

Сейчас одновременно существуют:

- глобальный mouse hook;
- timer fallback на 200 мс.

Это не критическая ошибка, но:

- поведение сложнее отлаживать;
- есть дублирование каналов вызова;
- debounce частично скрывает повторные срабатывания, а не устраняет архитектурную избыточность.

См. [TrayAppContext.cs](/W:/Projects/CURSOR/CURSORTrayApp/TrayAppContext.cs:102), [TrayAppContext.cs](/W:/Projects/CURSOR/CURSORTrayApp/TrayAppContext.cs:245) и [TrayAppContext.cs](/W:/Projects/CURSOR/CURSORTrayApp/TrayAppContext.cs:308).

### 7.6. `config.dat` рядом с `exe` удобен, но все еще зависит от прав записи

Проблема:

- запись идет в `AppContext.BaseDirectory`;
- если приложение запущено из каталога без прав записи, encrypted state может не сохраниться.

Следствие:

- теперь это уже не silent fail для storage-контура: ошибка сохраняется в `thealarm.log`, а пользователь получает message box;
- но сам architectural tradeoff portable-first storage никуда не делся.

### 7.7. Ошибки в проекте часто глотаются без логирования вне storage-контура

Во многих местах используются пустые `catch`.

Это встречается в:

- загрузке иконки;
- части low-level операций вокруг окон и процессов;
- операциях над процессами;
- `taskkill`;
- обходе окон;
- normalize helpers.

Следствие:

- приложение редко падает;
- но диагностика проблем почти отсутствует вне `AppStateRepository`/migration/save path;
- поведение "ничего не произошло" трудно расследовать.

### 7.8. Debug-элементы находятся в production UI

В `SettingsForm` есть:

- отображение координат курсора;
- кнопка `Kill Selected (Close)`;
- кнопка `Minimize Selected`.

Это удобно при отладке, но:

- смешивает пользовательский и служебный UX;
- часть логики кнопок отличается от основного runtime behavior;
- эти элементы лучше отделить флагом debug mode или убрать из production сборки.

См. [SettingsForm.cs](/W:/Projects/CURSOR/CURSORTrayApp/SettingsForm.cs:232).

## 8. Технические ограничения

### 8.1. Проект завязан на Windows

Это не кроссплатформенное приложение.
Основные механизмы используют:

- `user32.dll`
- `kernel32.dll`
- Windows Registry
- `taskkill`

### 8.2. Поведение зависит от устройства среды

Особенно чувствительны:

- multi-monitor конфигурации;
- DPI scaling;
- права пользователя;
- процессы без видимых top-level окон;
- приложения с нестандартными дочерними окнами;
- процессы, которые невозможно штатно завершить через `taskkill`.

### 8.3. У проекта нет тестового контура

В репозитории нет:

- unit tests;
- integration tests;
- smoke tests;
- логирования для сценариев runtime.

Любые изменения в low-level части лучше проверять вручную на Windows.

## 9. Практический порядок чтения проекта

Если нужно быстро войти в код, читать в таком порядке:

1. [Program.cs](/W:/Projects/CURSOR/CURSORTrayApp/Program.cs:11)
2. [TrayAppContext.cs](/W:/Projects/CURSOR/CURSORTrayApp/TrayAppContext.cs:16)
3. [SettingsForm.cs](/W:/Projects/CURSOR/CURSORTrayApp/SettingsForm.cs:12)
4. [AlarmForm.cs](/W:/Projects/CURSOR/CURSORTrayApp/AlarmForm.cs:9)
5. [GlobalHotkeyWindow.cs](/W:/Projects/CURSOR/CURSORTrayApp/GlobalHotkeyWindow.cs:7)
6. [LowLevelMouseHook.cs](/W:/Projects/CURSOR/CURSORTrayApp/LowLevelMouseHook.cs:6)
7. [PopupForm.cs](/W:/Projects/CURSOR/CURSORTrayApp/PopupForm.cs:8)

## 10. Рекомендуемые первые улучшения

Если продолжать проект дальше, наибольший эффект дадут такие шаги:

1. Убрать мертвые поля и события.
2. Привести hotkey behavior к одному понятному правилу.
3. Расширить логирование за пределы storage-контура.
4. Отделить debug UI от пользовательского UI.
5. Явно определить долгосрочную стратегию хранения: portable рядом с `exe` или user profile.
6. Добавить recovery/backups policy для поврежденного или нечитаемого `config.dat`.

## 11. Состояние на текущий момент

По состоянию текущего обзора:

- проект собирается командой `dotnet build -c Debug`;
- критических compile errors нет;
- реализованы `AppState`, `AppStateRepository`, DPAPI-шифрование `config.dat`, миграция с legacy `config.json` и персистентность alarm-ов;
- есть предупреждения про неиспользуемые члены;
- основная архитектура понятна и пригодна для дальнейшей доработки.
