using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HPImageViewer;
using HPImageViewer.RoutedEventArgs;
using InteVega.CBB.Common.Algorithm.Shapes;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.ComUI.Helpers;
using Bases = Ovl.UI.Bases;
//using InteVega.CBB.ImageDataExtensions;
//using Marvel.DomainService.Api.Interfaces;
//using Marvel.DomainService.Api.Models;
//using Marvel.Service.Cal;
//using Marvel.Service.Unit;
//using Meta.Type;

using InteVega.CBB.DomainService.Api.Extensions;
using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.DomainService.Api.Models;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using InteVega.CBB.Recipe.Core;
using InteVega.CBB.Recipe.Interface;
using InteVega.CBB.Recipe.Ui.ViewModel;
using InteVega.CBB.SectionManager.Interface;
using InteVega.CBB.SectionManager.Interface.Model;
using Marvel.Application.Services;

//using Marvel.Application.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using InteVega.CBB.MotionProof.Abstraction;
using Marvel.DataModels.Core;
using Marvel.DataModels.Defect;
using Marvel.DataModels.Recipe;
using Marvel.Interfaces.Application;
using Megarobo.Ovl.Service.IntfFn;
using Megarobo.Ovl.Service.IntfFn.fn;
using Megarobo.Ovl.Type;
using Microsoft.Extensions.DependencyInjection;
using Prism.Services.Dialogs;


using Ovl.UI.Controls;

namespace Ovl.UI.Defect.ViewModel;

public partial class DefectDetectViewModel : Bases.ViewModelBase
{
    const string jsonExt = ".mgjson";
    const string filter = $"mg文件|*{jsonExt}";
    static string _lastRecipeId = "";
    /// <summary>
    /// The title of the dialog that will show in the window title bar.
    /// </summary>
    public override string Title { get; set; } = "Defect Detect";

    protected ECamera _coarseAlignCamera = ECamera.Align;
    public DefectDetectViewModel(IServiceProvider serviceProvicder)
    {
        _serviceProvider = serviceProvicder;
        _alignmentService = serviceProvicder.GetService<IAlignmentService>();
        _mainRunThread = serviceProvicder.GetService<IMainRunThread>();
        _motionService = serviceProvicder.GetService<IMotionControllerService>();
        _waferService = serviceProvicder.GetService<IWaferService>();
        _defectDataService = serviceProvicder.GetService<IDefectDataService>();
        _messageBoxService = serviceProvicder.GetService<IMessageBoxService>();
        _calService = serviceProvicder.GetService<IServiceCalData>();
        _visionService = serviceProvicder.GetService<IVisionServiceManager>().GetMainVision();
        _focusService = serviceProvicder.GetService<IFocusService>();
        _servicePzt = serviceProvicder.GetService<IAsyncAblePztService>();
        UnSubsribeEvents();
        _visionService.OpticalConfigChanging += _visionService_OpticalConfigChanging;
        _visionService.OpticalConfigChanged += _visionService_OpticalConfigChanged;
        _waferService.StampProcessAction += StampProcessChanged;
        RecipeMenuViewModel recipeMenuViewModel = serviceProvicder.GetService<RecipeMenuViewModel>();

        RecipeMenuViewModel = recipeMenuViewModel;
        RecipeMenuViewModel.IsShowContextMenu = Visibility.Collapsed;
        recipeMenuViewModel.OutSideLoadMenuTree();
        recipeMenuViewModel.SearchRecipe("");
        RecipeMenuViewModel.GetRecipe = (node, recipe) => OpenRecipe(node, recipe);

    }



   protected  IServiceProvider _serviceProvider;
   protected  IMessageBoxService _messageBoxService;
    IAlignmentService _alignmentService;
    IMainRunThread _mainRunThread;
   protected  IMotionControllerService _motionService;
    IWaferService _waferService;
    IDefectDataService _defectDataService;
    IServiceCalData _calService;
   protected  IVisionService _visionService;
    IFocusService _focusService;
    IAsyncAblePztService _servicePzt;

