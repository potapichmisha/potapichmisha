using DotaAssistant.Vision.Imaging;
using DotaAssistant.Vision.Models;

namespace DotaAssistant.Vision.Matching;

public interface ITemplateMatcher
{
    /// <summary>
    /// Returns similarity in [0, 1] (1 = identical after mean-centering).
    /// </summary>
    double Score(GrayImage probe, GrayImage template);

    MatchCandidate? BestMatch(
        GrayImage probe,
        RoiRect roi,
        IEnumerable<TemplateEntry> templates,
        double minConfidence);
}
