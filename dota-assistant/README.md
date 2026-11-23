# Помощник Dota 2 — V1-alpha

Windows-приложение (C# / WPF) для новичков:

- **V0:** захват экрана DXGI, оверлей ↔ окно, прозрачность, горячая клавиша, ToS.
- **V0.5:** ROI + template matching — герой и 1–2 предмета на тестовой картинке HUD.
- **V1-alpha:** mid-советы из локального JSON — **1–3 следующих предмета** и фраза по **агрессии матчапа** (шаблоны на русском, без LLM).

Без LLM, без инжекта в игру, без управления Dota. Только mid.

> WinUI 3 в плане предпочтителен; здесь используется **WPF** (разрешённый fallback).

## Для новичка: как посмотреть советы (Windows)

1. Установите [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
2. Откройте PowerShell в папке репозитория и выполните:

```powershell
cd dota-assistant
dotnet restore DotaAssistant.sln
dotnet build DotaAssistant.sln -c Release
dotnet run --project src\DotaAssistant.App -c Release
```

3. В окне нажмите **«Тест на фикстуре + советы»**.
4. Должны появиться: герой **Pudge**, предметы **Blink** + **Bottle**, затем блок советов mid — матчап против **Invoker** и список из **1–3 следующих предметов** (например Boots, Magic Wand, Force Staff).

Игра Dota для этого демо не нужна. Для живого захвата экрана позже используйте Dota в режиме **Borderless Windowed**.

## Тесты

```powershell
cd dota-assistant
dotnet test DotaAssistant.sln -c Release
```

Ожидается: зелёные тесты Vision (герой/предметы на фикстуре) и Tips (сборка + матчап mid из `assets/tips/rules.json`).

Пересобрать синтетические иконки/фикстуру (Python 3, без доп. пакетов):

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
| Mid-сборки (JSON) | Да (стартовый набор героев) |
| Mid-матчап (агрессия) | Да (шаблонные RU-фразы) |
| Тест на фикстуре + советы | Да |
| Живое распознавание с DXGI-кадра | Позже |
| LLM / другие линии | После MVP |

## Структура

```
dota-assistant/
  DotaAssistant.sln
  assets/templates/     # иконки героев/предметов + catalog.json
  assets/fixtures/      # тестовый HUD 1920x1080
  assets/tips/          # rules.json — билды mid + матчапы + фразы
  src/DotaAssistant.App/
  src/DotaAssistant.Capture/
  src/DotaAssistant.Vision/
  src/DotaAssistant.Tips/
  tests/...
  tools/generate_fixtures.py
```

## Безопасность

- Только чтение рабочего стола / локальных PNG и JSON
- Нет ввода в игру, инжекта, чтения памяти Dota
- Сторонний оверлей — на свой риск; соблюдайте ToS Valve
