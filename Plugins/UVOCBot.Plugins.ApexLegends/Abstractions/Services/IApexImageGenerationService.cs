using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.ApexLegends.Objects.ApexQuery;

namespace UVOCBot.Plugins.ApexLegends.Abstractions.Services;

public interface IApexImageGenerationService
{
    Task<MemoryStream> GenerateCraftingBundleImageAsync
    (
        IReadOnlyList<CraftingBundle> bundles,
        CancellationToken ct = default
    );
}
