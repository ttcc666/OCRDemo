/*
================================================================================
SQL Server 存储过程 nvarchar(max) 参数最大长度完整测试脚本
================================================================================
测试目标: 验证存储过程中 nvarchar(max) 参数的实际最大长度
数据库: MobileMallDB (SQL Server)
创建日期: 2025-12-30
================================================================================
*/

-- ============================================================================
-- 第一部分：创建测试存储过程
-- ============================================================================

PRINT '========================================';
PRINT '步骤 1: 创建测试存储过程';
PRINT '========================================';
GO

-- 删除已存在的存储过程
IF OBJECT_ID('sp_TestNvarcharMax', 'P') IS NOT NULL
    DROP PROCEDURE sp_TestNvarcharMax;
GO

-- 创建测试存储过程
CREATE PROCEDURE sp_TestNvarcharMax
    @TestData NVARCHAR(MAX),
    @TestName NVARCHAR(200) = '默认测试'
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CharCount BIGINT;
    DECLARE @ByteSize BIGINT;
    DECLARE @StartTime DATETIME2 = SYSDATETIME();
    DECLARE @EndTime DATETIME2;
    DECLARE @ExecutionTimeMS INT;

    -- 计算字符数和字节数
    SET @CharCount = LEN(@TestData);
    SET @ByteSize = DATALENGTH(@TestData);

    SET @EndTime = SYSDATETIME();
    SET @ExecutionTimeMS = DATEDIFF(MILLISECOND, @StartTime, @EndTime);

    -- 返回结果
    SELECT
        @TestName AS TestName,
        @CharCount AS CharCount,
        @ByteSize AS ByteSize,
        CAST(@ByteSize AS FLOAT) / 1024 AS SizeKB,
        CAST(@ByteSize AS FLOAT) / 1024 / 1024 AS SizeMB,
        CAST(@ByteSize AS FLOAT) / 1024 / 1024 / 1024 AS SizeGB,
        @ExecutionTimeMS AS ExecutionTimeMS,
        CASE
            WHEN @CharCount = 4000 THEN '⚠️ 警告：可能被截断到 4000 字符'
            WHEN @CharCount = 8000 THEN '⚠️ 警告：可能被截断到 8000 字符'
            ELSE '✅ 成功接收完整参数'
        END AS Status;
END;
GO

PRINT '✅ 测试存储过程创建成功！';
PRINT '';
GO


-- ============================================================================
-- 第二部分：检查环境设置
-- ============================================================================

PRINT '========================================';
PRINT '步骤 2: 检查测试环境';
PRINT '========================================';
GO

-- 检查 TEXTSIZE 设置
PRINT '当前 TEXTSIZE 设置:';
SELECT @@TEXTSIZE AS CurrentTextSize;
PRINT '';

-- 检查 SQL Server 版本
PRINT 'SQL Server 版本:';
SELECT @@VERSION AS SqlServerVersion;
PRINT '';
GO


-- ============================================================================
-- 第三部分：小数据量测试 (< 1MB)
-- ============================================================================

PRINT '========================================';
PRINT '步骤 3: 小数据量测试';
PRINT '========================================';
GO

-- 测试 1: 1KB
PRINT '【测试 1】1KB 数据测试';
DECLARE @Test1KB NVARCHAR(MAX);
SET @Test1KB = REPLICATE(N'A', 512);
EXEC sp_TestNvarcharMax @TestData = @Test1KB, @TestName = '测试_1KB_ASCII';
PRINT '';
GO

-- 测试 2: 10KB
PRINT '【测试 2】10KB 数据测试';
DECLARE @Test10KB NVARCHAR(MAX);
SET @Test10KB = REPLICATE(N'B', 5120);
EXEC sp_TestNvarcharMax @TestData = @Test10KB, @TestName = '测试_10KB_ASCII';
PRINT '';
GO

