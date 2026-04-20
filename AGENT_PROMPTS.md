# TheAlarm: Agent Prompts

Этот файл содержит готовые промты для параллельной работы по проекту.
Каждый промт обязан ссылаться на:

- [CONTEXT.md](/W:/Projects/CURSOR/CURSORTrayApp/CONTEXT.md:1)
- [IMPLEMENTATION_PLAN.md](/W:/Projects/CURSOR/CURSORTrayApp/IMPLEMENTATION_PLAN.md:1)
- [TASK_SPLIT.md](/W:/Projects/CURSOR/CURSORTrayApp/TASK_SPLIT.md:1)

Общее правило для всех агентов:

1. Сначала прочитай `CONTEXT.md`, `IMPLEMENTATION_PLAN.md` и `TASK_SPLIT.md`.
2. Перед началом правок обнови `IMPLEMENTATION_PLAN.md`:
   - выстави своему блоку статус `In Progress`;
   - допиши краткий план своей реализации;
   - перечисли файлы, которые собираешься менять в рамках своего ownership из `TASK_SPLIT.md`.
3. После завершения реализации и локальной проверки:
   - обнови `IMPLEMENTATION_PLAN.md`;
   - поменяй статус на `Done` или `Partial`;
   - кратко перечисли фактические изменения, риски и хвосты.
4. `CONTEXT.md` не обновляй сразу после кода.
5. `CONTEXT.md` обновляй только после подтверждения, что работа по задаче принята и интегрирована.
6. Не выходи за границы ownership из `TASK_SPLIT.md`, кроме минимальных интеграционных правок.

## Prompt 1. Foundation + Encryption + Migration

```text
Ты работаешь над проектом TheAlarm. Перед началом обязательно прочитай:
- W:\Projects\CURSOR\CURSORTrayApp\CONTEXT.md
- W:\Projects\CURSOR\CURSORTrayApp\IMPLEMENTATION_PLAN.md
- W:\Projects\CURSOR\CURSORTrayApp\TASK_SPLIT.md

Твоя зона ответственности:
- foundation and storage refactor
- encrypted config
- migration со старого plaintext config
- базовая персистентность alarm-ов

Твой ownership определяется TASK_SPLIT.md.
Работай только в рамках зоны Agent A и не забирай задачи Agent B, Agent C или Agent D.

Что нужно сделать:
1. Перед кодом обнови IMPLEMENTATION_PLAN.md:
   - отметь этапы Foundation and storage refactor и Encrypted config and migration как In Progress;
   - кратко уточни подзадачи;
   - перечисли файлы, которые собираешься менять или добавлять в рамках ownership Agent A.
2. Реализуй единое состояние приложения и repository/service для его чтения и записи.
3. Добавь шифрование конфигов на базе DPAPI CurrentUser.
4. Реализуй миграцию со старого config.json.
5. Добавь реальную персистентность alarm-ов, потому что сейчас они in-memory.
6. Сохрани совместимость с текущим поведением process rules.
7. После завершения обнови IMPLEMENTATION_PLAN.md:
   - укажи, что реально сделано;
   - зафиксируй статус Done или Partial;
   - перечисли остаточные риски и непроверенные кейсы.

Ограничения:
- не придумывай собственное шифрование и не храни ключ в коде;
- не делай shell-based хранение или внешние батники;
- не делай UI для пользовательских hotkey;
- не реализуй Pomodoro;
- не обновляй CONTEXT.md сразу после реализации;
- CONTEXT.md обновляется только после подтверждения, что работа принята.
- если нужен touch чужого файла, ограничься минимальной интеграцией и зафиксируй это в IMPLEMENTATION_PLAN.md.

В финальном сообщении:
- кратко опиши изменения;
- перечисли измененные файлы;
- укажи, что именно нужно проверить вручную.
```

## Prompt 2. Hotkeys + Command Engine

