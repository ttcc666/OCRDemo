using System.Drawing;

namespace OCRDemo.Engines
{
    /// <summary>
    /// OCR 文本块详细信息
    /// </summary>
    public class OcrTextBlock
    {
        /// <summary>
        /// 识别的文本
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// 置信度 (0-1)，值越大表示识别越可信
        /// </summary>
        public float Confidence { get; set; }

        /// <summary>
        /// 文本框坐标（4个角点：左上、右上、右下、左下）
        /// </summary>
        public List<Point> BoxPoints { get; set; } = new();

        /// <summary>
        /// 外接矩形（简化版坐标，用于快速定位）
        /// </summary>
        public Rectangle BoundingBox { get; set; }

        /// <summary>
        /// 文本块类型（可选）
        /// </summary>
        public OcrTextBlockType BlockType { get; set; } = OcrTextBlockType.Line;

        /// <summary>
        /// 获取置信度百分比字符串
        /// </summary>
        public string ConfidencePercent => $"{Confidence * 100:F1}%";

        /// <summary>
        /// 获取边界框字符串表示
        /// </summary>
        public string BoundingBoxStr =>
            $"[{BoundingBox.X}, {BoundingBox.Y}, {BoundingBox.Width}, {BoundingBox.Height}]";
    }

    /// <summary>
    /// 文本块类型
    /// </summary>
    public enum OcrTextBlockType
    {
        /// <summary>
        /// 段落
        /// </summary>
        Paragraph,

        /// <summary>
        /// 文本行
        /// </summary>
        Line,

        /// <summary>
        /// 单词
        /// </summary>
        Word
    }
}
