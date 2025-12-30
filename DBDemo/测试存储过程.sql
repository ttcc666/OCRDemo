-- =============================================
-- 测试 nvarchar(max) 参数最大长度
-- =============================================

-- 创建测试存储过程
IF OBJECT_ID('sp_TestNvarcharMax', 'P') IS NOT NULL
    DROP PROCEDURE sp_TestNvarcharMax;
GO

CREATE PROCEDURE sp_TestNvarcharMax
    @TestData NVARCHAR(MAX),
    @TestName NVARCHAR(200) = '默认测试'
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CharCount INT;
    DECLARE @ByteSize INT;
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
        @ExecutionTimeMS AS ExecutionTimeMS,
        '成功接收参数' AS Status;
END;
GO

-- 创建生成测试数据的辅助函数（内联表值函数）
IF OBJECT_ID('fn_GenerateTestString', 'IF') IS NOT NULL
    DROP FUNCTION fn_GenerateTestString;
GO

-- 由于标量函数不能返回 nvarchar(max)，我们将在测试脚本中直接生成
