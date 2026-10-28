using HPImageViewer.Core;
using HPImageViewer.Utils;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Size = HPImageViewer.Core.Primitives.Size;


namespace HPImageViewer.Rendering
{
    /// <summary>
    /// 图像渲染器。
    /// <para>
    /// 句柄泄漏修复策略（连续 Live 显示）：
    /// 1) 后台 <see cref="Calculate"/> 只写入托管 byte[] 双缓冲，不每帧 new/Freeze WriteableBitmap
    ///    （Freeze 后的位图会占用 Section 句柄，只能等 GC，采图时句柄会单调上涨）；
    /// 2) UI 线程 <see cref="OnRender"/> 复用同一块 <see cref="WriteableBitmap"/> 做 WritePixels 后 DrawImage，
    ///    避免跨线程 Freezable 错误，且位图数量恒定为 1。
    /// </para>
    /// </summary>
    public class ImageRender : RenderBase, IDisposable
    {

        object _image = null;
        public ImageRender(object image)
        {
            _image = image;
        }

        /// <summary>
        /// 复用同一 ImageRender 实例时更新源图与变换（由 RenderSession 调用，避免每帧 new ImageRender）。
        /// </summary>
        public void Prepare(object image, ICoordTransform renderTransform)
        {
            _image = image;
            RenderTransform = renderTransform;
        }

        public Size ImageSize
        {
            get; private set;
        }
        private HPImageViewer.Core.Primitives.Rect ImageDeviceRect => this.RenderTransform.ToDevice(new HPImageViewer.Core.Primitives.Rect(0, 0, ImageSize.Width, ImageSize.Height));

        protected override bool NeedRender(RenderContext renderContext)
        {
            if (ImageSize.Width <= 0 || ImageSize.Height <= 0 || _frontPixels == null) return false;
            if (ImageDeviceRect.IntersectsWith(new HPImageViewer.Core.Primitives.Rect(0, 0, renderContext.RenderSize.Width, renderContext.RenderSize.Height)) == false) return false;

            //是否落到区域内
            return base.NeedRender(renderContext);
        }

        protected override void OnRender(RenderContext renderContext)
        {
#if DEBUG
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Restart();
#endif
            // 仅在 UI 线程创建/写入 WriteableBitmap，避免跨线程 Freezable，且复用固定位图不涨句柄
            var bitmap = EnsureAndUploadUiBitmap();
            if (bitmap != null)
            {
                renderContext.DrawingContext.DrawImage(bitmap, renderContext.RenderTransform.ToDevice(_renderImageRect));
            }

#if DEBUG
            stopwatch.Stop();
#endif
        }

        HPImageViewer.Core.Primitives.Rect GetRoundedImagePixelArea(double imageWidth, double imageHeight, HPImageViewer.Core.Primitives.Rect renderImagePixelArea)
        {
            var left = Math.Floor(renderImagePixelArea.Left);
            if (left < 0)
            {
                left = 0;
            }
            var top = Math.Floor(renderImagePixelArea.Top);
            if (top < 0)
            {
                top = 0;
            }

            var right = Math.Ceiling(renderImagePixelArea.Right);
            if (right >= imageWidth)
            {
                right = imageWidth;
            }

            var bottom = Math.Ceiling(renderImagePixelArea.Bottom);
            if (bottom >= imageHeight)
            {
                bottom = imageHeight;
            }

            return new HPImageViewer.Core.Primitives.Rect(left, top, right - left, bottom - top);
        }

