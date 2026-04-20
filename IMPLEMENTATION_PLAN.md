# TheAlarm: Implementation Plan

Этот документ переводит обзор из [CONTEXT.md](/W:/Projects/CURSOR/CURSORTrayApp/CONTEXT.md:1) в прикладной план реализации.
Он нужен для реальной разработки, декомпозиции задач и синхронизации нескольких агентов.

## 1. Правила работы с этим планом

Если задача выполняется агентом, он должен работать по следующему протоколу:

1. Сначала прочитать [CONTEXT.md](/W:/Projects/CURSOR/CURSORTrayApp/CONTEXT.md:1) и этот план.
2. Перед кодом обновить этот файл:
   - отметить свой этап как `In Progress`;
   - уточнить решения, допущения, риски и затронутые файлы;
   - при необходимости скорректировать подзадачи.
3. После завершения реализации и локальной проверки:
   - отметить задачу как `Done` или `Partial`;
   - кратко зафиксировать фактически сделанные изменения;
   - перечислить остаточные риски и непроверенные кейсы.
4. После подтверждения, что реализация принята:
   - обновить [CONTEXT.md](/W:/Projects/CURSOR/CURSORTrayApp/CONTEXT.md:1), чтобы техдок отражал новую реальность кода;
   - убрать из `Known Issues` уже закрытые пункты;
   - добавить новые принципы, модели и ограничения.

Этот файл является рабочим документом разработки, а `CONTEXT.md` является опорным техдоком по фактическому состоянию проекта.

## 2. Зафиксированные продуктовые решения

До отдельного пересмотра работаем с такими базовыми решениями:

1. Хранилище конфигов шифруется через DPAPI с `DataProtectionScope.CurrentUser`.
2. Сырые `.bat`-файлы и внешние скрипты для пользовательских hotkey не используются.
3. Пользовательские hotkey сначала работают через встроенный command engine, а не через произвольный shell.
4. Pomodoro встраивается в текущее окно будильника, а не в отдельную форму.
5. Конфиги, hotkey, alarm-ы и настройки Pomodoro сводятся в единый persistable state.
6. Любая новая персистентность должна проектироваться сразу под versioned migration.

## 3. Целевое состояние после внедрения

После завершения основных этапов проект должен иметь:

1. Единое зашифрованное хранилище настроек и runtime-state.
2. Миграцию со старого plaintext `config.json`.
3. Персистентные alarm-ы.
4. Редактор пользовательских hotkey в UI.
5. Поддержку нескольких глобальных hotkey.
6. Встроенный механизм исполнения команд hotkey без внешних батников.
7. Pomodoro tracker в интерфейсе будильника.
8. Обновленный техдок с новой картой архитектуры.

## 4. Общая декомпозиция

Работу рекомендуется вести по пяти крупным этапам:

1. Foundation and storage refactor.
2. Encrypted config and migration.
3. User hotkeys and command engine.
4. Pomodoro tracker.
5. Integration, stabilization and documentation sync.

## 5. Общая модель новых компонентов

Ниже не финальный API, а рекомендуемая архитектурная рамка.

### 5.1. Новые модели

Рекомендуемые сущности:

- `AppState`
- `EncryptedFileEnvelope`
- `ProcessRulesState`
- `AlarmState`
- `HotkeyDefinition`
- `PomodoroSettings`
- `PomodoroState`
- `ExecutionCommand` или `CommandDefinition`

### 5.2. Новые сервисы

Рекомендуемые сервисы:

- `AppStateRepository`
- `EncryptionService`
- `MigrationService`
- `HotkeyManager`
- `CommandExecutionService`
- `PomodoroService`

### 5.3. Принцип разделения ответственности

- Формы отображают состояние и инициируют действия.
- `TrayAppContext` оркестрирует runtime и связывает UI с сервисами.
- Сервисы содержат бизнес-логику.
- Модели состояния не должны зависеть от WinForms.

## 6. Этап 1. Foundation and Storage Refactor

### Status

`Todo`

