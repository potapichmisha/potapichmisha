namespace DotaAssistant.Vision.Tests;

internal static class AssetPaths
{
    /// <summary>
    /// Resolves dota-assistant/assets regardless of where the test host runs from.
    /// </summary>
    public static string AssetsRoot
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null)
            {
                var candidate = Path.Combine(dir.FullName, "assets");
                if (Directory.Exists(Path.Combine(candidate, "templates"))
                    && Directory.Exists(Path.Combine(candidate, "fixtures")))
                {
                    return candidate;
                }

                // Walk up from bin/... to the dota-assistant folder.
                var sibling = Path.Combine(dir.FullName, "dota-assistant", "assets");
                if (Directory.Exists(Path.Combine(sibling, "templates")))
                {
                    return sibling;
                }

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException(
                "Не найден каталог assets/ (templates + fixtures). Запускайте тесты из решения dota-assistant.");
        }
    }

    public static string TemplatesRoot => Path.Combine(AssetsRoot, "templates");
    public static string FixturesRoot => Path.Combine(AssetsRoot, "fixtures");
    public static string MatchHudFixture => Path.Combine(FixturesRoot, "match_hud_1920x1080.png");
    public static string MatchHudExpected => Path.Combine(FixturesRoot, "match_hud_expected.json");
}
