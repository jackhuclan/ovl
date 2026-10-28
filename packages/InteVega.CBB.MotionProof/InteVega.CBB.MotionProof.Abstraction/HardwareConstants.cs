
using Newtonsoft.Json;

namespace InteVega.CBB.MotionProof.Abstraction
{
    public class HardwareConstants
    {
        /// <summary>
        /// 代表T轴顶深周围和大理石的间隙，实际没有5mm故意设的大一点
        /// </summary>
        public double Rot_Hole_Size_With_Margin { get; set; } = 65;

        /// <summary>
        /// chuck盘半径
        /// </summary>
        public double CT_RAIDUS { get; set; } = 150;

        /// <summary>
        /// 标定板大小
        /// </summary>
        public double PM_Target_Size { get; set; } = 20;

        /// <summary>
        /// chuck盘半径加上标定板大小
        /// </summary>
        [JsonIgnore]
        public double CT_Radius_With_PM_Target => CT_RAIDUS + PM_Target_Size;

        /// <summary>
        /// CT气浮盘最小值
        /// </summary>
        public double CT_Pressure_Vac_Delta_Min = 60;

        /// <summary>
        /// CT气浮盘最大值
        /// </summary>
        public double CT_Pressure_Vac_Delta_Max = 75;
    }
}
