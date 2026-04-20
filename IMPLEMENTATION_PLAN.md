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
2. Пользовательские макросы не хранятся как отдельные `.bat`, `.cmd` или `.ps1` файлы на диске.
3. Макросы хранятся внутри общего зашифрованного контейнера приложения.
4. Макрос может исполняться через `cmd` или `PowerShell`, но запуск всегда скрытый.
5. Макросы запускаются в административном режиме по принятому продуктовым решением поведению.
6. Окно внутренних скрытых настроек и окно пользовательских макросов это разные подсистемы:
   - `Ctrl+Alt+F1` открывает внутреннее окно настроек;
   - `Ctrl+Alt+F2` открывает окно пользовательских макросов.
7. Pomodoro встраивается в текущее окно будильника, а не в отдельную форму.
8. Конфиги, макросы, alarm-ы и настройки Pomodoro сводятся в единый persistable state.
9. Любая новая персистентность должна проектироваться сразу под versioned migration.

## 3. Целевое состояние после внедрения

После завершения основных этапов проект должен иметь:

1. Единое зашифрованное хранилище настроек и runtime-state.
2. Миграцию со старого plaintext `config.json`.
3. Персистентные alarm-ы.
4. Отдельное окно макросов, открываемое по `Ctrl+Alt+F2`.
5. Редактор пользовательских макросов в UI в виде вертикального списка карточек со скроллом.
6. Поддержку нескольких глобальных hotkey для вызова макросов.
7. Исполнение пользовательских макросов через скрытый `cmd` или `PowerShell` без внешних файлов.
8. Общий зашифрованный контейнер с текстами макросов.
9. Pomodoro tracker в интерфейсе будильника.
10. Обновленный техдок с новой картой архитектуры.

## 4. Общая декомпозиция

Работу рекомендуется вести по пяти крупным этапам:

1. Foundation and storage refactor.
2. Encrypted config and migration.
3. Macro window and global macro hotkeys.
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
- `MacroDefinition`
- `PomodoroSettings`
- `PomodoroState`
- `MacroExecutionOptions`

### 5.2. Новые сервисы

Рекомендуемые сервисы:

- `AppStateRepository`
- `EncryptionService`
- `MigrationService`
- `HotkeyManager`
- `MacroExecutionService`
- `PomodoroService`

### 5.3. Принцип разделения ответственности

- Формы отображают состояние и инициируют действия.
- `TrayAppContext` оркестрирует runtime и связывает UI с сервисами.
- Сервисы содержат бизнес-логику.
- Модели состояния не должны зависеть от WinForms.

## 6. Этап 1. Foundation and Storage Refactor

### Status

`Done`

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
   - macro definitions;
   - Pomodoro.
5. Добавить минимальное логирование ошибок вместо части пустых `catch`.
6. Agent A: расширить `AppState` macro-section так, чтобы Agent B использовал общий storage contract, а не отдельный локальный файл.
7. Agent A: зафиксировать минимальный encrypted data contract для `IsActive`, hotkey, `RunnerType` и текста макроса.

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

### Agent A Macro Contract Notes

- Current task scope: только storage/state contract под макросы в общем encrypted container.
- Actual file touch:
  - `AppState.cs`
  - `IMPLEMENTATION_PLAN.md`
- Repository expectation:
  - `AppStateRepository` продолжает сериализовать весь `AppState` целиком;
  - отдельный plaintext storage для макросов не допускается.
- Constraint for Agent B:
  - использовать macro-section из `AppState`;
  - не вводить собственный файл или sidecar storage для hotkey/script text.

### Implementation Result

- `AppState` адаптирован под отдельный macro-section внутри общего state.
- В state добавлены модели для хранения macro definitions, hotkey payload и runner type.
- Существующий `AppStateRepository` не потребовал отдельного storage-кода, потому что он уже сериализует и шифрует весь `AppState` как один контейнер.

### Remaining Constraints For Agent B

- Agent B должен читать и писать макросы только через `AppState.Macros`.
- Допускается развивать UI, hotkey manager и execution поверх текущего контракта, но без собственного файла хранения.
- Если Agent B понадобится расширить macro payload, это нужно делать как совместимое расширение текущего state schema, а не обходным storage.

## 7. Этап 2. Encrypted Config and Migration

### Status

`Done`

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
6. Подготовить storage contract для общего контейнера пользовательских макросов:
   - без plaintext файлов;
   - с текстом макроса внутри общего state;
   - с возможностью хранить тип раннера `cmd` или `PowerShell`.
7. Зафиксировать поведение при ошибке расшифровки:
   - уведомление;
   - safe fallback;
   - недопущение silent corruption.
8. Обновить пути и вызовы сохранения в runtime.
9. Agent A: адаптировать encrypted state schema под macro metadata без реализации UI, hotkey manager и runner execution.

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

### Agent A Macro Contract Notes

- Macro data must stay inside the same encrypted `config.dat` container as other app state.
- Contract must support:
  - `IsActive`;
  - hotkey payload;
  - `RunnerType` as `cmd` or `PowerShell`;
  - script text.
- No migration from legacy plaintext macro storage is needed, because такого storage не существует и добавлять его нельзя.

### Implementation Result

