# GK2 Zombie HQ (фаза 1) — план реализации

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Плагин BepInEx `GK2ZombieHQ` — постоянный HUD «Зомби: N / лимит» и панель по горячей клавише со списком всех зомби и действиями «Открыть окно зомби» и «Отозвать с работы», EN/RU, конфиг.

**Architecture:** Чистая логика — в отдельном проекте `GK2ZombieHQ.Core` (netstandard2.0, xUnit). Плагин `GK2ZombieHQ` (netstandard2.1) линкует исходники Core `<Compile Include>` (одна DLL на выходе) и вызывает игровые API (`MainGame.Instance`, `ZombieSystemData`, `LazyUI`, `UIZombieWorkerWindow`). Harmony не используется.

**Tech Stack:** C# 9, .NET Standard 2.0/2.1, BepInEx 5.4.23.5, UnityEngine (uGUI + TextMeshPro), xUnit (net8.0), Mono/Unity 6000.3.9f1.

**Spec:** `docs/superpowers/specs/2026-09-25-gk2-zombie-hq-design.md`

## Global Constraints

- `GK2ZombieHQ.Core` — netstandard2.0, `LangVersion 9.0`, **без ссылок на Unity/BepInEx**.
- Плагин `GK2ZombieHQ` — netstandard2.1, `RootNamespace GK2ZombieHQ`; исходники Core линкуются `<Compile Include="..\GK2ZombieHQ.Core\*.cs" LinkBase="Core" />` (Core.dll не нужна).
- Ссылки на игру/BepInEx — с `<Private>false</Private>`; `GameDir` по умолчанию `E:\SteamLibrary\steamapps\common\Graveyard Keeper 2`.
- GUID плагина — `otkosss.gk2.zombiehq`; имя файла — `GK2ZombieHQ.dll`.
- Пользовательские тексты — только через `ZombieText` (EN/RU). Файлы UTF-8 без BOM.
- Строки ресурсов игры: `cur_zombies_count`, `zombies_limit_mechanic` (через `PlayerData.GetResInt`).
- Все вызовы игровых API — в `try/catch`, лог через `Plugin.Log`; панель и HUD не должны ронять игру.
- Сборка/тесты: `& "C:\Users\Проньки\dotnet-sdk\dotnet.exe" build|test -c Release` из корня репо `C:\Users\Проньки\Documents\OpenCode\GK2ZombieHQ`.
- Не менять логику игры, не делать Harmony-патчей, не трогать баланс.

## File Structure

**Создать**
- `GK2ZombieHQ.sln`
- `src/GK2ZombieHQ.Core/GK2ZombieHQ.Core.csproj`, `ZombieLanguage.cs`, `ZombieKind.cs`, `ZombieInfo.cs`, `HudFormat.cs`, `ZombieText.cs`, `RosterLogic.cs`
- `src/GK2ZombieHQ/GK2ZombieHQ.csproj`, `Plugin.cs`, `ZombieRoster.cs`, `ZombieHud.cs`, `ZombiePanel.cs`
- `tests/GK2ZombieHQ.Core.Tests/GK2ZombieHQ.Core.Tests.csproj`, `HudFormatTests.cs`, `ZombieTextTests.cs`, `RosterLogicTests.cs`
- `README.md`, `mod_info`-подобных файлов нет (BepInEx); `preview.png` (в Task 9)

**Изменить**
- — (новый репозиторий)

---

### Task 1: Каркас решения + Core-формат счётчика

**Files:**
- Create: `GK2ZombieHQ.sln`, `src/GK2ZombieHQ.Core/GK2ZombieHQ.Core.csproj`, `src/GK2ZombieHQ.Core/ZombieLanguage.cs`, `src/GK2ZombieHQ.Core/HudFormat.cs`
- Create: `tests/GK2ZombieHQ.Core.Tests/GK2ZombieHQ.Core.Tests.csproj`, `tests/GK2ZombieHQ.Core.Tests/HudFormatTests.cs`

**Interfaces:**
- Produces: `enum ZombieLanguage { En, Ru }`; `static string HudFormat.Count(ZombieLanguage lang, int count, int limit)`; `static bool HudFormat.AtLimit(int count, int limit)`.

- [ ] **Step 1: Создать проекты**

```powershell
$d="C:\Users\Проньки\Documents\OpenCode\GK2ZombieHQ"
& "C:\Users\Проньки\dotnet-sdk\dotnet.exe" new sln -n GK2ZombieHQ -o $d
& "C:\Users\Проньки\dotnet-sdk\dotnet.exe" new classlib -n GK2ZombieHQ.Core -o "$d\src\GK2ZombieHQ.Core"
& "C:\Users\Проньки\dotnet-sdk\dotnet.exe" new xunit -n GK2ZombieHQ.Core.Tests -o "$d\tests\GK2ZombieHQ.Core.Tests"
& "C:\Users\Проньки\dotnet-sdk\dotnet.exe" sln "$d\GK2ZombieHQ.sln" add "$d\src\GK2ZombieHQ.Core\GK2ZombieHQ.Core.csproj" "$d\tests\GK2ZombieHQ.Core.Tests\GK2ZombieHQ.Core.Tests.csproj"
Remove-Item "$d\src\GK2ZombieHQ.Core\Class1.cs","$d\tests\GK2ZombieHQ.Core.Tests\UnitTest1.cs" -ErrorAction SilentlyContinue
```
Заменить `src/GK2ZombieHQ.Core/GK2ZombieHQ.Core.csproj` целиком:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <Nullable>disable</Nullable>
    <AssemblyName>GK2ZombieHQ.Core</AssemblyName>
    <RootNamespace>GK2ZombieHQ.Core</RootNamespace>
  </PropertyGroup>
