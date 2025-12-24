using System.Diagnostics;
using System.IO;
using System.Net.Http;
using SkiaSharp;
using RapidOcrNet;

namespace OCRDemo.Engines
{
    /// <summary>
    /// RapidOCR 引擎实现
    /// 基于 RapidAI/RapidOCR 的 ONNX 模型,提供高性能 OCR 识别
    /// 特性: 无 OpenCV 依赖,使用 SkiaSharp 处理图像,支持自动下载模型
    /// </summary>
    public class RapidOcrEngine : IOcrEngine
    {
        private RapidOcr? _engine;
        private bool _isInitialized = false;
        private readonly string _modelsPath;
        private readonly bool _enablePreprocessing;  // 是否启用图像预处理

        // 模型文件下载 URL (来自 RapidOCR GitHub Releases)
        private const string ModelsBaseUrl = "https://github.com/RapidAI/RapidOCR/releases/download/v1.3.0";

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="modelsPath">模型文件路径(默认: Models/RapidOCR)</param>
        /// <param name="enablePreprocessing">是否启用图像预处理(默认启用)</param>
        public RapidOcrEngine(string? modelsPath = null, bool enablePreprocessing = true)
        {
            _modelsPath = modelsPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "RapidOCR");
            _enablePreprocessing = enablePreprocessing;
        }

        public string Name => "RapidOCR";
        public string Description => _enablePreprocessing
            ? "RapidOCR PP-OCR v5 - 启用图像预处理"
            : "RapidOCR PP-OCR v5 - 基础模式";

        public bool RequiresOnlineModel => false;
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 初始化引擎
        /// </summary>
        public async Task InitializeAsync(Action<string>? progressCallback = null)
        {
            try
            {
                progressCallback?.Invoke("正在初始化 RapidOCR 引擎...");

                // 第一步：确保模型文件存在（自动下载缺失的文件）
                await EnsureModelsExists(progressCallback);

                // 第二步：初始化 RapidOCR 引擎
                await Task.Run(() =>
                {
                    progressCallback?.Invoke("正在加载 RapidOCR 模型...");

                    // 初始化引擎
                    _engine = new RapidOcr();

                    // 初始化模型（使用默认路径,InitModels 会自动查找）
                    _engine.InitModels();
                });

                _isInitialized = true;
                string preprocessingStatus = _enablePreprocessing ? "(启用预处理)" : "(基础模式)";
                progressCallback?.Invoke($"✓ RapidOCR 引擎初始化成功 {preprocessingStatus}");
            }
            catch (Exception ex)
            {
                _isInitialized = false;
                throw new Exception(
                    $"RapidOCR 引擎初始化失败: {ex.Message}\n\n" +
                    "解决方案:\n" +
                    "1. 确保网络连接正常,程序会自动下载模型文件\n" +
                    "2. 检查 Models/RapidOCR 目录中的模型文件\n" +
                    "3. 如需手动下载,请访问: https://github.com/RapidAI/RapidOCR/releases",
                    ex);
            }
        }

        /// <summary>
        /// 确保所需的模型文件存在
        /// </summary>
        private async Task EnsureModelsExists(Action<string>? progressCallback)
        {
            // 确保目录结构存在
            var directories = new[]
            {
                Path.Combine(_modelsPath, "det_models"),
                Path.Combine(_modelsPath, "rec_models"),
                Path.Combine(_modelsPath, "cls_models"),
                Path.Combine(_modelsPath, "dict")
            };

            foreach (var dir in directories)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }

            // 需要下载的模型文件
            var models = new[]
            {
                new
                {
                    Name = "检测模型",
                    FileName = "ch_PP-OCRv5_mobile_det.onnx",
                    SubDir = "det_models",
                    Url = $"{ModelsBaseUrl}/ch_PP-OCRv5_mobile_det.onnx"
                },
                new
                {
                    Name = "识别模型",
                    FileName = "ch_PP-OCRv5_rec_mobile_infer.onnx",
                    SubDir = "rec_models",
                    Url = $"{ModelsBaseUrl}/ch_PP-OCRv5_rec_mobile_infer.onnx"
                },
                new
                {
                    Name = "分类模型",
                    FileName = "ch_ppocr_mobile_v2.0_cls_infer.onfer.onnx",
                    SubDir = "cls_models",
                    Url = $"{ModelsBaseUrl}/ch_ppocr_mobile_v2.0_cls_infer.onfer.onnx"
                },
                new
                {
                    Name = "识别字典",
                    FileName = "ppocr_keys_v1.txt",
                    SubDir = "dict",
                    Url = $"{ModelsBaseUrl}/ppocr_keys_v1.txt"
                }
            };

