using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.EfemApi;
using Marvel.DataModels.Enums;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace Ovl.UI.WaferHandler.ViewModel
{
    public partial class EditWaferInfoViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _positionName;

        [ObservableProperty]
        private WaferSize _selectedWaferSize;

        [ObservableProperty]
        private EnumSlotSource _selectedSourcePort;

        [ObservableProperty]
        private int _sourceSlot = 1;

        public ObservableCollection<WaferSize> WaferSizes { get; } = new();
        public ObservableCollection<EnumSlotSource> SourcePorts { get; } = new();

        public Action<bool>? CloseAction { get; set; }

        public EditWaferInfoViewModel(
            EnumSlotSource position,
            WaferSize waferSize,
            int sourcePort,
            int sourceSlot,
            bool includePort3 = false)
        {
            PositionName = position.ToString();

            foreach (var item in new[] { WaferSize.INCH_6, WaferSize.INCH_8, WaferSize.INCH_12 })
            {
                WaferSizes.Add(item);
            }

            SourcePorts.Add(EnumSlotSource.Port1);
            SourcePorts.Add(EnumSlotSource.Port2);
            if (includePort3)
            {
                SourcePorts.Add(EnumSlotSource.Port3);
            }

            SelectedWaferSize = waferSize == WaferSize.None ? WaferSize.INCH_6 : waferSize;

            var selectedPort = Enum.IsDefined(typeof(EnumSlotSource), sourcePort)
                ? (EnumSlotSource)sourcePort
                : EnumSlotSource.Port1;
            if (!SourcePorts.Contains(selectedPort))
            {
                selectedPort = EnumSlotSource.Port1;
            }

            SelectedSourcePort = selectedPort;
            SourceSlot = sourceSlot <= 0 ? 1 : sourceSlot;
        }

        [RelayCommand]
        private void Save()
        {
            if (SourceSlot < 1 || SourceSlot > 25)
            {
                MessageBox.Show("Source Slot must be between 1 and 25.", "Validation");
                return;
            }

            if (!SourcePorts.Contains(SelectedSourcePort))
            {
                MessageBox.Show("Source LP is invalid.", "Validation");
                return;
            }

            CloseAction?.Invoke(true);
        }

        [RelayCommand]
        private void Cancel()
        {
            CloseAction?.Invoke(false);
        }
    }
}