</Project>
```
Добавить в `tests/GK2ZombieHQ.Core.Tests/GK2ZombieHQ.Core.Tests.csproj` (в существующий `<ItemGroup>`):
```xml
    <ProjectReference Include="..\..\src\GK2ZombieHQ.Core\GK2ZombieHQ.Core.csproj" />
```

- [ ] **Step 2: Написать падающий тест**

`tests/GK2ZombieHQ.Core.Tests/HudFormatTests.cs`:
```csharp
using GK2ZombieHQ.Core;
using Xunit;

namespace GK2ZombieHQ.Core.Tests
{
    public class HudFormatTests
    {
        [Fact] public void En_counts_with_limit() => Assert.Equal("Zombies: 7 / 10", HudFormat.Count(ZombieLanguage.En, 7, 10));
        [Fact] public void Ru_counts_with_limit() => Assert.Equal("Зомби: 7 / 10", HudFormat.Count(ZombieLanguage.Ru, 7, 10));
        [Fact] public void No_limit_shows_only_count() => Assert.Equal("Zombies: 3", HudFormat.Count(ZombieLanguage.En, 3, 0));
        [Fact] public void Negative_count_clamped() => Assert.Equal("Zombies: 0 / 5", HudFormat.Count(ZombieLanguage.En, -2, 5));
        [Fact] public void AtLimit_true_at_or_above() { Assert.True(HudFormat.AtLimit(10, 10)); Assert.True(HudFormat.AtLimit(11, 10)); Assert.False(HudFormat.AtLimit(9, 10)); }
    }
}
```

- [ ] **Step 3: Запустить — падает**

Run: `& "C:\Users\Проньки\dotnet-sdk\dotnet.exe" test -c Release --filter "FullyQualifiedName~HudFormatTests"`
Expected: ошибка компиляции — нет `HudFormat`.

- [ ] **Step 4: Реализовать**

`src/GK2ZombieHQ.Core/ZombieLanguage.cs`:
```csharp
namespace GK2ZombieHQ.Core
{
    public enum ZombieLanguage { En, Ru }
}
```
`src/GK2ZombieHQ.Core/HudFormat.cs`:
```csharp
namespace GK2ZombieHQ.Core
{
    public static class HudFormat
    {
        public static string Count(ZombieLanguage lang, int count, int limit)
        {
            if (count < 0) count = 0;
            var label = lang == ZombieLanguage.Ru ? "Зомби" : "Zombies";
            if (limit <= 0) return label + ": " + count;
            return label + ": " + count + " / " + limit;
        }

        public static bool AtLimit(int count, int limit) => limit > 0 && count >= limit;
    }
}
```

- [ ] **Step 5: Запустить — проходят**

Run: `& "C:\Users\Проньки\dotnet-sdk\dotnet.exe" test -c Release --filter "FullyQualifiedName~HudFormatTests"`
Expected: `Passed! - Failed: 0, Passed: 5`.

- [ ] **Step 6: Коммит**

```bash
git add GK2ZombieHQ.sln src/GK2ZombieHQ.Core tests/GK2ZombieHQ.Core.Tests
git commit -m "feat(core): solution scaffold and zombie count formatter"
```

---

### Task 2: Core — модель зомби, типы и локализация

**Files:**
- Create: `src/GK2ZombieHQ.Core/ZombieKind.cs`, `src/GK2ZombieHQ.Core/ZombieInfo.cs`, `src/GK2ZombieHQ.Core/ZombieText.cs`
- Test: `tests/GK2ZombieHQ.Core.Tests/ZombieTextTests.cs`

**Interfaces:**
- Consumes: `ZombieLanguage` (Task 1).
- Produces: `enum ZombieKind { Unknown, Free, Crafter, Caretaker, ConveyorCrafter, Worker, Porter, Gardener, ConveyorTransporter, Fighter }`; `sealed class ZombieInfo { string Id, Name; ZombieKind Kind; int WhiteSkulls, RedSkulls; string Collar, Activity, Zone; bool CanRecall; }`; `static class ZombieText { static ZombieLanguage Language; static string Get(string key); static string KindName(ZombieKind kind); }`.

- [ ] **Step 1: Написать падающий тест**

`tests/GK2ZombieHQ.Core.Tests/ZombieTextTests.cs`:
```csharp
using GK2ZombieHQ.Core;
using Xunit;

namespace GK2ZombieHQ.Core.Tests
{
    public class ZombieTextTests
    {
        public ZombieTextTests() { ZombieText.Language = ZombieLanguage.En; }

        [Fact] public void Title_differs_by_language()
        {
            ZombieText.Language = ZombieLanguage.En;
            Assert.Equal("Zombie HQ", ZombieText.Get("Title"));
            ZombieText.Language = ZombieLanguage.Ru;
            Assert.Equal("Зомби-штаб", ZombieText.Get("Title"));
            ZombieText.Language = ZombieLanguage.En;
        }

        [Fact] public void Kind_names_are_localized()
        {
            Assert.Equal("Gardener", ZombieText.KindName(ZombieKind.Gardener));
            ZombieText.Language = ZombieLanguage.Ru;
            Assert.Equal("Садовник", ZombieText.KindName(ZombieKind.Gardener));
            ZombieText.Language = ZombieLanguage.En;
        }

