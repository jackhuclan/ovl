using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteVega.CBB.MotionProof.Abstraction
{
    public class AxisProofConfig
    {
        /// <summary>
        /// XY轴移动之前Z1轴避让的位置
        /// </summary>
        public double ZUP_AVOID_XY { get; set; }

        /// <summary>
        ///  Z1轴的最低门限位置
        ///  防止下降时撞道X轴
        /// </summary>
        public double ZUP_LIMIT_AVERT_X { get; set; }

        /// <summary>
        /// X轴的安全位门限位置
        /// 防止Z1下降时撞到
        /// </summary>
        public double X_LIMIT_AVERT_ZUP { get; set; }

        /// <summary>
        ///  T轴的安全位门限位置
        ///  防止XY移动时撞到
        /// </summary>
        public double ZROT_LIMIT_AVERT_XY { get; set; }

        /// <summary>
        /// 载台在上料位置时可以移动的冗余
        /// </summary>
        public double ZRot_Margin { get; set; } = 2;

        /// <summary>
        /// 载台是否在上料位置的判断冗余，肯定要比ZRot_Margin大
        /// </summary>
        public double LoadPos_Margin { get; set; } = 3;

        /// <summary>
        /// 载台是否可以移动的冗余，如果目标位置和当前位置的差值小于这个冗余则认为可以不需要移动
        /// </summary>
        public double LoadPos_CanMove_Margin { get; set; } = 0.0005;

        /// <summary>
        /// 基于 目标位置位置-门限位置 >= 0 的判断 乘的系数
        /// 在轴从负往正移动时，如果X轴的安全区域在在门限位置的负方向的话取1，如下图
        ///    安全区|
        ///    安全区|
        /// 负----------------> 正
        ///    安全区|
        ///    安全区|
        ///   
        /// 在轴从负往正移动时，如果X轴的安全区域在门限位置的正方向的话取-1，如下图
        ///         |安全区
        ///         |安全区
        /// 负----------------> 正
        ///         |安全区
        ///         |安全区
        /// </summary>
        public int SafeDirX { get; set; } = 1;

        /// <summary>
        /// 基于 目标位置位置-门限位置 >= 0 的判断 乘的系数
        /// 在轴从负往正移动时，如果T轴的安全区域在在门限位置的负方向的话取1，如下图
        ///    安全区|
        ///    安全区|
        /// 负----------------> 正
        ///    安全区|
        ///    安全区|
        ///   
        /// 在轴从负往正移动时，如果T轴的安全区域在门限位置的正方向的话取-1，如下图
        ///         |安全区
        ///         |安全区
        /// 负----------------> 正
        ///         |安全区
        ///         |安全区
        /// </summary>
        public int SafeDirZ_Rot { get; set; } = 1;

        /// <summary>
        /// 基于 目标位置位置-门限位置 >= 0 的判断 乘的系数
        /// 在轴从负往正移动时，如果Z轴的安全区域在在门限位置的负方向的话取1，如下图
        ///    安全区|
        ///    安全区|
        /// 负----------------> 正
        ///    安全区|
        ///    安全区|
        ///   
        /// 在轴从负往正移动时，如果Z轴的安全区域在门限位置的正方向的话取-1，如下图
        ///         |安全区
        ///         |安全区
        /// 负----------------> 正
        ///         |安全区
        ///         |安全区
        /// </summary>
        public int SafeDirZ_Up { get; set; } = 1;

        /// <summary>
        /// ZRot轴的安全位门限位置
        /// </summary>
        public double Z_ROT_SAFE { get; set; }

    }
}
