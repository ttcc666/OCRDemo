using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using Emgu.CV;
using Emgu.CV.OCR;
using Emgu.CV.Structure;
using Drawing = System.Drawing;

namespace OCRDemo.Engines
{
    /// <summary>
    /// EmguCV OCR 引擎实现 (基于 Tesseract)
    /// </summary>
    public class EmguCvOcrEngine : IOcrEngine
    {
        private Emgu.CV.OCR.Tesseract? _tesseract;
        private readonly string _tessdataPath;
        private readonly string _baseDir;
        private bool _isInitialized;

        public string Name => "EmguCV OCR";
        public string Description => "EmguCV + Tesseract,支持高级图像预处理";
        public bool RequiresOnlineModel => true; // 首次使用需要下载训练数据
        public bool IsInitialized => _isInitialized;

        public EmguCvOcrEngine()
        {
            // 设置 tessdata 目录路径
            _baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata_emgu");
            _tessdataPath = _baseDir;
        }

        public async Task InitializeAsync(Action<string>? progressCallback = null)
        {
            if (_isInitialized)
            {
                progressCallback?.Invoke("引擎已初始化");
                return;
            }

            try
            {
                progressCallback?.Invoke("正在检查训练数据文件...");

                // 确保目录存在
                if (!Directory.Exists(_tessdataPath))
                {
                    Directory.CreateDirectory(_tessdataPath);
                }

                // 检查并下载必要的训练数据文件
                await EnsureTrainedDataAsync("chi_sim", progressCallback); // 简体中文
                await EnsureTrainedDataAsync("eng", progressCallback);     // 英文

                progressCallback?.Invoke("正在初始化 EmguCV Tesseract 引擎...");

                // 初始化 Tesseract 引擎,支持中英文
                _tesseract = new Emgu.CV.OCR.Tesseract(_tessdataPath, "chi_sim+eng", OcrEngineMode.TesseractLstmCombined);

                _isInitialized = true;
                progressCallback?.Invoke("EmguCV Tesseract 引擎初始化成功");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"EmguCV Tesseract 引擎初始化失败: {ex.Message}", ex);
            }
        }

        public async Task<OcrResult> RecognizeAsync(string imagePath)
        {
            if (!_isInitialized || _tesseract == null)
            {
                return new OcrResult
                {
                    Success = false,
                    ErrorMessage = "引擎未初始化",
                    EngineName = Name
                };
            }

            var sw = Stopwatch.StartNew();

            try
            {
                // 使用 EmguCV 加载图片
                using var img = CvInvoke.Imread(imagePath);

                if (img.IsEmpty)
                {
                    throw new InvalidOperationException("无法加载图片");
                }

                // 图像预处理 - 转为灰度图
                using var gray = new Mat();
                CvInvoke.CvtColor(img, gray, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);

                // 自适应阈值处理,提高识别率
                using var binary = new Mat();
                CvInvoke.AdaptiveThreshold(
                    gray,
                    binary,
                    255,
                    Emgu.CV.CvEnum.AdaptiveThresholdType.GaussianC,
                    Emgu.CV.CvEnum.ThresholdType.Binary,
                    11,
                    2);

                // 使用预处理后的图像进行识别
                _tesseract.SetImage(binary);
                _tesseract.Recognize();

                // 获取识别结果
                var allText = _tesseract.GetUTF8Text();
                var textBlocks = new List<OcrTextBlock>();

                // 获取文本块信息（按行）
                var boxes = _tesseract.GetBoxText();
                if (!string.IsNullOrEmpty(boxes))
                {
                    var lines = boxes.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        var parts = line.Split(' ');
                        if (parts.Length >= 6)
                        {
                            var text = parts[0];
                            if (int.TryParse(parts[1], out int x) &&
                                int.TryParse(parts[2], out int y) &&
                                int.TryParse(parts[3], out int w) &&
                                int.TryParse(parts[4], out int h))
                            {
                                textBlocks.Add(new OcrTextBlock
                                {
                                    Text = text,
                                    Confidence = 0.9f, // EmguCV 不提供详细置信度,使用默认值
                                    BoundingBox = new Drawing.Rectangle(x, y, w - x, h - y),
                                    BoxPoints = new List<Drawing.Point>
                                    {
                                        new(x, y),
                                        new(w, y),
                                        new(w, h),
                                        new(x, h)
                                    },
                                    BlockType = OcrTextBlockType.Word
                                });
                            }
                        }
                    }
                }

                sw.Stop();

                return new OcrResult
                {
                    Success = true,
                    Text = allText?.Trim() ?? string.Empty,
                    RegionCount = textBlocks.Count,
                    TextBlocks = textBlocks,
                    ElapsedMilliseconds = sw.ElapsedMilliseconds,
                    EngineName = Name
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                return new OcrResult
                {
                    Success = false,
                    ErrorMessage = $"识别失败: {ex.Message}",
                    ElapsedMilliseconds = sw.ElapsedMilliseconds,
                    EngineName = Name
                };
            }
        }

        /// <summary>
        /// 确保训练数据文件存在,如果不存在则下载
        /// </summary>
        private async Task EnsureTrainedDataAsync(string lang, Action<string>? progressCallback)
        {
            var trainedDataFile = Path.Combine(_tessdataPath, $"{lang}.traineddata");

            if (File.Exists(trainedDataFile))
            {
                progressCallback?.Invoke($"训练数据 {lang} 已存在");
                return;
            }

            progressCallback?.Invoke($"正在下载 {lang} 训练数据...");

            try
            {
                // 从 GitHub 下载训练数据
                var url = $"https://github.com/tesseract-ocr/tessdata/raw/main/{lang}.traineddata";

                using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
                var data = await client.GetByteArrayAsync(url);

                await File.WriteAllBytesAsync(trainedDataFile, data);

                progressCallback?.Invoke($"训练数据 {lang} 下载完成");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"下载训练数据失败: {ex.Message}\n" +
                    $"请手动下载 {lang}.traineddata 文件到: {_tessdataPath}",
                    ex);
            }
        }

        public void Dispose()
        {
            _tesseract?.Dispose();
            _tesseract = null;
            _isInitialized = false;
        }
    }
}