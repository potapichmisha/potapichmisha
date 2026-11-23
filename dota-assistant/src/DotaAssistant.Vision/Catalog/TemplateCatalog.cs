using System.Text.Json;
using System.Text.Json.Serialization;
using DotaAssistant.Vision.Imaging;
using DotaAssistant.Vision.Models;

namespace DotaAssistant.Vision.Catalog;

public sealed class TemplateCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public TemplateCatalog(IReadOnlyList<TemplateEntry> heroes, IReadOnlyList<TemplateEntry> items)
    {
        Heroes = heroes;
        Items = items;
    }

    public IReadOnlyList<TemplateEntry> Heroes { get; }
    public IReadOnlyList<TemplateEntry> Items { get; }

    public static TemplateCatalog LoadFromDirectory(string templatesRoot)
    {
        var catalogPath = Path.Combine(templatesRoot, "catalog.json");
        if (!File.Exists(catalogPath))
        {
            throw new FileNotFoundException($"Template catalog not found: {catalogPath}");
        }

        var dto = JsonSerializer.Deserialize<CatalogDto>(File.ReadAllText(catalogPath), JsonOptions)
                  ?? throw new InvalidOperationException("catalog.json is empty.");

        var heroes = LoadEntries(templatesRoot, dto.Heroes, "hero");
        var items = LoadEntries(templatesRoot, dto.Items, "item");
        return new TemplateCatalog(heroes, items);
    }

    private static IReadOnlyList<TemplateEntry> LoadEntries(
        string root,
        IEnumerable<CatalogItemDto>? items,
        string kind)
    {
        var list = new List<TemplateEntry>();
        foreach (var item in items ?? Array.Empty<CatalogItemDto>())
        {
            var path = Path.Combine(root, item.File.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Template image missing: {path}");
            }

            list.Add(new TemplateEntry
            {
                Id = item.Id,
                DisplayNameRu = item.NameRu,
                Kind = kind,
                RelativePath = item.File,
                Image = ImageLoader.LoadGray(path)
            });
        }

        return list;
    }

    private sealed class CatalogDto
    {
        [JsonPropertyName("heroes")]
        public List<CatalogItemDto>? Heroes { get; set; }

        [JsonPropertyName("items")]
        public List<CatalogItemDto>? Items { get; set; }
    }

    private sealed class CatalogItemDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("name_ru")]
        public string NameRu { get; set; } = "";

        [JsonPropertyName("file")]
        public string File { get; set; } = "";
    }
}
