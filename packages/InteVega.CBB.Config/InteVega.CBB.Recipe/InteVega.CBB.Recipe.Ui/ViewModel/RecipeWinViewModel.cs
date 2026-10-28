using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using InteVega.CBB.Recipe.Core;
using InteVega.CBB.Recipe.Interface;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace InteVega.CBB.Recipe.Ui.ViewModel
{
    public partial class RecipeWinViewModel: ObservableObject
    {
        IMessageBoxService _MessageBoxService;
        [ObservableProperty]
        RecipeMenuViewModel _recipeMenuViewModel;
        [ObservableProperty]
        int _width;
        [ObservableProperty]
        public string _imgSouce;
        public RecipeWinViewModel(IRecipeService recipeService, IMessageBoxService messageBoxService,  RecipeMenuViewModel recipeMenuViewModel)
        {
            _MessageBoxService = messageBoxService;
            _recipeMenuViewModel = recipeMenuViewModel;
            _recipeMenuViewModel.GetRecipe =(node,recipe)=> OpenRecipe(node,recipe);
            Width = 350;
            ImgSouce = "pack://application:,,,/InteVega.CBB.Recipe.Ui;Component/Resources/Images/icon_ceblsq@2x.png";

        }
        public void OpenRecipe(IRecipeManager recipeNodeService,RecipeInfo recipeInfo) 
        {
            IRecipe recipe = recipeNodeService.GetCurrentRecipe(recipeInfo.Id);
            AmazonRecipe curRecipeParm = recipe.Get<AmazonRecipe>();
        }
        [RelayCommand]
        public void MouseDown() 
        {
            if (Width==0)
            {
                Width = 350;
                ImgSouce = "pack://application:,,,/InteVega.CBB.Recipe.Ui;Component/Resources/Images/icon_ceblsq@2x.png";
            }
            else
            {
                Width = 0;
                ImgSouce = "pack://application:,,,/InteVega.CBB.Recipe.Ui;Component/Resources/Images/icon_ceblzk@2x.png";
            }
        }
    }

    public class AmazonRecipe : IRecipeNode
    {
        public string DateTime { get; set; }
        public double StartX { get; set; }
        public double StartY { get; set; }
        public double StartZ { get; set; }
        /// <summary>
        /// 曝光时间
        /// </summary>
        public double Exposure { get; set; }
        /// <summary>
        /// 触发间隔
        /// </summary>
        public double Interval { get; set; }
        /// <summary>
        /// 有效线长
        /// </summary>
        public double LineLength { get; set; }

        /// <summary>
        /// 线长方向覆盖范围
        /// </summary>
        public double LineRange { get; set; }
        /// <summary>
        /// 速度
        /// </summary>
        public double Velocity { get; set; }
        /// <summary>
        /// 扫描距离
        /// </summary>
        public double Distance { get; set; }
        public double LineInterpolation { get; set; }

        public int reg_pulse_freq { get; set; }

        public double MaxHeight { get; set; }
        public double MinHeight { get; set; }

        public double Coplanar { get; set; }

        public float min_bump_height_threshold { get; set; }//最下高度阈值 0.08f;
        public float max_bump_height_threshold { get; set; }//最大高度阈值 0.2f;
        public float coplanarity_threshold { get; set; }//共面性阈值
        public float SepThreshold { get; set; }
        public float CalThreshold { get; set; }
        //模组参数
        public int PeakThreshold { get; set; }
        public int FirLength { get; set; }
        public int SignalDetectionFilterLength { get; set; }
        public int PeakAverageIntensityFilterLength { get; set; }
        //回程速度
        public double BackVel { get; set; }
        public BitmapImage GetImage { get; set; }
    }
}
