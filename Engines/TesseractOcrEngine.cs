using System.Diagnostics;
using System.IO;
using System.Net.Http;
using Tesseract;
using Drawing = System.Drawing;

namespace OCRDemo.Engines
{
    /// <summary>
    /// Tesseract OCR 引擎实现
    /// </summary>
    public class TesseractOcrEngine : IOcrEngine
    {
        private TesseractEngine? _engine;
        private readonly string _tessdataPath;
        private readonly string _baseDir;
        private bool _isInitialized;

        public string Name => "Tesseract OCR";
        public string Description => "Google 开源 OCR 引擎,支持100+种语言";
        public bool RequiresOnlineModel => true; // 首次使用需要下载训练数据
        public bool IsInitialized => _isInitialized;

        public TesseractOcrEngine()
        {
            // 设置 tessdata 目录路径
            _baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
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

                progressCallback?.Invoke("正在初始化 Tesseract 引擎...");

                // 初始化 Tesseract 引擎,支持中英文
                _engine = new TesseractEngine(_tessdataPath, "chi_sim+eng", EngineMode.Default);

                // 设置识别参数
                _engine.SetVariable("tessedit_char_whitelist", ""); // 不限制字符
                _engine.SetVariable("preserve_interword_spaces", "1"); // 保留单词间空格

                _isInitialized = true;
                progressCallback?.Invoke("Tesseract 引擎初始化成功");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Tesseract 引擎初始化失败: {ex.Message}", ex);
            }
        }

        public async Task<OcrResult> RecognizeAsync(string imagePath)
        {
            if (!_isInitialized || _engine == null)
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
                using var img = Pix.LoadFromFile(imagePath);
                using var page = _engine.Process(img);

                var textBlocks = new List<OcrTextBlock>();
                var allText = page.GetText();

                // 使用 Tesseract 的迭代器获取详细信息
                using var iter = page.GetIterator();
                iter.Begin();

                do
                {
                    // 获取单词级别的信息
                    if (iter.TryGetBoundingBox(PageIteratorLevel.Word, out var bounds))
                    {
                        var text = iter.GetText(PageIteratorLevel.Word);
                        var confidence = iter.GetConfidence(PageIteratorLevel.Word) / 100f;

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            textBlocks.Add(new OcrTextBlock
                            {
                                Text = text,
                                Confidence = confidence,
                                BoundingBox = new Drawing.Rectangle(
                                    bounds.X1, bounds.Y1,
                                    bounds.X2 - bounds.X1,
                                    bounds.Y2 - bounds.Y1
                                ),
                                BoxPoints = new List<Drawing.Point>
                                {
                                    new(bounds.X1, bounds.Y1), // 左上
                                    new(bounds.X2, bounds.Y1), // 右上
                                    new(bounds.X2, bounds.Y2), // 右下
                                    new(bounds.X1, bounds.Y2)  // 左下
                                },
                                BlockType = OcrTextBlockType.Word
                            });
                        }
                    }
                } while (iter.Next(PageIteratorLevel.Word));

                sw.Stop();

                return new OcrResult
                {
                    Success = true,
                    Text = allText.Trim(),
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
            _engine?.Dispose();
            _engine = null;
            _isInitialized = false;
        }
    }
}