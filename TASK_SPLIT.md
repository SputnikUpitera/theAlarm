# TheAlarm: Task Split

Этот документ фиксирует безопасное разделение задач между несколькими агентами.
Его цель:

1. Снизить вероятность конфликтов по файлам.
2. Зафиксировать ownership по подсистемам.
3. Упростить параллельную работу над проектом.

Этот файл используется вместе с:

- [CONTEXT.md](/W:/Projects/CURSOR/CURSORTrayApp/CONTEXT.md:1)
- [IMPLEMENTATION_PLAN.md](/W:/Projects/CURSOR/CURSORTrayApp/IMPLEMENTATION_PLAN.md:1)
- [AGENT_PROMPTS.md](/W:/Projects/CURSOR/CURSORTrayApp/AGENT_PROMPTS.md:1)

## 1. Общие правила для всех агентов

Каждый агент обязан:

1. Перед началом прочитать `CONTEXT.md`, `IMPLEMENTATION_PLAN.md` и `TASK_SPLIT.md`.
2. Перед кодом обновить `IMPLEMENTATION_PLAN.md`:
   - перевести свой блок в `In Progress`;
   - кратко зафиксировать подзадачи;
   - перечислить файлы своей зоны ответственности.
3. Не вносить произвольные изменения в файлы другого ownership, если это не указано явно в этом документе.
4. Если для завершения задачи нужен touch чужого файла:
   - вносить только минимальный интеграционный change;
   - явно отметить это в `IMPLEMENTATION_PLAN.md`.
5. После завершения:
   - обновить `IMPLEMENTATION_PLAN.md`;
   - поставить `Done` или `Partial`;
   - перечислить сделанное и хвосты.
6. Не обновлять `CONTEXT.md` сразу после кодинга.
7. Обновлять `CONTEXT.md` только после подтверждения, что задача принята и интегрирована.

## 2. Общая стратегия интеграции

Работа должна идти в таком порядке:

1. Агент A задает общий storage/state контракт.
2. Агент B подключает подсистему макросов и hotkey к этому контракту.
3. Агент C подключает Pomodoro к тому же контракту.
4. Агент D делает финальную зачистку, интеграцию и синхронизацию документации.

Если работа реально идет параллельно, то:

1. Агент A является источником истины по модели хранения.
2. Агенты B и C не создают собственное альтернативное хранилище.
3. Агент D не делает крупный рефактор, пока A/B/C не завершили свои ветки.

## 3. Ownership по агентам

### 3.1. Агент A: Foundation, Storage, Encryption, Migration

Основная зона ответственности:

- единая модель `AppState`
- repository/storage
- encryption
- migration
- базовая персистентность alarm-ов
- минимальное логирование для хранения и загрузки state

Основной ownership файлов:

- `AppState.cs`
- `AppStateRepository.cs`
- `EncryptionService.cs`
- `MigrationService.cs`
- `AppLog.cs`

Разрешенные интеграционные правки:

- [TrayAppContext.cs](/W:/Projects/CURSOR/CURSORTrayApp/TrayAppContext.cs:16)
- [AlarmForm.cs](/W:/Projects/CURSOR/CURSORTrayApp/AlarmForm.cs:9)
- [SettingsForm.cs](/W:/Projects/CURSOR/CURSORTrayApp/SettingsForm.cs:12)

Ограничения:

- не строить UI для hotkey;
- не реализовывать Pomodoro;
- не переписывать глобальные hotkey beyond minimal compatibility.

### 3.2. Агент B: Macro Window, Macro Hotkeys and Macro Execution

Основная зона ответственности:

- user-defined macros
- multi-hotkey registration for macros
- macro execution via hidden `cmd` or `PowerShell`
- отдельное окно макросов по `Ctrl+Alt+F2`
- UI редактирования макросов

Основной ownership файлов:

- `HotkeyManager.cs`
- `MacroExecutionService.cs`
- `MacroForm.cs`

Основной ownership существующих файлов:

- [GlobalHotkeyWindow.cs](/W:/Projects/CURSOR/CURSORTrayApp/GlobalHotkeyWindow.cs:7)
- [AppState.cs](/W:/Projects/CURSOR/CURSORTrayApp/AppState.cs:8)

Разрешенные интеграционные правки:

- [TrayAppContext.cs](/W:/Projects/CURSOR/CURSORTrayApp/TrayAppContext.cs:16)
- `AppState.cs`

Ограничения:

- не менять storage contract без явной необходимости;
- не создавать отдельный формат хранения макросов мимо `AppStateRepository`;
- не реализовывать Pomodoro UI и логику;
- не влезать в миграции старого конфига, кроме чтения уже заданной модели.

### 3.3. Агент C: Pomodoro

Основная зона ответственности:

