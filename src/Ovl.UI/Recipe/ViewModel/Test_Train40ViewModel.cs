using CommunityToolkit.Mvvm.ComponentModel;
using InteVega.CBB.DomainService.Api.Interfaces;
using InteVega.CBB.TemplateMatching.Core;
using Marvel.DataModels.Recipe40;
using Marvel.Interfaces.Application;
using Megarobo.Ovl.Service.IntfFn;
using Microsoft.Extensions.DependencyInjection;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ovl.UI.Recipe.View;
namespace Ovl.UI.Recipe.ViewModel
{
    internal partial class Test_Train40ViewModel :Test_TrainViewModelBase<TestInfo40>
    {
        [ObservableProperty]
        private object _trainContent;

        public Test_Train40ViewModel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public override void OpenDialogThread(IDialogParameters parameters)
        {
            base.OpenDialogThread(parameters);
         
            if (TestInfo.IsDoubleFace && !TestInfo.IsEffectiveCrossFov)
            {
                TrainContent = _serviceProvider.GetService<Test_Train_DoubleFaceControl>();
            }
            else
            {
                TrainContent = _serviceProvider.GetService<Test_Train_SingleFaceControl>();
            }
        } 


    }
}
