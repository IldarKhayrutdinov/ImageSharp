// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;
using System.Threading.Tasks;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace SixLabors.ImageSharp.Tests.Formats.Tiff
{
    [Trait("Category", "Tiff.BlackBox.Encoder")]
    [Trait("Category", "Tiff")]
    public class ImageExtensionsTest
    {
        [Theory]
        [WithFile(TestImages.Tiff.RgbUncompressed, PixelTypes.Rgba32)]
        public void ThrowsSavingNotImplemented<TPixel>(TestImageProvider<TPixel> provider)
            where TPixel : unmanaged, IPixel<TPixel>
        {
            Assert.Throws<NotImplementedException>(() =>
            {
                string dir = TestEnvironment.CreateOutputDirectory(nameof(ImageExtensionsTest));
                string file = Path.Combine(dir, "SaveAsTiff_Path.tiff");
                using var image = provider.GetImage(new TiffDecoder());
                image.SaveAsTiff(new MemoryStream());
            });
        }
    }
}
