using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPImageViewer.Core.Persistence;
using InteVega.CBB.Common.Algorithm.Shapes;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.CommonLib.Helpers.Reflection;
using InteVega.CBB.DomainService.Api.Extensions;
using Marvel.DataModels.Extensions;
using Marvel.DataModels.Recipe.CD;
using Megarobo.Ovl.Type;
using OVL.Algorithm.DoNet;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Wafer.Algorithm.Common.DoNet;

using Ovl.UI.ViewModels.Extensions;
namespace Ovl.UI.ViewModels.Recipe.ViewModel
{
    /// <summary>
    /// ROI和定位模板设置
    /// </summary>
    public partial class Test_TrainCDParamViewModelBase
    {
        [ObservableProperty]
        private bool _areaROIEnabled;

        #region ROIMultiRoi 量测ROI的各种操作
        #region 事件
        /// <summary>
        /// 选择Orientention时，更改角度值
        /// </summary>
        /// <param name="obj"></param>
        [RelayCommand]
        private void CDOriententionChanged(object obj)
        {
            var multiRoi = ((MultiRoiInfo)obj);
            switch (multiRoi.CDOrientention)
            {
                case CD_Orientention.Horizontal:
                    multiRoi.Angle = 180;
                    break;
                case CD_Orientention.Vertical:
                    multiRoi.Angle = 90;
                    break;
            }
            var selectRoi = MultiRoiList.FirstOrDefault(f => f.Id == multiRoi.Id);
            selectRoi.Angle = multiRoi.Angle;
            selectRoi.MeasureROI = null;

            SetMultiRoiList(obj, false);
        }
        /// <summary>
        /// 设置多维量测ROI
        /// </summary>
        /// <param name="obj"></param>
        [RelayCommand]
        private void SetROIMultiRoi(object obj)
        {
            if (!isTrainedFocus)
            {
                _messageBoxService.ErrorDialog($"Please complete the focusing operation!");
                return;
            }

            HpImageViewer.Image = LoadFocusImageData(true);
            if (HpImageViewer.Image == null)
            {
                return;
            }
            //停止采集
            _StageController.StopGrabbing();
            //获取当前的ROI数据
            var multiRoi = ((MultiRoiInfo)obj);
            //获取ROI
            List<ROIDesc> roiList = multiRoi.MeasureROI != null ? new List<ROIDesc>() { multiRoi.MeasureROI } : null;
            //创建or打开量测ROI
            if (!SetMeasureRoi(multiRoi.CDOrientention, roiList)) { return; }
            //控制MultROI grid 的显示和数据绑定
            SetMultiRoiList(multiRoi, true);
        }

        /// <summary>
        /// 确认多维量测ROI
        /// </summary>
        /// <param name="obj"></param>
        [RelayCommand]
        private void ConfirmMultiRoi(object obj)
        {
            var multiRoi = ((MultiRoiInfo)obj);
            multiRoi.ROIEdgeList = null;
            //保存量测ROI
            if (!SaveMultiMeasureRoi(multiRoi)) { return; }


            //执行量测ROI的聚焦
            //当所有ROI都设置完后进行量测ROI的聚焦
            if (!MultiRoiList.Any(a => a.MeasureROI == null))
            {
                FocusByMeasureRoi();
                //设置完后需要重新模板定位，如果未能定位模板则量测失败
                IsNeedCreateModel = true;
            }

            HpImageViewer.UpdateROIs();
            TrainSetting_CD.MultiRoiList = MultiRoiList;
        }

        /// <summary>
        /// 关闭量测ROI
        /// </summary>
        /// <param name="obj"></param>
        [RelayCommand]
        private void CancelMultiRoi(object obj)
        {
            HpImageViewer.UpdateROIs();
            SetMultiRoiList(obj, false);
        }
        /// <summary>
        /// 重置量测ROI
        /// </summary>
        /// <param name="obj"></param>
        [RelayCommand]
        private void ResetMultiRoi(object obj)
        {
            var multiRoi = ((MultiRoiInfo)obj);
            if (!SetMeasureRoi(multiRoi.CDOrientention, null)) { return; }

            SetMultiRoiList(obj, true);
        }

