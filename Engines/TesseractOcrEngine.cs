using System.Diagnostics;
using System.IO;
using System.Net.Http;
using Tesseract;

namespace OCRDemo.Engines
{
    /// <summary>
    /// Tesseract OCR 引擎实现
    /// </summary>
    public class TesseractOcrEngine : IOcrEngine
    {
        private TesseractEngine? _engine;
        private bool _isInitialized = false;
        private readonly string _tessDataPath;

        public TesseractOcrEngine(string? tessDataPath = null)
        {
            // 如果未指定路径，使用默认的 tessdata 文件夹
            _tessDataPath = tessDataPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
        }

        public string Name => "Tesseract OCR";
        public string Description => "开源 OCR 引擎 - 支持多语言识别";
        public bool RequiresOnlineModel => false;
        public bool IsInitialized => _isInitialized;

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
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(10);
                    var response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? 0;
                    double totalMB = totalBytes / (1024.0 * 1024.0);

                    using (var fileStream = File.Create(outputPath))
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
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

        public async Task InitializeAsync(Action<string>? progressCallback = null)
        {
            try
            {
                progressCallback?.Invoke("正在初始化 Tesseract OCR 引擎...");

                // 第一步：确保训练数据文件存在（自动下载缺失的文件）
                await EnsureTrainedDataExists(progressCallback);

                // 第二步：初始化 Tesseract 引擎
                await Task.Run(() =>
                {
                    progressCallback?.Invoke("正在加载 Tesseract OCR 引擎...");
                    // 初始化 Tesseract 引擎（使用中文和英文）
                    _engine = new TesseractEngine(_tessDataPath, "chi_sim+eng", EngineMode.Default);
                });

                _isInitialized = true;
                progressCallback?.Invoke("✓ Tesseract OCR 引擎初始化成功");
            }
            catch (Exception ex)
            {
                _isInitialized = false;
                throw new Exception(
                    $"Tesseract OCR 引擎初始化失败: {ex.Message}\n\n" +
                    "解决方案：\n" +
                    "1. 检查网络连接，程序会自动下载训练数据\n" +
                    "2. 手动下载训练数据：\n" +
                    "   - 访问：https://github.com/tesseract-ocr/tessdata\n" +
                    "   - 下载：chi_sim.traineddata 和 eng.traineddata\n" +
                    $"   - 放到：{_tessDataPath}\n\n" +
                    "3. 推荐使用 PaddleOCR，无需额外配置", ex);
            }
        }

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
                    using (var img = Pix.LoadFromFile(imagePath))
                    using (var page = _engine.Process(img))
                    {
                        Stopwatch sw = Stopwatch.StartNew();
                        string text = page.GetText();
                        sw.Stop();

                        // 获取详细信息
                        var iterator = page.GetIterator();
                        int regionCount = 0;
                        if (iterator != null)
                        {
                            iterator.Begin();
                            do
                            {
                                regionCount++;
                            } while (iterator.Next(PageIteratorLevel.Block));
                        }

                        return new OcrResult
                        {
                            Success = true,
                            Text = text.Trim(),
                            RegionCount = regionCount,
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

        public void Dispose()
        {
            _engine?.Dispose();
            _engine = null;
            _isInitialized = false;
        }
    }
}
