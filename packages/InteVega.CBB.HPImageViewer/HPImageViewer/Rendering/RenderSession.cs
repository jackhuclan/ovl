using System;
using System.Threading;
using System.Threading.Tasks;

namespace HPImageViewer.Rendering
{
    internal class RenderSession
    {
        IDrawingCanvas _drawingCanvas;
        public RenderContext RenderContext { get; }
        public RenderSession(IDrawingCanvas drawingCanvas, RenderContext renderContext)
        {
            _drawingCanvas = drawingCanvas;
            RenderContext = renderContext;

        }

        public async Task RenderDataAsync(CancellationToken cancellationToken, RenderContext renderContext, Func<CancellationToken, Task> invalidateAction, SemaphoreSlim semaphoreSlim)
        {
            try
            {
                await semaphoreSlim.WaitAsync(cancellationToken);
            }
            catch
            {
                return;
            }

            try
            {

                ImageRender imageRender = null;
                if (renderContext.Image != null)
                {
                    await Task.Run(() =>
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }
                        //  imageRender = new ImageRender(renderContext.Image) { RenderTransform = renderContext.RenderTransform };
                        imageRender.Calculate(renderContext);

                    }, cancellationToken);

                }

                if (cancellationToken.IsCancellationRequested == false)
                {
                    _drawingCanvas.ImageRender = imageRender;
                    await invalidateAction(cancellationToken);

                }

            }
            finally
            {
                semaphoreSlim.Release();
            }


        }

        /// <summary>
        /// 计算本帧 ImageRender：复用画布上已有实例（内部托管双缓冲 + UI 侧单一 WriteableBitmap），
        /// 避免每帧 new ImageRender / new WriteableBitmap 导致句柄堆积。
        /// </summary>
        public ImageRender RenderData()
        {
            if (RenderContext.Image == null)
            {
                return null;
            }

            // 复用画布上的 ImageRender，避免每帧 new；位图由 ImageRender 在 UI 侧复用
            var imageRender = _drawingCanvas.ImageRender;
            if (imageRender == null)
            {
                imageRender = new ImageRender(RenderContext.Image) { RenderTransform = RenderContext.RenderTransform };
            }
            else
            {
                imageRender.Prepare(RenderContext.Image, RenderContext.RenderTransform);
            }

            imageRender.Calculate(RenderContext);
            return imageRender;
        }

    }
}