        #endregion
        #region MultiRoi 量测方法
        /// <summary>
        /// 加载多维量测列表
        /// </summary>
        void LoadCDMultiRoi()
        {
            if (TrainSetting_CD.MultiRoiList == null || TrainSetting_CD.MultiRoiList.Count == 0)
            {
                TrainSetting_CD.MultiRoiList = CloneHelper.ReflectionClone<ObservableCollection<MultiRoiInfo>>(TestInfo.TrainSetting_CD.MultiRoiList);
                if (TrainSetting_CD.MultiRoiList != null)
                {
                    foreach (var item in TrainSetting_CD.MultiRoiList)
                    {
                        item.MeasureROI = null;
                    }
                }
            }
            MultiRoiList = TrainSetting_CD.MultiRoiList;
            SetMultiRoiList(null, false);
        }
        /// <summary>
        /// 设置多维量测列表的各种控制和数据绑定
        /// </summary>
        /// <param name="multiRoi"></param>
        /// <param name="isSetROI"></param>
        private void SetMultiRoiList(object multiRoi, bool isSetROI)
        {

            foreach (var item in MultiRoiList)
            {
                item.IsSetROI = false;
                if (item.CDOrientention != CD_Orientention.User_Defined)
                {
                    item.IsEnableAngle = false;
                }
                else
                {
                    item.IsEnableAngle = true;
                }
            }
            if (multiRoi != null)
            {
                var id = ((MultiRoiInfo)multiRoi).Id;
                MultiRoiList.FirstOrDefault(f => f.Id == id).IsSetROI = isSetROI;
            }

        }
        /// <summary>
        /// 保存多维量测ROI，并创建FocusKernel
        /// </summary>
        /// <param name="multiRoi"></param>
        /// <returns></returns>
        bool SaveMultiMeasureRoi(MultiRoiInfo multiRoi)
        {
            if (HpImageViewer.Image == null)
            {
                _messageBoxService.InfoDialog("The current image data is invalid！");
                return false;
            }
            var roiDescs = HpImageViewer.ImageViewerDesc.ROIDescs;
            if (!(roiDescs?.Any() ?? false))
            {
                _messageBoxService.ErrorDialog("None Roi, Please 'Cancel' and 'Set' to Create ROI!", "Error");
                return false;
            }
            try
            {
                MultiRoiList.FirstOrDefault(f => f.Id == multiRoi.Id).MeasureROI = roiDescs.FirstOrDefault();
                SetMultiRoiList(multiRoi, false);
                //调用GenCDModelInfo接口生成CD量测的模板信息
                CDROIBase cdRoi = MultiRoiList.CreateMultiDimensionROI(IsAcrossTheCameraVision);
                FocusKernelModel focusKernelModel = _focusService.GetImageKernel((ImageData)HpImageViewer.Image, cdRoi.ToDimension2d());
                TrainSetting_CD.FocusKernelModel = focusKernelModel;
                LogHelper.LogInfo($"Save FocusKernel.");
                SaveVisionParams();
                return true;
            }
            catch
            {
                _messageBoxService.ErrorDialog("Roi type error, please reset!", "Error");
                return false;
            }
        }

        #endregion
        #endregion
        #region 选择搜索区域
        [RelayCommand]
        void AreaRoiSet()
        {
            AreaROIEnabled = true;
        }
        [RelayCommand]
        void AreaRoiConfirm()
        {
            var rOIDescs = HpImageViewer.ImageViewerDesc.ROIDescs.FirstOrDefault();
            if (rOIDescs == null || rOIDescs is not RectangleDesc desc)
            {
                _messageBoxService.InfoDialog("Please draw the Rect ROI！");
                return;
            }
            SetSearchingRect(desc.CreateRoi() as Rect2d);
            AreaROIEnabled = false;
            HpImageViewer.UpdateROIs();
        }
        [RelayCommand]
        void AreaRoiCancel()
        {
            AreaROIEnabled = false;
        }
        [RelayCommand]
        void AreaRoiClear()
        {
            SetSearchingRect(null);
            HpImageViewer.UpdateROIs();
            AreaROIEnabled = false;
        }
        private void SetSearchingRect(Rect2d rect)
        {
            TrainSetting_CD?.MarkModels[0]?.UpdateSearchArea(rect);
        }

        internal IEnumerable<CDCurve> GetMultiRoiProfileCurves(ImageData imagedata)
        {
            var roi = MultiRoiList.CreateMultiDimensionROI(IsAcrossTheCameraVision);
            return _measService.GetCDProfileForImageView(imagedata, roi);
        }

        #endregion
    }
}