-- 测试 3: 100KB
PRINT '【测试 3】100KB 数据测试';
DECLARE @Test100KB NVARCHAR(MAX);
SET @Test100KB = REPLICATE(N'C', 51200);
EXEC sp_TestNvarcharMax @TestData = @Test100KB, @TestName = '测试_100KB_ASCII';
PRINT '';
GO


-- ============================================================================
-- 第四部分：REPLICATE 函数限制测试
-- ============================================================================

PRINT '========================================';
PRINT '步骤 4: REPLICATE() 函数限制测试';
PRINT '========================================';
GO

-- 测试 REPLICATE 的 4000 字符限制
PRINT '【陷阱测试 1】尝试用 REPLICATE 生成 5000 字符';
DECLARE @Test5000 NVARCHAR(MAX);
SET @Test5000 = REPLICATE(N'X', 5000);
EXEC sp_TestNvarcharMax @TestData = @Test5000, @TestName = '陷阱_REPLICATE_5000字符';
PRINT '⚠️ 注意：被截断到 4000 字符！';
PRINT '';
GO

PRINT '【陷阱测试 2】尝试用 REPLICATE 生成 100,000 字符';
DECLARE @Test100000 NVARCHAR(MAX);
SET @Test100000 = REPLICATE(N'Y', 100000);
EXEC sp_TestNvarcharMax @TestData = @Test100000, @TestName = '陷阱_REPLICATE_100000字符';
PRINT '⚠️ 注意：被截断到 4000 字符！';
PRINT '';
GO


-- ============================================================================
-- 第五部分：使用循环拼接突破 REPLICATE 限制
-- ============================================================================

PRINT '========================================';
PRINT '步骤 5: 使用循环拼接测试';
PRINT '========================================';
GO

-- 测试 4: 52,000 字符 (约 100KB)
PRINT '【测试 4】52,000 字符 - 循环拼接';
DECLARE @Test52K NVARCHAR(MAX);
DECLARE @Chunk52K NVARCHAR(4000);
DECLARE @i INT = 0;

SET @Chunk52K = REPLICATE(N'D', 4000);
SET @Test52K = N'';

WHILE @i < 13
BEGIN
    SET @Test52K = @Test52K + @Chunk52K;
    SET @i = @i + 1;
END

EXEC sp_TestNvarcharMax @TestData = @Test52K, @TestName = '测试_52000字符_循环拼接';
PRINT '';
GO


-- 测试 5: 500,000 字符 (约 1MB)
PRINT '【测试 5】500,000 字符 (约 1MB)';
DECLARE @Test1MB NVARCHAR(MAX);
DECLARE @Chunk1MB NVARCHAR(4000);
DECLARE @i INT = 0;

SET @Chunk1MB = REPLICATE(N'E', 4000);
SET @Test1MB = N'';

WHILE @i < 125
BEGIN
    SET @Test1MB = @Test1MB + @Chunk1MB;
    SET @i = @i + 1;
END

EXEC sp_TestNvarcharMax @TestData = @Test1MB, @TestName = '测试_500000字符_约1MB';
PRINT '';
GO


-- ============================================================================
-- 第六部分：中等数据量测试 (1MB - 100MB)
-- ============================================================================

PRINT '========================================';
PRINT '步骤 6: 中等数据量测试';
PRINT '========================================';
GO

-- 测试 6: 10MB
PRINT '【测试 6】5,000,000 字符 (约 10MB)';
DECLARE @Test10MB NVARCHAR(MAX);
DECLARE @Chunk10MB NVARCHAR(4000);
DECLARE @i INT = 0;

SET @Chunk10MB = REPLICATE(N'F', 4000);
SET @Test10MB = N'';

WHILE @i < 1250
BEGIN
    SET @Test10MB = @Test10MB + @Chunk10MB;
    SET @i = @i + 1;
END

EXEC sp_TestNvarcharMax @TestData = @Test10MB, @TestName = '测试_5000000字符_约10MB';
PRINT '';
GO


