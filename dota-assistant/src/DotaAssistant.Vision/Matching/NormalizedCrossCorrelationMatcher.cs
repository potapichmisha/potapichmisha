using DotaAssistant.Vision.Imaging;
using DotaAssistant.Vision.Models;

namespace DotaAssistant.Vision.Matching;

/// <summary>
/// Zero-mean normalized cross-correlation (ZNCC) between grayscale patches.
/// Similarity is mapped from [-1, 1] to confidence [0, 1].
/// </summary>
public sealed class NormalizedCrossCorrelationMatcher : ITemplateMatcher
{
    public double Score(GrayImage probe, GrayImage template)
    {
        if (probe.Width != template.Width || probe.Height != template.Height)
        {
            var resized = ResizeGrayNearest(template, probe.Width, probe.Height);
            return ScoreSameSize(probe, resized);
        }

        return ScoreSameSize(probe, template);
    }

    public MatchCandidate? BestMatch(
        GrayImage probe,
        RoiRect roi,
        IEnumerable<TemplateEntry> templates,
        double minConfidence)
    {
        MatchCandidate? best = null;
        foreach (var template in templates)
        {
            var score = Score(probe, template.Image);
            if (best is null || score > best.Confidence)
            {
                best = new MatchCandidate
                {
                    Id = template.Id,
                    DisplayNameRu = template.DisplayNameRu,
                    Kind = template.Kind,
                    Confidence = score,
                    Roi = roi,
                    IsConfident = score >= minConfidence
                };
            }
        }

        return best;
    }

    private static double ScoreSameSize(GrayImage a, GrayImage b)
    {
        var n = a.Pixels.Length;
        if (n == 0)
        {
            return 0;
        }

        double sumA = 0, sumB = 0;
        for (var i = 0; i < n; i++)
        {
            sumA += a.Pixels[i];
            sumB += b.Pixels[i];
        }

        var meanA = sumA / n;
        var meanB = sumB / n;

        double num = 0, denA = 0, denB = 0;
        for (var i = 0; i < n; i++)
        {
            var da = a.Pixels[i] - meanA;
            var db = b.Pixels[i] - meanB;
            num += da * db;
            denA += da * da;
            denB += db * db;
        }

        if (denA <= 1e-9 || denB <= 1e-9)
        {
            return Math.Abs(meanA - meanB) < 1.0 ? 1.0 : 0.0;
        }

        var corr = num / Math.Sqrt(denA * denB);
        return Math.Clamp((corr + 1.0) * 0.5, 0.0, 1.0);
    }

    private static GrayImage ResizeGrayNearest(GrayImage source, int width, int height)
    {
        var rgba = new byte[source.Width * source.Height * 4];
        for (var i = 0; i < source.Pixels.Length; i++)
        {
            var g = source.Pixels[i];
            var o = i * 4;
            rgba[o] = g;
            rgba[o + 1] = g;
            rgba[o + 2] = g;
            rgba[o + 3] = 255;
        }

        return new RgbaImage(source.Width, source.Height, rgba)
            .ResizeNearest(width, height)
            .ToGray();
    }
}
