using AdonisUI;
using Marvel.DataModels.Enums;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Ovl.UI.WaferHandler.ViewModel;
namespace Ovl.UI.WaferHandler.View
{
    /// <summary>
    /// SlotView.xaml 的交互逻辑
    /// </summary>
    public partial class SlotView : UserControl
    {
        [DllImport("user32.dll", EntryPoint = "DestroyIcon", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);
        public static readonly DependencyProperty ContentProperty =
           DependencyProperty.Register("Content", typeof(object), typeof(SlotView), new PropertyMetadata(null));

        public object Content
        {
            get { return GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        public static readonly DependencyProperty TagProperty =
   DependencyProperty.Register("Tag", typeof(string), typeof(SlotView), new PropertyMetadata(null));

        public object Tag
        {
            get { return GetValue(TagProperty); }
            set { SetValue(TagProperty, value); }
        }

        string _text = "";
        public SlotView()
        {
            InitializeComponent();
        }
        public SlotView(SlotView c)  //重写构造
        {
            InitializeComponent();
            this.lblSolt.Height = c.lblSolt.Height;
            this.lblSolt.Width = c.lblSolt.Height;
            //this.lblSolt.Fill = c.lblSolt.Fill;
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            // 判断左键是否按下
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DataObject data = new DataObject();
                data.SetData("Object", this);
                SlotItem slotItem = (SlotItem)this.lblSolt.Content;
                Console.WriteLine($"{slotItem is null}, {slotItem?.Status}");
                if (slotItem != null && slotItem.Status == EnumSlotStatus.Normal)
                {
                     _text = slotItem.Value;
                    DragDrop.DoDragDrop(this, data, DragDropEffects.Copy | DragDropEffects.Move);
                }
               
            }
        }
        protected override void OnGiveFeedback(GiveFeedbackEventArgs e)
        {
            base.OnGiveFeedback(e);
            //Effects获取拖动类型
            if (e.Effects.HasFlag(DragDropEffects.Copy)|| e.Effects.HasFlag(DragDropEffects.Move))
            {
                Bitmap dimage = FillGraphToBitmap();
                var handle = dimage.GetHicon();
                var c = new CursorSafeHandle(handle, true);
                var cursor = CursorInteropHelper.Create(c);
                Mouse.SetCursor(cursor); //移动显示笔形
               // 确保使用后释放 HICON
                if (handle != IntPtr.Zero)
                {
                    DestroyIcon(handle);
                }
            }
            else
            {
                Mouse.SetCursor(Cursors.No);
            }
            e.Handled = true;
        }
        protected override void OnDragEnter(DragEventArgs e)
        {
            base.OnDragEnter(e);
        }

        protected override void OnDragLeave(DragEventArgs e)
        {
            base.OnDragLeave(e);
            // Undo the preview that was applied in OnDragEnter.
            // circleUI.Fill = _previousFill;
        }
        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            //检查数据是否包含制定字符串类型数据
            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                //获取字符串
                string dataString = (string)e.Data.GetData(DataFormats.StringFormat);

                //使用BrushConverter将字符串转换成Brush对象并应用于提供圆形控件 UI 的 Ellipse 的 Fill
                BrushConverter converter = new BrushConverter();
                if (converter.IsValid(dataString))
                {
                    if (e.KeyStates.HasFlag(DragDropKeyStates.ControlKey))
                    {
                        e.Effects = DragDropEffects.Copy;
                    }
                    else
                    {
                        e.Effects = DragDropEffects.Move;
                    }
                }
            }
            e.Handled = true;
        }
        private Bitmap FillGraphToBitmap()
        {
            var backBitmap = new Bitmap(148, 16);
            using (Graphics g = Graphics.FromImage(backBitmap))
            {
                float fontSize = 12.0f;    //字体大小  
                float textWidth = 10 * fontSize;  //文本的长度  
                //下面定义一个矩形区域，以后在这个矩形里画上白底黑字  
                float rectX = 0;
                float rectY = 0;
                float rectWidth = 148;
                float rectHeight = 16;
                //声明矩形域  
                RectangleF textArea = new RectangleF(50, rectY, rectWidth, rectHeight);
                Font font = new Font("宋体", fontSize);   //定义字体  
                System.Drawing.Brush whiteBrush = new SolidBrush(System.Drawing.Color.White);   //白笔刷，画文字用  
                System.Drawing.Brush blackBrush = new SolidBrush(System.Drawing.Color.FromArgb(0x30, 0x80, 0xB4, 0x36));   //黑笔刷，画背景用  
                g.FillRectangle(blackBrush, rectX, rectY, rectWidth, rectHeight);
                g.DrawString(_text, font, whiteBrush, textArea);
                g.Dispose();
            }
            return backBitmap;
        }
    }

    public class CursorSafeHandle : SafeHandle
    {
        public CursorSafeHandle(IntPtr preexistingHandle, bool ownsHandle)
            : base(IntPtr.Zero, ownsHandle)
        {
            handle = preexistingHandle;
        }
        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }
        protected override bool ReleaseHandle()
        {
            return true;
        }
    }
}
