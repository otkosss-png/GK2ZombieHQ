# GK2 Zombie HQ / «Зомби-штаб» — дизайн

- **Дата:** 2026-09-25
- **Статус:** на ревью
- **Игра:** Graveyard Keeper 2 (Steam appid 4358690), Unity 6000.3.9f1 (Mono x64)
- **Тип:** код-мод BepInEx 5 (netstandard2.1), новый проект

## 1. Зачем

Форум GK2 завален вопросами про зомби: «где смотреть лимит зомби / PR-лимит?», «как снять
ошейник / избавиться от зомби-работника?», «зомби не работает / не могу назначить». В игре
есть только окно **одного** зомби (`UIZombieWorkerWindow`), обзорного списка и постоянного
счётчика лимита нет. Модов на зомби-систему в Workshop нет вообще.

**Цель:** мод «Зомби-штаб» — постоянный HUD «зомби N/лимит» и панель управления всеми зомби
(обзор, отзыв с работы, переход в штатное окно зомби), локализация EN/RU, всё настраивается.

## 2. Scope

**В объёме (фаза 1):** счётчик HUD; панель по горячей клавише; список всех зомби со статусом;
действия «Открыть окно зомби» и «Отозвать с работы»; конфиг (вкл/выкл HUD, клавиша, позиция,
язык); отдельная тестируемая библиотека чистой логики.

**Вне объёма (фаза 2):** «снять ошейник/освободить», «камера к зомби», смена ошейника в панели,
таланты/тех-очки (отдаём штатному окну зомби). Не трогаем баланс, не патчим логику игры.

## 3. Технические опоры (recon, проверено по Assembly-CSharp)

- Лимит: `MainGame.Instance.PlayerData.GetResInt("zombies_limit_mechanic")`.
- Текущее число: `MainGame.Instance.PlayerData.GetResInt("cur_zombies_count")`
  (ключ используется в `Flow_StartResurrection.StartResurrection`; лимит рисует `WorldZoneWidget.Redraw`).
- Все зомби: `ZombieSystemData` — поле `cache: Dictionary<Guid, ZombieWgoData>` и список
  `zombieOnSceneWgoIds`. Доступ к `ZombieSystemData` — через те же пути, что у игры (уточняется:
  синглтон/сервис; см. «Открытые вопросы»).
- Модель зомби `ZombieWgoData`: `Name`, `ZombieType` (enum: Free/Crafter/Caretaker/ConveyorCrafter/
  Worker/Porter/Gardener/ConveyorTransporter/Fighter), `WhiteSkulls`, `RedSkulls`, `Collar`, `Id`,
  `WorkerActivity`, `AttachedWgoData`, состояния `CaretakerState`/`GardenerState`/
  `ConveyorTransporterState`, таланты (`talentData`), тех-очки (`techRed/Blue/Green`).
- Открыть штатное окно (как `ZombieInteractionHandler.Interact2`):
  `LazyBearTechnology.LazyUI.GetWindow<UIZombieWorkerWindow>().Open(new UIZombieWorkerWindowData(zombie))`.
- Отозвать с работы: `ZombieSystemData.PutZombieFromGameSceneToStoreForPlayer(PlayerData, ZombieWgoData)`
  (семантика проверяется в игре).

## 4. Архитектура

Новый git-репозиторий `GK2ZombieHQ`, раскладка:

```
src/GK2ZombieHQ.Core/        netstandard2.0 — чистая логика (без Unity/игры), xUnit-тесты
src/GK2ZombieHQ/             netstandard2.1 — BepInEx-плагин, ссылки на BepInEx + UnityEngine + Assembly-CSharp
tests/GK2ZombieHQ.Core.Tests/ net8.0 + xUnit
docs/superpowers/{specs,plans}/
```

- `AssemblyName` = `GK2ZombieHQ`; GUID плагина `otkosss.gk2.zombiehq`.
- Ссылки на DLL игры: `$(GameDir)\GraveyardKeeper2_Data\Managed\Assembly-CSharp.dll`,
  `UnityEngine.CoreModule.dll`, `UnityEngine.UI.dll`, `Unity.TextMeshPro.dll`; BepInEx — из
  `$(GameDir)\BepInEx\core`. `<Private>false</Private>` (DLL не копируем). `GameDir` по умолчанию
  `E:\SteamLibrary\steamapps\common\Graveyard Keeper 2`.
- Harmony **не нужен**: HUD и панель — MonoBehaviour-компоненты, состояние читаем, действия
  вызываем через игровые API.
- Деплой для теста: `%USERPROFILE%\Documents\Klei\...` не нужен (это не ONI) — кладём в
  `$(GameDir)\BepInEx\plugins\GK2ZombieHQ\GK2ZombieHQ.dll` (или папку `mods\dev`? — нет; BepInEx
  грузит `BepInEx\plugins`). Для локального теста — прямая копия DLL.

## 5. Компоненты

- **Plugin** (`BaseUnityPlugin`) — вход, `ConfigFile` (см. §8), логгер, создаёт `DontDestroyOnLoad`
  GameObject с `ZombieHud`, хранит ссылку на панель.