    public ImageViewer ImageViewer { get; set; }
    public WaferMapStageControl WaferStageControl { get; set; }
    [ObservableProperty]
    private RecipeElementsInfo _selectedRecipe;

    [ObservableProperty]
    RecipeMenuViewModel _recipeMenuViewModel;


    [ObservableProperty]
    Visibility _isShowContent = Visibility.Collapsed;
    [ObservableProperty]
    Visibility _isShowFirst = Visibility.Visible;
    [ObservableProperty]
    bool isRunning = false;

    [ObservableProperty]
    BitmapSource _waferImage;

    [ObservableProperty]
    DefectResultInfo _defectResult;
    [ObservableProperty]
    DefectInfo _selectedDefect;

    [ObservableProperty]
    string _waferName;
    [ObservableProperty]
    double _imageWidth;
    [ObservableProperty]
    double _imageHeight;
    [ObservableProperty]
    string _changeCameraInfo;
    [ObservableProperty]
    Visibility _fineFocusVisible = Visibility.Collapsed;
    [ObservableProperty]
    Visibility _focus40Visible = Visibility.Collapsed;
    private string _currentFile = string.Empty;

    double alignPercent = 5;
    double loadImagePercent;
    /// <summary>
    /// 获取当前相机下的晶圆中心的机台坐标
    /// </summary>
    /// <returns>返回机台坐标</returns>
    private Point2d GetCowAxisPoint()
    {
        return _alignmentService.GetAxisPoint(new WcsPoint(), _coarseAlignCamera.GetCameraWaferCoordi(),CameraAttr.Default(null));
    }

    public void OpenRecipe(IRecipeManager recipeManager, RecipeInfo recipeInfo)
    {
        Task.Run(() =>
        {
            ProgressDialogHelper.ShowDialog("Loading recipe...", () =>
            {

                try
                {
                    IRecipe recipeNodeService = recipeManager.GetCurrentRecipe(recipeInfo.Id);
                    string recipeName = recipeInfo.RecipeName;
                    var recipeElementsInfo = recipeNodeService.Get<RecipeElementsInfo>();
                    if (recipeElementsInfo == null)
                    {
                        recipeElementsInfo = new RecipeElementsInfo() { Id = recipeInfo.Id };
                    }
                    recipeElementsInfo.Name = recipeName;
                    SelectedRecipe = recipeElementsInfo;
                    _alignmentService.Init(recipeElementsInfo);
                    AppHelper.DispatcherInvoke(() =>
                    {
                        IsShowContent = Visibility.Visible;
                        IsShowFirst = Visibility.Collapsed;
                        WaferStageControl.Init(_alignmentService, _alignmentService.WaferMapInfo);
                    });
                    _visionService.StartGrabbing();
                }
                catch (Exception ex)
                {
                    string err = "Failed to open recipe! Please make sure that the file exists and that the file format is not corrupted";
                    _messageBoxService.ErrorDialog(err);
                    LogHelper.LogError(ex, "Select recipe for job errored!");
                    return false;
                }
                _lastRecipeId = recipeInfo.Id;
                GetCameraInfo();
                return true;
            });
        });
    }
    private void InvokeAlign(double totalPercent)
    {
        LogHelper.LogRunMethodStart();
        SubscribeRunErr();
        bool isAligning = true;
        //添加拉平过程的模拟进度
        Task.Run(() =>
        {
            for (int i = 1; i <= 10; i++)
            {
                if (!IsRunning || !isAligning)
                    break;
                Thread.Sleep(1000);
                double p = new Random().NextDouble() * 0.1 + 0.9;
                double progress = totalPercent * p / 10 * i;
                ProgressShowCancelDialogHelper.UpdateProgressValueInfo(progress, "Aligning...");
            }
        });
        _serviceProvider.GetService<ISectionManager>().SetSectionToJob(new JobSectionInfo("ScanWafer", DateTime.Now.ToString("HHmmss_fff"), SelectedRecipe.Name));
        _alignmentService.Init(SelectedRecipe, true);
        _mainRunThread.ResetStatus();

        //_motionService.SingleAxisToPoint(AxisType.T, SelectedRecipe.Alignment.StageAngle, false);
        _alignmentService.RotateTo(SelectedRecipe.Alignment.StageAngle);
        _mainRunThread.FocusAlign(true, SelectedRecipe, SelectedRecipe.RunParameter);

        isAligning = false;
        ProgressShowCancelDialogHelper.UpdateProgressValueInfo(totalPercent, "Aligning...");
        Thread.Sleep(100);
        LogHelper.LogRunMethodEnd();
    }

