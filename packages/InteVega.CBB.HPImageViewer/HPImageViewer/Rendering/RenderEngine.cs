using System;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow;

namespace HPImageViewer.Rendering
{
    internal class RenderEngine
    {
        private readonly ActionBlock<RenderDataItem> _renderDataSessionTransformBlock;

        private long _currentRenderSessionTimestampTick = 0;
        public event EventHandler<RenderSet> RenderRequested;
        private readonly object _syncLock = new object();
        public RenderEngine()
        {
            var executionDataflowBlockOptions = new ExecutionDataflowBlockOptions();
            executionDataflowBlockOptions.EnsureOrdered = true;
            executionDataflowBlockOptions.MaxDegreeOfParallelism = 1;
            // 有界队列：忙时丢弃更新的 Post，只保留 1 个待处理帧；入队帧一律渲染（不再用 20ms 节流跳过）
            executionDataflowBlockOptions.BoundedCapacity = 1;
            _renderDataSessionTransformBlock = new ActionBlock<RenderDataItem>(renderDataItem =>
            {
                try
                {
                    lock (_syncLock)
                    {
                        // 只渲染更新的 session，过期帧直接跳过
                        if (renderDataItem.TimestampTick < _currentRenderSessionTimestampTick)
                            return;
                        _currentRenderSessionTimestampTick = renderDataItem.TimestampTick;
                    }

                    var imageRender = renderDataItem.RenderSession.RenderData();
                    RenderRequested?.Invoke(this, new RenderSet(renderDataItem.RenderSession.RenderContext) { ImageRender = imageRender });
                }
                catch
                {
                    // 单帧失败不得让 ActionBlock Faulted，否则后续所有渲染都会停掉
                }
            }, executionDataflowBlockOptions);



        }
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        public void PostRenderSession(RenderSession renderSession)
        {
            _renderDataSessionTransformBlock.Post(new RenderDataItem(renderSession, _stopwatch.ElapsedTicks));
        }


        class RenderDataItem
        {
            public RenderSession RenderSession { get; }
            public long TimestampTick { get; }
            public RenderDataItem(RenderSession renderSession, long timestampTick)
            {
                RenderSession = renderSession;
                TimestampTick = timestampTick;
            }



        }



    }
}