```text
Ты работаешь над проектом TheAlarm. Перед началом обязательно прочитай:
- W:\Projects\CURSOR\CURSORTrayApp\CONTEXT.md
- W:\Projects\CURSOR\CURSORTrayApp\IMPLEMENTATION_PLAN.md
- W:\Projects\CURSOR\CURSORTrayApp\TASK_SPLIT.md

Твоя зона ответственности:
- пользовательские hotkey
- UI для hotkey
- менеджер регистрации нескольких global hotkeys
- встроенный command engine без внешних bat-файлов

Твой ownership определяется TASK_SPLIT.md.
Работай только в рамках зоны Agent B и не лезь в зону Agent A, Agent C или Agent D кроме разрешенных интеграционных файлов.

Что нужно сделать:
1. Перед кодом обнови IMPLEMENTATION_PLAN.md:
   - отметь этап User hotkeys and command engine как In Progress;
   - уточни подзадачи;
   - перечисли файлы, которые планируешь изменить в рамках ownership Agent B.
2. Спроектируй модель hotkey и менеджер регистрации нескольких сочетаний клавиш.
3. Перепиши или расширь текущий GlobalHotkeyWindow так, чтобы поддерживалось несколько hotkey.
4. Добавь в интерфейс настройку hotkey:
   - создание;
   - редактирование;
   - удаление;
   - включение и отключение;
   - ввод комбинации прямо в UI.
5. Реализуй встроенный command engine:
   - без внешних bat-файлов;
   - с валидацией;
   - с понятным списком поддерживаемых команд.
6. Подключи хранение hotkey к общей модели хранения из плана.
7. После завершения обнови IMPLEMENTATION_PLAN.md:
   - что сделано по факту;
   - статус Done или Partial;
   - риски, ограничения, непроверенные сценарии.

Ограничения:
- не исполняй произвольный shell как основную модель;
- не создавай зависимость от внешних скриптов;
- не ломай существующий tray behavior;
- не создавай отдельный формат хранения hotkey мимо общего state/repository;
- не меняй storage contract без явной необходимости;
- не реализуй Pomodoro;
- не обновляй CONTEXT.md до подтверждения приемки задачи.
- если нужен touch чужого файла, ограничься минимальной интеграцией и зафиксируй это в IMPLEMENTATION_PLAN.md.

В финальном сообщении:
- кратко опиши модель hotkey;
- перечисли поддержанные команды;
- перечисли измененные файлы;
- укажи ручные сценарии проверки.
```

## Prompt 3. Pomodoro Tracker

```text
Ты работаешь над проектом TheAlarm. Перед началом обязательно прочитай:
- W:\Projects\CURSOR\CURSORTrayApp\CONTEXT.md
- W:\Projects\CURSOR\CURSORTrayApp\IMPLEMENTATION_PLAN.md
- W:\Projects\CURSOR\CURSORTrayApp\TASK_SPLIT.md

Твоя зона ответственности:
- Pomodoro tracker внутри будильника
- сервис состояния Pomodoro
- UI интеграция в AlarmForm

Твой ownership определяется TASK_SPLIT.md.
Работай только в рамках зоны Agent C и не лезь в зону Agent A, Agent B или Agent D кроме разрешенных интеграционных файлов.

Что нужно сделать:
1. Перед кодом обнови IMPLEMENTATION_PLAN.md:
   - отметь этап Pomodoro tracker как In Progress;
   - уточни подзадачи;
   - перечисли файлы, которые будешь менять в рамках ownership Agent C.
2. Спроектируй PomodoroSettings и PomodoroState.
3. Реализуй PomodoroService:
   - start;
   - pause;
   - resume;
   - skip;
   - reset;
   - переходы между фазами.
4. Встрой Pomodoro в AlarmForm как часть текущего UI, а не как отдельную форму.
5. Используй существующий popup механизм для уведомлений о завершении фаз.
6. Подключи хранение настроек и состояния Pomodoro к общей модели хранения из плана.
7. После завершения обнови IMPLEMENTATION_PLAN.md:
   - что сделано;
   - статус Done или Partial;
   - остаточные риски и хвосты.

Ограничения:
- не смешивай Pomodoro и список alarm-ов в одну и ту же внутреннюю коллекцию;
- не создавай отдельный способ хранения state мимо общего repository;
- не меняй storage contract без явной необходимости;
- не переписывай hotkey subsystem;
- не обновляй CONTEXT.md до подтверждения приемки задачи.
- если нужен touch чужого файла, ограничься минимальной интеграцией и зафиксируй это в IMPLEMENTATION_PLAN.md.

В финальном сообщении:
- кратко опиши UX Pomodoro;
- перечисли измененные файлы;
- укажи, что проверить вручную.
```