    private void NewScanJob()
    {

    }

    private void InvokeScan()
    {
        //像素当量在此处需要除以缩放比
        var zoom = _visionService.CurrentStageCameraConfig.Zoom;
        var ratio = _visionService.CurrentStageCameraConfig.ScanRangeRatio;
        ratio = Math.Max(ratio, 0.01);
        ratio = Math.Min(ratio, 1);
        var diameter = SelectedRecipe.WaferMap.Diameter;
        double length = diameter * ratio; //扫描范围的长度 （机台坐标）
        //获取晶圆中心的机台坐标
        var cow = GetCowAxisPoint();

        CachePanorama(panoramCache =>
          {
              //现在扫描范围在0.1-1之间
              _waferService.ScanFullPicture(length, cow, panoramCache, zoom);
          });

        var pixelInfo = _calService.GetPixelSize(ECamera.Align,null);
        DefectResult = new DefectResultInfo() { RecipeID = SelectedRecipe.Id, RecipeName = SelectedRecipe.Name, DefectInfos = new ObservableCollection<DefectInfo>(), PixelEquivalentX = pixelInfo.Item1 / zoom, PixelEquivalentY = pixelInfo.Item2 / zoom };
        var imageData = _visionService.GrabOneImage();

        var pixelLeftTop = new Point();//图片的左上角就是扫描的起始机台坐标位置
        var pixelCowToLefTop = ConverToPixelPoint(new Point2d(length / 2, -length / 2));//晶圆中心相对于左上角的像素偏移就等于机台偏移量除以像素当量
        DefectResult.CowX = pixelLeftTop.X + pixelCowToLefTop.X;
        DefectResult.CowY = pixelLeftTop.Y + pixelCowToLefTop.Y;

    }



