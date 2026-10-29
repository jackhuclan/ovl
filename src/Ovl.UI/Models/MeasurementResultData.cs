using InteVega.CBB.Mvvm.Bases;

namespace Ovl.UI.Models
{
    public class MeasurementResultData : BindableBase
    {
        private string _axit;
        public string Axit
        {
            get { return _axit; }
            set
            {
                SetProperty(ref _axit, value);
            }
        }
        public double _oVLMisreg;
        public double OVLMisreg 
        {
            get { return _oVLMisreg; }
            set
            {
                SetProperty(ref _oVLMisreg, value);
            }
        }
        public double _centerInner;
        public double CenterInner 
        {
            get { return _centerInner; }
            set
            {
                SetProperty(ref _centerInner, value);
            }
        }
        private double _centerOuter;
        public double CenterOuter
        {
            get { return _centerOuter; }
            set
            {
                SetProperty(ref _centerOuter, value);
            }
        }
        double _qMeritTarget;
        public double QMeritTarget {
            get { return _qMeritTarget; }
            set
            {
                SetProperty(ref _qMeritTarget, value);
            }
        }
        double _qMeritInner;
        public double QMeritInner
        {
            get { return _qMeritInner; }
            set
            {
                SetProperty(ref _qMeritInner, value);
            }
        }
        double _qMeritOuter;
        public double QMeritOuter
        {
            get { return _qMeritOuter; }
            set
            {
                SetProperty(ref _qMeritOuter, value);
            }
        }
    }
}
