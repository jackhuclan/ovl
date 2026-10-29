using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using InteVega.CBB.ComUI.Helpers;
using Marvel.DataModels.Recipe;

namespace Ovl.UI.Controls;

/// <summary>
/// 处理与Check相关的交互逻辑
/// </summary>
public partial class WaferMapControl : UserControl
{
    List<Button> checkedBtns = new List<Button>();
    /// <summary>
    /// 勾选Field，更改背景色
    /// </summary>
    /// <param name="ele"></param>
    private void Check(Button ele)
    {
        if (ele == null) return;
        //此处需要判断Field是否被选中
        if (selectedBtns.Contains(ele))
        {
            ele.Content = CreateContent();
            if (!checkedBtns.Contains(ele))
                checkedBtns.Add(ele);
        }

    }

    private object CreateContent()
    {
        Path p = new Path();
        p.StrokeThickness = 2;
        p.Stroke = ConvertHelper.BrushFromString("70b603");
        p.Data = (Geometry)FindResource("Path_FieldChecked");
        p.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
        p.VerticalAlignment = System.Windows.VerticalAlignment.Center;
        p.Stretch = Stretch.Uniform;
        return p;
    }

    /// <summary>
    /// 勾选Field，更改背景色
    /// </summary>
    /// <param name="ele"></param>
    private void UnCheck(Button ele)
    {
        if (ele == null) return;
        ele.Content = null;
        if (checkedBtns.Contains(ele))
        {
            checkedBtns.Remove(ele);
            Select(ele);
            Check(checkedBtns, false);
        }
    }



    /// <summary>
    /// 批量勾选
    /// </summary>
    /// <param name="lstChecked">需要勾选的Field列表</param>
    /// <param name="isClearOthers">是否清空其它勾选项</param>
    private void Check(List<Button> lstChecked, bool isClearOthers)
    {
        if (isClearOthers)
            UnCheckAll();
        foreach (var btn in lstChecked)
        {
            Check(btn);
        }
    }

    /// <summary>
    /// 反向勾选
    /// </summary>
    /// <param name="btn">需要进行反向勾选的Field</param>
    /// <param name="isClearOther">是否需要清除其它勾选项</param>
    private void ReverseCheck(Button btn, bool isClearOther = false)
    {
        bool isChecked = checkedBtns.Contains(btn);
        if (isChecked)
        {
            UnCheck(btn);
        }
        else
        {
            if (isClearOther)
                UnCheckAll();
            Check(btn);
        }
    }

    /// <summary>
    /// 勾选指定的Field列表
    /// </summary>
    /// <param name="fields">需要勾选的Field列表</param>
    public void Check(IList<RowColumnInfo> fields)
    {
        if (fields == null) return;
        foreach (var fieldInfo in fields)
        {
            var absField=GetAbsoluteField(fieldInfo);
            var id = GetId(absField.RowIndex, absField.ColumnIndex);
            if (dicElments.TryGetValue(id, out var btn))
            {
                Check(btn);
            }
        }
    }

    /// <summary>
    /// 全选
    /// </summary>
    public void CheckAll()
    {
        foreach (var ele in selectedBtns)
        {
            Check(ele);
        }
    }
    /// <summary>
    /// 清空所勾选的项
    /// </summary>
    public void UnCheckAll()
    {
        while (checkedBtns.Count > 0)
        {
            UnCheck(checkedBtns[0]);
        }

    }


    /// <summary>
    /// 获取被勾选的Field列表
    /// </summary>
    /// <returns></returns>
    public List<RowColumnInfo> GetCheckedFields()
    {
        List<RowColumnInfo> fields = new List<RowColumnInfo>();
        foreach (var btn in checkedBtns)
        {
            RowColumnInfo field = btn.DataContext as RowColumnInfo;
            fields.Add(field);
        }
        return fields;
    }
}