    /// <summary>
    /// 缓存全景图到文件，并且加载全景图到界面
    /// </summary>
    /// <param name="act">缓存全景图的方法（扫描或者拷贝）</param>
    private void CachePanorama(Action<string> act)
    {
        _defectDataService.CachePanorama(panoramCache =>
        {
            AppHelper.DispatcherInvoke(() => { ImageSourceHelper.ClearImage(WaferImage); });
            act?.Invoke(panoramCache);
            AppHelper.DispatcherInvoke(() =>
            {

                var imagedata = ImageData.ReadImage(panoramCache);
                ImageViewer.Image = imagedata;
                ImageWidth = imagedata.Width;
                ImageHeight = imagedata.Height;
                return;
                using (FileStream stream = new FileStream(panoramCache, FileMode.Open, FileAccess.Read))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze(); // 可选，使图片跨线程可用
                                     // 使用图片...
                    WaferImage = bitmap;
                    ImageWidth = bitmap.PixelWidth;
                    ImageHeight = bitmap.PixelHeight;

                }

            });
        });
    }



    private void RunAsync(string info, Action act)
    {
        LogHelper.LogInfo(info + $",IsRunning={IsRunning}");
        if (IsRunning) return;
        bool isStopping = false;
        IsRunning = true;
        Task task = Task.Run(() =>
        {
            try
            {
                act?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex);
                if (!isStopping)
                    _messageBoxService.ErrorDialog(ex.Message);
                return false;
            }
            finally
            {
                LogHelper.LogInfo("IsRunning=false");
                IsRunning = false;
                AssistantBase.ActionErrored = null;
                ProgressShowCancelDialogHelper.UpdateProgressValueInfo(0, "");
            }
        });
        ProgressShowCancelDialogHelper.ShowCancelDialog(info, task, () =>
        {
            if (!IsRunning)
            {
                CloseDialog();
                return;
            }
            var res = _messageBoxService.AskDialog("Are you sure you want to stop?");
            if (res != ButtonResult.OK)
                return;

            _alignmentService.Stop();
            _waferService.StopScan();
            isStopping = true;
        }, new InteVega.CBB.Common.Range(0, 100), null, CloseDialog);
    }

    private void CloseDialog()
    {
        try
        {
            ProgressShowCancelDialogHelper.CloseDialog();
        }
        catch
        { }
    }


    private void StampProcessChanged(int progress)
    {
        //将拉平过程分配5%的进度
        double percent = alignPercent + (100 - alignPercent - loadImagePercent) / 100 * progress;
        ProgressShowCancelDialogHelper.UpdateProgressValueInfo(percent, "Scaning...");
    }

    [RelayCommand]
    public void Scan()
    {
        RunAsync("Aligning...", () =>
        {

            //测试代码
            //CachePanorama(cachePath => { FileHelper.CopyFile(@"E:\截图\4.jpg", cachePath); });
            //pixelInfo.Item1 = _alignmentService.WaferMapInfo.Diameter / ImageWidth;
            //pixelInfo.Item2 = _alignmentService.WaferMapInfo.Diameter / ImageHeight;

            InvokeAlign(alignPercent);
            ProgressShowCancelDialogHelper.UpdateProgressValueInfo(alignPercent, "Scaning...");
            loadImagePercent = new Random().NextDouble() * 3;
            InvokeScan();
        });
    }

    [RelayCommand]
    public void OpenImage()
    {
        FileDialogHelper.OpenFileDialog(filter, filepath =>
         {
             ImageSourceHelper.ClearImage(WaferImage);
             ProgressDialogHelper.ShowDialog("Open defect file ...", () =>
             {
                 try
                 {
                     DefectResult = _defectDataService.OpenDefectFile(filepath);
                     if (DefectResult.RecipeID != SelectedRecipe.Id)
                     {
                         var res = _messageBoxService.AskDialog($"The defect file was not generated by the current recipe, it is generated by the recipe \"{DefectResult.RecipeName}\",Are you sure you want to open it?");
                         if (res != ButtonResult.OK)
                         {
                             return false;
                         }
                     }
                     CachePanorama((cacheFile) =>
                       {
                           //拷贝原始缺陷文件的全景图到缓存中
                           FileHelper.CopyFile(DefectResult.PanoramaPath, cacheFile);
                       });


                     WaferName = Path.GetFileNameWithoutExtension(filepath);
                     _currentFile = filepath;
                     return true;
                 }
                 catch (Exception ex)
                 {
                     LogHelper.LogError(ex);
                     _messageBoxService.ErrorDialog(ex.Message);
                     return false;
                 }
             });

         });
    }

    [RelayCommand]
    public void Adapt()
    {
        ImageViewer?.FitImageToArea();
    }
    [RelayCommand]
    public void Align()
    {
        RunAsync("Aligning...", () =>
         {
             InvokeAlign(100);
         });
    }


    [RelayCommand]
    public void CoarseFocus()
    {
        LogHelper.LogRunMethodStart();
        ChangeCameraInfo = "Coarse focusing ...";
        Task.Run(() =>
        {
            try
            {
                InvokeCoarseFocus();
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex);
                _messageBoxService.ErrorDialog(ex.Message);
            }
            ChangeCameraInfo = "";
            LogHelper.LogRunMethodEnd("", "CoarseFocus");
        });
    }

    protected virtual void InvokeCoarseFocus()
    {
        _mainRunThread.ExcuteCoarseFocus(SelectedRecipe, IServiceCamera.CurCamera);

        //var runFocusInfo = _visionService.GetSystemFocusInfo(IServiceCamera.CurCamera, false);
        //_alignmentService.GetCalData(IServiceCamera.CurCamera);
        ////训练得到的粗聚焦绝对高度
        //var trainedFocusHeight = SelectedRecipe.Alignment.FocusHeight + _alignmentService.MacroFocusZ;
        //runFocusInfo.StartPosition = trainedFocusHeight;
        //_focusService.CoarseFocus(runFocusInfo);
    }


    [RelayCommand]
    public  void FineFocus()
    {
        LogHelper.LogRunMethodStart();
        ChangeCameraInfo = "Fine focusing ...";
        Task.Run(() =>
        {
            try
            {
                InvokeFineFocus();
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex);
                _messageBoxService.ErrorDialog(ex.Message);
            }
            finally
            {
                ChangeCameraInfo = "";
                LogHelper.LogRunMethodEnd("", "FineFocus");
            }
        });
    }

    protected virtual void InvokeFineFocus()
    {
        if (IServiceCamera.CurCamera != ECamera.Meas) return;
        var focusInfo = _visionService.GetSystemFocusInfo(IServiceCamera.CurCamera, true);
        //var focusInfo = _visionService.CameraDeviceConfig.FocusInfo.FineFocusInfo;

        double pzt = _focusService.FineFocus(focusInfo, out _);
        _servicePzt?.AMov(pzt);
    }

    /// <summary>
    /// 添加Defect
    /// </summary>
    [RelayCommand]
    public void AddDefect()
    {
        if (DefectResult == null) return;
        DefectResult.DefectInfos.Add(new DefectInfo());
    }

    /// <summary>
    /// 删除Defect
    /// </summary>
    [RelayCommand]
    public void DeleteDefect()
    {
        if (DefectResult == null) return;
        if (SelectedDefect == null) return;
        DefectResult.DefectInfos.Remove(SelectedDefect);
    }

    /// <summary>
    /// 清空Defect
    /// </summary>
    [RelayCommand]
    public void ClearDefect()
    {
        if (DefectResult == null) return;
        DefectResult.DefectInfos.Clear();
    }


    [RelayCommand]
    public async void Goto()
    {
        if (SelectedDefect == null)
            return;
        await Task.Run(() =>
        {

            var point = _alignmentService.GetAxisPoint(SelectedDefect.Location, _coarseAlignCamera.GetCameraWaferCoordi(), CameraAttr.Default(null));

            _alignmentService.MoveTo(point, $"Defect:{SelectedDefect}");
        });
    }

    [RelayCommand]
    public async void GetR3()
    {
        if (SelectedDefect == null)
            return;


        await Task.Run(() =>
        {
            //获取field0左下角对应的机台坐标
            var cow = GetCowAxisPoint();
            var axisPoint = _alignmentService.GetCurrentAxisPosition();
            var r3 = axisPoint - cow;
            SelectedDefect.Location = WcsPoint.FromPoint2d(r3);

            //采集一张图片并存入缓存
            var imagedata = _visionService.GrabOneImage();
            _defectDataService.CacheDefectImage(SelectedDefect, defectFile =>
            {
                imagedata.SaveBmp(defectFile);
            });
        });
    }

    private void Save(string filePath)
    {
        LogHelper.LogRunMethodStart($"filePath={filePath}");
        WaferName = Path.GetFileNameWithoutExtension(filePath);
        ProgressDialogHelper.ShowDialog("Save defects ...", () =>
        {
            try
            {
                _defectDataService.SaveDefectFile(filePath, DefectResult);
                _currentFile = filePath;
                _messageBoxService.Success("Data saved successfully！", "DefectDetectMessage");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex);
                _messageBoxService.ErrorDialog(ex.Message);
                return false;
            }
        });

        LogHelper.LogRunMethodEnd($"filePath={filePath}");
    }

    [RelayCommand]
    public void SaveAs()
    {
        if (DefectResult == null)
        {
            _messageBoxService.ErrorDialog("Data is empty,Please scan wafer first!");
            return;
        }
        FileDialogHelper.SaveFileDialog(filter, filepath =>
        {
            Save(filepath);
        });
    }


    [RelayCommand]
    public void SaveData()
    {
        if (!string.IsNullOrEmpty(_currentFile))
        {
            if (DefectResult == null)
            {
                _messageBoxService.ErrorDialog("Data is empty,Please scan wafer first!");
                return;
            }
            Save(_currentFile);
        }
        else
        {
            SaveAs();
        }
    }


    public override bool CanCloseDialog()
    {
        if (IsRunning)
        {
            _messageBoxService.InfoDialog("The program is running now. please stop it first!");
            return false;
        }
        return base.CanCloseDialog();
    }

    [RelayCommand]
    public void Close()
    {
        RaiseRequestClose(new DialogResult());
    }

    public override void OnDialogClosed()
    {
        base.OnDialogClosed();
        ImageSourceHelper.ClearImage(WaferImage);
        _defectDataService.ClearCache();
        UnSubsribeEvents();
    }

    private void UnSubsribeEvents()
    {
        _visionService.OpticalConfigChanging -= _visionService_OpticalConfigChanging;
        _visionService.OpticalConfigChanged -= _visionService_OpticalConfigChanged;
        _waferService.StampProcessAction -= StampProcessChanged;
    }


    Dictionary<ECamera, double> dicFocusHeight = new Dictionary<ECamera, double>();
    private void _visionService_OpticalConfigChanged(object sender, StageCameraConfig e)
    {
        ChangeCameraInfo = "";
        GetCameraInfo();
        if (dicFocusHeight.ContainsKey(IServiceCamera.CurCamera))
        {
            _servicePzt?.AMov(0);
            _serviceProvider.GetService<IServicePlatNoProof>().MoveZ(false, AxisType.Z, dicFocusHeight[IServiceCamera.CurCamera]);
        }
    }

    private void GetCameraInfo()
    {
        FineFocusVisible = IServiceCamera.CurCamera == ECamera.Align || IServiceCamera.CurCamera == ECamera.AlignExt ? Visibility.Collapsed : Visibility.Visible;
    }

    private void _visionService_OpticalConfigChanging(object sender, StageCameraConfig e)
    {
        ChangeCameraInfo = "Camera switching …";
        var z = _motionService.GetCurrentZ();
        if (_servicePzt != null)
        {
            var pzt = _servicePzt.GetFPos();
            z += pzt / 1000;
        }
        dicFocusHeight.AddOrReplace(IServiceCamera.CurCamera, z);
    }

    [RelayCommand]
    async Task HandleImageDoubleClicked(ImageDoubleClickedRoutedEventArgs args)
    {
        if (_motionService.IsUserInteraction == true) return;

        MoveTo(args.Position);
    }
    /// <summary>
    /// 移动轴到指定的位置
    /// </summary>
    /// <param name="p"></param>
    internal void MoveTo(Point p)
    {
        var point2d = new Point(p.X - DefectResult.CowX, p.Y - DefectResult.CowY);
        var wcsPoint = ConvertToWcsPoint(point2d);
        var axisPoint = _alignmentService.GetAxisPoint(wcsPoint, _coarseAlignCamera.GetCameraWaferCoordi(), CameraAttr.Default(null));
        LogHelper.LogInfo($"Click Picture to move:Pixel={p},wcsPoint={wcsPoint},axisPoint={axisPoint}");
        _alignmentService.MoveTo(axisPoint);
    }

    private WcsPoint ConvertToWcsPoint(Point point2d)
    {
        var x = point2d.X * DefectResult.PixelEquivalentX;
        var y = -point2d.Y * DefectResult.PixelEquivalentY; //Y方向需要取反，因为与晶圆的Y方向相反
        return new WcsPoint(x, y);
    }

    private Point ConverToPixelPoint(Point2d point2d)
    {
        var x = point2d.X / DefectResult.PixelEquivalentX;
        var y = -point2d.Y / DefectResult.PixelEquivalentY; //Y方向需要取反，因为与晶圆的Y方向相反
        return new Point(x, y);
    }

    public Point GetAxisPoint()
    {
        if (DefectResult == null) return new Point();
        var axisPoint = _alignmentService.GetCurrentAxisPosition();
        var cow = GetCowAxisPoint();
        var pixelPoint = ConverToPixelPoint(axisPoint - cow);
        var x = pixelPoint.X + DefectResult.CowX;
        var y = pixelPoint.Y + DefectResult.CowY;
        return new Point(x, y);
    }
}
