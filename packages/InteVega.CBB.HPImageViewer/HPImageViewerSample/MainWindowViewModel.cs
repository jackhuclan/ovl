using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HalconDotNet;
using HPImageViewer.Core;
using HPImageViewer.Core.Persistence;
using HPImageViewer.Extensions.Extensions;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media.Media3D;
using Point = HPImageViewer.Core.Primitives.Point;

namespace HPImageViewerSample
{
    public partial class MainWindowViewModel : ObservableObject
    {
        public MainWindowViewModel(IHPImageViewer imageViewer)
        {
            AggregationIndexerFactory.Instance.RegisterDefaultIndexerFactory();
            _imageViewer = imageViewer;
            var pointCurve = new List<HPImageViewer.Core.Primitives.Point>();
            pointCurve.Add(new Point(50, 50));
            pointCurve.Add(new Point(50, 100));
            pointCurve.Add(new Point(80, 100));
            _imageViewer.ImageViewerDesc = new ImageViewerDesc()
            {
                ROIDescs = new List<ROIDesc>()
                {
                    //new RotatedRectDesc(){Angle=30},
                    //new RotatedRectDesc(){Angle=60},
                    //new RotatedRectDesc(){Angle=90},
                    //new RotatedRectDesc(){Angle=120},
                    //new RotatedRectDesc(){Angle=150},
                    //new EllipseDoubleDesc() { CenterX = 120, CenterY = 120, R = 100,BandLength=30 },
                    //new EllipseDesc(){ CenterX=100,CenterY=100,R=50 },
                    //new BoxDesc(){ BandWidth=30,Width=40,Height=40,Top=100,Left=100,Color="#FFFF00" },
                    // new Cross2dRectangleDesc(){Top=10,Left=100,Width=200,Height=200,BandWidth=0,ColumnWidth=20,Color="#FFFF00",BandLength=20},
                    //new CrossRectangleDesc(){Top=10,Left=100,Width=200,Height=200,BandWidth=0,ColumnWidth=40,Color="#FFFF00",BandLength=80},
                    //new QuadRectangleDesc() { Top = 10, Left = 10, Width = 200, Height = 200, BandWidth = 100 * 0.3, Color = "#FFFF00", BandLength = 40 },
                    //new Cross4RectangleDesc() { Top = 10, Left = 10, Width = 200, Height = 200, BandWidth = 100 * 0.3, Color = "#FFFF00", BandPercent = 50 },
                    //new RectangleDesc() { Left = 20, Top = 20, Width = 200, Height = 200 },
                    //new BoxDesc() { Width = 200, Height = 200, Left = 210, Top = 210, BandWidth = 100 * 0.2, Color = "#FFFF00" },
                    //new AimXYSDesc(){Top=80,Left=180,Width=400,Height=400,BandWidth=20,Color="#FFFF00"},
                    //new AimRectangleDesc(){Top=80,Left=180,Width=400,Height=400,BandWidth=20,Color="#FFFF00"},

                    //new PolygonDesc() { Vertices = pointCurve, IsClosed = false,IsMove=true },
                    //new CrosshairsDesc() { CenterX = 200, CenterY = 300, Color = "Lime", Size = 10,IsMove=true },
                    //new RotatedPickersDesc()
                    //        {
                    //            CenterX = 328,
                    //            CenterY = 247,
                    //            RadiusW = 218,
                    //            RadiusH = 164,
                    //            RectWidth = 50,
                    //            RectHeight = 200,
                    //            StartAngle = 0,
                    //            PickerCount = 4,
                    //            IsShowCircle = false,
                    //            StrokeThickness =1,
                    //            SpinLength=50,
                    //            IsSpin = true,
                    //            IsEdge = true,
                    //        },


                    //new RotatedPickersDesc()
                    //{
                    //    CenterX=200,
                    //    CenterY=300,
                    //    //Radius = 100,
                    //    RectWidth=30,
                    //    RectHeight=20,
                    //    StartAngle=0,
                    //    Color="#FFFF00",
                    //    PickerCount = 12,
                    //    IsShowCircle = true
                    //},
                    //new RotatedPickersDesc()
                    //{
                    //    CenterX=100,
                    //    CenterY=100,
                    //    //Radius = 100,
                    //    RectWidth=100,
                    //    RectHeight=100,
                    //    StartAngle=90,
                    //    Color="#FFFF00",
                    //    PickerCount = 2,
                    //    IsShowCircle = false
                    //},
                    new LRectangleDesc()
                    {
                        Direction = LShapeDirection.BottomLeft,
                    },
                }
            };
        }
        private IHPImageViewer _imageViewer;
        [RelayCommand]
        private void ExecuteOpenImage()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp|All Files|*.*"; // 设置筛选文件类型
            if (openFileDialog.ShowDialog() == true)
            {
                var selectedFileName = openFileDialog.FileName;

                try
                {
                    /*
                     BitmapImage bitmap = new BitmapImage(new Uri(selectedFileName, UriKind.Relative));
                       bitmap.Freeze();

                       //var image = Cv2.ImRead(selectedFileName);

                        HOperatorSet.ReadImage(out var hImage, selectedFileName);
                       _imageViewer.FitNewImageToArea = true;
                       _imageViewer.SetImage(bitmap);

                    */

                    HOperatorSet.ReadImage(out var image, selectedFileName);
                    //_imageViewer.FitNewImageToArea = true;
                    //var bitmap = new Bitmap(openFileDialog.FileName);


                    //  var image = Cv2.ImRead(selectedFileName, ImreadModes.Grayscale);

                    _imageViewer.SetImage(image);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("加载图片出错：" + ex.Message);
                }
            }

        }


