# Помощник Dota 2 — V0.5

Windows-приложение (C# / WPF):

- **V0:** захват экрана DXGI, оверлей ↔ окно, прозрачность, горячая клавиша, ToS.
- **V0.5:** ROI + template matching — распознать **героя** и **1–2 предмета** на тестовой картинке HUD.

Без LLM, без инжекта в игру, без управления Dota.

> WinUI 3 в плане предпочтителен; здесь используется **WPF** (разрешённый fallback).

## Требования

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Dota 2 в режиме **Borderless Windowed** (для живого захвата; тесты распознавания игру не требуют)

## Сборка и запуск приложения

Откройте PowerShell и скопируйте команды по очереди:

```powershell
cd dota-assistant
dotnet restore DotaAssistant.sln
dotnet build DotaAssistant.sln -c Release
dotnet run --project src\DotaAssistant.App -c Release
```

В окне приложения нажмите **«Тест на фикстуре»** — должно показать героя **Pudge** и предметы **Blink Dagger**, **Bottle**.

## Тесты распознавания (без игры)

Скопируйте в PowerShell:

```powershell
cd dota-assistant
dotnet test tests\DotaAssistant.Vision.Tests -c Release
```

Ожидается: все тесты зелёные (герой `pudge`, предметы `blink_dagger` и `bottle` на `assets/fixtures/match_hud_1920x1080.png`).

Пересобрать синтетические иконки/фикстуру (нужен Python 3, без доп. пакетов):

```powershell
python tools\generate_fixtures.py
```

## Что умеет сейчас

| Функция | Статус |
|---------|--------|
| DXGI Desktop Duplication | Да (+ stub, если DXGI недоступен) |
| Оверлей ↔ окно, opacity, hotkey | Да (`Ctrl+Shift+H`) |
| ROI топ-бар + инвентарь | Да (синтетический Full HD layout) |
| Template matching (ZNCC) | Да |
| Тест на фикстуре / `dotnet test` | Да |
| Живое распознавание с DXGI-кадра | Позже |
| Советы по предметам / mid-матчапам | V1-alpha |

## Структура

```
dota-assistant/
  DotaAssistant.sln
  assets/templates/     # иконки героев/предметов + catalog.json
  assets/fixtures/      # тестовый HUD 1920x1080
  src/DotaAssistant.App/
  src/DotaAssistant.Capture/
  src/DotaAssistant.Vision/
  tests/DotaAssistant.Vision.Tests/
  tools/generate_fixtures.py
```

## Безопасность

- Только чтение рабочего стола / локальных PNG
- Нет ввода в игру, инжекта, чтения памяти Dota
- Сторонний оверлей — на свой риск; соблюдайте ToS Valve
