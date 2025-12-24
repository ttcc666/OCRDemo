# OCRDemo 项目文档

欢迎阅读 OCRDemo 多引擎 OCR 识别工具的完整文档。

## 📚 文档列表

### [总体架构文档](./00-总体架构文档.md)
**推荐首先阅读!**

内容概览:
- 项目整体架构设计
- 核心接口定义 (IOcrEngine)
- 5 个 OCR 引擎对比
- 集成流程说明
- 性能基准测试
- 常见问题解答

---

## 🔧 OCR 引擎集成文档

### [1️⃣ Tesseract OCR 集成文档](./01-Tesseract-OCR集成文档.md)
**适合**: 学习 OCR 原理、开源项目

核心内容:
- Tesseract 引擎简介
- NuGet 依赖安装
- 自动语言模型下载
- 图像预处理技巧
- 性能优化建议

**优点**:
- ✅ 完全开源免费
- ✅ 支持 100+ 种语言
- ✅ 社区活跃,文档丰富

**缺点**:
- ❌ 识别速度较慢 (500-1000ms/张)
- ❌ 中文准确率相对较低 (~80%)

---

### [2️⃣ PaddleOCR (Sdcb) 集成文档](./02-PaddleOCR-Sdcb集成文档.md)
**适合**: 需要最新模型、在线更新

核心内容:
- Sdcb.PaddleOCR 封装库介绍
- 在线模型下载
- GPU/CPU 模式切换
- 批量识别优化
- 高级功能示例

**优点**:
- ✅ 官方 .NET 封装,更新及时
- ✅ 准确率高 (93%+)
- ✅ 支持在线模型更新

**缺点**:
- ❌ 首次需要联网下载模型
- ❌ 依赖 OpenCV

---

### [3️⃣ PaddleOCRSharp 集成文档](./03-PaddleOCRSharp集成文档.md)
**适合**: 离线环境、快速部署

核心内容:
- PaddleOCRSharp 封装库介绍
- **必须安装** Paddle.Runtime.win_x64
- 内置轻量级模型使用
- OCR 参数详解
- 常见 DLL 加载问题解决

**优点**:
- ✅ **离线部署**,无需联网
- ✅ 自带 PP-OCRv4 模型
- ✅ 中文文档丰富

**缺点**:
- ❌ 需要单独安装运行时包
- ❌ 模型更新相对较慢

---

### [4️⃣ Emgu CV OCR 集成文档](./04-Emgu-CV-OCR集成文档.md)
**适合**: 需要 OpenCV 图像处理能力

核心内容:
- Emgu CV (OpenCV .NET 包装器) 介绍
- 强大的图像预处理管道
- 命名空间别名隔离技巧
- 与 OpenCvSharp4 共存方案

**优点**:
- ✅ 强大的图像预处理能力
- ✅ 与 OpenCV 无缝集成
- ✅ 提升识别准确率

**缺点**:
- ❌ **商业项目需购买许可证** (~$200+)
- ❌ 与 OpenCvSharp4 可能冲突
- ❌ GPL v3 许可证限制

---

### [5️⃣ RapidOCR 集成文档](./05-RapidOCR集成文档.md)
**适合**: 高性能需求、商业项目

核心内容:
- RapidOcrNet 封装库介绍
- 基于 ONNX 的 PP-OCR v5 模型
- **无 OpenCV 依赖**
- 自动模型下载
- 性能对比

**优点**:
- ✅ **速度最快** (100-200ms/张)
- ✅ **准确率最高** (95%+)
- ✅ **无 OpenCV 依赖**
- ✅ **Apache 2.0 许可,可商用**

**缺点**:
- ❌ 首次需要下载模型 (约 15MB)
- ❌ 相对较新的项目

---

## 🎯 引擎选择指南

### 快速选择

| 需求 | 推荐引擎 | 理由 |
|------|---------|------|
| **高性能** | RapidOCR ⭐ | 速度最快 + 准确率最高 |
| **离线环境** | PaddleOCRSharp | 自带模型,无需联网 |
| **学习研究** | Tesseract | 文档丰富,开源免费 |
| **图像预处理** | Emgu CV OCR | OpenCV 强大能力 |
| **在线更新** | Sdcb.PaddleOCR | 自动下载最新模型 |

### 性能对比

```
速度排序 (快→慢):
RapidOCR (⚡⚡⚡) > PaddleOCRSharp ≈ Sdcb.PaddleOCR (⚡⚡) > Emgu CV OCR (⚡) > Tesseract (⚩)

准确率排序 (高→低):
RapidOCR (🟢🟢🟢) ≈ Sdcb.PaddleOCR ≈ PaddleOCRSharp (🟢🟢🟢) > Emgu CV OCR (🟢🟢) > Tesseract (🟡)
```

---

## 🚀 快速开始

### 1. 选择引擎并安装依赖

```bash
# 示例: 安装 RapidOCR
dotnet add package RapidOcrNet --version 1.0.0
```

### 2. 创建引擎类

参考各引擎的集成文档,创建 `XxxOcrEngine.cs` 文件。

### 3. 注册到主程序

```csharp
// MainWindow.xaml.cs
_availableEngines.Add(new RapidOcrEngine());
```

### 4. 运行测试

```bash
dotnet run --project OCRDemo.csproj
```

---

## 📖 阅读顺序建议

### 初学者
1. [总体架构文档](./00-总体架构文档.md) - 了解项目整体设计
2. [Tesseract OCR 集成文档](./01-Tesseract-OCR集成文档.md) - 从最简单的开始

### 进阶开发者
1. [总体架构文档](./00-总体架构文档.md)
2. [RapidOCR 集成文档](./05-RapidOCR集成文档.md) - 学习最新的高性能方案
3. [PaddleOCRSharp 集成文档](./03-PaddleOCRSharp集成文档.md) - 学习离线部署方案

### 高级用户
- 全部文档阅读
- 重点研究性能优化部分
- 参考架构设计模式

---

## 🔧 技术栈总览

### 核心技术
- **.NET 8.0** - 最新 .NET 框架
- **WPF** - Windows 桌面应用框架
- **C# 12** - 最新 C# 语言特性

### OCR 引擎
- Tesseract 5.2.0
- Sdcb.PaddleOCR 3.0.1
- PaddleOCRSharp 6.0.0
- Emgu.CV 4.12.0
- RapidOcrNet 1.0.0

### 图像处理
- OpenCvSharp4 4.11.0
- SkiaSharp 3.119.1
- Emgu.CV 4.12.0

### 模型推理
- Paddle Inference 3.1.0
- ONNX Runtime 1.23.2

---

## 📞 获取帮助

### 常见问题
查看各个引擎文档中的 "⚠️ 常见问题" 章节

### 文档反馈
如发现文档问题,欢迎提交 Issue 或 Pull Request

---

## 📄 许可证

本项目文档采用 **CC BY 4.0** 许可证,可自由分享和修改。

---

**文档维护**: Claude Code Assistant
**最后更新**: 2025-12-24
**文档版本**: 1.0