        [Fact] public void Unknown_key_throws() => Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => ZombieText.Get("Nope"));
    }
}
```

- [ ] **Step 2: Запустить — падает**

Run: `& "C:\Users\Проньки\dotnet-sdk\dotnet.exe" test -c Release --filter "FullyQualifiedName~ZombieTextTests"`
Expected: ошибка компиляции — нет `ZombieText`/`ZombieKind`.

- [ ] **Step 3: Реализовать**

`src/GK2ZombieHQ.Core/ZombieKind.cs`:
```csharp
namespace GK2ZombieHQ.Core
{
    public enum ZombieKind { Unknown, Free, Crafter, Caretaker, ConveyorCrafter, Worker, Porter, Gardener, ConveyorTransporter, Fighter }
}
```
`src/GK2ZombieHQ.Core/ZombieInfo.cs`:
```csharp
namespace GK2ZombieHQ.Core
{
    public sealed class ZombieInfo
    {
        public string Id;
        public string Name;
        public ZombieKind Kind;
        public int WhiteSkulls;
        public int RedSkulls;
        public string Collar;
        public string Activity;
        public string Zone;
        public bool CanRecall;
    }
}
```
`src/GK2ZombieHQ.Core/ZombieText.cs`:
```csharp
using System;
using System.Collections.Generic;

namespace GK2ZombieHQ.Core
{
    public static class ZombieText
    {
        public static ZombieLanguage Language { get; set; } = ZombieLanguage.En;

        private static readonly Dictionary<string, (string En, string Ru)> Table =
            new Dictionary<string, (string, string)>(StringComparer.Ordinal)
        {
            ["Title"] = ("Zombie HQ", "Зомби-штаб"),
            ["Open"] = ("Open", "Открыть"),
            ["Recall"] = ("Recall", "Отозвать"),
            ["Close"] = ("Close", "Закрыть"),
            ["NoZombies"] = ("No zombies", "Зомби нет"),
            ["Free"] = ("free", "свободен"),
            ["Error"] = ("Error", "Ошибка"),
        };

        private static readonly Dictionary<ZombieKind, (string En, string Ru)> Kinds =
            new Dictionary<ZombieKind, (string, string)>
        {
            [ZombieKind.Unknown] = ("Unknown", "Неизвестно"),
            [ZombieKind.Free] = ("Free", "Свободный"),
            [ZombieKind.Crafter] = ("Crafter", "Ремесленник"),
            [ZombieKind.Caretaker] = ("Caretaker", "Смотритель"),
            [ZombieKind.ConveyorCrafter] = ("Conveyor crafter", "Конвейерщик"),
            [ZombieKind.Worker] = ("Worker", "Рабочий"),
            [ZombieKind.Porter] = ("Porter", "Носильщик"),
            [ZombieKind.Gardener] = ("Gardener", "Садовник"),
            [ZombieKind.ConveyorTransporter] = ("Transporter", "Транспортёр"),
            [ZombieKind.Fighter] = ("Fighter", "Боец"),
        };

        public static string Get(string key)
        {
            if (key == null || !Table.TryGetValue(key, out var v))
                throw new KeyNotFoundException("ZombieText key not found: " + (key ?? "<null>"));
            return Language == ZombieLanguage.Ru ? v.Ru : v.En;
        }

        public static string KindName(ZombieKind kind)
        {
            if (!Kinds.TryGetValue(kind, out var v)) v = Kinds[ZombieKind.Unknown];
            return Language == ZombieLanguage.Ru ? v.Ru : v.En;
        }
    }
}
```

- [ ] **Step 4: Запустить — проходят**

Run: `& "C:\Users\Проньки\dotnet-sdk\dotnet.exe" test -c Release --filter "FullyQualifiedName~ZombieTextTests"`
Expected: `Passed! - Failed: 0, Passed: 3`.

- [ ] **Step 5: Коммит**

```bash
git add src/GK2ZombieHQ.Core tests/GK2ZombieHQ.Core.Tests
git commit -m "feat(core): zombie model, kind enum and EN/RU text"
```

---

### Task 3: Core — сортировка и формат ростера

**Files:**
- Create: `src/GK2ZombieHQ.Core/RosterLogic.cs`
- Test: `tests/GK2ZombieHQ.Core.Tests/RosterLogicTests.cs`

**Interfaces:**
- Consumes: `ZombieInfo`, `ZombieKind` (Task 2).
- Produces: `static void RosterLogic.Sort(List<ZombieInfo>)` (по `Kind`, затем `Name` без учёта регистра); `static string RosterLogic.Skulls(ZombieInfo)` → `"W/R"`.

- [ ] **Step 1: Написать падающий тест**

`tests/GK2ZombieHQ.Core.Tests/RosterLogicTests.cs`:
```csharp
using System.Collections.Generic;
using GK2ZombieHQ.Core;
using Xunit;

namespace GK2ZombieHQ.Core.Tests
{
    public class RosterLogicTests
    {
        [Fact] public void Sort_groups_by_kind_then_name()
        {
            var list = new List<ZombieInfo>
            {
                new ZombieInfo { Name = "Zed", Kind = ZombieKind.Gardener },
                new ZombieInfo { Name = "Bob", Kind = ZombieKind.Fighter },
                new ZombieInfo { Name = "Ann", Kind = ZombieKind.Gardener },
            };
            RosterLogic.Sort(list);
            Assert.Equal(new[] { "Bob", "Ann", "Zed" }, new[] { list[0].Name, list[1].Name, list[2].Name });
        }

