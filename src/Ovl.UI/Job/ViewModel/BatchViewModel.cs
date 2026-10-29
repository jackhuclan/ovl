using Bases = Ovl.UI.Bases;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.CommonLib.Classes;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.Mvvm.Bases;
using InteVega.CBB.Prism.UI.Extensions.Dialogs;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using Marvel.DataModels.Job;
using Microsoft.Extensions.DependencyInjection;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

using Ovl.UI.Job.View;
using DataModels = Marvel.DataModels;
namespace Ovl.UI.Job.ViewModel
{
    public partial class BatchViewModel : Bases.ViewModelBase
    {
        public static Action<IList<JobItem>, string> ActionAddBatchJobs;
        public BatchViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _messageBox = serviceProvider.GetService<IMessageBoxService>();
            _dialogService = serviceProvider.GetService<IDialogService>();
            _allNodes = JsonHelper.DeserializeFile<ObservableCollection<TreeNodeItemBase>>(_batchesJson);
            if (_allNodes == null)
            {
                _allNodes = new ObservableCollection<TreeNodeItemBase>()
                {
                    new TreeNodeItemBase(){ Name ="Batches",IsFolder =true }
                };
            }
            SetParent(_allNodes[0]);
            Search();
        }



        string _batchesPath = @"../../data/Jobs/Batches";
        string _batchesJson { get { return Path.Combine(_batchesPath, @"BatchNodes.json"); } }
        private string GetBatchFile(TreeNodeItemBase node)
        {
            return Path.Combine(_batchesPath, node?.Name + "#" + node?.Id);
        }

        string _currentBatchJson { get { return GetBatchFile(SelectedNode); } }

        public BatchView View { get; internal set; }

        IMessageBoxService _messageBox;
        IServiceProvider _serviceProvider;
        IDialogService _dialogService;
        [ObservableProperty]
        string _searchText;
        [ObservableProperty]
        TreeNodeItemBase _selectedNode;
        [ObservableProperty]
        ObservableCollection<TreeNodeItemBase> _allNodes;


        [ObservableProperty]
        ObservableCollection<JobItem> _batchJobs;
        [ObservableProperty]
        JobItem _selectedJob;
        CollectionEx<JobItem> collectionEx;

        #region  Batch 树形结构操作
        public void SaveBatchesData()
        {
            JsonHelper.SerializerJsonFile(AllNodes, _batchesJson);
        }

        private void SetParent(TreeNodeItemBase node)
        {
            foreach (var child in node.Children)
            {
                child.Parent = node;
                SetParent(child);
            }
        }

        [RelayCommand]
        public void Search()
        {
            AllNodes[0].IsShowing = true;
            SetShowing(AllNodes[0].Children);
        }

        private void SetShowing(ObservableCollection<TreeNodeItemBase> children)
        {
            foreach (TreeNodeItemBase node in children)
            {
                SetShowing(node.Children);
                bool isContain = string.IsNullOrWhiteSpace(SearchText) || node.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
                node.IsShowing = isContain || node.Children.Any(x => x.IsShowing);
            }
        }

        [RelayCommand]
        public void ClearSearch()
        {
            SearchText = string.Empty;
            Search();
        }



        /// <summary>
        /// 新增Batch 或Folder 
        /// </summary>
        /// <param name="isFolder">是否是文件夹</param>
        /// <param name="parentNode">父文件夹</param>
        private void Add(bool isFolder, TreeNodeItemBase parentNode)
        {
            View.AddBatchView view = new View.AddBatchView(isFolder, parentNode.Name);
            view.Owner = View;
            view.funcSave = (name) =>
            {
                if (parentNode.Children.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) != null)
                {
                    return $"A batch or folder with the same name already exists in this folder:{parentNode.Name}";
                }
                TreeNodeItemBase node = new TreeNodeItemBase() { Name = name, IsFolder = isFolder, Parent = parentNode };
                parentNode.Children.Add(node);
                SaveBatchesData();
                return "";
            };

