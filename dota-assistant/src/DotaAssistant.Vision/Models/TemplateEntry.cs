using DotaAssistant.Vision.Imaging;

namespace DotaAssistant.Vision.Models;

public sealed class TemplateEntry
{
    public required string Id { get; init; }
    public required string DisplayNameRu { get; init; }
    public required string Kind { get; init; }
    public required string RelativePath { get; init; }
    public required GrayImage Image { get; init; }
}
