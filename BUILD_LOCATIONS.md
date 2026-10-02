# Где находятся сборки TheAlarm

Актуальная версия: **1.3.1**. Все пути ниже указаны от корня проекта.

## Для обычного запуска и передачи на другой компьютер

| Вариант | Локальный EXE | Архив |
|---|---|---|
| Portable, Windows 64-bit (x64) | `artifacts/release/win-x64/TheAlarm.exe` | `artifacts/TheAlarm-win-x64.zip` |
| Portable, Windows 32-bit (x86) | `artifacts/release/win-x86/TheAlarm.exe` | `artifacts/TheAlarm-win-x86.zip` |

Обе версии автономные: достаточно распаковать ZIP, установка .NET не нужна. x86 означает 32 бита. Для большинства современных компьютеров выбирайте x64.

На этом компьютере корень проекта: `W:\Projects\CURSOR\CURSORTrayApp`.

Пользовательский `config.dat` создаётся рядом с запускаемым EXE. Разные папки сборок могут содержать разные настройки. При обновлении заменяйте EXE в той папке, откуда обычно запускаете программу; свой `config.dat` сохраните. DPAPI привязывает его к пользователю Windows.

## Для разработки

`bin/Release/net8.0-windows/TheAlarm.exe` — обычная Release-сборка. Для запуска нужны файлы из этой папки и установленный .NET Desktop Runtime 8. Это не самостоятельный переносимый EXE.

`bin/Debug/` и `tests/Smoke/bin/` — отладочные сборки и тесты.

## Старые локальные артефакты

Следующие папки содержат предыдущие или экспериментальные сборки и не обновляются при текущей публикации:

- `artifacts/release/publish/`
- `artifacts/portable/`
- `artifacts/publish/`

Для актуальной портативной версии используйте только `artifacts/release/win-x64/` или `artifacts/release/win-x86/`.

## GitHub

- [Portable x64 1.3.1](https://github.com/SputnikUpitera/theAlarm/releases/download/v1.3.1/TheAlarm-win-x64.zip)
- [Portable x86 1.3.1](https://github.com/SputnikUpitera/theAlarm/releases/download/v1.3.1/TheAlarm-win-x86.zip)
- [Все релизы](https://github.com/SputnikUpitera/theAlarm/releases)

Теги `v*` запускают GitHub Actions: он собирает обе архитектуры, упаковывает только EXE и публикует ZIP. Бинарные файлы и пользовательские конфиги не хранятся в Git. Локальные портативные папки обновляются отдельной сборкой или распаковкой соответствующего релизного ZIP.