        /// <summary>
        /// 后台线程：将源图缩放到设备尺寸并写入托管像素双缓冲，发布为 front 供 UI 上传。
        /// </summary>
        public void Calculate(RenderContext renderContext)
        {
#if DEBUG
            var stopwatch = new Stopwatch();
#endif

            var deviceDrawingArea = new HPImageViewer.Core.Primitives.Rect(0, 0, renderContext.RenderSize.Width, renderContext.RenderSize.Height);
            using var indexer = AggregationIndexerFactory.Instance.CreatePixelDataIndexer(_image);
            if (!indexer.HasPixelData)
            {
                return;
            }
            ImageSize = indexer.ImageSize;
            var imageDeviceRectTemp = ImageDeviceRect;
            imageDeviceRectTemp.Intersect(deviceDrawingArea);
            if (imageDeviceRectTemp.IsEmpty)
            {
                return;
            }

            var renderDeviceArea = deviceDrawingArea;
            var renderImagePixelArea = renderContext.RenderTransform.ToDomain(renderDeviceArea);

            var roundedImagePixelRect = GetRoundedImagePixelArea(ImageSize.Width, ImageSize.Height, renderImagePixelArea);

            var finalRoundedImagePixelRect = new HPImageViewer.Core.Primitives.Rect(roundedImagePixelRect.X, roundedImagePixelRect.Y, roundedImagePixelRect.Width, roundedImagePixelRect.Height);
            var deviceDrawingWidth = deviceDrawingArea.Width < 1 ? 1 : deviceDrawingArea.Width;
            var deviceDrawingHeight = deviceDrawingArea.Height < 1 ? 1 : deviceDrawingArea.Height;

#if DEBUG
            stopwatch.Restart();
#endif

            var channelCount = indexer.ChannelCount;
            var pixelFormats = channelCount == 1 ? PixelFormats.Gray8 : PixelFormats.Rgb24;
            var resizeHeight = (int)Math.Ceiling(deviceDrawingHeight);
            var resizeWidth = (int)Math.Ceiling(deviceDrawingWidth);
            // 布局抖动时 RenderSize 可能异常巨大，按设备尺寸分配会把 UI/内存打满
            const int maxRenderDimension = 8192;
            if (resizeWidth <= 0 || resizeHeight <= 0
                || resizeWidth > maxRenderDimension || resizeHeight > maxRenderDimension)
            {
                return;
            }

            // 后台只写托管 byte[]，不创建 WriteableBitmap（避免每帧 Freeze 产生 Section 句柄）
            var stride = resizeWidth * channelCount;
            var buffer = AcquireBackBuffer(stride * resizeHeight);
            FillPixelBuffer(indexer, finalRoundedImagePixelRect, resizeWidth, resizeHeight, channelCount, stride, buffer);

            // 发布 front：UI OnRender 读取这些字段做 WritePixels
            _renderImageRect = finalRoundedImagePixelRect;
            _frontWidth = resizeWidth;
            _frontHeight = resizeHeight;
            _frontStride = stride;
            _frontFormat = pixelFormats;
            _frontPixels = buffer;
            _useBufferA = !_useBufferA;

#if DEBUG
            stopwatch.Stop();
#endif
        }


        // ---- 像素双缓冲（托管数组，无 OS 句柄）+ UI 侧单一 WriteableBitmap ----
        private byte[] _bufferA;
        private byte[] _bufferB;
        private bool _useBufferA = true;
        private volatile byte[] _frontPixels;
        /// <summary>UI 正在 WritePixels 的缓冲；Calculate 不得覆写同一数组。</summary>
        private volatile byte[] _uiBusyBuffer;
        private int _frontWidth;
        private int _frontHeight;
        private int _frontStride;
        private PixelFormat _frontFormat;
        private HPImageViewer.Core.Primitives.Rect _renderImageRect;
        /// <summary>仅 UI 线程创建与写入，尺寸变化时才重建。</summary>
        private WriteableBitmap _uiBitmap;

        /// <summary>
        /// 取 back 缓冲；若 UI 仍在读该数组则新分配，避免与 WritePixels 竞态。
        /// </summary>
        private byte[] AcquireBackBuffer(int byteCount)
        {
            var back = _useBufferA ? _bufferB : _bufferA;
            // UI 仍在读该缓冲时不可覆写，改用新数组（托管内存，不产生句柄）
            if (back == null || back.Length != byteCount || ReferenceEquals(back, _uiBusyBuffer))
            {
                back = new byte[byteCount];
            }

            if (_useBufferA)
                _bufferB = back;
            else
                _bufferA = back;

            return back;
        }

