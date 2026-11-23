# Тестовые фикстуры V0.5

`match_hud_1920x1080.png` — **синтетический** кадр 1920×1080: иконка героя в топ-баре и два предмета в инвентаре.

Это не скриншот реальной Dota 2. Нужен, чтобы проверить ROI + template matching без игры.

Пересобрать иконки и фикстуру:

```powershell
python tools\generate_fixtures.py
```

Ожидаемый результат: `match_hud_expected.json` (герой `pudge`, предметы `blink_dagger` и `bottle`).