        [RelayCommand]
        private void ExecuteOpenDocument()
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "ImageViewer文件 (*.IMVDesc)|*.IMVDesc"; // 设置筛选文件
            if (openFileDialog.ShowDialog() != true) return;
            try
            {
                var selectedFileName = openFileDialog.FileName;
                var imageViewerDescString = File.ReadAllText(selectedFileName);
                if (string.IsNullOrEmpty(imageViewerDescString)) return;
                var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
                var imageViewerDesc = Newtonsoft.Json.JsonConvert.DeserializeObject<ImageViewerDesc>(imageViewerDescString, settings);
                if (imageViewerDesc == null) return;

                _imageViewer.ImageViewerDesc = imageViewerDesc;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());

            }



        }
        [RelayCommand]
        private void ExecuteSaveDocument()
        {
            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "ImageViewer文件 (*.IMVDesc)|*.IMVDesc";
            if (saveFileDialog.ShowDialog() != true)
            {
                return;
            }

            var fileName = saveFileDialog.FileName;
            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            try
            {
                var imageViewerDesc = _imageViewer.ImageViewerDesc;
                var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
                var imageViewerDescString = Newtonsoft.Json.JsonConvert.SerializeObject(imageViewerDesc, settings);
                if (string.IsNullOrEmpty(imageViewerDescString)) return;
                File.WriteAllText(fileName, imageViewerDescString);
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.ToString());
            }

        }

        [RelayCommand]
        private void ExecuteChangeToolType(ToolType toolType)
        {
            _imageViewer.ActivatedTool = toolType;
        }

        [RelayCommand]
        private void ExecuteAddRoi(ROIType roiType)
        {
            ROIDesc roi = null;
            switch (roiType)
            {
                case ROIType.ToolRectangle:
                    roi = new RectangleDesc() { Left = 0, Top = 0, Width = 200, Height = 200 };
                    break;
                case ROIType.ToolEllipse:
                    roi = new EllipseDesc() { CenterX = 0, CenterY = 0, R = 100 };
                    break;
                case ROIType.ToolPolygon:
                    roi = new PolygonDesc()
                    {
                        Vertices = new List<Point>()
                        {
                            new Point(0,0),
                            new Point(100,50),
                            new Point(50,100),
                        },
                        IsClosed = false
                    };
                    break;
                case ROIType.ToolRotatedPickers:
                    roi = new RotatedPickersDesc()
                    {
                        CenterX = 500,
                        CenterY = 500,
                        RadiusW = 300,
                        RadiusH = 300,
                        RectWidth = 10,
                        RectHeight = 300,
                        PickerCount = 2,
                        SpinLength = 50,
                        IsSpin = true
                    };
                    break;
            }

            if (roi != null)
            {
                _imageViewer.AddROIs(roi);
            }
             
        }
        [RelayCommand]
        private void GetRoiData() 
        {
            List<ROIDesc> rOIDescs = _imageViewer.ImageViewerDesc.ROIDescs;
            
        }
    }

    public enum ROIType
    {
        ToolRectangle,
        ToolEllipse,
        ToolPolygon,
        ToolRotatedPickers
    }
}