        [Fact] public void Skulls_formats_w_r()
        {
            Assert.Equal("3/1", RosterLogic.Skulls(new ZombieInfo { WhiteSkulls = 3, RedSkulls = 1 }));
        }
    }
}
```

- [ ] **Step 2: Запустить — падает**

Run: `& "C:\Users\Проньки\dotnet-sdk\dotnet.exe" test -c Release --filter "FullyQualifiedName~RosterLogicTests"`
Expected: ошибка компиляции — нет `RosterLogic`.

- [ ] **Step 3: Реализовать**

`src/GK2ZombieHQ.Core/RosterLogic.cs`:
```csharp
using System;
using System.Collections.Generic;

namespace GK2ZombieHQ.Core
{
    public static class RosterLogic
    {
        public static void Sort(List<ZombieInfo> list)
        {
            if (list == null) return;
            list.Sort((a, b) =>
            {
                int c = a.Kind.CompareTo(b.Kind);
                if (c != 0) return c;
                return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });
        }

        public static string Skulls(ZombieInfo z) => z == null ? "0/0" : z.WhiteSkulls + "/" + z.RedSkulls;
    }
}
```

- [ ] **Step 4: Запустить — проходят**

Run: `& "C:\Users\Проньки\dotnet-sdk\dotnet.exe" test -c Release --filter "FullyQualifiedName~RosterLogicTests"`
Expected: `Passed! - Failed: 0, Passed: 2`.

- [ ] **Step 5: Коммит**

```bash
git add src/GK2ZombieHQ.Core tests/GK2ZombieHQ.Core.Tests
git commit -m "feat(core): roster sorting and skull formatting"
```

---

### Task 4: Проект плагина + Plugin + конфиг + хост HUD/панели

**Files:**
- Create: `src/GK2ZombieHQ/GK2ZombieHQ.csproj`, `src/GK2ZombieHQ/Plugin.cs`
- Modify: `GK2ZombieHQ.sln` (добавить проект)

**Interfaces:**
- Consumes: `ZombieText`, `ZombieLanguage`, `HudFormat` (Core, линкуются).
- Produces: класс `Plugin` (GUID `otkosss.gk2.zombiehq`), поля `ConfigEntry` (см. код), `static Plugin Instance`, `static BepInEx.Logging.ManualLogSource Log`, `static ZombieLanguage ResolveLanguage(string)`; GameObject `GK2ZombieHQ` с компонентами `ZombieHud` и `ZombiePanel` (добавятся в Task 6/7; в этом шаге — заглушки-классы, чтобы собралось).

- [ ] **Step 1: Создать csproj**

`src/GK2ZombieHQ/GK2ZombieHQ.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <Nullable>disable</Nullable>
    <AssemblyName>GK2ZombieHQ</AssemblyName>
    <RootNamespace>GK2ZombieHQ</RootNamespace>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <Version>1.0.0</Version>
    <GameDir Condition="'$(GameDir)' == ''">E:\SteamLibrary\steamapps\common\Graveyard Keeper 2</GameDir>
    <ManagedDir>$(GameDir)\GraveyardKeeper2_Data\Managed</ManagedDir>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="..\GK2ZombieHQ.Core\*.cs" LinkBase="Core" />
  </ItemGroup>
  <ItemGroup>
    <Reference Include="BepInEx"><HintPath>$(GameDir)\BepInEx\core\BepInEx.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="Assembly-CSharp"><HintPath>$(ManagedDir)\Assembly-CSharp.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="UnityEngine.CoreModule"><HintPath>$(ManagedDir)\UnityEngine.CoreModule.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="UnityEngine.UI"><HintPath>$(ManagedDir)\UnityEngine.UI.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="UnityEngine.UIModule"><HintPath>$(ManagedDir)\UnityEngine.UIModule.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="UnityEngine.TextRenderingModule"><HintPath>$(ManagedDir)\UnityEngine.TextRenderingModule.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="Unity.TextMeshPro"><HintPath>$(ManagedDir)\Unity.TextMeshPro.dll</HintPath><Private>false</Private></Reference>
  </ItemGroup>
</Project>
```
```powershell
& "C:\Users\Проньки\dotnet-sdk\dotnet.exe" sln "C:\Users\Проньки\Documents\OpenCode\GK2ZombieHQ\GK2ZombieHQ.sln" add "src\GK2ZombieHQ\GK2ZombieHQ.csproj"
```

- [ ] **Step 2: Plugin + временные заглушки компонентов**

`src/GK2ZombieHQ/Plugin.cs`:
```csharp
using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GK2ZombieHQ.Core;
using UnityEngine;

