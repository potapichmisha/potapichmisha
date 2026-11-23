# Помощник Dota 2 — V1 (статус-панель)

Windows-приложение (C# / WPF) для новичков:

- **V0:** захват экрана DXGI, оверлей ↔ окно, прозрачность, горячая клавиша, ToS.
- **V0.5:** ROI + template matching — герой и предметы на тестовой картинке HUD.
- **V1-alpha:** mid-советы из локального JSON — следующие предметы и матчап (без LLM).
- **V1:** упорядоченная **статус-панель** (ваш герой → союзники → противники → предметы → совет → уверенность) + живое распознавание с DXGI-кадра, когда пиксели доступны.

Без LLM, без инжекта в игру, без управления Dota. Только mid.

> WinUI 3 в плане предпочтителен; здесь используется **WPF** (разрешённый fallback).

## Для новичка: как проверить, что распознавание работает (Windows)

1. Установите [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
2. Откройте PowerShell в папке репозитория и выполните:

```powershell
cd dota-assistant
dotnet restore DotaAssistant.sln
dotnet build DotaAssistant.sln -c Release
dotnet run --project src\DotaAssistant.App -c Release
```

3. В окне нажмите **«Тест на фикстуре»**.
4. В блоке **«Статус распознавания»** сверху вниз должны появиться: **Ваш герой (Pudge)**, союзники, противники, предметы, совет/матчап и строка **Уверенность** (не «не уверен»).

Игра Dota для этого демо не нужна. Для живого захвата включите **«Живое распознавание»** и запустите Dota в режиме **Borderless Windowed** — смотрите те же строки статуса и «Пиксели для vision».

## Тесты

```powershell
cd dota-assistant
dotnet test DotaAssistant.sln -c Release
```

Пересобрать синтетические иконки/фикстуру (Python 3, без доп. пакетов):

```powershell
python tools\generate_fixtures.py
```

## Что умеет сейчас

| Функция | Статус |
|---------|--------|
| DXGI Desktop Duplication + копия пикселей | Да (+ stub, если DXGI недоступен) |
| Оверлей ↔ окно, opacity, hotkey | Да (`Ctrl+Shift+H`) |
| Статус-панель (герой/союзники/противники/предметы/совет/уверенность) | Да |
| ROI топ-бар + инвентарь | Да (синтетический Full HD layout) |
| Template matching (ZNCC) + «не уверен» | Да |
| Mid-сборки и матчап (JSON) | Да |
| Тест на фикстуре | Да |
| Живое распознавание с DXGI | Да (когда есть пиксели) |
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
