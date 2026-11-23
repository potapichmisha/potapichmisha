# Помощник Dota 2 — V0 каркас

Windows-приложение (C# / WPF): захват экрана через **DXGI Desktop Duplication**, переключение **оверлей ↔ отдельное окно**, слайдер прозрачности, горячая клавиша скрыть/показать, предупреждение ToS.

Vision / советы по предметам и матчапам — **не в V0** (только заглушка пайплайна с FPS и временем последнего кадра).

> WinUI 3 выбран в плане как предпочтение; в этом репозитории V0 собран на **WPF** (разрешённый fallback), чтобы каркас собирался без Windows App SDK / Visual Studio Packaging.

## Требования

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Dota 2 в режиме **Borderless Windowed** (рекомендуется для стабильного DXGI-захвата)

## Сборка и запуск

```powershell
cd dota-assistant
dotnet restore DotaAssistant.sln
dotnet build DotaAssistant.sln -c Release
dotnet run --project src\DotaAssistant.App -c Release
```

Либо откройте `DotaAssistant.sln` в Visual Studio 2022 и запустите проект `DotaAssistant.App`.

## Что умеет V0

| Функция | Статус |
|---------|--------|
| DXGI Desktop Duplication | Да (с fallback-заглушкой, если DXGI недоступен) |
| Показ FPS / времени последнего кадра | Да |
| Оверлей ↔ отдельное окно | Да |
| Слайдер opacity | Да |
| Hotkey скрыть/показать (`Ctrl+Shift+H`) | Да |
| Клик-through в оверлее | Да (опционально) |
| ToS-дисклеймер (RU) | Да |
| Vision / AI / советы | Нет (позже) |

## Ограничения безопасности

- Только чтение рабочего стола
- Нет ввода в игру, инжекта, чтения памяти процесса Dota
- Сторонний оверлей — на свой риск; соблюдайте ToS Valve

## Структура

```
dota-assistant/
  DotaAssistant.sln
  src/DotaAssistant.App/       # WPF UI (русский)
  src/DotaAssistant.Capture/   # DXGI + stub
```
