using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using InteVega.CBB.Recipe.Interface;
using Marvel.DataModels.Enums;
using Marvel.DataModels.Job;
using Marvel.DataModels.Recipe;
using InteVega.CBB.DomainService.Api.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Marvel.DataModels.Core;

namespace Ovl.UI.ViewModels.Job.ViewModel
{
    public partial class SelectPortViewModel : Bases.ViewModelBase
    {
        [ObservableProperty]
        string _name;
        [ObservableProperty]
        string _description;
        [ObservableProperty]
        int _portIndex;
        [ObservableProperty]
        int _formWidth;
        [ObservableProperty]
        int _formHeight;
        [ObservableProperty]
        string _title;
        [ObservableProperty]
        ObservableCollection<PortInfo> _portInfoList;
        [ObservableProperty]
        string _message;
        [ObservableProperty]
        Visibility _isShowPort;
        [ObservableProperty]
        PortInfo _selectedPortInfo;
        public IMessageBoxService MessageBoxService { get; set; }
        public IEfemService EfemService { get; set; }
        private IServiceProvider _serviceProvider;
        public IEnumerable<JobItem> JobInfoModelList { get; set; }
        private JobItem _jobItem;
        public JobItem JobItem 
        {
            get => _jobItem;
            set
            {
                _jobItem = value;
                SelectPortInfo(_jobItem);
            }
       }

        /// <summary>
        /// 记录已开始运行的Job队列是否是手动模式,
        /// job队列只能有一种模式Auto或manual，null代表job队列已不在运行状态
        /// </summary>
        public bool? IsRunningJobMode { get;  set; }

        public Action<JobItem> CallbackRefresh { get; set; }
        public SelectPortViewModel(IServiceProvider prod)
        {
            SelectedPortInfo = new PortInfo();
            FormWidth = 330;
            FormHeight = 120;
            Title = "LoadPort";
            _serviceProvider = prod;
            BindPortInfo();
        }
        public void BindPortInfo() 
        {
            PortInfoList = new ObservableCollection<PortInfo>();
            PortInfoList.Add(new PortInfo { PortName = "Port1", PortNum = 1 });
            PortInfoList.Add(new PortInfo { PortName = "Port2", PortNum = 2 });
            if (MarvelVersion.CurrentVersion == EnumOMVersion.OM600)
            {
                PortInfoList.Add(new PortInfo { PortName = "Port3", PortNum = 3 });
            }
            PortInfoList.Add(new PortInfo { PortName = "M(Manual)", PortNum = -1, IsSelected = true });

        }

        private void SelectPortInfo(JobItem jobItem)
        {
            SelectedPortInfo = PortInfoList.FirstOrDefault(_ => _.PortNum == jobItem.PortIndex) ?? new PortInfo();
            if(SelectedPortInfo == null)
            {
                SelectedPortInfo = new PortInfo();
            }
            else
            {
                SelectedPortInfo.IsSelected = true;
            }
        }

        private PortInfo GetSelectedPortInfo() => PortInfoList.FirstOrDefault(_ => _.IsSelected);

        [RelayCommand]
        public void Confirm() 
        {
            var portInfo = GetSelectedPortInfo();
            if (IsShowPort == Visibility.Collapsed)
            {
                JobItem.PortIndex = 0;
                JobItem.State = JobItem.PortIndex != 0 ? EnumJobStates.Waiting : EnumJobStates.Normal;
            }
            else
            {
                if (JobItem?.RunParameters == null)
                {
                    MessageBoxService?.WarningDialog("Job run parameters are missing. Please edit the job and try again.");
                    return;
                }
                if (IsModePortUnMatch(JobItem)) return;
                if (IsPortWaferSizeUnMatch(JobItem)) return;
                if (CheckExistJobMode(JobItem)) return;
                if (ValidateRanJobMode(JobItem)) return;

                if (portInfo == null)
                {
                    MessageBoxService?.WarningDialog("No port selected!");
                    return;
                }

                JobItem.PortIndex = portInfo.PortNum;
                JobItem.State = JobItem.PortIndex != 0 ? EnumJobStates.Waiting : EnumJobStates.Normal;
            }
            
            CallbackRefresh?.Invoke(JobItem);
            RaiseRequestClose(new DialogResult());
        }