### Цель

Подготовить проект к новым функциям, убрав зависимость важных данных от локальных списков внутри форм.

### Подзадачи

1. Спроектировать общую модель `AppState`.
2. Вынести текущее хранение process config из [TrayAppContext.cs](/W:/Projects/CURSOR/CURSORTrayApp/TrayAppContext.cs:617) в отдельный repository/service.
3. Определить, какие части состояния принадлежат:
   - persistent config;
   - persistent runtime state;
   - transient UI state.
4. Подготовить точки интеграции для:
   - process rules;
   - alarm-ов;
   - hotkey;
   - Pomodoro.
5. Добавить минимальное логирование ошибок вместо части пустых `catch`.

### Рекомендуемые файлы

- новый файл `AppState.cs`
- новый файл `AppStateRepository.cs`
- новый файл `Logging` или небольшой `AppLog.cs`
- [TrayAppContext.cs](/W:/Projects/CURSOR/CURSORTrayApp/TrayAppContext.cs:16)
- [AlarmForm.cs](/W:/Projects/CURSOR/CURSORTrayApp/AlarmForm.cs:9)
- [SettingsForm.cs](/W:/Projects/CURSOR/CURSORTrayApp/SettingsForm.cs:12)

### Критерии готовности

1. Проект использует единый объект состояния.
2. Process config не загружается и не сохраняется напрямую из UI-слоя.
3. Есть понятная точка расширения для alarm/hotkey/Pomodoro.
4. Сборка проходит без регрессии базового поведения.

### Риски

1. Слишком ранний большой рефактор может сломать текущие сценарии tray behavior.
2. Если сделать слишком абстрактно, последующие этапы станут тяжелее, а не легче.

## 7. Этап 2. Encrypted Config and Migration

### Status

`Todo`

### Цель

Перевести конфиги с plaintext хранения на зашифрованное хранение и обеспечить миграцию старых данных.

### Подзадачи

1. Выбрать формат нового файла:
   - `config.dat`
   - или versioned JSON envelope с ciphertext.
2. Реализовать `EncryptionService` на базе DPAPI:
   - `Protect`
   - `Unprotect`
3. Реализовать `AppStateRepository` чтения и записи:
   - сериализация state;
   - шифрование;
   - сохранение;
   - чтение;
   - обработка ошибок.
4. Реализовать миграцию старого `config.json`.
5. Добавить персистентность alarm-ов в ту же схему хранения.
6. Зафиксировать поведение при ошибке расшифровки:
   - уведомление;
   - safe fallback;
   - недопущение silent corruption.
7. Обновить пути и вызовы сохранения в runtime.

### Рекомендуемые файлы

- новый файл `EncryptionService.cs`
- новый файл `MigrationService.cs`
- новый файл `AppStateRepository.cs`
- новый файл `AppState.cs`
- [TrayAppContext.cs](/W:/Projects/CURSOR/CURSORTrayApp/TrayAppContext.cs:617)
- [AlarmForm.cs](/W:/Projects/CURSOR/CURSORTrayApp/AlarmForm.cs:249)
- [AlarmForm.cs](/W:/Projects/CURSOR/CURSORTrayApp/AlarmForm.cs:263)

### Критерии готовности

1. Старый plaintext config при наличии мигрируется автоматически.
2. Новый конфиг на диске не читается как обычный JSON.
3. Process rules и alarm-ы восстанавливаются после перезапуска.
4. Поведение при ошибке расшифровки не ломает приложение молча.

### Риски

1. DPAPI привяжет данные к Windows user context.
2. Нужна аккуратная миграция без потери process config.

## 8. Этап 3. User Hotkeys and Command Engine

### Status

`Todo`

### Цель

Добавить настраиваемые глобальные hotkey и встроенные команды без хранения внешних батников.

### Подзадачи

1. Спроектировать модель `HotkeyDefinition`:
   - `Id`
   - `Name`
   - `Enabled`
   - `Modifiers`
   - `Key`
   - `CommandText`
   - `Description`
