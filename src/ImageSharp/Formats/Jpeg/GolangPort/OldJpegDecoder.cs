namespace ImageSharp.Formats.Jpeg.GolangPort
{
    using System.IO;

    using ImageSharp.PixelFormats;

    /// <summary>
    /// Image decoder for generating an image out of a jpg stream.
    /// </summary>
    public sealed class OldJpegDecoder : IImageDecoder, IJpegDecoderOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether the metadata should be ignored when the image is being decoded.
        /// </summary>
        public bool IgnoreMetadata { get; set; }

        /// <inheritdoc/>
        public Image<TPixel> Decode<TPixel>(Configuration configuration, Stream stream)
            where TPixel : struct, IPixel<TPixel>
        {
            Guard.NotNull(stream, nameof(stream));
            
            using (var decoder = new OldJpegDecoderCore(configuration, this))
            {
                return decoder.Decode<TPixel>(stream);
            }
        }
    }
}