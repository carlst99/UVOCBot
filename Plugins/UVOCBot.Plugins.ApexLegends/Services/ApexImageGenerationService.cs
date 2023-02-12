using Microsoft.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UVOCBot.Plugins.ApexLegends.Abstractions.Services;
using UVOCBot.Plugins.ApexLegends.Objects.ApexQuery;

namespace UVOCBot.Plugins.ApexLegends.Services;

public sealed class ApexImageGenerationService : IApexImageGenerationService
{
    private readonly HttpClient _httpClient;
    private readonly RecyclableMemoryStreamManager _msManager;

    public ApexImageGenerationService
    (
        HttpClient client,
        RecyclableMemoryStreamManager msManager
    )
    {
        _httpClient = client;
        _msManager = msManager;
    }

    public async Task<MemoryStream> GenerateCraftingBundleImageAsync
    (
        IReadOnlyList<CraftingBundle> bundles,
        CancellationToken ct = default
    )
    {
        const int ICON_HEIGHT = 80;

        int height = bundles.Count * ICON_HEIGHT * 2;
        int width = bundles.Max(x => x.BundleContent.Count) * ICON_HEIGHT * 2;
        using Image<Rgba32> image = new(width, height, Rgba32.ParseHex("313332"));

        int offsetX = ICON_HEIGHT / 2 - 1;
        int offsetY = offsetX;

        foreach (CraftingBundle bundle in bundles)
        {
            foreach (CraftingBundleContent content in bundle.BundleContent)
            {
                Stream assetStream = await _httpClient.GetStreamAsync(content.ItemType.Asset, ct);
                using Image<Rgba32> icon = await Image.LoadAsync<Rgba32>(assetStream, ct).ConfigureAwait(false);
                icon.Mutate(x => x.Resize(new Size(ICON_HEIGHT, ICON_HEIGHT)));

                image.ProcessPixelRows(icon, (imageAccessor, iconAccessor) =>
                {
                    for (int i = 0; i < iconAccessor.Height; i++)
                    {
                        Span<Rgba32> imageRow = imageAccessor.GetRowSpan(offsetY++);
                        Span<Rgba32> iconRow = iconAccessor.GetRowSpan(i);

                        iconRow.CopyTo(imageRow[offsetX..]);
                    }
                });

                offsetY -= ICON_HEIGHT;
                offsetX += ICON_HEIGHT * 2;
            }

            offsetX = ICON_HEIGHT / 2 - 1;
            offsetY += ICON_HEIGHT * 2;
        }

        MemoryStream ms = _msManager.GetStream();
        await image.SaveAsync(ms, new WebpEncoder(), ct).ConfigureAwait(false);
        ms.Seek(0, SeekOrigin.Begin);

        return ms;
    }
}