2. Переписать текущую схему hotkey из [GlobalHotkeyWindow.cs](/W:/Projects/CURSOR/CURSORTrayApp/GlobalHotkeyWindow.cs:7) на мульти-hotkey архитектуру.
3. Реализовать `HotkeyManager` с:
   - регистрацией нескольких hotkey;
   - удалением;
   - обновлением;
   - обработкой конфликтов;
   - событиями активации.
4. Реализовать `CommandExecutionService`.
5. Спроектировать и внедрить минимальный DSL/список команд.
6. Добавить UI-редактор hotkey в [SettingsForm.cs](/W:/Projects/CURSOR/CURSORTrayApp/SettingsForm.cs:12).
7. Добавить валидацию:
   - пустая комбинация;
   - недопустимая комбинация;
   - конфликт;
   - пустая команда;
   - некорректный синтаксис команды.
8. Реализовать тестовый вызов команды из интерфейса.

### Рекомендуемый первый набор встроенных команд

- `show_alarm`
- `show_settings`
- `close_process:<name>`
- `minimize_process:<name>`
- `notify:<text>`
- `pomodoro:start`
- `pomodoro:pause`
- `pomodoro:resume`
- `pomodoro:skip`

### Рекомендуемые файлы

- новый файл `HotkeyDefinition.cs`
- новый файл `HotkeyManager.cs`
- новый файл `CommandExecutionService.cs`
- новый файл `CommandParser.cs`
- [GlobalHotkeyWindow.cs](/W:/Projects/CURSOR/CURSORTrayApp/GlobalHotkeyWindow.cs:7)
- [TrayAppContext.cs](/W:/Projects/CURSOR/CURSORTrayApp/TrayAppContext.cs:16)
- [SettingsForm.cs](/W:/Projects/CURSOR/CURSORTrayApp/SettingsForm.cs:12)

### Критерии готовности

1. Пользователь может создать, отредактировать и удалить hotkey через UI.
2. Hotkey сохраняются в зашифрованном конфиге.
3. Несколько hotkey могут работать одновременно.
4. Команды исполняются без внешних `.bat`.
5. Ошибки регистрации и конфликтов объясняются в интерфейсе.

### Риски

1. Некоторые сочетания Windows не удастся зарегистрировать.
2. Непродуманный command DSL быстро станет нерасширяемым.
3. Слишком гибкое исполнение команд ухудшит безопасность.

## 9. Этап 4. Pomodoro Tracker

### Status

`Todo`

### Цель

Встроить Pomodoro tracker в окно будильника без разлома текущего alarm UX.

### Подзадачи

1. Спроектировать `PomodoroSettings`.
2. Спроектировать `PomodoroState`.
3. Реализовать `PomodoroService`:
   - start;
   - pause;
   - resume;
   - skip;
   - reset;
   - phase transition.
4. Выбрать UX размещения:
   - вкладка;
   - отдельная секция;
   - переключаемый режим.
5. Обновить [AlarmForm.cs](/W:/Projects/CURSOR/CURSORTrayApp/AlarmForm.cs:9):
   - таймер отсчета;
   - кнопки управления;
   - настройки длительности;
   - статус текущей фазы.
6. Переиспользовать `PopupForm` для завершения focus/break фаз.
7. Сохранить настройки и runtime-state Pomodoro в зашифрованное хранилище.
8. Решить поведение при рестарте приложения в середине сессии.

### Рекомендуемое целевое UX

Минимум:

- текущая фаза;
- обратный отсчет;
- старт, пауза, продолжить, пропустить, сброс;
- длительность Focus;
- длительность Short Break;
- длительность Long Break;
- цикл до Long Break;
- опция auto-start next phase.

### Рекомендуемые файлы

- новый файл `PomodoroState.cs`
- новый файл `PomodoroService.cs`
- [AlarmForm.cs](/W:/Projects/CURSOR/CURSORTrayApp/AlarmForm.cs:9)
- [PopupForm.cs](/W:/Projects/CURSOR/CURSORTrayApp/PopupForm.cs:8)
- [TrayAppContext.cs](/W:/Projects/CURSOR/CURSORTrayApp/TrayAppContext.cs:16)