- Encrypted state schema now supports macro payload in the same `config.dat`.
- Macro contract is covered by shared repository serialization, DPAPI encryption and the existing corruption/decryption fallback path.
- Separate plaintext macro storage was not introduced.

### Remaining Constraints For Agent B

- Runner type in storage is constrained to `cmd` or `PowerShell`.
- Hotkey storage is state-only payload at this stage; registration/runtime semantics belong to Agent B.
- Macro execution, hidden launch, admin launch and UI validation are intentionally out of scope for Agent A.

## 8. Этап 3. Macro Window and Global Macro Hotkeys

### Status

`Todo`

### Цель

Добавить отдельное окно пользовательских макросов и глобальные hotkey для их запуска без хранения внешних файлов макросов на диске.

### Подзадачи

1. Спроектировать модель `MacroDefinition`:
   - `Id`
   - `IsActive`
   - `Modifiers`
   - `Key`
   - `RunnerType`
   - `ScriptText`
   - `CreatedUtc`
   - `UpdatedUtc`
2. Явно разделить внутренние hotkey окон и пользовательские hotkey макросов:
   - `Ctrl+Alt+F1` остается для текущего скрытого окна настроек;
   - `Ctrl+Alt+F2` открывает отдельное окно макросов;
   - пользовательские hotkey работают независимо от видимости окон.
3. Переписать текущую схему hotkey из [GlobalHotkeyWindow.cs](/W:/Projects/CURSOR/CURSORTrayApp/GlobalHotkeyWindow.cs:7) на мульти-hotkey архитектуру.
4. Реализовать `HotkeyManager` с:
   - регистрацией нескольких hotkey;
   - удалением;
   - обновлением;
   - обработкой конфликтов;
   - событиями активации.
5. Реализовать `MacroExecutionService`:
   - запуск через `cmd`;
   - запуск через `PowerShell`;
   - скрытый запуск без окна консоли;
   - запуск с административными правами;
   - кнопка ручного тестового запуска из UI.
6. Добавить отдельное окно макросов, вызываемое по `Ctrl+Alt+F2`.
7. Реализовать UI окна макросов по карточкам:
   - `Active`;
   - поле hotkey;
   - большой многострочный редактор текста;
   - `Delete`;
   - `+` для добавления новой карточки;
   - вертикальный скролл без жесткого лимита на количество макросов.
8. Добавить валидацию:
   - пустая комбинация;
   - конфликт с другим пользовательским макросом;
   - пустой текст макроса.
9. Разрешить любые сочетания клавиш, включая системные и внутренние:
   - конфликт запрещается только между пользовательскими макросами;
   - `Ctrl+Alt+F1` и `Ctrl+Alt+F2` допускаются для пользовательского макроса как побочное параллельное поведение.
10. Подключить хранение макросов к общему зашифрованному state.

### Рекомендуемые файлы

- новый файл `MacroDefinition.cs`
- новый файл `HotkeyManager.cs`
- новый файл `MacroExecutionService.cs`
- новый файл `MacroForm.cs`
- [GlobalHotkeyWindow.cs](/W:/Projects/CURSOR/CURSORTrayApp/GlobalHotkeyWindow.cs:7)
- [TrayAppContext.cs](/W:/Projects/CURSOR/CURSORTrayApp/TrayAppContext.cs:16)
- `AppState.cs`

### Критерии готовности

1. `Ctrl+Alt+F2` открывает отдельное окно макросов.
2. Пользователь может создать, отредактировать, активировать и удалить макрос через UI.
3. Макросы сохраняются в зашифрованном контейнере приложения.
4. Несколько hotkey макросов могут работать одновременно.
5. Макрос исполняется без внешних `.bat/.cmd/.ps1` файлов.
6. Запуск из UI и запуск по hotkey работают при положении приложения в трее.

### Риски

1. Некоторые сочетания Windows не удастся зарегистрировать через стандартный API, даже если продуктово они разрешены.
2. Запуск с повышенными правами потребует аккуратной реализации и проверки фактического поведения UAC.
3. Скрытый запуск shell-команд усложняет диагностику ошибок исполнения.

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
   - macro hotkeys and macro window;
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
- `MacroDefinition.cs`
- `HotkeyManager.cs`
- `MacroExecutionService.cs`
- `MacroForm.cs`
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
2. Агент B: macro models, macro window, hotkey manager, macro execution.
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
| Foundation and storage refactor | Done | Agent A | Shared AppState macro section added; Agent B must reuse this storage contract |
| Encrypted config and migration | Done | Agent A | Shared encrypted config.dat schema confirmed to carry macro payload in the common container |
| Macro window and global macro hotkeys | Todo | Unassigned | |
| Pomodoro tracker | Todo | Unassigned | |
| Integration and documentation sync | Todo | Unassigned | |

## 14. Definition of Done for the Whole Initiative

Инициатива считается завершенной, когда выполнены все условия:

1. Конфиг и новые пользовательские настройки шифруются на диске.
2. Старые данные мигрируются без ручного вмешательства.
3. Пользователь может создавать свои макросы через отдельное окно.
4. Макросы выполняются по hotkey без внешних файлов на диске.
5. В приложении есть работающий Pomodoro tracker.
6. Alarm-ы и Pomodoro-параметры переживают перезапуск.
7. `CONTEXT.md` и этот план приведены в актуальное состояние.