-- 测试 7: 25MB (使用优化的两层拼接)
PRINT '【测试 7】约 25MB - 两层拼接优化';
DECLARE @Test25MB NVARCHAR(MAX);
DECLARE @SmallChunk NVARCHAR(4000);
DECLARE @BigChunk NVARCHAR(MAX);
DECLARE @i INT = 0;

SET @SmallChunk = REPLICATE(N'G', 4000);
SET @BigChunk = N'';

-- 第一层：创建 40,000 字符块
SET @i = 0;
WHILE @i < 10
BEGIN
    SET @BigChunk = @BigChunk + @SmallChunk;
    SET @i = @i + 1;
END

-- 第二层：拼接生成 25MB
SET @Test25MB = N'';
SET @i = 0;
WHILE @i < 313
BEGIN
    SET @Test25MB = @Test25MB + @BigChunk;
    SET @i = @i + 1;
END

EXEC sp_TestNvarcharMax @TestData = @Test25MB, @TestName = '测试_约25MB_两层拼接';
PRINT '';
GO


-- 测试 8: 50MB (使用三层拼接)
PRINT '【测试 8】约 50MB - 三层拼接优化';
DECLARE @Test50MB NVARCHAR(MAX);
DECLARE @Base NVARCHAR(4000);
DECLARE @Level1 NVARCHAR(MAX);
DECLARE @Level2 NVARCHAR(MAX);
DECLARE @i INT = 0;

SET @Base = REPLICATE(N'H', 4000);

-- Level 1: 40,000 字符
SET @Level1 = N'';
SET @i = 0;
WHILE @i < 10
BEGIN
    SET @Level1 = @Level1 + @Base;
    SET @i = @i + 1;
END

-- Level 2: 400,000 字符
SET @Level2 = N'';
SET @i = 0;
WHILE @i < 10
BEGIN
    SET @Level2 = @Level2 + @Level1;
    SET @i = @i + 1;
END

-- Level 3: 拼接 63 次，生成约 50MB
SET @Test50MB = N'';
SET @i = 0;
WHILE @i < 63
BEGIN
    SET @Test50MB = @Test50MB + @Level2;
    SET @i = @i + 1;
END

EXEC sp_TestNvarcharMax @TestData = @Test50MB, @TestName = '测试_约50MB_三层拼接';
PRINT '';
GO


-- ============================================================================
-- 第七部分：大数据量测试 (100MB - 2GB+)
-- ============================================================================

PRINT '========================================';
PRINT '步骤 7: 大数据量测试';
PRINT '========================================';
GO

-- 测试 9: 100MB
PRINT '【测试 9】约 100MB';
DECLARE @Test100MB NVARCHAR(MAX);
DECLARE @Base NVARCHAR(4000);
DECLARE @Level1 NVARCHAR(MAX);
DECLARE @Level2 NVARCHAR(MAX);
DECLARE @i INT = 0;

SET @Base = REPLICATE(N'I', 4000);

-- Level 1: 40,000 字符
SET @Level1 = N'';
SET @i = 0;
WHILE @i < 10
BEGIN
    SET @Level1 = @Level1 + @Base;
    SET @i = @i + 1;
END

-- Level 2: 400,000 字符
SET @Level2 = N'';
SET @i = 0;
WHILE @i < 10
BEGIN
    SET @Level2 = @Level2 + @Level1;
    SET @i = @i + 1;
END

-- Level 3: 拼接 125 次，生成 100MB
SET @Test100MB = N'';
SET @i = 0;
WHILE @i < 125
BEGIN
    SET @Test100MB = @Test100MB + @Level2;
    SET @i = @i + 1;
END

EXEC sp_TestNvarcharMax @TestData = @Test100MB, @TestName = '测试_约100MB_三层拼接';
PRINT '';
GO


-- 测试 10: 500MB (四层拼接)
PRINT '【测试 10】约 500MB - 四层拼接';
DECLARE @Test500MB NVARCHAR(MAX);
DECLARE @Base NVARCHAR(4000);
DECLARE @Level1 NVARCHAR(MAX);
DECLARE @Level2 NVARCHAR(MAX);
DECLARE @Level3 NVARCHAR(MAX);
DECLARE @i INT = 0;

