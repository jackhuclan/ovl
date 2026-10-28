using HPImageViewer.Core;
using InteVega.CBB.DomainService.Api.Extensions;
using InteVega.CBB.TemplateMatching.Core;
using Megarobo.Ovl.Type;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ovl.UI.ViewModels.Extensions
{
    public static class HPImageViewerExtention
    {
        /// <summary>
        /// 将模板匹配的原图设置到图像控件
        /// </summary>
        /// <param name="hpImageView">图像控件</param>
        /// <param name="context">多模板对象</param>
        /// <param name="folder">模板图片所在文件夹路径</param>
        public static void SetTemplateImage(this IHPImageViewer hpImageView, MultiTemplateMatchingContext context, string folder)
        {
            var temps = context.TemplateContexts;
            SetTemplateImage(hpImageView, temps, folder);
        }


        /// <summary>
        /// 将模板匹配的原图设置到图像控件
        /// </summary>
        /// <param name="hpImageView">图像控件</param>
        /// <param name="temps">多模板对象</param>
        /// <param name="folder">模板图片所在文件夹路径</param>
        public static void SetTemplateImage(this IHPImageViewer hpImageView, List<SingleTemplateMatchingContext> temps, string folder)
        {

            if (temps != null && temps.Count > 0)
            {
                var defaultTemp = temps[0];
                string filePath = Path.Combine(folder, defaultTemp.DefaultModelFileName + ".bmp");
                if (File.Exists(filePath))
                {
                    hpImageView.Image = ImageData.ReadImage(filePath);
                }
            }
        }

        /// <summary>
        /// 将模板匹配的原图设置到图像控件
        /// </summary>
        /// <param name="hpImageView">图像控件</param>
        /// <param name="temps">多模板对象</param>
        /// <param name="folder">模板图片所在文件夹路径</param>
        public static void SetTemplateImage(this IHPImageViewer hpImageView, SingleTemplateMatchingContext temp, string folder)
        {
            string filePath = Path.Combine(folder, temp.DefaultModelFileName + ".bmp");
            if (File.Exists(filePath))
            {
                hpImageView.Image = ImageData.ReadImage(filePath);
            }
        }
    }
}