        /// <summary>
        /// UI 线程：复用同一 WriteableBitmap，将最新像素缓冲 WritePixels 后返回供 DrawImage。
        /// </summary>
        private WriteableBitmap EnsureAndUploadUiBitmap()
        {
            var pixels = _frontPixels;
            var width = _frontWidth;
            var height = _frontHeight;
            var stride = _frontStride;
            var format = _frontFormat;
            if (pixels == null || width <= 0 || height <= 0 || stride <= 0)
            {
                return null;
            }

            // 仅分辨率/格式变化时重建，正常 Live 路径始终复用，句柄数恒定
            if (_uiBitmap == null
                || _uiBitmap.PixelWidth != width
                || _uiBitmap.PixelHeight != height
                || _uiBitmap.Format != format)
            {
                _uiBitmap = new WriteableBitmap(width, height, 96, 96, format, null);
            }

            _uiBusyBuffer = pixels;
            try
            {
                _uiBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
            }
            finally
            {
                _uiBusyBuffer = null;
            }

            return _uiBitmap;
        }

        public void Dispose()
        {
            _frontPixels = null;
            _uiBusyBuffer = null;
            _bufferA = null;
            _bufferB = null;
            _uiBitmap = null;
            _renderImageRect = default;
            _frontWidth = 0;
            _frontHeight = 0;
            _frontStride = 0;
        }

        /// <summary>
        /// 将源图像素采样写入 dest（设备尺寸）。使用数组下标而非 fixed 指针，以便 Parallel.For。
        /// </summary>
        private static void FillPixelBuffer(
            PixelDataIndexer indexer,
            Core.Primitives.Rect clipImageRect,
            int resizeWidth,
            int resizeHeight,
            int byteCountPerPixel,
            int stride,
            byte[] dest)
        {
            var scaleX = clipImageRect.Width / resizeWidth;
            var scaleY = clipImageRect.Height / resizeHeight;
            var startCol = clipImageRect.Left;
            var startRow = clipImageRect.Top;

            indexer.LockData();
            try
            {
                // 用数组下标写入，避免 fixed 指针进入 Parallel.For lambda（CS1764）
                Parallel.For(0, resizeHeight, row =>
                {
                    var rowInClip = Math.Floor(scaleY * row);
                    var rowInSrc = startRow + rowInClip;
                    var rowOffset = row * stride;
                    for (var i = 0; i < resizeWidth; i++)
                    {
                        var colInClip = Math.Floor(scaleX * i);
                        var colInSrc = startCol + colInClip;
                        var pixelOffset = rowOffset + i * byteCountPerPixel;

                        for (var j = 0; j < byteCountPerPixel; j++)
                        {
                            dest[pixelOffset + j] = indexer.GetPixelData(j, (int)rowInSrc, (int)colInSrc);
                        }
                    }
                });
            }
            finally
            {
                indexer.UnlockData();
            }
        }

        /// <summary>
        /// 兼容旧调用（会新建位图；连续显示路径请走 Calculate + OnRender 复用）。
        /// </summary>
        public static WriteableBitmap ToWriteableBitmap(PixelDataIndexer indexer, Core.Primitives.Rect clipImageRect, Core.Primitives.Size deviceMapSize)
        {
            var channelCount = indexer.ChannelCount;
            var pixelFormats = channelCount == 1 ? PixelFormats.Gray8 : PixelFormats.Rgb24;
            var resizeHeight = (int)Math.Ceiling(deviceMapSize.Height);
            var resizeWidth = (int)Math.Ceiling(deviceMapSize.Width);
            if (resizeWidth <= 0 || resizeHeight <= 0)
            {
                return null;
            }

            var stride = resizeWidth * channelCount;
            var buffer = new byte[stride * resizeHeight];
            FillPixelBuffer(indexer, clipImageRect, resizeWidth, resizeHeight, channelCount, stride, buffer);
            var dest = new WriteableBitmap(resizeWidth, resizeHeight, 96, 96, pixelFormats, null);
            dest.WritePixels(new Int32Rect(0, 0, resizeWidth, resizeHeight), buffer, stride, 0);
            return dest;
        }
    }
}
