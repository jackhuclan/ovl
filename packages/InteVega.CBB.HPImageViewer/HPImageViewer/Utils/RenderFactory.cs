using HPImageViewer.Core.Persistence;
using HPImageViewer.Rendering.ROIRenders;

namespace HPImageViewer.Utils
{
    internal static class RenderFactory
    {

        public static ROIRender CreateROIRender(ROIDesc rOIDesc)
        {
            dynamic rOI = rOIDesc;
            return CreateROIRenderInternal(rOI);
        }

        private static RectangleRender CreateROIRenderInternal(RectangleDesc rectangleDesc)
        {
            return new RectangleRender(rectangleDesc);
        }

        private static EllipseRender CreateROIRenderInternal(EllipseDesc ellipseDesc)
        {
            return new EllipseRender(ellipseDesc);
        }
        private static EllipseDoubleRender CreateROIRenderInternal(EllipseDoubleDesc ellipseDesc)
        {
            return new EllipseDoubleRender(ellipseDesc);
        }

        private static PolygonRender CreateROIRenderInternal(PolygonDesc polygonDesc)
        {
            return new PolygonRender(polygonDesc);
        }

        private static RotatedRectRender CreateROIRenderInternal(RotatedRectDesc polygonDesc)
        {
            return new RotatedRectRender(polygonDesc);
        }

        private static BoxRender CreateROIRenderInternal(BoxDesc boxDesc)
        {
            return new BoxRender(boxDesc);
        }
        private static QuadRectangleRender CreateROIRenderInternal(QuadRectangleDesc quadRectangleDesc)
        {
            return new QuadRectangleRender(quadRectangleDesc);
        }
        private static Cross4RectangleRender CreateROIRenderInternal(Cross4RectangleDesc towRectangleDesc)
        {
            return new Cross4RectangleRender(towRectangleDesc);
        }
        private static CrossRectangleRender CreateROIRenderInternal(CrossRectangleDesc crossRectangleDesc)
        {
            return new CrossRectangleRender(crossRectangleDesc);
        }
        private static Cross2dRectangleRender CreateROIRenderInternal(Cross2dRectangleDesc cross2dRectangleDesc)
        {
            return new Cross2dRectangleRender(cross2dRectangleDesc);
        }
        private static RotatedPickersRender CreateROIRenderInternal(RotatedPickersDesc quadRectangleDesc)
        {
            return new RotatedPickersRender(quadRectangleDesc);
        }
        private static AimRectangleRender CreateROIRenderInternal(AimRectangleDesc aimRectangleDesc)
        {
            return new AimRectangleRender(aimRectangleDesc);
        }
        private static AimXYSRender CreateROIRenderInternal(AimXYSDesc aimXYSDesc)
        {
            return new AimXYSRender(aimXYSDesc);
        }
        private static CrosshairsRender CreateROIRenderInternal(CrosshairsDesc crosshairsDesc)
        {
            return new CrosshairsRender(crosshairsDesc);
        }

        private static LRectangleRender CreateROIRenderInternal(LRectangleDesc quadRectangleDesc)
        {
            return new LRectangleRender(quadRectangleDesc);
        }
    }
}
