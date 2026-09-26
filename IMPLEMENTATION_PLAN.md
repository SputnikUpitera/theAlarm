# TheAlarm: Implementation Plan

## Screenshot Alignment Follow-up (2026-09-26)

Implemented the six user screenshot corrections: vertically centered switch track/text and single-line input text; explicit Name and Function/trigger captions above the editor fields; editor enlarged to 960x880 with process panels consuming flexible remaining height and denser 28px minimum rows; removed outer window outline and caption separator; body/code font 10pt -> 12pt, card headings 13pt -> 15pt. Palette now follows the neutral dark shadcn theme (background #0a0a0a, surface #171717, foreground #fafafa, muted #a3a3a3), retaining blue primary accents. Table empty areas use the same surface as their rows. This is a native adaptation, not pixel-identical browser rendering. Geometry regression checks cover input centering, captions and panel expansion; final visual acceptance remains with the user. Existing menu-lifetime fixes retained.

## Stability Audit (2026-09-26)

User requested crash investigation and a fresh logic/performance audit; visual alignment is paused pending user screenshots. Findings, evidence, fixes, tests and unresolved risks are recorded in STABILITY_AUDIT.md. The option-menu crash is now tied to an actual ObjectDisposedException log, unlike earlier unconfirmed background reports. ChoiceButton owns its menu until disposal, not until Closed. Also fixed failed-load overwrite protection, process-name normalization/argument boundaries, Win capture, stale hotkey-message validation, process selection persistence, multi-alarm messages and ancient daily-alarm catchup. Optimized process snapshots and macro/text allocations. Smoke checks include 250 menu cycles, 50 modal editor lifecycles and a 15-second tray-loop diagnostic; these do not replace long-term testing. Remaining contract gaps (elevation, full batch semantics, OS-reserved shortcuts) are explicitly open, not marked complete. CONTEXT.md sync remains gated on user acceptance.

## Selected Visual Direction (2026-09-26, latest override)

User selected the shadcn/ui dashboard reference, with blue accent. Implemented as native WinForms components, not embedded React or a second web runtime. This section supersedes the initial mixed-style revision below.

- Replaced alarm and popup layout, shared borderless frame, neutral graphite palette, blue primary actions, dark owner-drawn table headers/rows, rounded input containers, and geometric close/minimize/plus icons. No native frame is created then swapped in OnLoad; ModernForm starts borderless in its constructor. All application windows center on the cursor monitor's working area on every opening.
- Autostart is directly in AlarmForm. Removed the tray startup item and deleted the unused legacy SettingsForm. Registry logic now lives in StartupService; the setting is not changed by tests.
- Each corner has an independent persisted ProcessAction. Defaults preserve old behavior for existing data. Circular controls have centers on the actual rectangle vertices; adjacent selectors choose close/minimize. Global macro switches remain conventional labelled switches.
- Smoke tests also check borderless state before first show, centering and corner action clone/JSON roundtrip. Rendered alarm, macro window and action editor inspected at current desktop DPI. Physical multi-monitor/high-DPI behavior and final visual acceptance remain manual checks.
- Existing OS dialogs (file selection and error messages) remain OS dialogs; they are not a second legacy application interface. Existing hotkey and DPAPI limitations described below still apply. Pomodoro is still a separate task.

## Unified Macros Revision (2026-09-26, supersedes earlier UI contracts)

Status: implemented and smoke-tested; pending user acceptance of design and longer background operation.

- One hidden MacroForm replaces the former settings/macros split. Ctrl+Alt+F1 toggles alarm/macros only when one is visible; no internal Ctrl+Alt+F2 binding. User macro routes remain active in tray. Editing temporarily pauses user routes to capture keys safely.
- Built-in screen-corner macro is migrated once from ProcessRules, including child protection. Four enlarged switches sit in the monitor corners with no abbreviated labels. Upper corners close, lower corners minimize; right corners initially enabled. Trigger is entry into a corner of the primary monitor, not repeated timer execution while resting there.
- Additional macros have hotkeys, independent close/minimize lists, process picker and optional cmd/PowerShell commands. Actions and text stay in AppState.Macros.Definitions in the shared encrypted config.dat. Old script macros retain enabled scripts; disabling a script preserves its text.
- MacroActionForm owns editing; MacroForm has compact scrollable cards, so large command fields no longer intercept scrolling of the macro list. Duplicate user shortcuts are rejected.
- ModernForm and RoundedControls provide custom chrome, gray/blue palette, rounded buttons/cards/toggles. Button alignment and corner-switch feedback incorporated. MP3 signal and running-process picker retained. Autostart is now accessible from the tray menu (existing registry mechanism retained).
- Background hardening: process actions receive snapshots instead of reading controls on workers; removed redundant mouse hook/settings timer; dispose process handles; bound cross-process minimize messaging; orderly context disposal on exit. These are preventive fixes, NOT proof of the reported crash cause.
- Validation: `dotnet run --project tests/Smoke/Smoke.csproj -p:PublishSingleFile=false` covers migration/idempotence, disabled-state preservation, DPAPI and repository roundtrips, tray gating, F1 toggle, process-only hotkey route and reusable popup. Tests use their own output-directory config, never the user's release config. Rendering harness also exercised alarm, macro cards and editor.
- Pending manual acceptance: actual physical hotkeys alongside other apps, elevated process actions, high-DPI/multi-monitor layouts, sustained background use and visual approval. Existing RegisterHotKey limitations remain: OS-reserved combinations cannot all be guaranteed. DPAPI protects disk contents, not against another program running as the same Windows user.
- Pomodoro remains the separate, unimplemented Agent C task; this revision does not claim to complete it. CONTEXT.md remains the accepted baseline until user confirmation, as required by the documentation protocol.

The sections below retain earlier milestones. Where they mention separate settings, internal F2, mandatory script text or the old card layout, this revision takes precedence.

## UI and Stability Follow-up (2026-09-26)

Status: implemented, awaiting Windows UI acceptance.

- Macro empty state shows only the first-macro action; existing cards show the header add action.
- Mouse wheel over a script scrolls cards; Ctrl+wheel scrolls the editor. Card actions occupy a separate row.
- Added DPI awareness, high-contrast support and accessible control names. This is an initial accessibility pass, not a complete visual redesign.
- Popup user-close now hides the reusable form, preventing reuse of a disposed popup. The reported crash is not yet reproduced; unhandled errors are logged under LocalApplicationData/TheAlarm.
- Alarm signal supports an external MP3 path stored in encrypted AppState, preview, stop and default-sound fallback. The audio file must remain available.
- Running-process picker supports search, refresh and multiple checked processes for both action lists; rules remain name-based and affect matching instances.
- Build and diff checks performed. Pending manual acceptance: repeated alarm after closing popup, MP3 playback, DPI/high contrast, keyboard navigation and scrolling, process selection and restart persistence.
- CONTEXT.md awaits user acceptance under the documentation protocol.

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

`Done`

### Цель

Добавить отдельное окно пользовательских макросов и глобальные hotkey для их запуска без хранения внешних файлов макросов на диске.

### Подзадачи

1. Использовать существующую storage-модель из `AppState.cs`, а не создавать новую:
   - `AppState.Macros.Definitions`
   - `MacroDefinition`
   - `MacroHotkey`
   - `MacroRunnerTypes`
   Если для UI/runtime понадобится расширение payload, делать это как совместимое изменение текущей схемы.
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

- новый файл `HotkeyManager.cs`
- новый файл `MacroExecutionService.cs`
- новый файл `MacroForm.cs`
- [GlobalHotkeyWindow.cs](/W:/Projects/CURSOR/CURSORTrayApp/GlobalHotkeyWindow.cs:7)
- [TrayAppContext.cs](/W:/Projects/CURSOR/CURSORTrayApp/TrayAppContext.cs:16)
- [AppState.cs](/W:/Projects/CURSOR/CURSORTrayApp/AppState.cs:8)

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

### Agent B Working Notes

- Owner: `Agent B`
- Scope for current implementation:
  - перевести runtime с single-hotkey окна на multi-hotkey registration для внутренних окон и пользовательских макросов;
  - добавить отдельное окно макросов по `Ctrl+Alt+F2` без изменения поведения tray;
  - хранить и редактировать макросы через существующий `AppState.Macros.Definitions`;
  - выполнять макросы через скрытый `cmd` или `PowerShell` без внешних `.bat/.cmd/.ps1` файлов;
  - различать продуктовую валидацию конфликта между пользовательскими макросами и фактический результат регистрации Win32.
- Refined subtasks:
  - расширить `GlobalHotkeyWindow` до поддержки набора регистрируемых hotkey и маршрутизации callbacks;
  - добавить `HotkeyManager` для внутренних hotkey окон и пользовательских macro hotkey;
  - реализовать `MacroExecutionService` с hidden + runas запуском и test-run из UI;
  - реализовать `MacroForm` с вертикальным списком карточек, добавлением, удалением, редактированием и отображением ошибок регистрации;
  - интегрировать загрузку/сохранение macro definitions и refresh hotkey registration в `TrayAppContext`;
  - при необходимости ограничиться минимальным совместимым расширением `AppState.cs` без sidecar storage.
- Planned file touches within ownership:
  - `GlobalHotkeyWindow.cs`
  - `HotkeyManager.cs`
  - `MacroExecutionService.cs`
  - `MacroForm.cs`
  - `TrayAppContext.cs` (minimal integration only)
  - `AppState.cs` (only if compatible schema extension is required)
  - `IMPLEMENTATION_PLAN.md`

### Implementation Result

- Реализовано отдельное окно `MacroForm` по `Ctrl+Alt+F2` с вертикальным scrollable-списком карточек.
- Каждая карточка содержит:
  - `Active`
  - hotkey capture field
  - runner selector (`cmd` / `PowerShell`)
  - `Test`
  - `Delete`
  - большой многострочный editor для текста макроса
- Добавлен `HotkeyManager`, который:
  - держит несколько глобальных hotkey одновременно;
  - объединяет внутренние `Ctrl+Alt+F1` / `Ctrl+Alt+F2` и пользовательские macro hotkeys в единую registration map;
  - разрешает тем же сочетаниям быть использованными и для внутренних окон, и для макросов;
  - запрещает конфликт только между пользовательскими макросами;
  - отражает фактический Win32 registration failure обратно в статус макроса.
- `GlobalHotkeyWindow` переписан с single-hotkey на multi-registration окно.
- Добавлен `MacroExecutionService`:
  - хранение текста макроса остается внутри общего encrypted `config.dat`;
  - `.bat/.cmd/.ps1` sidecar-файлы не создаются;
  - `PowerShell` macro запускается hidden + `runas`;
  - `cmd` macro запускается через elevated hidden PowerShell bootstrap, который поднимает скрытый `cmd` и передает текст через stdin без внешнего файла.
- Выполнена интеграция в `TrayAppContext`:
  - загрузка и сохранение идут через `AppStateRepository`;
  - макросы читаются и пишутся только через `AppState.Macros.Definitions`;
  - hotkey registration refresh происходит после изменений макросов;
  - отдельное legacy `config.json` сохранение в `TrayAppContext` удалено, чтобы macros/process rules/alarms жили в общей state-модели.
- Фактические file touches Agent B:
  - `GlobalHotkeyWindow.cs`
  - `HotkeyManager.cs`
  - `MacroExecutionService.cs`
  - `MacroForm.cs`
  - `TrayAppContext.cs`
  - `IMPLEMENTATION_PLAN.md`
- `AppState.cs` не менялся: текущего контракта хватило.

### Remaining Risks / Untested Scenarios

- Статус `Done`: ручная проверка окна макросов, карточек, кнопок добавления и live-scroll подтверждена.
- Не проверены фактические UAC сценарии:
  - user cancels `runas`
  - скрытость окна после elevation
  - поведение при уже повышенном процессе
- Не проверены реальные Win32 registration collisions с внешними приложениями, которые уже держат тот же global hotkey.
- Частично проверен end-to-end сценарий UI и скрытого доступа:
  - окно макросов открывается только по внутреннему `Ctrl+Alt+F2`;
  - пункт открытия макросов из tray menu убран;
  - добавление, удаление, прокрутка и базовое редактирование карточек работают.
- Не проверен end-to-end сценарий совместного срабатывания:
  - `Ctrl+Alt+F1` открывает settings
  - macro с тем же hotkey запускается параллельно
- Не проверены edge-case команды `cmd`, чувствительные к console encoding или интерактивному вводу.

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
| Macro window and global macro hotkeys | Done | Agent B | Multi-hotkey runtime, macro window, hidden cmd/PowerShell execution, AppState.Macros integration, add buttons and live-scroll behavior are implemented; core manual UI verification confirmed |
| Pomodoro tracker | Todo | Unassigned | |
| Integration and documentation sync | Todo | Unassigned | |

## 14. Definition of Done for the Whole Initiative

### UI refinement and macro window removal, 2026-09-26

- Release 1.3.0: corrected checkbox border pixel geometry so the inner square is centered; added self-contained win-x86 alongside win-x64 to tagged release packaging. Archives explicitly include only TheAlarm.exe.

- Follow-up implemented: compact 9pt bold body text, proportionally smaller windows/buttons, compact aligned tray menu. Process protection is a separate right-hand column; one click on its checkbox toggles it, double-click on the rest of the row toggles it, single-click on the row only selects it. Space remains available for keyboard operation. Caption changed to "Свернуть процессы". Click behavior is covered by native-message smoke tests; final visual acceptance remains with the user.

- Implemented: themed tray menu, bold typography, darker blue accent (#1447e6), larger gaps between sections and captions attached to the following controls.
- Clarified: hide-first applies to applications targeted by macros, not to closing TheAlarm itself. Removed the unrelated application closing changes.
- Implemented: minimize all matching target windows (including unprotected descendants) before starting taskkill. Use forced minimization without the former per-window 500 ms message wait. This deliberately keeps surviving windows recoverable from the taskbar if termination fails; it is not permanent SW_HIDE.
- Pending user acceptance: visual appearance and behavior with actual target applications, including unresponsive/elevated windows. Arbitrary cmd/PowerShell commands are not rewritten.

Инициатива считается завершенной, когда выполнены все условия:

1. Конфиг и новые пользовательские настройки шифруются на диске.
2. Старые данные мигрируются без ручного вмешательства.
3. Пользователь может создавать свои макросы через отдельное окно.
4. Макросы выполняются по hotkey без внешних файлов на диске.
5. В приложении есть работающий Pomodoro tracker.
6. Alarm-ы и Pomodoro-параметры переживают перезапуск.
7. `CONTEXT.md` и этот план приведены в актуальное состояние.
