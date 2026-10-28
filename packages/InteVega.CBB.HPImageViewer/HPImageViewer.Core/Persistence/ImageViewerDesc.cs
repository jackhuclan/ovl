using System;
using System.Collections.Generic;

namespace HPImageViewer.Core.Persistence
{
    [Serializable]
    public class ImageViewerDesc
    {
        public ImageViewerDesc()
        {
            Id = Guid.NewGuid();
        }
        public Guid Id { get; set; }

        public List<ROIDesc> ROIDescs
        {
            get;
            set;
        } = new List<ROIDesc>();



    }
}
