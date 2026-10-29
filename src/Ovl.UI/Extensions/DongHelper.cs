using Dm;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ovl.UI.Extensions
{
    /// <summary>
    /// 加密狗
    /// </summary>
    public class DongHelper
    {
        public static void Verify(IServiceProvider serviceProvider)
        {
            try
            {
                LogHelper.LogInfo("Check dongle state.");
                Ovl.Encryption.DoNet.Encryptor.Verify();
                //Wafer.Algorithm.Common.DoNet.Aligner.CalcWaferAngleByNotch(new Point2d(), new Point2d(1, 1), out _);
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex, "dongle error");
                AppHelper.DispatcherInvoke(() =>
                {
                    serviceProvider.GetService<IMessageBoxService>().ErrorDialog("The dongle does not exist or has expired; Please check the dongle and then restart the program!");
                    Environment.Exit(1);
                });
            }
        }
    }
}
