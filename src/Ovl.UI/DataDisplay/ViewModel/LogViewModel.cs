using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InteVega.CBB.Log.Core;
using InteVega.CBB.Log.Display.Interfaces;
using InteVega.CBB.Log.Display.Models;
using InteVega.CBB.Prism.UI.Extensions.MessageBox;
using Serilog.Events;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Ovl.UI.DataDisplay.ViewModel
{
    public partial class LogViewModel : ObservableObject
    {
        private SqlSugarClient _sugarClient;
        [ObservableProperty]
        private ObservableCollection<VegaDisplayLogItem> _logItemCollection;
        /// <summary>
        /// 选中的行
        /// </summary>
        [ObservableProperty]
        private VegaDisplayLogItem _selectedLogItem;

        //右键菜单
        [ObservableProperty]
        private ContextMenu _rightClickMenu;


        #region 过滤、导出功能属性、命令

        /// <summary>
        /// 开始时间
        /// </summary>
        [ObservableProperty]
        private DateTime? _startTime = DateTime.Today;

        /// <summary>
        /// 结束时间
        /// </summary>
        [ObservableProperty]
        private DateTime? _endTime = DateTime.Today.AddDays(1).AddSeconds(-1);

        /// <summary>
        /// ScrollViewer高度
        /// </summary>
        [ObservableProperty]
        private double _scrollViewerHeight;

        /// <summary>
        /// DataGrid信息列宽度
        /// </summary>
        [ObservableProperty]
        private double _dataGridHeight;

        /// <summary>
        /// DataGrid信息列宽度
        /// </summary>
        [ObservableProperty]
        private double _messageColumnWidth;

        /// <summary>
        /// DataGrid等级列宽度
        /// </summary>
        [ObservableProperty]
        private double _levelColumnWidth;

        /// <summary>
        /// DataGrid时间列宽度
        /// </summary>
        [ObservableProperty]
        private double _timeColumnWidth;

        #endregion 过滤、导出功能属性、命令

        #region 构造函数

        public LogViewModel(IMessageBoxService messageBoxService, ILogDisplaySinkService logDisplaySinkService)
        {
            Init();
        }

        #endregion 构造函数

        #region Init()：初始化

        public void Init()
        {
            LevelColumnWidth = 80;
            TimeColumnWidth = 140;

            ConnectionConfig connectionConfig = new ConnectionConfig();
            connectionConfig.DbType = DbType.Sqlite;
            connectionConfig.ConnectionString = $"Data Source={Path.Combine(InteVegaLogHelper.LogConfig.SaveDirectory, "log.db")}";
            _sugarClient = new SqlSugarClient(connectionConfig);

            Task.Run(async () =>
            {
                while (true)
                {

                    //var todayLogs = _sugarClient.Queryable<LogDbEntity>()
                    //                            .Where(_ => DateTime.Parse(_.Timestamp) >= StartTime
                    //                                     && DateTime.Parse(_.Timestamp) <= EndTime
                    //                                     && _.Level.ToString() != Serilog.Events.LogEventLevel.Debug.ToString())
                    //                            .OrderByDescending(o => o.Timestamp).Take(50)
                    //                            .ToList().Select(_ => _.ConvertToLogItem());
                    //LogItemCollection = new ObservableCollection<VegaDisplayLogItem>(todayLogs);
                    var logs = _sugarClient.Queryable<LogDbEntity>().Where(exp.ToExpression()).Where(_ => _.Level.ToString() != Serilog.Events.LogEventLevel.Debug.ToString()).OrderByDescending(o => o.Timestamp).Take(100).ToList().Select(_ => _.ConvertToLogItem()).ToList();
                    var sourceFilteredLogs = new List<VegaDisplayLogItem>();
                    sourceFilteredLogs.AddRange(logs);
                    LogItemCollection = new ObservableCollection<VegaDisplayLogItem>(TrimNewLine(sourceFilteredLogs));

                    await Task.Delay(1500);
                }

            });

        }

        #endregion Init()：初始化

        #region 过滤、导出命令方法
        [RelayCommand]
        private void ControlSizeChanged(SizeChangedEventArgs args)
        {
            //MessageColumnWidth = args.NewSize.Width - LevelColumnWidth - TimeColumnWidth - 16;
            //ScrollViewerHeight = args.NewSize.Height;
        }

        #endregion 过滤、导出命令方法

        #region 筛选功能
        private List<VegaDisplayLogItem> TrimNewLine(List<VegaDisplayLogItem> logs)
        {
            foreach (var item in logs)
            {
                item.Message = item.Message.TrimEnd('\r', '\n');
            }
            return logs;
        }
        /// <summary>
        /// 日志过滤等级集合
        /// </summary>
        public IEnumerable<string> LogLevels => Enum.GetNames(typeof(LogEventLevel));

        private bool _isApplyFilter = false;
        [ObservableProperty]
        private string _levelFilter;
        /// <summary>
        /// 过滤关键词
        /// </summary>
        [ObservableProperty]
        private string _secretWords;
        Expressionable<LogDbEntity> exp = Expressionable.Create<LogDbEntity>();
        /// <summary>
        /// 应用过滤条件
        /// </summary>
        /// <param name="isApply"></param>
        [RelayCommand]
        private void ApplyFilter(string isApply)
        {
            _isApplyFilter = Boolean.Parse(isApply);
            if (_isApplyFilter)
            {
                exp = Expressionable.Create<LogDbEntity>();
                if (StartTime != null)
                {
                    exp.And(_ => DateTime.Parse(_.Timestamp) >= StartTime);
                }
                if (EndTime != null)
                {
                    exp.And(_ => DateTime.Parse(_.Timestamp) <= EndTime);
                }
                if (!string.IsNullOrWhiteSpace(LevelFilter))
                {
                    exp.And(_ => _.Level.ToString() == LevelFilter);
                }
                if (!string.IsNullOrWhiteSpace(SecretWords))
                {
                    exp.And(_ => SqlFunc.Contains(_.RenderedMessage, SecretWords));
                }
                //exp.And(_ => SqlFunc.Contains(_.Properties.ToString(), "WaferHandlerViewModel") || SqlFunc.Contains(_.Properties.ToString(), "WaferHandler600ViewModel") || SqlFunc.Contains(_.Properties.ToString(), "EFEM"));
                //var logs = _sugarClient.Queryable<LogDbEntity>().Where(exp.ToExpression()).OrderByDescending(o => o.Timestamp).Take(100).ToList().Select(_ => _.ConvertToLogItem()).ToList();
                //var sourceFilteredLogs = new List<VegaDisplayLogItem>();
                //sourceFilteredLogs.AddRange(logs);
                //LogItemCollection = new ObservableCollection<VegaDisplayLogItem>(TrimNewLine(sourceFilteredLogs));
            }
            else
            {
                StartTime = DateTime.Today;
                EndTime = DateTime.Today.AddDays(1).AddSeconds(-1);
                SecretWords = "";
                var todayLogs = _sugarClient.Queryable<LogDbEntity>()
                    .Where(_ => DateTime.Parse(_.Timestamp) >= StartTime && DateTime.Parse(_.Timestamp) <= EndTime)
                    .ToList()
                    .Select(_ => _.ConvertToLogItem());
                LogItemCollection = new ObservableCollection<VegaDisplayLogItem>(TrimNewLine(todayLogs.ToList()));
            }
        }

        #endregion
    }


    public static class LogEntityExtension
    {
        public static VegaDisplayLogItem ConvertToLogItem(this LogDbEntity dbEntity)
        {
            if (dbEntity == null) return null;
            var timestamp = dbEntity.Timestamp;
            var logItem = new VegaDisplayLogItem
            {
                Timestamp = timestamp,
                OperateTime = DateTime.Parse(timestamp),
                Level = dbEntity.Level.ToString().ToLower(),
                Message = dbEntity.RenderedMessage,
                EventId = dbEntity.Properties.ContainsKey("EventId") ? Convert.ToInt32(dbEntity.Properties["EventId"]["Id"].ToString()) : 0
            };
            if (dbEntity.Properties.ContainsKey("SourceContext"))
            {
                logItem.SourceContext = dbEntity.Properties["SourceContext"].ToString();
            }
            else
            {
                logItem.SourceContext = "Universal";
            }
            logItem.Message = $"{logItem.SourceContext} : {logItem.Message}";
            return logItem;
        }
    }
}
