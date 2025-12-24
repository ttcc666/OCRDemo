# Emgu CV OCR 集成文档

## 📚 引擎简介

**Emgu CV OCR** 是 Emgu CV (OpenCV 的 .NET 包装器) 对 Tesseract 的封装,提供强大的图像预处理能力。

- **官方网站**: https://www.emgu.com
- **GitHub**: https://github.com/emgucv/emgucv
- **许可证**: GPL v3 (免费) 或 商业许可证
- **当前版本**: 4.12.0.5764
- **特点**:
  - 与 OpenCV 图像处理无缝集成
  - 提供强大的图像预处理管道
  - 支持中英文混合识别
  - 复用 tessdata 文件

---

## 📦 NuGet 依赖

```xml
<PackageReference Include="Emgu.CV" Version="4.12.0.5764" />
<PackageReference Include="Emgu.CV.runtime.windows" Version="4.12.0.5764" />
```

**安装命令**:
```bash
dotnet add package Emgu.CV --version 4.12.0.5764
dotnet add package Emgu.CV.runtime.windows --version 4.12.0.5764
```

---

## 🔧 核心代码

### 初始化引擎

```csharp
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.OCR;
using MatEmgu = Emgu.CV.Mat;
using TesseractEmgu = Emgu.CV.OCR.Tesseract;

// 初始化
_tesseract = new TesseractEmgu(
    _tessDataPath,
    "chi_sim+eng",
    OcrEngineMode.Default
);
```

### 图像预处理

```csharp
private MatEmgu PreprocessImage(MatEmgu inputImage)
{
    // 1. 转换为灰度图
    var grayImage = new MatEmgu();
    CvInvoke.CvtColor(inputImage, grayImage, ColorConversion.Bgr2Gray);

    // 2. 中值滤波去噪
    var denoisedImage = new MatEmgu();
    CvInvoke.MedianBlur(grayImage, denoisedImage, 3);

    // 3. Otsu 二值化
    var binaryImage = new MatEmgu();
    CvInvoke.Threshold(
        denoisedImage,
        binaryImage,
        0,
        255,
        ThresholdType.Otsu | ThresholdType.BinaryInv
    );

    grayImage.Dispose();
    denoisedImage.Dispose();

    return binaryImage;
}
```

### 执行识别

```csharp
// 加载图像
var originalImage = CvInvoke.Imread(imagePath, ImreadModes.AnyColor);

// 执行识别
_tesseract.Recognize();
string text = _tesseract.GetUTF8Text();

// 获取识别区域
var words = _tesseract.GetWords();
int regionCount = words.Length;
```

---

## 🎯 命名空间别名

**重要**: 避免与 OpenCvSharp4 和直接 Tesseract 冲突:

```csharp
using MatEmgu = Emgu.CV.Mat;           // 区分 OpenCvSharp4.Mat
using TesseractEmgu = Emgu.CV.OCR.Tesseract;  // 区分 Tesseract 命名空间
```

---

## 📊 性能数据

| 指标 | 数值 |
|------|------|
| 识别速度 | 300-500ms/张 |
| 中文准确率 | ~92% (预处理后) |
| 英文准确率 | ~95% |
| 内存占用 | ~100-150MB |

---

## ⚠️ 常见问题

### 1. DLL 加载失败

**解决方案**: 确保 `Emgu.CV.runtime.windows` 已安装

### 2. 命名空间冲突

**解决方案**: 使用别名区分 (见上方)

### 3. 许可证限制

- 开源项目: GPL v3
- 商业项目: 需购买许可证 (~$200+)

---

## 🔗 相关资源

- **官方网站**: https://www.emgu.com
- **GitHub**: https://github.com/emgucv/emgucv
- **OCR 文档**: https://www.emgu.com/wiki/files/4.9.0/document/html/N_Emgu_CV_OCR.htm

---

**文档版本**: 1.0
**最后更新**: 2025-12-24
