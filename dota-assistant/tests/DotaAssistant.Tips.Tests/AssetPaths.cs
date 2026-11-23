namespace DotaAssistant.Tips.Tests;

internal static class AssetPaths
{
    public static string AssetsRoot
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null)
            {
                var candidate = Path.Combine(dir.FullName, "assets");
                if (Directory.Exists(Path.Combine(candidate, "tips")))
                {
                    return candidate;
                }

                var sibling = Path.Combine(dir.FullName, "dota-assistant", "assets");
                if (Directory.Exists(Path.Combine(sibling, "tips")))
                {
                    return sibling;
                }

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException(
                "Не найден каталог assets/tips. Запускайте тесты из решения dota-assistant.");
        }
    }

    public static string TipsRoot => Path.Combine(AssetsRoot, "tips");
    public static string RulesJson => Path.Combine(TipsRoot, "rules.json");
}