- Pomodoro models
- Pomodoro service
- UI Pomodoro внутри будильника
- интеграция Pomodoro с popup

Основной ownership файлов:

- `PomodoroState.cs`
- `PomodoroService.cs`

Основной ownership существующих файлов:

- [AlarmForm.cs](/W:/Projects/CURSOR/CURSORTrayApp/AlarmForm.cs:9)

Разрешенные интеграционные правки:

- [TrayAppContext.cs](/W:/Projects/CURSOR/CURSORTrayApp/TrayAppContext.cs:16)
- [PopupForm.cs](/W:/Projects/CURSOR/CURSORTrayApp/PopupForm.cs:8)
- `AppState.cs`

Ограничения:

- не создавать собственное отдельное хранилище;
- не менять storage contract без необходимости;
- не переписывать macro/hotkey subsystem;
- не менять `SettingsForm` кроме минимальных привязок, если это неизбежно.

### 3.4. Агент D: Integration, Cleanup, Documentation

Основная зона ответственности:

- cleanup мертвого кода
- приведение интеграции к консистентному состоянию
- финальная синхронизация документации
- обновление `README.md` при необходимости

Основной ownership файлов:

- [CONTEXT.md](/W:/Projects/CURSOR/CURSORTrayApp/CONTEXT.md:1)
- [IMPLEMENTATION_PLAN.md](/W:/Projects/CURSOR/CURSORTrayApp/IMPLEMENTATION_PLAN.md:1)
- [README.md](/W:/Projects/CURSOR/CURSORTrayApp/README.md:1)

Разрешенные интеграционные правки:

- любой файл проекта, но только для cleanup, а не для новой крупной функциональности

Ограничения:

- не переизобретать storage, macro subsystem или Pomodoro;
- не переписывать большие фичи вместо точечной интеграции;
- `CONTEXT.md` обновлять только после подтверждения приемки соответствующих изменений.

## 4. Разрешенные общие файлы пересечения

Следующие файлы могут затрагиваться несколькими агентами, но только минимально и осознанно:

- [TrayAppContext.cs](/W:/Projects/CURSOR/CURSORTrayApp/TrayAppContext.cs:16)
- `AppState.cs`
- [IMPLEMENTATION_PLAN.md](/W:/Projects/CURSOR/CURSORTrayApp/IMPLEMENTATION_PLAN.md:1)

Правило:

1. Каждый агент меняет в этих файлах только свой интеграционный слой.
2. Нельзя попутно делать чужой рефактор "раз уж открыл файл".
3. Любая спорная правка должна быть отражена в `IMPLEMENTATION_PLAN.md`.

## 5. Suggested File Touch Matrix

| Agent | Primary files | Allowed shared files | Should avoid |
|---|---|---|---|
| A | `AppState.cs`, `AppStateRepository.cs`, `EncryptionService.cs`, `MigrationService.cs`, `AppLog.cs` | `TrayAppContext.cs`, `AlarmForm.cs`, `SettingsForm.cs`, `IMPLEMENTATION_PLAN.md` | `GlobalHotkeyWindow.cs`, `PomodoroService.cs`, `MacroForm.cs` |
| B | `HotkeyManager.cs`, `MacroExecutionService.cs`, `MacroForm.cs`, `GlobalHotkeyWindow.cs` | `TrayAppContext.cs`, `AppState.cs`, `IMPLEMENTATION_PLAN.md` | `MigrationService.cs`, `PomodoroService.cs`, heavy edits in `AlarmForm.cs`, `SettingsForm.cs` |
| C | `PomodoroState.cs`, `PomodoroService.cs`, `AlarmForm.cs` | `TrayAppContext.cs`, `PopupForm.cs`, `AppState.cs`, `IMPLEMENTATION_PLAN.md` | `GlobalHotkeyWindow.cs`, `SettingsForm.cs`, `MigrationService.cs` |
| D | `CONTEXT.md`, `IMPLEMENTATION_PLAN.md`, `README.md` | any file for cleanup-only integration | large new feature work |

## 6. Practical Merge Order

Если изменения не мержатся автоматически, предпочтительный порядок интеграции такой:

1. Agent A
2. Agent B
3. Agent C
4. Agent D

Причина:

- B и C зависят от storage contract;
- D зависит от результата всех предыдущих.

## 7. Handover Expectations

Каждый агент в финале своей работы должен оставить:

1. Краткое summary результата.
2. Список измененных файлов.
3. Что проверить вручную.
4. Какие хвосты остались.
5. Требуется ли обновление `CONTEXT.md` после подтверждения.

## 8. Definition of Safe Parallel Work

Параллельная работа считается безопасной, если:

1. У каждого агента есть четкий ownership.
2. Общий storage contract задается централизованно.
3. Общие файлы меняются только минимально.
4. Документация обновляется по протоколу, а не хаотично.