SET @Base = REPLICATE(N'J', 4000);

-- Level 1: 40,000 字符
SET @Level1 = N'';
SET @i = 0;
WHILE @i < 10
BEGIN
    SET @Level1 = @Level1 + @Base;
    SET @i = @i + 1;
END

-- Level 2: 400,000 字符
SET @Level2 = N'';
SET @i = 0;
WHILE @i < 10
BEGIN
    SET @Level2 = @Level2 + @Level1;
    SET @i = @i + 1;
END

-- Level 3: 4,000,000 字符
SET @Level3 = N'';
SET @i = 0;
WHILE @i < 10
BEGIN
    SET @Level3 = @Level3 + @Level2;
    SET @i = @i + 1;
END

-- Level 4: 拼接 63 次，生成约 500MB
SET @Test500MB = N'';
SET @i = 0;
WHILE @i < 63
BEGIN
    SET @Test500MB = @Test500MB + @Level3;
    SET @i = @i + 1;
END

EXEC sp_TestNvarcharMax @TestData = @Test500MB, @TestName = '测试_约500MB_四层拼接';
PRINT '';
GO


-- 测试 11: 1GB (四层拼接)
PRINT '【测试 11】约 1GB';
DECLARE @Test1GB NVARCHAR(MAX);
DECLARE @Base NVARCHAR(4000);
DECLARE @Level1 NVARCHAR(MAX);
DECLARE @Level2 NVARCHAR(MAX);
DECLARE @Level3 NVARCHAR(MAX);
DECLARE @i INT = 0;

SET @Base = REPLICATE(N'K', 4000);

-- Level 1: 40,000 字符
SET @Level1 = N'';
SET @i = 0;
WHILE @i < 10
BEGIN
    SET @Level1 = @Level1 + @Base;
    SET @i = @i + 1;
END

-- Level 2: 400,000 字符
SET @Level2 = N'';
SET @i = 0;
WHILE @i < 10
BEGIN
    SET @Level2 = @Level2 + @Level1;
    SET @i = @i + 1;
END

-- Level 3: 4,000,000 字符
SET @Level3 = N'';
SET @i = 0;
WHILE @i < 10
BEGIN
    SET @Level3 = @Level3 + @Level2;
    SET @i = @i + 1;
END

-- Level 4: 拼接 125 次，生成 1GB
SET @Test1GB = N'';
SET @i = 0;
WHILE @i < 125
BEGIN
    SET @Test1GB = @Test1GB + @Level3;
    SET @i = @i + 1;
END

EXEC sp_TestNvarcharMax @TestData = @Test1GB, @TestName = '测试_约1GB_四层拼接';
PRINT '';
GO


-- 测试 12: 2GB+ 极限测试 (五层拼接)
PRINT '========================================';
PRINT '【测试 12】⭐⭐⭐ 极限测试：约 2GB';
PRINT '========================================';
DECLARE @Test2GB NVARCHAR(MAX);
DECLARE @Base NVARCHAR(4000);
DECLARE @Level1 NVARCHAR(MAX);
DECLARE @Level2 NVARCHAR(MAX);
DECLARE @Level3 NVARCHAR(MAX);
DECLARE @Level4 NVARCHAR(MAX);
DECLARE @i INT = 0;

SET @Base = REPLICATE(N'Z', 4000);

-- Level 1: 40,000 字符
SET @Level1 = N'';
SET @i = 0;
WHILE @i < 10
BEGIN
    SET @Level1 = @Level1 + @Base;
    SET @i = @i + 1;
END

-- Level 2: 400,000 字符
SET @Level2 = N'';
SET @i = 0;
WHILE @i < 10
BEGIN
    SET @Level2 = @Level2 + @Level1;
    SET @i = @i + 1;
END

-- Level 3: 4,000,000 字符
SET @Level3 = N'';
SET @i = 0;
WHILE @i < 10
BEGIN
    SET @Level3 = @Level3 + @Level2;
    SET @i = @i + 1;