namespace GK2ZombieHQ
{
    [BepInPlugin(Guid, "GK2 Zombie HQ", "1.0.0")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "otkosss.gk2.zombiehq";
        public static Plugin Instance;
        public static ManualLogSource Log;

        internal ConfigEntry<bool> HudEnabled;
        internal ConfigEntry<int> HudOffsetX, HudOffsetY, HudFontSize;
        internal ConfigEntry<KeyCode> HudToggleKey, PanelKey;
        internal ConfigEntry<string> Language;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            HudEnabled = Config.Bind("Hud", "Enabled", true, "Show the zombie count HUD");
            HudOffsetX = Config.Bind("Hud", "OffsetX", 12, "HUD X offset (px)");
            HudOffsetY = Config.Bind("Hud", "OffsetY", 12, "HUD Y offset (px)");
            HudFontSize = Config.Bind("Hud", "FontSize", 22, "HUD font size");
            HudToggleKey = Config.Bind("Keys", "HudToggle", KeyCode.F7, "Toggle HUD");
            PanelKey = Config.Bind("Keys", "Panel", KeyCode.F8, "Open the zombie panel");
            Language = Config.Bind("General", "Language", "auto", "auto | en | ru");

            ZombieText.Language = ResolveLanguage(Language.Value);

            var go = new GameObject("GK2ZombieHQ");
            DontDestroyOnLoad(go);
            go.AddComponent<ZombieHud>();
            go.AddComponent<ZombiePanel>();
            Logger.LogInfo("GK2 Zombie HQ " + Version + " loaded.");
        }

        internal static ZombieLanguage ResolveLanguage(string value)
        {
            if (string.Equals(value, "en", StringComparison.OrdinalIgnoreCase)) return ZombieLanguage.En;
            if (string.Equals(value, "ru", StringComparison.OrdinalIgnoreCase)) return ZombieLanguage.Ru;
            var two = System.Globalization.CultureInfo.CurrentUICulture?.TwoLetterISOLanguageName;
            return string.Equals(two, "ru", StringComparison.OrdinalIgnoreCase) ? ZombieLanguage.Ru : ZombieLanguage.En;
        }

        internal static string Version => typeof(Plugin).Assembly.GetName().Version?.ToString() ?? "1.0.0";
    }
}
```
`src/GK2ZombieHQ/ZombieHud.cs` (заглушка на этот таск):
```csharp
using UnityEngine;

namespace GK2ZombieHQ
{
    internal sealed class ZombieHud : MonoBehaviour { }
}
```
`src/GK2ZombieHQ/ZombiePanel.cs` (заглушка на этот таск):
```csharp
using UnityEngine;

namespace GK2ZombieHQ
{
    internal sealed class ZombiePanel : MonoBehaviour { }
}
```

- [ ] **Step 3: Собрать**

Run: `& "C:\Users\Проньки\dotnet-sdk\dotnet.exe" build -c Release`
Expected: `Сборка успешно завершена`, 0 ошибок; `src\GK2ZombieHQ\bin\Release\GK2ZombieHQ.dll` существует.

- [ ] **Step 4: Коммит**

```bash
git add src/GK2ZombieHQ GK2ZombieHQ.sln
git commit -m "feat(plugin): BepInEx entry, config and component host"
```

---

### Task 5: Обёртка над игрой — ZombieRoster

**Files:**
- Create: `src/GK2ZombieHQ/ZombieRoster.cs`
- Modify: `src/GK2ZombieHQ/ZombieHud.cs`, `src/GK2ZombieHQ/ZombiePanel.cs` (убрать заглушки? — нет, они перепишутся в Task 6/7)

**Interfaces:**
- Consumes: Core (`ZombieInfo`, `ZombieKind`, `RosterLogic`), игровые типы (`MainGame`, `ZombieSystemData`, `ZombieWgoData`, `ZombieType`, `PlayerData`, `LazyUI`, `UIZombieWorkerWindow`, `UIZombieWorkerWindowData`).
- Produces: `sealed class RosterEntry { ZombieInfo Info; ZombieWgoData Data; }`; `static class ZombieRoster { int Count(); int Limit(); List<RosterEntry> Load(); bool Recall(RosterEntry); void OpenWindow(RosterEntry); }`.

- [ ] **Step 1: Реализовать (юнит-тестами не покрываем — вызовы игры)**

`src/GK2ZombieHQ/ZombieRoster.cs`:
```csharp
using System;
using System.Collections.Generic;
using GK2ZombieHQ.Core;
using LazyBearTechnology;

namespace GK2ZombieHQ
{
    internal sealed class RosterEntry
    {
        public ZombieInfo Info;
        public ZombieWgoData Data;
    }

    internal static class ZombieRoster
    {
        internal static int Count()
        {
            try { return MainGame.Instance.PlayerData.GetResInt("cur_zombies_count"); }
            catch (Exception ex) { Plugin.Log.LogWarning("zombie count: " + ex.Message); return -1; }
        }

        internal static int Limit()
        {
            try { return MainGame.Instance.PlayerData.GetResInt("zombies_limit_mechanic"); }
            catch (Exception ex) { Plugin.Log.LogWarning("zombie limit: " + ex.Message); return -1; }
        }

        internal static List<RosterEntry> Load()
        {
            var result = new List<RosterEntry>();
            try
            {
                var sys = MainGame.Instance.ZombieSystemData;
                if (sys == null || sys.Cache == null) return result;
                foreach (var kv in sys.Cache)
                {
                    var z = kv.Value;
                    if (z == null) continue;
                    result.Add(new RosterEntry
                    {
                        Data = z,
                        Info = new ZombieInfo
                        {
                            Id = kv.Key.ToString(),
                            Name = string.IsNullOrEmpty(z.Name) ? kv.Key.ToString() : z.Name,
                            Kind = MapKind(z.ZombieType),
                            WhiteSkulls = z.WhiteSkulls,
                            RedSkulls = z.RedSkulls,
                            Collar = z.Collar != null ? SafeItemName(z.Collar) : null,
                            Activity = z.WorkerActivity != null ? z.WorkerActivity.ToString() : null,
                            CanRecall = true
                        }
                    });
                }
                result.Sort((a, b) =>
                {
                    int c = a.Info.Kind.CompareTo(b.Info.Kind);
                    return c != 0 ? c : string.Compare(a.Info.Name, b.Info.Name, StringComparison.OrdinalIgnoreCase);
                });
            }
            catch (Exception ex) { Plugin.Log.LogWarning("roster load: " + ex); }
            return result;
        }

