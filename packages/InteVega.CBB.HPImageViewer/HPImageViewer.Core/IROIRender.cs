using System;
using System.Collections.Generic;
using System.Text;

namespace HPImageViewer.Core
{
    public interface IROIRender
    {
        public Guid Id { get; set; }
        public bool IsSelected { get; set; }
        public bool IsLocked { get; set; }
        public string Color { get; set; }
    }
}