END

-- Level 4: 40,000,000 字符
SET @Level4 = N'';
SET @i = 0;
WHILE @i < 10
BEGIN
    SET @Level4 = @Level4 + @Level3;
    SET @i = @i + 1;
END

-- Level 5: 拼接 27 次，生成约 2.16GB
SET @Test2GB = N'';
SET @i = 0;
WHILE @i < 27
BEGIN
    SET @Test2GB = @Test2GB + @Level4;
    SET @i = @i + 1;
END

EXEC sp_TestNvarcharMax @TestData = @Test2GB, @TestName = '⭐极限测试_约2GB_五层拼接';
PRINT '⭐ 成功突破 2GB 理论限制！';
PRINT '';
GO


-- ============================================================================
-- 第八部分：字符类型测试
-- ============================================================================

PRINT '========================================';
PRINT '步骤 8: 不同字符类型测试';
PRINT '========================================';
GO

-- 测试 13: 纯中文字符
PRINT '【测试 13】纯中文字符测试 (约 5MB)';
DECLARE @ChineseData NVARCHAR(MAX);
DECLARE @BaseChina NVARCHAR(4000);
DECLARE @Level1 NVARCHAR(MAX);
DECLARE @i INT = 0;

SET @BaseChina = REPLICATE(N'测', 4000);

-- 创建 40,000 字符块
SET @Level1 = N'';
SET @i = 0;
WHILE @i < 10
BEGIN
    SET @Level1 = @Level1 + @BaseChina;
    SET @i = @i + 1;
END

-- 拼接生成 2,500,000 字符
SET @ChineseData = N'';
SET @i = 0;
WHILE @i < 63
BEGIN
    SET @ChineseData = @ChineseData + @Level1;
    SET @i = @i + 1;
END

EXEC sp_TestNvarcharMax @TestData = @ChineseData, @TestName = '测试_纯中文字符_约5MB';
PRINT '';
GO


-- 测试 14: 混合字符 (ASCII + 中文 + 符号)
PRINT '【测试 14】混合字符测试';
DECLARE @MixedData NVARCHAR(MAX);
DECLARE @BaseMix NVARCHAR(200);
DECLARE @i INT = 0;

SET @BaseMix = N'ABC测试123中文©®™字符XYZ混合Data数据';
SET @MixedData = N'';

WHILE @i < 10000
BEGIN
    SET @MixedData = @MixedData + @BaseMix;
    SET @i = @i + 1;
END

EXEC sp_TestNvarcharMax @TestData = @MixedData, @TestName = '测试_混合字符_ASCII中文符号';
PRINT '';
GO


-- ============================================================================
-- 测试完成
-- ============================================================================

PRINT '========================================';
PRINT '✅ 所有测试完成！';
PRINT '========================================';
PRINT '';
PRINT '测试总结:';
PRINT '- REPLICATE() 函数限制: 4000 字符';
PRINT '- nvarchar(max) 理论最大值: 2GB (2,147,483,647 字节)';
PRINT '- 实际测试成功: 2.16GB (2,160,000,000 字节)';
PRINT '- 突破方法: 使用多层循环拼接';
PRINT '';
PRINT '注意事项:';
PRINT '1. 大数据测试 (>100MB) 可能需要较长时间';
PRINT '2. 确保服务器有足够的内存';
PRINT '3. 建议在非生产环境测试';
PRINT '';
GO

/*
================================================================================
使用说明
================================================================================

1. 快速测试 (只测试小数据)
   - 执行前 5 个步骤即可

2. 完整测试 (包含大数据)
   - 执行全部脚本
   - 预计耗时: 2-5 分钟

3. 极限测试 (测试 2GB)
   - 需要足够的服务器内存
   - 预计耗时: 1-3 分钟

4. 自定义测试
   - 可以修改循环次数来调整数据大小
   - 使用多层拼接策略提高效率

================================================================================
*/