        internal static bool Recall(RosterEntry entry)
        {
            try
            {
                if (entry == null || entry.Data == null) return false;
                MainGame.Instance.ZombieSystemData
                    .PutZombieFromGameSceneToStoreForPlayer(MainGame.Instance.PlayerData, entry.Data);
                return true;
            }
            catch (Exception ex) { Plugin.Log.LogWarning("recall: " + ex); return false; }
        }

        internal static void OpenWindow(RosterEntry entry)
        {
            try
            {
                if (entry == null || entry.Data == null) return;
                LazyUI.GetWindow<UIZombieWorkerWindow>().Open(new UIZombieWorkerWindowData(entry.Data));
            }
            catch (Exception ex) { Plugin.Log.LogWarning("open zombie window: " + ex); }
        }

        private static string SafeItemName(Item item)
        {
            try { return item != null ? item.Id : null; } catch { return null; }
        }

        private static ZombieKind MapKind(ZombieType t)
        {
            switch (t)
            {
                case ZombieType.Free: return ZombieKind.Free;
                case ZombieType.Crafter: return ZombieKind.Crafter;
                case ZombieType.Caretaker: return ZombieKind.Caretaker;
                case ZombieType.ConveyorCrafter: return ZombieKind.ConveyorCrafter;
                case ZombieType.Worker: return ZombieKind.Worker;
                case ZombieType.Porter: return ZombieKind.Porter;
                case ZombieType.Gardener: return ZombieKind.Gardener;
                case ZombieType.ConveyorTransporter: return ZombieKind.ConveyorTransporter;
                case ZombieType.Fighter: return ZombieKind.Fighter;
                default: return ZombieKind.Unknown;
            }
        }
    }
}
```
Примечание: имена `Item.Id`, `z.Name`, `z.ZombieType`, `z.WhiteSkulls`, `z.RedSkulls`, `z.Collar`, `z.WorkerActivity` — из recon (`ZombieWgoData`). Если свойство называется иначе (`Item` id), поправить по компилятору.

- [ ] **Step 2: Собрать**

Run: `& "C:\Users\Проньки\dotnet-sdk\dotnet.exe" build -c Release`
Expected: 0 ошибок. Если компилятор ругается на имена членов игровых типов — поправить по факту (recon-файл `%TEMP%\opencode\zombie_members.txt`).

- [ ] **Step 3: Коммит**

```bash
git add src/GK2ZombieHQ/ZombieRoster.cs
git commit -m "feat(plugin): game wrapper (count, limit, roster, recall, open window)"
```

---

### Task 6: HUD со счётчиком

**Files:**
- Modify: `src/GK2ZombieHQ/ZombieHud.cs`

**Interfaces:**
- Consumes: `ZombieRoster.Count/Limit`, `HudFormat.Count/AtLimit`, `Plugin` config.
- Produces: компонент `ZombieHud` с Canvas+TMP-текстом, обновление раз в 0.5 с.

- [ ] **Step 1: Реализовать**

`src/GK2ZombieHQ/ZombieHud.cs`:
```csharp
using GK2ZombieHQ.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2ZombieHQ
{
    internal sealed class ZombieHud : MonoBehaviour
    {
        private TextMeshProUGUI _text;
        private float _timer;
        private bool _visible = true;

        private void Start()
        {
            var canvasGo = new GameObject("GK2ZombieHQ_HudCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
            canvasGo.AddComponent<CanvasScaler>();

            var textGo = new GameObject("Count");
            textGo.transform.SetParent(canvasGo.transform, false);
            _text = textGo.AddComponent<TextMeshProUGUI>();
            _text.fontSize = Plugin.Instance.HudFontSize.Value;
            _text.alignment = TextAlignmentOptions.TopLeft;
            _text.color = Color.white;

            var rt = _text.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(Plugin.Instance.HudOffsetX.Value, -Plugin.Instance.HudOffsetY.Value);
            rt.sizeDelta = new Vector2(420, 40);

            _visible = Plugin.Instance.HudEnabled.Value;
            canvasGo.SetActive(_visible);
        }

        private void Update()
        {
            try
            {
                if (Input.GetKeyDown(Plugin.Instance.HudToggleKey.Value))
                {
                    _visible = !_visible;
                    if (_text != null) _text.gameObject.SetActive(_visible);
                }

                _timer -= Time.unscaledDeltaTime;
                if (_timer > 0f || _text == null || !_visible) return;
                _timer = 0.5f;

                int count = ZombieRoster.Count();
                int limit = ZombieRoster.Limit();
                if (count < 0) { _text.text = ""; return; }
                _text.text = HudFormat.Count(ZombieText.Language, count, limit);
                _text.color = HudFormat.AtLimit(count, limit) ? new Color(1f, 0.4f, 0.4f) : Color.white;
            }
            catch (System.Exception ex) { Plugin.Log.LogWarning("hud: " + ex.Message); }
        }
    }
}
```

- [ ] **Step 2: Собрать**

Run: `& "C:\Users\Проньки\dotnet-sdk\dotnet.exe" build -c Release`
Expected: 0 ошибок.

- [ ] **Step 3: Коммит**

```bash
git add src/GK2ZombieHQ/ZombieHud.cs
git commit -m "feat(plugin): zombie count HUD"
```

---

### Task 7: Панель «Зомби-штаб»

**Files:**
- Modify: `src/GK2ZombieHQ/ZombiePanel.cs`
- Create: `src/GK2ZombieHQ/UiFactory.cs` (мелкие помощники: панель, кнопка, текст)

**Interfaces:**
- Consumes: `ZombieRoster.Load/Recall/OpenWindow`, `ZombieText`, `Plugin` config.
- Produces: компонент `ZombiePanel` (открытие по `Keys.Panel`), кнопки на строке.

- [ ] **Step 1: Реализовать помощники и панель**

`src/GK2ZombieHQ/UiFactory.cs`:
```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2ZombieHQ
{
    internal static class UiFactory
    {
        internal static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        internal static Image PanelImage(string name, Transform parent, Color color)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        internal static TextMeshProUGUI Label(string name, Transform parent, string text, int size, TextAlignmentOptions align)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = Color.white;
            return t;
        }

        internal static Button TextButton(string name, Transform parent, string text, int size)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0.22f, 0.22f, 0.25f, 0.95f);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var label = Label("Label", rt, text, size, TextAlignmentOptions.Center);
            var lrt = label.rectTransform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            return btn;
        }
    }
}
```
`src/GK2ZombieHQ/ZombiePanel.cs`:
```csharp
using System.Collections.Generic;
using GK2ZombieHQ.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GK2ZombieHQ
{
    internal sealed class ZombiePanel : MonoBehaviour
    {
        private GameObject _root;
        private RectTransform _content;
        private TextMeshProUGUI _countLabel;
        private float _timer;

        private void Start()
        {
            BuildUi();
            _root.SetActive(false);
        }

        private void BuildUi()
        {
            _root = new GameObject("GK2ZombieHQ_Panel");
            _root.transform.SetParent(transform, false);
            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5100;
            _root.AddComponent<CanvasScaler>();
            _root.AddComponent<GraphicRaycaster>();

            var overlay = UiFactory.PanelImage("Overlay", _root.transform, new Color(0f, 0f, 0f, 0.6f));
            Stretch(overlay.rectTransform);

            var panel = UiFactory.PanelImage("Panel", overlay.transform, new Color(0.10f, 0.10f, 0.12f, 0.98f));
            var prt = panel.rectTransform;
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(720, 560);
            prt.anchoredPosition = Vector2.zero;

            var title = UiFactory.Label("Title", prt, ZombieText.Get("Title"), 26, TextAlignmentOptions.Left);
            var trt = title.rectTransform;
            trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1);
            trt.pivot = new Vector2(0.5f, 1); trt.anchoredPosition = new Vector2(0, -8);
            trt.sizeDelta = new Vector2(-24, 36);

            var close = UiFactory.TextButton("Close", prt, ZombieText.Get("Close"), 18);
            var crt = close.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(1, 1); crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(1, 1); crt.anchoredPosition = new Vector2(-8, -8);
            crt.sizeDelta = new Vector2(96, 32);
            close.onClick.AddListener(() => _root.SetActive(false));

            _countLabel = UiFactory.Label("Count", prt, "", 20, TextAlignmentOptions.Left);
            var cnt = _countLabel.rectTransform;
            cnt.anchorMin = new Vector2(0, 1); cnt.anchorMax = new Vector2(1, 1);
            cnt.pivot = new Vector2(0.5f, 1); cnt.anchoredPosition = new Vector2(0, -48);
            cnt.sizeDelta = new Vector2(-24, 28);

            var viewport = UiFactory.PanelImage("Viewport", prt, new Color(0, 0, 0, 0.25f));
            var vrt = viewport.rectTransform;
            vrt.anchorMin = new Vector2(0, 0); vrt.anchorMax = new Vector2(1, 1);
            vrt.offsetMin = new Vector2(12, 12); vrt.offsetMax = new Vector2(-12, -84);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<ScrollRect>();
            var scroll = viewport.gameObject.GetComponent<ScrollRect>();

            _content = UiFactory.Rect("Content", vrt);
            _content.anchorMin = new Vector2(0, 1); _content.anchorMax = new Vector2(1, 1);
            _content.pivot = new Vector2(0.5f, 1); _content.anchoredPosition = Vector2.zero;
            var vlg = _content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true; vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = true;
            vlg.spacing = 4; vlg.padding = new RectOffset(6, 6, 6, 6);
            _content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = _content;
            scroll.viewport = vrt;
            scroll.horizontal = false;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private void Update()
        {
            try
            {
                if (Input.GetKeyDown(Plugin.Instance.PanelKey.Value))
                {
                    bool show = !_root.activeSelf;
                    _root.SetActive(show);
                    if (show) Refresh();
                }
                if (!_root.activeSelf) return;
                _timer -= Time.unscaledDeltaTime;
                if (_timer <= 0f) { _timer = 0.75f; Refresh(); }
            }
            catch (System.Exception ex) { Plugin.Log.LogWarning("panel: " + ex.Message); }
        }

        private void Refresh()
        {
            int count = ZombieRoster.Count();
            int limit = ZombieRoster.Limit();
            _countLabel.text = count < 0 ? "" : HudFormat.Count(ZombieText.Language, count, limit);

            foreach (Transform child in _content) Destroy(child.gameObject);
            var entries = ZombieRoster.Load();
            if (entries.Count == 0)
            {
                var empty = UiFactory.Label("Empty", _content, ZombieText.Get("NoZombies"), 18, TextAlignmentOptions.Left);
                empty.gameObject.AddComponent<LayoutElement>().preferredHeight = 32;
                return;
            }

            foreach (var e in entries)
            {
                var row = UiFactory.Rect("Row", _content);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;
                var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                hlg.childControlHeight = true; hlg.childControlWidth = true;
                hlg.childForceExpandWidth = false; hlg.spacing = 6;

                string label = e.Info.Name + "  ·  " + ZombieText.KindName(e.Info.Kind)
                    + "  ·  " + RosterLogic.Skulls(e.Info) + (e.Info.Collar != null ? "  ·  " + e.Info.Collar : "");
                var name = UiFactory.Label("Name", row, label, 18, TextAlignmentOptions.Left);
                name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

                var openBtn = UiFactory.TextButton("Open", row, ZombieText.Get("Open"), 16);
                openBtn.gameObject.AddComponent<LayoutElement>().preferredWidth = 110;
                var entry = e;
                openBtn.onClick.AddListener(() => { ZombieRoster.OpenWindow(entry); _root.SetActive(false); });

                if (e.Info.CanRecall)
                {
                    var rec = UiFactory.TextButton("Recall", row, ZombieText.Get("Recall"), 16);
                    rec.gameObject.AddComponent<LayoutElement>().preferredWidth = 110;
                    rec.onClick.AddListener(() => { ZombieRoster.Recall(entry); Refresh(); });
                }
            }
        }
    }
}
```

- [ ] **Step 2: Собрать**

Run: `& "C:\Users\Проньки\dotnet-sdk\dotnet.exe" build -c Release`
Expected: 0 ошибок.

- [ ] **Step 3: Прогнать тесты Core**

Run: `& "C:\Users\Проньки\dotnet-sdk\dotnet.exe" test -c Release`
Expected: все зелёные (10 тестов из Tasks 1-3).

- [ ] **Step 4: Коммит**

```bash
git add src/GK2ZombieHQ
git commit -m "feat(plugin): zombie HQ panel with roster and actions"
```

---

### Task 8: Деплой и E2E в игре

**Files:** — (ручная проверка)

- [ ] **Step 1: Разложить и запустить**

```powershell
$d="C:\Users\Проньки\Documents\OpenCode\GK2ZombieHQ"
$game="E:\SteamLibrary\steamapps\common\Graveyard Keeper 2"
New-Item -ItemType Directory -Force -Path "$game\BepInEx\plugins\GK2ZombieHQ" | Out-Null
Copy-Item "$d\src\GK2ZombieHQ\bin\Release\GK2ZombieHQ.dll" "$game\BepInEx\plugins\GK2ZombieHQ\GK2ZombieHQ.dll" -Force
```
Игра закрыта; после — удалить `BepInEx\cache` при необходимости. Запустить игру.

- [ ] **Step 2: Чек-лист E2E**

- `BepInEx\LogOutput.log`: `GK2 Zombie HQ 1.0.0.0 loaded.`, ошибок нет.
- HUD слева сверху: «Зомби: N / лимит» (F7 скрывает/показывает).
- F8 открывает панель; список зомби со именами/типами/черепами; «Открыть» показывает штатное окно зомби; «Отозвать» снимает зомби с работы.
- Esc/«Закрыть» закрывает панель.
- Сообщить о результате; при расхождениях — поправить имена игровых членов/условия.

- [ ] **Step 3: Зафиксировать результат**

Дописать в `README.md` строку о статусе и в коммит — что наблюдалось.

- [ ] **Step 4: Коммит**

```bash
git add -A
git commit -m "test: E2E results and fixes"
```

---

### Task 9: Публикация (после «ок» пользователя)

**Files:**
- Create: `E:\GK2Upload\GK2ZombieHQ\` (папка айтема), `README.md`, `description.txt`, `preview.png`
- Publish: новый Workshop-айтем через `tools\GK2Publisher\GK2Publisher.exe --create ...`

- [ ] **Step 1: Подготовить контент**

`E:\GK2Upload\GK2ZombieHQ\BepInEx\plugins\GK2ZombieHQ\GK2ZombieHQ.dll` + `README.txt`; описание (EN+RU) < 8000 байт; превью 512×512.

- [ ] **Step 2: Создать айтем**

```powershell
& "C:\Users\Проньки\Documents\OpenCode\GK2ModInstaller\tools\GK2Publisher\bin\Release\GK2Publisher.exe" --create --folder "E:\GK2Upload\GK2ZombieHQ" --title "GK2 Zombie HQ — count, list & manage zombies" --desc-file "E:\GK2Upload\GK2ZombieHQ_description.txt" --tags "Gameplay" --preview "E:\GK2Upload\GK2ZombieHQ_preview.png" --public
```
Expected: `SubmitItemUpdate: k_EResultOK` (после — привязать превью через `--update <id> --preview`). Публиковать только после явного «ок» пользователя.

- [ ] **Step 3: GitHub Release**

Новый репозиторий `otkosss-png/GK2ZombieHQ`, релиз v1.0.0 (zip: DLL + README).
