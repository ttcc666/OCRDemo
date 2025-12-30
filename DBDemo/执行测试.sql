-- =============================================
-- 执行 nvarchar(max) 参数测试
-- =============================================

-- 测试 1: 小数据量 (1KB)
DECLARE @Test1KB NVARCHAR(MAX);
SET @Test1KB = REPLICATE(N'A', 512); -- 512 字符 * 2 字节 = 1024 字节 = 1KB
EXEC sp_TestNvarcharMax @TestData = @Test1KB, @TestName = '测试_1KB_ASCII';
GO

-- 测试 2: 10KB
DECLARE @Test10KB NVARCHAR(MAX);
SET @Test10KB = REPLICATE(N'B', 5120); -- 5120 * 2 = 10240 字节 = 10KB
EXEC sp_TestNvarcharMax @TestData = @Test10KB, @TestName = '测试_10KB_ASCII';
GO

-- 测试 3: 100KB
DECLARE @Test100KB NVARCHAR(MAX);
SET @Test100KB = REPLICATE(N'C', 51200); -- 51200 * 2 = 102400 字节 = 100KB
EXEC sp_TestNvarcharMax @TestData = @Test100KB, @TestName = '测试_100KB_ASCII';
GO

-- 测试 4: 1MB
DECLARE @Test1MB NVARCHAR(MAX);
SET @Test1MB = REPLICATE(N'D', 524288); -- 524288 * 2 = 1048576 字节 = 1MB
EXEC sp_TestNvarcharMax @TestData = @Test1MB, @TestName = '测试_1MB_ASCII';
GO

-- 测试 5: 10MB
DECLARE @Test10MB NVARCHAR(MAX);
SET @Test10MB = REPLICATE(N'E', 5242880); -- 5242880 * 2 = 10485760 字节 = 10MB
EXEC sp_TestNvarcharMax @TestData = @Test10MB, @TestName = '测试_10MB_ASCII';
GO

-- 测试 6: 中文字符 1MB
DECLARE @TestChinese1MB NVARCHAR(MAX);
SET @TestChinese1MB = REPLICATE(N'中', 524288); -- 中文字符，每个 2 字节
EXEC sp_TestNvarcharMax @TestData = @TestChinese1MB, @TestName = '测试_1MB_中文';
GO

-- 测试 7: 混合字符 1MB
DECLARE @TestMixed1MB NVARCHAR(MAX);
SET @TestMixed1MB = REPLICATE(N'A中B文C', 104857); -- 混合字符
EXEC sp_TestNvarcharMax @TestData = @TestMixed1MB, @TestName = '测试_1MB_混合字符';
GO
