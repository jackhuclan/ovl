using System.Text;
using InteVega.CBB.Common;
using InteVega.CBB.CommonLib.Helpers;
using InteVega.CBB.TemplateMatching.Core;
using Marvel.Application.Exceptions;
using Marvel.Application.Services;
using Marvel.DataModels.Enums;
using Marvel.Interfaces.Application;
using Marvel.Interfaces.Business;
using Microsoft.Extensions.DependencyInjection;

using Ovl.UI.Execute;
using Ovl.UI.Execute.View;

namespace Ovl.UI.Defect.ViewModel;

public partial class DefectDetectViewModel
{
    protected void SubscribeRunErr()
    {
        AssistantBase.ActionErrored = (s, re) =>
        {
            Error(re.RunParam, re.Message);
        };
    }

    protected void Error(RunArgBase runArg, string title)
    {
        //如果正在停止过程中，不提示错误信息
        if ((_mainRunThread as RunThreadBase).IsStopping)
            return;
        string message = runArg.ToString();
        AppHelper.DispatcherInvoke(() =>
        {
            if (!runArg.IsEnableSkip || (_mainRunThread.CurrentJob?.RunParameters.IsAssistOnFail ?? false))
            {
                switch (runArg)
                {
                    case FocusRunArg focusRunArg:
                        ShowAssitant(message, focusRunArg.FocusInfo, focusRunArg.IsPzt);
                        break;
                    case FindEdgeArg edgeArg:
                        ShowAssitant(message);
                        break;
                    //case MeasureTargetArg measureTargetArg:
                    //    ShowAssitant(message);
                    //    break;
                    case FindTargetForMeasueArg findTargetForMeasueArg:
                        ShowAssitant(message, null, true, temps: findTargetForMeasueArg.MatchingContexts, false);
                        break;
                    //case FindTargetArg findTargetArg:
                    //    ShowAssitant(message, null, false, findTargetArg.MatchingContexts);
                    //    break;
                    case FindAlignTemplateArg findTemplateArg:
                        ShowAssitant(message, null, false, findTemplateArg.MatchingContexts);
                        break;
                    default:
                        _messageBoxService.ErrorDialog(message, title);
                        break;
                }

            }
            else
            {
                _messageBoxService.Error(message, title);
                AssistantBase.Skip();
            }
        });
    }

    private void ShowAssitant(string message, AutoRunInfo focusInfo = null, bool isPzt = false, List<SingleTemplateMatchingContext> temps = null, bool isEnableMove = true)
    {
        // if (mainRunThread.CurrentEapMode == EnumEapMode.Remote) return;
        try
        {
            var baseMainThread = _mainRunThread as RunThreadBase;
            if (baseMainThread.IsStopping)
            { return; }
            DebugView db = new DebugView();
            db.Height = 500;
            db.SetFocusInfo(focusInfo, isPzt);
            db.SetTemplates(temps, isEnableMove, _alignmentService.IsReversed);

            db.CloseBuzzer = Db_CloseBuzzer;
            AssitantDialogWindow dw = new AssitantDialogWindow();
            string tip = " \r\nIf click the \"Retry\" button, it will continue execution and retry again; \r\nOtherwise, it ignores the exception and continues.";
            var temp0Name = temps?.FirstOrDefault()?.Name;
            LogHelper.LogInfo($"Show DebugView,focusInfo={focusInfo},isPzt={isPzt},temp={temp0Name}");
            var res = dw.ShowDialog(db, message, tip);
            if (res == true && !baseMainThread.IsStopping)
            {
                AssistantBase.Retry();
            }
            else
            {
                AssistantBase.Skip();
            }
        }
        catch (Exception ex)
        {
            LogHelper.LogError(ex);
            _messageBoxService.ErrorDialog(ex.Message);
        }
    }

    private void Db_CloseBuzzer()
    {
        IMachineStatus_LightBuzzer _MachineStatus_LightBuzzer = _serviceProvider.GetService<IMachineStatus_LightBuzzer>();
        _MachineStatus_LightBuzzer.BuzzerControl(false);
    }
}
