// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;

namespace SixLabors.ImageSharp.Processing.Processors.Convolution
{
    /// <summary>
    /// Defines a processor that uses two one-dimensional matrices to perform two-pass convolution against an image.
    /// </summary>
    /// <typeparam name="TPixel">The pixel format.</typeparam>
    internal class Convolution2PassProcessor<TPixel> : ImageProcessor<TPixel>
        where TPixel : struct, IPixel<TPixel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Convolution2PassProcessor{TPixel}"/> class.
        /// </summary>
        /// <param name="configuration">The configuration which allows altering default behaviour or extending the library.</param>
        /// <param name="kernelX">The horizontal gradient operator.</param>
        /// <param name="kernelY">The vertical gradient operator.</param>
        /// <param name="preserveAlpha">Whether the convolution filter is applied to alpha as well as the color channels.</param>
        /// <param name="source">The source <see cref="Image{TPixel}"/> for the current processor instance.</param>
        /// <param name="sourceRectangle">The source area to process for the current processor instance.</param>
        public Convolution2PassProcessor(
            Configuration configuration,
            in DenseMatrix<float> kernelX,
            in DenseMatrix<float> kernelY,
            bool preserveAlpha,
            Image<TPixel> source,
            Rectangle sourceRectangle)
            : base(configuration, source, sourceRectangle)
        {
            this.KernelX = kernelX;
            this.KernelY = kernelY;
            this.PreserveAlpha = preserveAlpha;
        }

        /// <summary>
        /// Gets the horizontal gradient operator.
        /// </summary>
        public DenseMatrix<float> KernelX { get; }

        /// <summary>
        /// Gets the vertical gradient operator.
        /// </summary>
        public DenseMatrix<float> KernelY { get; }

        /// <summary>
        /// Gets a value indicating whether the convolution filter is applied to alpha as well as the color channels.
        /// </summary>
        public bool PreserveAlpha { get; }

        /// <inheritdoc/>
        protected override void OnFrameApply(ImageFrame<TPixel> source)
        {
            using Buffer2D<TPixel> firstPassPixels = this.Configuration.MemoryAllocator.Allocate2D<TPixel>(source.Size());

            var interest = Rectangle.Intersect(this.SourceRectangle, source.Bounds());

            // Horizontal convolution
            ParallelRowIterator.IterateRows<RowIntervalAction, Vector4>(
                interest,
                this.Configuration,
                new RowIntervalAction(interest, firstPassPixels, source.PixelBuffer, this.KernelX, this.Configuration, this.PreserveAlpha));

            // Vertical convolution
            ParallelRowIterator.IterateRows<RowIntervalAction, Vector4>(
                interest,
                this.Configuration,
                new RowIntervalAction(interest, source.PixelBuffer, firstPassPixels, this.KernelY, this.Configuration, this.PreserveAlpha));
        }

        /// <summary>
        /// A <see langword="struct"/> implementing the convolution logic for <see cref="Convolution2PassProcessor{T}"/>.
        /// </summary>
        private readonly struct RowIntervalAction : IRowIntervalAction<Vector4>
        {
            private readonly Rectangle bounds;
            private readonly Buffer2D<TPixel> targetPixels;
            private readonly Buffer2D<TPixel> sourcePixels;
            private readonly DenseMatrix<float> kernel;
            private readonly Configuration configuration;
            private readonly bool preserveAlpha;

            /// <summary>
            /// Initializes a new instance of the <see cref="RowIntervalAction"/> struct.
            /// </summary>
            /// <param name="bounds">The target processing bounds for the current instance.</param>
            /// <param name="targetPixels">The target pixel buffer to adjust.</param>
            /// <param name="sourcePixels">The source pixels. Cannot be null.</param>
            /// <param name="kernel">The kernel operator.</param>
            /// <param name="configuration">The <see cref="Configuration"/></param>
            /// <param name="preserveAlpha">Whether the convolution filter is applied to alpha as well as the color channels.</param>
            [MethodImpl(InliningOptions.ShortMethod)]
            public RowIntervalAction(
                Rectangle bounds,
                Buffer2D<TPixel> targetPixels,
                Buffer2D<TPixel> sourcePixels,
                DenseMatrix<float> kernel,
                Configuration configuration,
                bool preserveAlpha)
            {
                this.bounds = bounds;
                this.targetPixels = targetPixels;
                this.sourcePixels = sourcePixels;
                this.kernel = kernel;
                this.configuration = configuration;
                this.preserveAlpha = preserveAlpha;
            }

            /// <inheritdoc/>
            [MethodImpl(InliningOptions.ShortMethod)]
            public void Invoke(in RowInterval rows, Memory<Vector4> memory)
            {
                Span<Vector4> vectorSpan = memory.Span;
                int length = vectorSpan.Length;
                ref Vector4 vectorSpanRef = ref MemoryMarshal.GetReference(vectorSpan);

                int maxY = this.bounds.Bottom - 1;
                int maxX = this.bounds.Right - 1;

                for (int y = rows.Min; y < rows.Max; y++)
                {
                    Span<TPixel> targetRowSpan = this.targetPixels.GetRowSpan(y).Slice(this.bounds.X);
                    PixelOperations<TPixel>.Instance.ToVector4(this.configuration, targetRowSpan.Slice(0, length), vectorSpan);

                    if (this.preserveAlpha)
                    {
                        for (int x = 0; x < this.bounds.Width; x++)
                        {
                            DenseMatrixUtils.Convolve3(
                                in this.kernel,
                                this.sourcePixels,
                                ref vectorSpanRef,
                                y,
                                x,
                                this.bounds.Y,
                                maxY,
                                this.bounds.X,
                                maxX);
                        }
                    }
                    else
                    {
                        for (int x = 0; x < this.bounds.Width; x++)
                        {
                            DenseMatrixUtils.Convolve4(
                                in this.kernel,
                                this.sourcePixels,
                                ref vectorSpanRef,
                                y,
                                x,
                                this.bounds.Y,
                                maxY,
                                this.bounds.X,
                                maxX);
                        }
                    }

                    PixelOperations<TPixel>.Instance.FromVector4Destructive(this.configuration, vectorSpan, targetRowSpan);
                }
            }
        }
    }
}