        /// <summary>
        /// 校验JobItem的RunMode和当前运行的Job队列的RunMode是否一致
        /// </summary>
        /// <param name="jobItem"></param>
        /// <returns></returns>
        private bool ValidateRanJobMode(JobItem jobItem)
        {
            // 如果上一个运行过的JobItem的RunMode为null，代表已跳出job运行，则跳过判断
            if (IsRunningJobMode == null)
            {
                return false;
            }
            else
            {
                var curMode = IsRunningJobMode == true ? "Manual" : "Auto";

                // 如果当前JobItem的RunMode和上一个运行过的JobItem的RunMode不一致，则跳过
                if (jobItem.RunParameters.IsManual != IsRunningJobMode)
                {
                    MessageBoxService.WarningDialog($"Current running job queue mode is {curMode}, please stop and reload new job!");
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 判断RunMode和所选port是否一致
        /// </summary>
        /// <param name="jobItem">传入的JobItem对象</param>
        /// <returns>true：不一致</returns>
        private bool IsModePortUnMatch(JobItem jobItem)
        {
            var portInfo = GetSelectedPortInfo();
            // 没有选择任何port，则认为unmatch
            if (portInfo == null)
            {
                MessageBoxService.WarningDialog("No port selected!");
                return true;
            }
            if (jobItem.RunParameters.IsManual)
            {
               if(portInfo.PortNum != -1)
                {
                    MessageBoxService.WarningDialog("Manual run mode job is need to load manual port");
                    return true;
                }
            }
            else
            {
                if (portInfo.PortNum < 0)
                {
                    MessageBoxService.WarningDialog("Auto run mode job is need to load port 1 / port 2");
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 判断Recipe WaferSize与所选LoadPort Size是否一致。
        /// Recipe未创建WaferMap时返回true并提示，避免空引用。
        /// </summary>
        /// <param name="jobItem">传入的JobItem对象</param>
        /// <returns>true：不一致或无法校验（应阻止LoadPort）</returns>
        private bool IsPortWaferSizeUnMatch(JobItem jobItem)
        {
            var portInfo = GetSelectedPortInfo();
            if (portInfo == null || portInfo.PortNum == -1) // 手动模式不判断Wafersize和port口size吻合
            {
                return false;
            }

            if (EfemService == null)
            {
                MessageBoxService?.WarningDialog("EFEM service is unavailable. Cannot verify load port wafer size.");
                return true;
            }

            var recipeManager = _serviceProvider.GetService<IRecipeManager>();
            if (recipeManager == null)
            {
                MessageBoxService?.WarningDialog("Recipe manager is unavailable. Cannot verify recipe wafer size.");
                return true;
            }

            var irecipe = recipeManager.GetCurrentRecipe(jobItem.RecipeId);
            var recipe = irecipe?.Get<RecipeElementsInfo>();
            if (recipe?.WaferMap == null)
            {
                MessageBoxService?.WarningDialog(
                    $"Recipe \"{jobItem.RecipeName}\" has no WaferMap.\r\n" +
                    "Please open the recipe, create/save WaferMap (including Wafer Size), then load the port again.");
                return true;
            }

            var waferSize = EfemService.GetLoadPortSize(portInfo.PortNum);

            // 判断Wafer Size是否匹配
            bool isMismatch = (int)waferSize != (int)recipe.WaferMap.WaferSizeType;
            if (isMismatch)
            {
                MessageBoxService?.WarningDialog($"Select Wafer Size UnMatch!\r\n" +
                    $"Recipe Wafer Size: {recipe.WaferMap.WaferSizeType}\r\n" +
                    $"Load Port Size: {waferSize}");
            }
            return isMismatch;
        }

        /// <summary>
        /// Checks if there is a conflict between the run mode of the incoming job and the run modes of existing waiting jobs.
        /// </summary>
        private bool CheckExistJobMode(JobItem jobItem)
        {
            // Get the run mode of the incoming job
            bool incomingJobMode = jobItem.RunParameters.IsManual;

            // Find if there are any existing waiting jobs with a different run mode
            bool hasConflictingModes = JobInfoModelList != null
                && JobInfoModelList
                    .Where(job => job != null && job.State == EnumJobStates.Waiting && job.RunParameters != null)
                    .Any(job => job.RunParameters.IsManual != incomingJobMode);

            if (hasConflictingModes)
            {
                MessageBoxService?.WarningDialog("Unable to load two types of run mode jobs (Auto & Manual) into the JobQueue!");
                return true;
            }

            return false;
        }
        [RelayCommand]
        public void Cancel() 
        {
            RaiseRequestClose(new DialogResult());
        }
    }
 
    public partial class PortInfo : ObservableObject
    {
        [ObservableProperty]
        public string _portName;
        [ObservableProperty]
        public int _portNum = 0;
        [ObservableProperty]
        public bool _isSelected;
    } 
    
}
