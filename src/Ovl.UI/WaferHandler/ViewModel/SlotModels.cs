using CommunityToolkit.Mvvm.ComponentModel;
using Marvel.DataModels.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ovl.UI.WaferHandler.ViewModel
{
    public partial class SlotSource : ObservableObject
    {
        [ObservableProperty]
        SlotItem _slotItem;
    }
    public partial class SlotItem : ObservableObject
    {
        [ObservableProperty]
        string _id;
        [ObservableProperty]
        int _minIndex;
        [ObservableProperty]
        int _maxIndex;
        [ObservableProperty]
        string _value;
        [ObservableProperty]
        EnumSlotSource _portNo;
        [ObservableProperty]
        EnumSlotSource _sourcePortNo;
        /// <summary>
        /// 0：正常，1：警告，2：报警,3：无
        /// </summary>
        [ObservableProperty]
        EnumSlotStatus _status;

        public Action<string, SlotItem, SlotItem> CallbackDrag;


    }
}
