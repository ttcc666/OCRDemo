# RapidOCR 集成文档

## 📚 引擎简介

**RapidOCR** 是基于 PaddleOCR ONNX 模型的高性能 OCR 引擎,移除了 OpenCV 依赖,使用 SkiaSharp 进行图像处理。

- **GitHub**: https://github.com/BobLd/RapidOcrNet
- **RapidAI**: https://github.com/RapidAI/RapidOCR
- **许可证**: Apache 2.0
- **当前版本**: 1.0.0
- **特点**:
  - 无 OpenCV 依赖,部署简单
  - 支持 PP-OCR v5 模型
  - 高性能 (100-200ms/张)
  - 自动模型下载
  - 可商用

---

## 📦 NuGet 依赖

```xml
<PackageReference Include="RapidOcrNet" Version="1.0.0" />
```

**自动安装的依赖**:
- SkiaSharp 3.119.1
- Microsoft.ML.OnnxRuntime 1.23.2
- SkiaSharp.NativeAssets.Win32

**安装命令**:
```bash
dotnet add package RapidOcrNet --version 1.0.0
```

---

## 🔧 核心代码

### 初始化引擎

```csharp
using RapidOcrNet;
using SkiaSharp;

// 创建引擎
_engine = new RapidOcr();

// 初始化模型 (使用默认路径)
_engine.InitModels();
```

### 执行识别

```csharp
// 加载图像
using var bitmap = SKBitmap.Decode(imagePath);

// 执行识别
var ocrResult = _engine.Detect(bitmap, RapidOcrOptions.Default);

// 获取结果
string text = ocrResult.StrRes;
int regionCount = ocrResult.TextBlocks.Count();
```

### 自动模型下载

```csharp
// 模型文件自动下载到 Models/RapidOCR/
Models/RapidOCR/
├── det_models/ch_PP-OCRv5_mobile_det.onnx
├── rec_models/ch_PP-OCRv5_rec_mobile_infer.onnx
├── cls_models/ch_ppocr_mobile_v2.0_cls_infer.onfer.onnx
└── dict/ppocr_keys_v1.txt
```

---

## 🎯 核心特性

### 1. 无 OpenCV 依赖

- 使用 SkiaSharp 替代 OpenCV
- 与 OpenCvSharp4 完全兼容
- 部署更简单

### 2. 高性能

- CPU 模式下 100-200ms/张
- 比 Tesseract 快 5-10 倍
- 与 PaddleOCR 性能相当

### 3. 自动模型管理

- 首次运行自动下载模型
- 模型文件约 15MB
- 支持 PP-OCR v5 最新模型

---

## 📊 性能数据

| 指标 | 数值 |
|------|------|
| 识别速度 | 100-200ms/张 |
| 中文准确率 | ~95% |
| 英文准确率 | ~97% |
| 内存占用 | ~100-150MB |
| 模型文件大小 | ~15MB |

---

## 🔄 与其他引擎对比

| 特性 | RapidOCR | PaddleOCR | Emgu CV | Tesseract |
|------|----------|-----------|---------|-----------|
| 速度 | ⚡⚡⚡ | ⚡⚡ | ⚡ | ⚩ |
| 准确率 | 🟢🟢🟢 | 🟢🟢 | 🟢🟢 | 🟡 |
| 部署难度 | 简单 | 中等 | 中等 | 简单 |
| 依赖冲突 | 无 | 无 | 有 | 无 |

**推荐场景**:
- 需要高性能 + 高准确率
- 不想处理 OpenCV 依赖冲突
- 商业项目 (Apache 2.0 许可)

---

## 🔗 相关资源

- **GitHub**: https://github.com/BobLd/RapidOcrNet
- **RapidAI**: https://github.com/RapidAI/RapidOCR
- **NuGet**: https://www.nuget.org/packages/RapidOcrNet

---

**文档版本**: 1.0
**最后更新**: 2025-12-24