            foreach (var model in models)
            {
                var outputPath = Path.Combine(_modelsPath, model.SubDir, model.FileName);
                if (!File.Exists(outputPath))
                {
                    await DownloadModel(model.Name, model.Url, outputPath, progressCallback);
                }
                else
                {
                    progressCallback?.Invoke($"✓ 已找到 {model.Name}");
                }
            }
        }

        /// <summary>
        /// 从 GitHub 下载模型文件
        /// </summary>
        private async Task DownloadModel(string modelName, string url, string outputPath, Action<string>? progressCallback)
        {
            progressCallback?.Invoke($"正在下载 {modelName}...");

            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromMinutes(10);
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                double totalMB = totalBytes / (1024.0 * 1024.0);

                using var fileStream = File.Create(outputPath);
                using var stream = await response.Content.ReadAsStreamAsync();
                var buffer = new byte[8192];
                long totalRead = 0;
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalRead += bytesRead;

                    if (totalBytes > 0)
                    {
                        double progress = (double)totalRead / totalBytes * 100;
                        double downloadedMB = totalRead / (1024.0 * 1024.0);
                        progressCallback?.Invoke($"正在下载 {modelName}... {downloadedMB:F1}MB / {totalMB:F1}MB ({progress:F1}%)");
                    }
                }

                progressCallback?.Invoke($"✓ {modelName} 下载完成");
            }
            catch (Exception ex)
            {
                throw new Exception($"下载 {modelName} 失败: {ex.Message}\n\n" +
                    $"请手动下载：\n" +
                    $"1. 访问：{url}\n" +
                    $"2. 下载后放到：{outputPath}", ex);
            }
        }

        /// <summary>
        /// 识别图片中的文字
        /// </summary>
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
                Stopwatch sw = Stopwatch.StartNew();

                try
                {
                    // 使用 SkiaSharp 读取图片
                    using var bitmap = SKBitmap.Decode(imagePath);

                    if (bitmap == null)
                    {
                        return new OcrResult
                        {
                            Success = false,
                            ErrorMessage = "无法读取图片文件"
                        };
                    }

                    // 根据配置决定是否进行预处理
                    SKBitmap imageToOcr = _enablePreprocessing
                        ? PreprocessImage(bitmap)
                        : bitmap;

                    // 如果启用了预处理且创建了新 Bitmap,需要释放
                    bool needDisposePreprocessed = _enablePreprocessing;
                    try
                    {
                        // 执行 OCR 识别
                        var ocrResult = _engine.Detect(imageToOcr, RapidOcrOptions.Default);

                        sw.Stop();

                        return new OcrResult
                        {
                            Success = ocrResult != null,
                            Text = ocrResult?.StrRes?.Trim() ?? string.Empty,
                            RegionCount = ocrResult?.TextBlocks?.Count() ?? 0,
                            ElapsedMilliseconds = sw.ElapsedMilliseconds,
                            EngineName = Name
                        };
                    }
                    finally
                    {
                        // 释放预处理生成的图像
                        if (needDisposePreprocessed && !ReferenceEquals(imageToOcr, bitmap))
                        {
                            imageToOcr.Dispose();
                        }
                    }
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    return new OcrResult
                    {
                        Success = false,
                        ErrorMessage = $"识别异常: {ex.Message}"
                    };
                }
            });
        }

        /// <summary>
        /// 图像预处理管道 - 使用 SkiaSharp 提升识别准确率
        /// </summary>
        /// <param name="input">原始图像</param>
        /// <returns>预处理后的图像</returns>
        private SKBitmap PreprocessImage(SKBitmap input)
        {
            // 步骤 1: 转换为灰度图
            var grayImage = new SKBitmap(input.Width, input.Height);
            using (var canvas = new SKCanvas(grayImage))
            {
                using (var paint = new SKPaint())
                {
                    // 使用 grayscale 滤镜
                    paint.ColorFilter = SKColorFilter.CreateColorMatrix(
                        new float[] {
                            0.299f, 0.587f, 0.114f, 0, 0,  // R
                            0.299f, 0.587f, 0.114f, 0, 0,  // G
                            0.299f, 0.587f, 0.114f, 0, 0,  // B
                            0, 0, 0, 1, 0                  // A
                        }
                    );
                    canvas.DrawBitmap(input, 0, 0, paint);
                }
            }

            // 步骤 2: 增强对比度 (简单的阈值处理)
            // 注意: SkiaSharp 的图像处理能力有限,这里仅做灰度转换
            // 更复杂的处理建议使用 OpenCV 或其他图像处理库

            return grayImage;
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
