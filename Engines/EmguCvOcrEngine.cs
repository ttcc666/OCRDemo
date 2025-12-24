using System.Diagnostics;
using System.IO;
using System.Net.Http;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.OCR;
using Emgu.CV.Structure;
using MatEmgu = Emgu.CV.Mat;  // 命名空间别名,避免与 OpenCvSharp4 冲突
using TesseractEmgu = Emgu.CV.OCR.Tesseract;  // 别名避免与 Tesseract 命名空间冲突

namespace OCRDemo.Engines
{
    /// <summary>
    /// Emgu CV OCR 引擎实现
    /// 基于 Emgu.CV.Tesseract 封装,提供增强的图像预处理能力
    /// 特性: 与 OpenCV 图像处理无缝集成,支持强大的预处理管道
    /// </summary>
    public class EmguCvOcrEngine : IOcrEngine
    {
        private TesseractEmgu? _tesseract;
        private bool _isInitialized = false;
        private readonly string _tessDataPath;
        private readonly bool _enablePreprocessing;  // 是否启用图像预处理

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="tessDataPath">tessdata 路径,默认与 Tesseract 引擎共享</param>
        /// <param name="enablePreprocessing">是否启用图像预处理(默认启用以提升识别准确率)</param>
        public EmguCvOcrEngine(string? tessDataPath = null, bool enablePreprocessing = true)
        {
            // 复用现有 tessdata 文件夹,与 Tesseract 引擎共享语言模型
            _tessDataPath = tessDataPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
            _enablePreprocessing = enablePreprocessing;
        }

        public string Name => "Emgu CV OCR";
        public string Description => _enablePreprocessing
            ? "Emgu CV Tesseract 封装 - 启用图像预处理增强"
            : "Emgu CV Tesseract 封装 - 基础模式";

        public bool RequiresOnlineModel => false;
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 初始化引擎
        /// </summary>
        public async Task InitializeAsync(Action<string>? progressCallback = null)
        {
            try
            {
                progressCallback?.Invoke("正在初始化 Emgu CV OCR 引擎...");

                // 第一步：确保训练数据文件存在（自动下载缺失的文件）
                await EnsureTrainedDataExists(progressCallback);

                // 第二步：初始化 Tesseract 引擎
                await Task.Run(() =>
                {
                    progressCallback?.Invoke("正在加载 Emgu CV Tesseract 引擎...");

                    // 初始化 Tesseract 引擎
                    // OcrEngineMode.Default: 使用默认 LSTM 引擎
                    _tesseract = new TesseractEmgu(
                        _tessDataPath,
                        "chi_sim+eng",  // 中英文混合识别
                        OcrEngineMode.Default
                    );

                    // 可选: 设置识别参数以优化性能
                    _tesseract.SetVariable("tessedit_pageseg_mode", "6");  // 假设单列文本
                });

                _isInitialized = true;
                string preprocessingStatus = _enablePreprocessing ? "(启用预处理)" : "(基础模式)";
                progressCallback?.Invoke($"✓ Emgu CV OCR 引擎初始化成功 {preprocessingStatus}");
            }
            catch (Exception ex)
            {
                _isInitialized = false;
                throw new Exception(
                    $"Emgu CV OCR 引擎初始化失败: {ex.Message}\n\n" +
                    "解决方案:\n" +
                    "1. 确保已安装 Emgu.CV.runtime.windows NuGet 包\n" +
                    "2. 检查 tessdata 文件夹中是否存在 chi_sim.traineddata 和 eng.traineddata\n" +
                    $"   路径: {_tessDataPath}\n" +
                    "3. 程序会自动下载缺失的训练数据文件",
                    ex);
            }
        }

        /// <summary>
        /// 确保所需的训练数据文件存在
        /// </summary>
        private async Task EnsureTrainedDataExists(Action<string>? progressCallback)
        {
            // 确保目录存在
            if (!Directory.Exists(_tessDataPath))
            {
                Directory.CreateDirectory(_tessDataPath);
            }

            // 需要下载的语言文件
            string[] requiredLanguages = { "chi_sim", "eng" };

            foreach (string lang in requiredLanguages)
            {
                string trainedDataPath = Path.Combine(_tessDataPath, $"{lang}.traineddata");
                if (!File.Exists(trainedDataPath))
                {
                    await DownloadTrainedData(lang, _tessDataPath, progressCallback);
                }
                else
                {
                    progressCallback?.Invoke($"已找到 {lang}.traineddata");
                }
            }
        }

