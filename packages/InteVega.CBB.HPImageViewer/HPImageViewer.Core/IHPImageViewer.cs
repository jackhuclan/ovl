using System;
using System.Collections.Generic;
using HPImageViewer.Core.Persistence;

namespace HPImageViewer.Core
{
    public interface IHPImageViewer : IDocument
    {
        [Obsolete("请使用Image属性")]
        public void SetImage(object Image);
        public object Image { get; set; }
        public void AddROIs(params ROIDesc[] rois);
        public void UpdateROIs(params ROIDesc[] rois);
        

        void FitImageToArea();

        /// <summary>
        /// 是否是实时采图模式
        /// </summary>
        bool IsAlive { set; }
        public ToolType ActivatedTool { 
            get; 
            set; 
        }

        event EventHandler ShapeDrawCompleted;

        IEnumerable<IROIRender> ROIRenders { get; }
    }
}
