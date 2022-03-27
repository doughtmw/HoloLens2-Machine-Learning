using System.Drawing;

namespace ObjectDetection.Parser
{
    public class BoundingBoxDimensions : DimensionsBase { }

    public class BoundingBox
    {
        public BoundingBoxDimensions Dimensions { get; set; }

    }

}