            view.ShowDialog();
        }

        public void AddBatchFolder(TreeNodeItemBase parentNode)
        {
            Add(true, parentNode);
        }

        public void AddBatch(TreeNodeItemBase parentNode)
        {
            Add(false, parentNode);
        }

        public void RenameBatch(TreeNodeItemBase node)
        {
            var parentNode = node.Parent;
            View.AddBatchView view = new View.AddBatchView(false, parentNode.Name, node.Name);
            view.Owner = View;
            view.funcSave = (name) =>
            {
                if (parentNode.Children.FirstOrDefault(x => x != node && x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) != null)
                {
                    return $"A batch or folder with the same name already exists in this folder:{parentNode.Name}";
                }
                string batchFile = GetBatchFile(node);

                node.Name = name;
                if (FileHelper.ExistFile(batchFile))
                {
                    string newBatchFile = GetBatchFile(node);
                    FileHelper.Rename(batchFile, newBatchFile);
                }
                SaveBatchesData();
                return "";
            };
            view.ShowDialog();
        }

        public void DeleteBatch(TreeNodeItemBase node)
        {
            if (node == null) return;
            if (_messageBox.AskDialog($"Are you sure to delete {node.Name}?") == Prism.Services.Dialogs.ButtonResult.OK)
            {
                var parent = node.Parent;
                parent.Children.Remove(node);
                string batchFile = GetBatchFile(node);
                FileHelper.DeleteFile(batchFile);
                SaveBatchesData();
                if (node == SelectedNode)
                {
                    BatchJobs.Clear();
                    SelectedNode = null;
                }
            }
        }

        #endregion  Batch 树形结构操作


        #region  Jobs 列表结构操作
        private void SaveJobsData()
        {
            if (SelectedNode == null) return;
            for (int i = 0; i < BatchJobs.Count; i++)
            {
                BatchJobs[i].OrderNum = i + 1;
            }
            JsonHelper.SerializerJsonFile(BatchJobs, _currentBatchJson);
        }

        [RelayCommand]
        public void SelectItem(TreeNodeItemBase node)
        {
            if (node.IsFolder)
                return;
            SelectedNode = node;

            BatchJobs = JsonHelper.DeserializeFile<ObservableCollection<JobItem>>(_currentBatchJson);
            if (BatchJobs == null)
            {
                BatchJobs = new ObservableCollection<JobItem>();
            }
            collectionEx = new CollectionEx<JobItem>(BatchJobs);
        }



        [RelayCommand]
        public void AddJob()
        {
            AddJob(null);
        }

        [RelayCommand]
        public void RunBatch()
        {
            if (SelectedNode == null)
            {
                _messageBox.ErrorDialog("Please selecte one batch!");
                return;
            }
            if (BatchJobs.Count == 0)
            {
                _messageBox.ErrorDialog("Please add jobs to batch!");
                return;
            }
            ActionAddBatchJobs?.Invoke(BatchJobs, SelectedNode?.Name);
        }

        [RelayCommand]
        public void RemoveJobs()
        {
            if (_messageBox.AskDialog($"Are you sure to delete all jobs?") == Prism.Services.Dialogs.ButtonResult.OK)
            {
                BatchJobs.Clear();
                SaveJobsData();
            }
        }

        [RelayCommand]
        public void InsertJob()
        {
            AddJob(SelectedJob);
        }

        private void AddJob(JobItem curentJob)
        {
            AddJobViewModel addJobViewModel = new AddJobViewModel(_serviceProvider);
            addJobViewModel.IsAdd = true;
            addJobViewModel.IsShowBatchAdd = Visibility.Visible;
            addJobViewModel.JobItem = new JobItem();
            addJobViewModel.RunParamViewModel.RunParameter = new DataModels.Recipe.RunParameter();
            addJobViewModel.IsShowContent = Visibility.Collapsed;
            addJobViewModel.IsShowFirst = Visibility.Visible;
            addJobViewModel.CallbackRefresh = (jobs) =>
            {
                var allAdded = true;
                foreach (var job in jobs)
                {
                    if (BatchJobs.FirstOrDefault(x => x.LotId == job.LotId) != null)
                    {
                        _messageBox.Warning($"Job（{job.LotId}） is already in the batch.");
                        allAdded = false;
                        continue;
                    }
                    if (curentJob == null)
                    { collectionEx.Add(job); }
                    else
                    {
                        collectionEx.InsertBefore(job, curentJob);
                    };
                }
                SaveJobsData();
                return allAdded;
            };
            _dialogService.ShowDialog("AddJobView", addJobViewModel);
        }

        [RelayCommand]
        public void EditJob()
        {
            if (SelectedJob == null)
                return;
            JobItem jobInfoModel = SelectedJob;
            AddJobViewModel addJobViewModel = new AddJobViewModel(_serviceProvider);
            addJobViewModel.JobItem = jobInfoModel;
            addJobViewModel.RunParamViewModel.RunParameter = jobInfoModel.RunParameters;
            addJobViewModel.RunParamViewModel.LoadPage(jobInfoModel.RecipeId);
            addJobViewModel.IsAdd = false;
            addJobViewModel.IsShowBatchAdd = Visibility.Collapsed;
            addJobViewModel.IsShowContent = Visibility.Visible;
            addJobViewModel.IsShowFirst = Visibility.Collapsed;

            addJobViewModel.CallbackRefresh = (jobs) =>
            {
                SaveJobsData();
                return true;
            };
            _dialogService.ShowDialog("AddJobView", addJobViewModel);
        }

        [RelayCommand]
        public void MoveDownJob()
        {
            if (SelectedJob == null) return;
            var selected = SelectedJob;
            collectionEx.MoveDown(SelectedJob);
            SelectedJob = selected;
            SaveJobsData();
        }

        [RelayCommand]
        public void MoveUpJob()
        {
            if (SelectedJob == null) return;
            var selected = SelectedJob;
            collectionEx.MoveUp(SelectedJob);
            SelectedJob = selected;
            SaveJobsData();
        }

        [RelayCommand]
        public void DeleteJob()
        {
            if (SelectedJob == null) return;
            if (_messageBox.AskDialog($"Are you sure to delete {SelectedJob.LotId}?") == ButtonResult.OK)
            {
                collectionEx.Remove(SelectedJob);
                SaveJobsData();
            }
        }

        [RelayCommand]
        public void Join()
        {
            if (SelectedJob == null) return;
            ActionAddBatchJobs?.Invoke(new List<JobItem>() { SelectedJob }, SelectedNode?.Name);
        }

        #endregion  Jobs 列表结构操作
    }
}
