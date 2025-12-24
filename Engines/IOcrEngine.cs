namespace OCRDemo.Engines
{
    /// <summary>
    /// OCR 引擎接口
    /// </summary>
    public interface IOcrEngine : IDisposable
    {
        /// <summary>
        /// 引擎名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 引擎描述
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 是否需要联网下载模型
        /// </summary>
        bool RequiresOnlineModel { get; }

        /// <summary>
        /// 初始化引擎
        /// </summary>
        /// <param name="progressCallback">进度回调</param>
        Task InitializeAsync(Action<string>? progressCallback = null);

        /// <summary>
        /// 识别图片中的文字
        /// </summary>
        /// <param name="imagePath">图片路径</param>
        /// <returns>识别结果</returns>
        Task<OcrResult> RecognizeAsync(string imagePath);

        /// <summary>
        /// 是否已初始化
        /// </summary>
        bool IsInitialized { get; }
    }

    /// <summary>
    /// OCR 识别结果
    /// </summary>
    public class OcrResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 识别到的文本
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// 文本区域数量
        /// </summary>
        public int RegionCount { get; set; }

        /// <summary>
        /// 识别耗时（毫秒）
        /// </summary>
        public long ElapsedMilliseconds { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 使用的引擎名称
        /// </summary>
        public string EngineName { get; set; } = string.Empty;
    }
}
