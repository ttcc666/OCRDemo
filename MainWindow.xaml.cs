using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using OCRDemo.Engines;

namespace OCRDemo
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private string? _currentImagePath;
        private IOcrEngine? _currentEngine;
        private readonly List<IOcrEngine> _availableEngines;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;

            // 初始化可用的 OCR 引擎列表
            _availableEngines = new List<IOcrEngine>
            {
                new SdcbPaddleOcrEngine(),
                new TesseractOcrEngine()
            };
        }

        /// <summary>
        /// 窗口加载时填充引擎下拉框
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 填充引擎选择下拉框
            foreach (var engine in _availableEngines)
            {
                CmbOcrEngine.Items.Add(new EngineItem
                {
                    Engine = engine,
                    DisplayName = $"{engine.Name} - {engine.Description}"
                });
            }

            // 默认选择第一个引擎
            if (CmbOcrEngine.Items.Count > 0)
            {
                CmbOcrEngine.SelectedIndex = 0;
            }

            UpdateEngineInfo();
        }

        /// <summary>
        /// 引擎选择改变事件
        /// </summary>
        private void CmbOcrEngine_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var selectedItem = CmbOcrEngine.SelectedItem as EngineItem;
            if (selectedItem != null)
            {
                BtnInitEngine.IsEnabled = true;
                UpdateEngineInfo(selectedItem.Engine);
            }
        }

        /// <summary>
        /// 更新引擎信息显示
        /// </summary>
        private void UpdateEngineInfo(IOcrEngine? engine = null)
        {
            engine ??= _currentEngine;
            if (engine != null)
            {
                TxtEngineInfo.Text = $"当前引擎: {engine.Name} | " +
                    $"状态: {(engine.IsInitialized ? "✓ 已初始化" : "✗ 未初始化")} | " +
                    $"需联网: {(engine.RequiresOnlineModel ? "是" : "否")}";
            }
            else
            {
                TxtEngineInfo.Text = "";
            }
        }

        /// <summary>
        /// 初始化引擎按钮点击事件
        /// </summary>
        private async void BtnInitEngine_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = CmbOcrEngine.SelectedItem as EngineItem;
            if (selectedItem?.Engine == null) return;

            try
            {
                ShowLoading(true, "正在初始化引擎...");
                UpdateStatus("正在初始化 OCR 引擎...");
                BtnInitEngine.IsEnabled = false;

                // 如果已有引擎，先释放
                if (_currentEngine != null && _currentEngine != selectedItem.Engine)
                {
                    _currentEngine.Dispose();
                    _currentEngine = null;
                }

                _currentEngine = selectedItem.Engine;

                await _currentEngine.InitializeAsync(progress =>
                {
                    UpdateStatus(progress ?? "");
                });

                UpdateStatus($"{_currentEngine.Name} 初始化成功");
                UpdateEngineInfo();

                // 如果已加载图片，启用识别按钮
                if (!string.IsNullOrEmpty(_currentImagePath))
                {
                    BtnRecognize.IsEnabled = true;
                }

                ShowLoading(false);
                BtnInitEngine.IsEnabled = true;
            }
            catch (Exception ex)
            {
                ShowLoading(false);
                BtnInitEngine.IsEnabled = true;
                MessageBox.Show($"引擎初始化失败：\n{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus("引擎初始化失败");
            }
        }

        /// <summary>
        /// 选择图片按钮点击事件
        /// </summary>
        private void BtnSelectImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "选择图片文件",
                Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp|所有文件|*.*",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                LoadImage(dialog.FileName);
            }
        }

        /// <summary>
        /// 加载并显示图片
        /// </summary>
        private void LoadImage(string imagePath)
        {
            try
            {
                _currentImagePath = imagePath;

                // 显示图片预览
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                ImgPreview.Source = bitmap;
                ImgPreview.Visibility = Visibility.Visible;
                TxtPlaceholder.Visibility = Visibility.Collapsed;

                // 清空之前的识别结果
                TxtResult.Text = "";

                // 如果引擎已初始化，启用识别按钮
                if (_currentEngine != null && _currentEngine.IsInitialized)
                {
                    BtnRecognize.IsEnabled = true;
                }
                else
                {
                    BtnRecognize.IsEnabled = false;
                }

                UpdateStatus($"已加载图片: {Path.GetFileName(imagePath)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载图片失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 识别按钮点击事件
        /// </summary>
        private async void BtnRecognize_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentImagePath) || _currentEngine == null)
            {
                MessageBox.Show("请先选择图片并初始化 OCR 引擎", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_currentEngine.IsInitialized)
            {
                MessageBox.Show("OCR 引擎未初始化，请先点击「初始化引擎」按钮", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                ShowLoading(true, "正在识别文字...");
                UpdateStatus("识别中...");
                BtnRecognize.IsEnabled = false;

                string imagePath = _currentImagePath;
                OcrResult result = await _currentEngine.RecognizeAsync(imagePath);

                if (result.Success)
                {
                    // 格式化输出结果
                    string output = $"✓ 识别完成！耗时: {result.ElapsedMilliseconds} ms\n";
                    output += $"✓ 使用引擎: {result.EngineName}\n";
                    output += $"✓ 识别到 {result.RegionCount} 个文本区域\n";
                    output += new string('=', 50) + "\n\n";
                    output += result.Text;

                    TxtResult.Text = output;
                    UpdateStatus("识别完成");
                }
                else
                {
                    TxtResult.Text = $"识别失败：{result.ErrorMessage}";
                    UpdateStatus("识别失败");
                }

                ShowLoading(false);
                BtnRecognize.IsEnabled = true;
            }
            catch (Exception ex)
            {
                ShowLoading(false);
                BtnRecognize.IsEnabled = true;
                MessageBox.Show($"识别失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus("识别失败");
            }
        }

        /// <summary>
        /// 清空按钮点击事件
        /// </summary>
        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            _currentImagePath = null;
            ImgPreview.Source = null;
            ImgPreview.Visibility = Visibility.Collapsed;
            TxtPlaceholder.Visibility = Visibility.Visible;
            TxtResult.Text = "";
            BtnRecognize.IsEnabled = false;
            UpdateStatus("就绪");
        }

        /// <summary>
        /// 显示/隐藏加载遮罩
        /// </summary>
        private void ShowLoading(bool show, string message = "加载中...")
        {
            Dispatcher.Invoke(() =>
            {
                LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
                TxtLoading.Text = message;
            });
        }

        /// <summary>
        /// 更新状态栏文本
        /// </summary>
        private void UpdateStatus(string status)
        {
            Dispatcher.Invoke(() =>
            {
                TxtStatus.Text = status;
            });
        }

        /// <summary>
        /// 窗口关闭时释放资源
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            _currentEngine?.Dispose();
            base.OnClosed(e);
        }

        /// <summary>
        /// 引擎下拉框项
        /// </summary>
        private class EngineItem
        {
            public IOcrEngine? Engine { get; set; }
            public string? DisplayName { get; set; }

            public override string ToString()
            {
                return DisplayName ?? "";
            }
        }
    }
}