### Критерии готовности

1. Пользователь может полноценно вести Pomodoro-сессию в UI.
2. Состояние Pomodoro не теряется при скрытии окна.
3. Настройки Pomodoro сохраняются между запусками.
4. Завершение фазы визуально сигнализируется.

### Риски

1. Если смешать Pomodoro и alarm в одной логике таймеров, код станет хрупким.
2. Слишком перегруженный `AlarmForm` ухудшит UX и поддержку.

## 10. Этап 5. Integration, Stabilization and Documentation Sync

### Status

`Todo`

### Цель

Довести внедрение до стабильного состояния и привести документацию к коду.

### Подзадачи

1. Проверить интеграцию:
   - encrypted storage;
   - alarm persistence;
   - custom hotkeys;
   - Pomodoro.
2. Удалить или закрыть мертвые части старой архитектуры:
   - `_closeId`
   - `RequestPopup`
   - устаревшие методы и поля
3. Свести обработку ошибок к понятному поведению.
4. Обновить `README.md` при необходимости.
5. Обновить [CONTEXT.md](/W:/Projects/CURSOR/CURSORTrayApp/CONTEXT.md:1):
   - структура;
   - принципы;
   - известные ограничения;
   - закрытые ошибки.
6. Финально перечитать этот план и отметить выполненные этапы.

### Критерии готовности

1. Техдок соответствует реальному состоянию кода.
2. План отражает, что уже завершено, а что осталось.
3. Базовая сборка проходит.
4. Основные пользовательские сценарии не регрессировали.

## 11. Suggested File Map

Ниже практичный ориентир, какие файлы, скорее всего, появятся в проекте.

- `AppState.cs`
- `AppStateRepository.cs`
- `EncryptionService.cs`
- `MigrationService.cs`
- `HotkeyDefinition.cs`
- `HotkeyManager.cs`
- `CommandExecutionService.cs`
- `CommandParser.cs`
- `PomodoroState.cs`
- `PomodoroService.cs`
- `AppLog.cs`

Если в ходе работы будет выбрана папочная структура, предпочтительны каталоги:

- `Models`
- `Services`
- `Infrastructure`

Но только если это не приведет к лишнему размазыванию маленького проекта.

## 12. Suggested Agent Split

Если работу делить между агентами, безопасная декомпозиция такая:

1. Агент A: foundation, storage, encryption, migration.
2. Агент B: hotkey models, manager, UI, command engine.
3. Агент C: Pomodoro models, service, UI integration.
4. Агент D: integration cleanup, documentation sync, final stabilization.

Важно:

- Агент B не должен ломать storage-модель, согласованную агентом A.
- Агент C не должен изобретать собственное хранение state в обход общего repository.
- Агент D обновляет `CONTEXT.md` только после подтвержденного факта, что изменения приняты.

## 13. Progress Tracker

Этот раздел должен редактироваться по мере реальной работы.

| Area | Status | Owner | Notes |
|---|---|---|---|
| Foundation and storage refactor | Todo | Unassigned | |
| Encrypted config and migration | Todo | Unassigned | |
| User hotkeys and command engine | Todo | Unassigned | |
| Pomodoro tracker | Todo | Unassigned | |
| Integration and documentation sync | Todo | Unassigned | |

## 14. Definition of Done for the Whole Initiative

Инициатива считается завершенной, когда выполнены все условия:

1. Конфиг и новые пользовательские настройки шифруются на диске.
2. Старые данные мигрируются без ручного вмешательства.
3. Пользователь может создавать свои hotkey через UI.
4. Hotkey выполняют команды без внешних батников.
5. В приложении есть работающий Pomodoro tracker.
6. Alarm-ы и Pomodoro-параметры переживают перезапуск.
7. `CONTEXT.md` и этот план приведены в актуальное состояние.