        /// <summary>
        /// 从 GitHub 下载指定的训练数据文件
        /// </summary>
        private async Task DownloadTrainedData(string language, string tessdataPath, Action<string>? progressCallback)
        {
            string url = $"https://github.com/tesseract-ocr/tessdata/raw/main/{language}.traineddata";
            string outputPath = Path.Combine(tessdataPath, $"{language}.traineddata");

            progressCallback?.Invoke($"正在下载 {language}.traineddata...");

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
                        progressCallback?.Invoke($"正在下载 {language}.traineddata... {downloadedMB:F1}MB / {totalMB:F1}MB ({progress:F1}%)");
                    }
                }

                progressCallback?.Invoke($"✓ {language}.traineddata 下载完成");
            }
            catch (Exception ex)
            {
                throw new Exception($"下载 {language}.traineddata 失败: {ex.Message}\n\n" +
                    $"请手动下载：\n" +
                    $"1. 访问：{url}\n" +
                    $"2. 下载后放到：{tessdataPath}", ex);
            }
        }

        /// <summary>
        /// 识别图片中的文字
        /// </summary>
        public Task<OcrResult> RecognizeAsync(string imagePath)
        {
            if (!_isInitialized || _tesseract == null)
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
                    // 加载图像 - 使用 CvInvoke.Imread
                    var originalImage = CvInvoke.Imread(imagePath, Emgu.CV.CvEnum.ImreadModes.AnyColor);

                    if (originalImage.IsEmpty)
                    {
                        return new OcrResult
                        {
                            Success = false,
                            ErrorMessage = "无法加载图像文件"
                        };
                    }

                    // 根据配置决定是否进行预处理
                    MatEmgu imageToOcr = _enablePreprocessing
                        ? PreprocessImage(originalImage)
                        : originalImage;

                    // 如果启用了预处理且创建了新 Mat,需要使用 using 语句
                    bool needDisposePreprocessed = _enablePreprocessing;
                    try
                    {
                        // 执行 OCR 识别
                        // Emgu CV 的 Recognize 方法不接受参数
                        _tesseract.Recognize();

                        // 获取识别结果
                        string text = _tesseract.GetUTF8Text();

                        // 获取识别区域数量(使用 GetWords 作为替代)
                        var words = _tesseract.GetWords();
                        int regionCount = words != null ? words.Length : 0;

                        sw.Stop();

                        return new OcrResult
                        {
                            Success = true,
                            Text = text.Trim(),
                            RegionCount = regionCount,
                            ElapsedMilliseconds = sw.ElapsedMilliseconds,
                            EngineName = Name
                        };
                    }
                    finally
                    {
                        // 释放预处理生成的图像
                        if (needDisposePreprocessed && !ReferenceEquals(imageToOcr, originalImage))
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
        /// 图像预处理管道 - 使用 Emgu CV 的强大图像处理能力提升 OCR 准确率
        /// </summary>
        /// <param name="inputImage">原始图像</param>
        /// <returns>预处理后的图像</returns>
        private MatEmgu PreprocessImage(MatEmgu inputImage)
        {
            // 步骤 1: 转换为灰度图
            var grayImage = new MatEmgu();
            CvInvoke.CvtColor(inputImage, grayImage, ColorConversion.Bgr2Gray);

            // 步骤 2: 中值滤波去噪
            var denoisedImage = new MatEmgu();
            CvInvoke.MedianBlur(grayImage, denoisedImage, 3);

            // 步骤 3: 自适应阈值二值化
            // 使用 Otsu 二值化 + 反转,使文字变为黑色,背景为白色
            var binaryImage = new MatEmgu();
            CvInvoke.Threshold(
                denoisedImage,
                binaryImage,
                0,                           // 阈值 (0 = 自动)
                255,                         // 最大值
                ThresholdType.Otsu | ThresholdType.BinaryInv  // Otsu + 反转
            );

            // 清理中间变量
            grayImage.Dispose();
            denoisedImage.Dispose();

            return binaryImage;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _tesseract?.Dispose();
            _tesseract = null;
            _isInitialized = false;
        }
    }
}