- **ZombieHud** (`MonoBehaviour`) — раз в ~0.5 c читает count/limit и обновляет TMP-текст
  «Зомби: N / L» (RU) / «Zombies: N / L» (EN); при `N >= L` — красный цвет. Позиция/видимость из
  конфига; создаёт собственный `Canvas` (ScreenSpaceOverlay) при старте.
- **ZombiePanel** — по горячей клавише открывает/закрывает окно (uGUI/TMP): заголовок, счётчик,
  список строк-зомби, кнопки на строке и общие. Перестраивается при открытии и по таймеру, пока
  открыта. Пауза не обязательна (панель не модальная).
- **ZombieRoster** (обёртка над игрой) — `Load()` возвращает `List<ZombieInfo>` (снимок),
  `Recall(zombie)`, `OpenWindow(zombie)`. Все вызовы игры — в try/catch.
- **ZombieInfo** (в Core) — `Id, Name, Type, WhiteSkulls, RedSkulls, Collar, Activity, Zone,
  CanRecall`.
- **ZombieText** (в Core) — словарь EN/RU по ключам (заголовок, «Отозвать», «Открыть», типы зомби,
  статусы, ошибки). Язык: `auto|en|ru`, `auto` = русский на русской системе (как в наших модах).
- **Core** — форматирование строки счётчика, сортировка/фильтр ростера, маппинг `ZombieType` →
  текст, решение доступности действий. Без Unity — тестируется xUnit.

## 6. Поток данных и действия

1. HUD: каждый тик — `count = GetResInt("cur_zombies_count")`, `limit = GetResInt("zombies_limit_mechanic")`
   → формат → текст/цвет.
2. Панель: по клавише — `ZombieRoster.Load()` → снимок списка → отрисовка строк.
3. «Открыть окно зомби» → `LazyUI.GetWindow<UIZombieWorkerWindow>().Open(new UIZombieWorkerWindowData(zombie))`.
4. «Отозвать» → `PutZombieFromGameSceneToStoreForPlayer(PlayerData, zombie)`; после — обновить снимок.
5. Ошибки — в лог BepInEx, панель показывает «—», игра не падает.

## 7. UI

- HUD: одна строка TMP, по умолчанию слева сверху, размер/позиция — константы + офсет из конфига.
  Вкл/выкл конфигом и горячей клавишей (напр. F7 — HUD, F8 — панель).
- Панель: заголовок «Зомби-штаб», строка счётчика, скролл-список. В строке: имя, тип, черепа
  (бел/красн), ошейник, занятие/зона; кнопки «Открыть», «Отозвать» (неактивна, если нельзя).
  Кнопка закрытия + Esc. Стиль — минимальный, в духе игры (тёмная полупрозрачная панель).

## 8. Конфиг (`BepInEx\config\otkosss.gk2.zombiehq.cfg`)

- `Hud.Enabled` (bool, true), `Hud.OffsetX`/`Hud.OffsetY` (int), `Hud.FontSize` (int).
- `Keys.HudToggle` (default F7), `Keys.Panel` (default F8).
- `General.Language` (auto|en|ru, auto).
- (при необходимости) `Panel.RefreshSeconds` (float, 0.5).

## 9. Тестируемость

- `GK2ZombieHQ.Core.Tests` (net8.0, xUnit): формат счётчика (обычный/на лимите/лимит 0),
  сортировка ростера (по имени/типу/черепам), маппинг типов, доступность «Отозвать».
- E2E вручную в игре: HUD показывает N/лимит; F8 открывает панель; «Открыть» показывает штатное
  окно; «Отозвать» снимает зомби с работы; лог чистый.
- Сборка/тесты: `& "C:\Users\Проньки\dotnet-sdk\dotnet.exe" build|test -c Release` из корня репо.

## 10. Фазы

- **Фаза 1 (этот релиз, v1.0.0):** HUD + панель-список + «Открыть окно зомби» + «Отозвать»; конфиг;
  локализация; Core + тесты; публикация в Workshop (отдельный айтем).
- **Фаза 2 (v1.1.0):** «Снять ошейник/освободить» (после разбора механики), «камера к зомби»,
  опц. «сменить ошейник» через выбор из инвентаря.

## 11. Риски

- **Recall-семантика:** `PutZombieFromGameSceneToStoreForPlayer` может требовать доп. условий
  (зомби в сцене, не занят). Проверяем в игре; если нельзя — кнопку скрываем/дизейблим.
- **Доступ к `ZombieSystemData`:** нужно найти канонический путь (сервис/синглтон). Если его нет —
  перечисление через `WorldZone`/`Wgo` по типу `ZombieWgoData`. Решается на этапе реализации.
- **Текст занятия/зоны:** может не иметь готовой локализации — тогда показываем тип+состояние по
  своим строкам.
- **Снять ошейник (фаза 2):** механика неочевидна; вне фазы 1, чтобы не блокировать релиз.

## 12. Открытые вопросы (проверить в игре/коде при реализации)

1. Как получить `ZombieSystemData` (какой синглтон/сервис)?
2. Точные условия и эффект `PutZombieFromGameSceneToStoreForPlayer`.
3. Нужно ли ставить игру на паузу при открытии панели (вероятно, нет).
4. Как называется и где лежит айтем-иконка/превью для публикации (сделаем превью).
