using System.Diagnostics;
using OpenCvSharp;
using Sdcb.PaddleInference;
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models;
using Sdcb.PaddleOCR.Models.Online;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;

namespace OCRDemo.Engines
{
    /// <summary>
    /// Sdcb.PaddleOCR 引擎实现
    /// </summary>
    public class SdcbPaddleOcrEngine : IOcrEngine
    {
        private PaddleOcrAll? _engine;
        private bool _isInitialized = false;

        public string Name => "PaddleOCR (Sdcb.PaddleOCR)";
        public string Description => "PaddleOCR V4 中文模型 - 高精度OCR识别引擎";
        public bool RequiresOnlineModel => true;
        public bool IsInitialized => _isInitialized;

        public async Task InitializeAsync(Action<string>? progressCallback = null)
        {
            try
            {
                progressCallback?.Invoke("正在下载/加载 PaddleOCR V4 模型...");

                await Task.Run(async () =>
                {
                    // 下载/加载模型
                    FullOcrModel model = await OnlineFullModels.ChineseV4.DownloadAsync();

                    progressCallback?.Invoke("正在初始化引擎...");

                    // 初始化引擎
                    _engine = new PaddleOcrAll(model, PaddleDevice.Mkldnn())
                    {
                        AllowRotateDetection = true,
                        Enable180Classification = true,
                    };
                });

                _isInitialized = true;
                progressCallback?.Invoke("模型加载成功");
            }
            catch (Exception ex)
            {
                _isInitialized = false;
                throw new Exception($"PaddleOCR 引擎初始化失败: {ex.Message}", ex);
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
                    using (Mat src = Cv2.ImRead(imagePath, ImreadModes.Color))
                    {
                        if (src.Empty())
                        {
                            return new OcrResult
                            {
                                Success = false,
                                ErrorMessage = "无法读取图片文件"
                            };
                        }

                        Stopwatch sw = Stopwatch.StartNew();
                        PaddleOcrResult ocrResult = _engine.Run(src);
                        sw.Stop();

                        // 构建结构化文本块列表
                        var textBlocks = new List<OcrTextBlock>();
                        foreach (var region in ocrResult.Regions)
                        {
                            // 从 RotatedRect 获取边界框
                            var rect = region.Rect;
                            var size = rect.Size;
                            var center = rect.Center;

                            textBlocks.Add(new OcrTextBlock
                            {
                                Text = region.Text,
                                Confidence = (float)region.Score,
                                BoxPoints = new List<Point>
                                {
                                    new Point((int)(center.X - size.Width / 2), (int)(center.Y - size.Height / 2)),
                                    new Point((int)(center.X + size.Width / 2), (int)(center.Y - size.Height / 2)),
                                    new Point((int)(center.X + size.Width / 2), (int)(center.Y + size.Height / 2)),
                                    new Point((int)(center.X - size.Width / 2), (int)(center.Y + size.Height / 2))
                                },
                                BoundingBox = new Rectangle(
                                    (int)(center.X - size.Width / 2),
                                    (int)(center.Y - size.Height / 2),
                                    (int)size.Width,
                                    (int)size.Height
                                ),
                                BlockType = OcrTextBlockType.Line
                            });
                        }

                        return new OcrResult
                        {
                            Success = true,
                            Text = ocrResult.Text,
                            RegionCount = ocrResult.Regions.Length,
                            ElapsedMilliseconds = sw.ElapsedMilliseconds,
                            EngineName = Name,
                            TextBlocks = textBlocks
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
