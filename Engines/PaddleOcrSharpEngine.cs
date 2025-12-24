using System.Diagnostics;
using PaddleOCRSharp;

namespace OCRDemo.Engines
{
    /// <summary>
    /// PaddleOCRSharp 引擎实现
    /// 基于 raoyutian/PaddleOCRSharp 封装库
    /// 特性: 离线部署,自带 PP-OCRv4 轻量模型,无需联网下载
    /// </summary>
    public class PaddleOcrSharpEngine : IOcrEngine
    {
        private PaddleOCREngine? _engine;
        private bool _isInitialized = false;

        /// <summary>
        /// 引擎名称
        /// </summary>
        public string Name => "PaddleOCRSharp (raoyutian)";

        /// <summary>
        /// 引擎描述
        /// </summary>
        public string Description => "PaddleOCRSharp 离线引擎 - PP-OCRv4 轻量模型";

        /// <summary>
        /// 是否需要联网下载模型
        /// PaddleOCRSharp 自带轻量级模型,无需联网
        /// </summary>
        public bool RequiresOnlineModel => false;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 初始化引擎
        /// </summary>
        /// <param name="progressCallback">进度回调</param>
        public async Task InitializeAsync(Action<string>? progressCallback = null)
        {
            try
            {
                progressCallback?.Invoke("正在初始化 PaddleOCRSharp 引擎...");

                await Task.Run(() =>
                {
                    // 模型配置
                    // null = 使用内置的轻量级 PP-OCRv4 模型 (推荐)
                    // 也可指定外部服务器模型以获得更高精度
                    OCRModelConfig config = null;

                    // OCR 参数配置
                    OCRParameter parameter = new OCRParameter
                    {
                        cpu_math_library_num_threads = 10,  // 并发线程数
                        enable_mkldnn = true,               // 启用 MKL-DNN 加速
                        cls = false,                        // 禁用文字方向分类 (提升速度)
                        det = true,                         // 启用检测
                        use_angle_cls = false,              // 禁用 180 度旋转分类
                        det_db_score_mode = true            // 使用多边形文本区域
                    };

                    // 初始化引擎
                    _engine = new PaddleOCREngine(config, parameter);
                });

                _isInitialized = true;
                progressCallback?.Invoke("PaddleOCRSharp 引擎初始化完成 ✓");
            }
            catch (Exception ex)
            {
                _isInitialized = false;
                throw new Exception($"PaddleOCRSharp 引擎初始化失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 识别图片中的文字
        /// </summary>
        /// <param name="imagePath">图片文件路径</param>
        /// <returns>识别结果</returns>
        public Task<OcrResult> RecognizeAsync(string imagePath)
        {
            if (!_isInitialized || _engine == null)
            {
                return Task.FromResult(new OcrResult
                {
                    Success = false,
                    ErrorMessage = "引擎未初始化"
                });
            }

            return Task.Run(() =>
            {
                try
                {
                    using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(imagePath))
                    {
                        if (bmp == null)
                        {
                            return new OcrResult
                            {
                                Success = false,
                                ErrorMessage = "无法读取图片文件"
                            };
                        }

                        Stopwatch sw = Stopwatch.StartNew();
                        OCRResult ocrResult = _engine.DetectText(bmp);
                        sw.Stop();

                        // 提取所有文本
                        string allText = string.Join("\n", ocrResult.TextBlocks.Select(tb => tb.Text));

                        return new OcrResult
                        {
                            Success = ocrResult.TextBlocks.Count > 0,
                            Text = allText,
                            RegionCount = ocrResult.TextBlocks.Count,
                            ElapsedMilliseconds = sw.ElapsedMilliseconds,
                            EngineName = Name
                        };
                    }
                }
                catch (Exception ex)
                {
                    return new OcrResult
                    {
                        Success = false,
                        ErrorMessage = ex.Message
                    };
                }
            });
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _engine?.Dispose();
            _engine = null;
            _isInitialized = false;
        }
    }
}