## Prompt 4. Integration Cleanup + Doc Sync

```text
Ты работаешь над проектом TheAlarm. Перед началом обязательно прочитай:
- W:\Projects\CURSOR\CURSORTrayApp\CONTEXT.md
- W:\Projects\CURSOR\CURSORTrayApp\IMPLEMENTATION_PLAN.md
- W:\Projects\CURSOR\CURSORTrayApp\TASK_SPLIT.md

Твоя зона ответственности:
- интеграционная зачистка
- удаление мертвых частей старой архитектуры
- финальная синхронизация документации

Твой ownership определяется TASK_SPLIT.md.
Работай только в рамках зоны Agent D и не переписывай большие части фич других агентов.

Что нужно сделать:
1. Перед кодом обнови IMPLEMENTATION_PLAN.md:
   - отметь этап Integration, stabilization and documentation sync как In Progress;
   - перечисли файлы и cleanup-задачи.
2. Проверь, какие старые элементы архитектуры уже устарели после внедрения новых функций.
3. Удали или адаптируй мертвые поля, события и старые ветки поведения.
4. Приведи документацию в актуальное состояние.
5. После завершения реализации и проверки обнови IMPLEMENTATION_PLAN.md:
   - что сделано;
   - статус Done или Partial;
   - остаточные риски.
6. Только после подтверждения, что изменения приняты, обнови CONTEXT.md:
   - структуру;
   - принципы;
   - known issues;
   - file map;
   - runtime behavior.

Ограничения:
- не переписывай заново уже работающие подсистемы без необходимости;
- не обновляй CONTEXT.md до подтверждения приемки;
- не оставляй документацию в противоречии с кодом.
- не перехватывай ownership Agent A, B или C под видом cleanup.

В финальном сообщении:
- перечисли cleanup-пункты;
- перечисли измененные файлы;
- отдельно укажи, обновлялся ли уже CONTEXT.md или он еще ждет подтверждения.
```

## Prompt 5. Универсальный промт для любого агента

```text
Работаешь в репозитории TheAlarm.

Перед началом обязательно прочитай:
- W:\Projects\CURSOR\CURSORTrayApp\CONTEXT.md
- W:\Projects\CURSOR\CURSORTrayApp\IMPLEMENTATION_PLAN.md
- W:\Projects\CURSOR\CURSORTrayApp\TASK_SPLIT.md

Правила работы:
1. До внесения кода обнови IMPLEMENTATION_PLAN.md:
   - пометь свой этап как In Progress;
   - уточни подзадачи;
   - перечисли файлы, которые меняешь в рамках своего ownership из TASK_SPLIT.md.
2. Выполняй только свою зону ответственности и не ломай общий storage/state контракт.
3. После завершения реализации и локальной проверки снова обнови IMPLEMENTATION_PLAN.md:
   - переведи статус в Done или Partial;
   - перечисли сделанные изменения;
   - укажи риски, хвосты и ручные проверки.
4. CONTEXT.md не меняй сразу после реализации.
5. CONTEXT.md обновляй только после подтверждения, что задача принята.
6. Если без touch чужого файла не обойтись, делай только минимальную интеграционную правку и отмечай это в IMPLEMENTATION_PLAN.md.

В финальном ответе обязательно:
- кратко опиши результат;
- перечисли измененные файлы;
- перечисли ручные проверки;
- укажи, требуется ли последующее обновление CONTEXT.md после подтверждения.
```
