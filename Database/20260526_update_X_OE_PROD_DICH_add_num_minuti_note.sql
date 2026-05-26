USE [U_MOLINA]
GO

IF COL_LENGTH('dbo.X_OE_PROD_DICH', 'num_minuti_note') IS NULL
BEGIN
    ALTER TABLE [dbo].[X_OE_PROD_DICH]
        ADD [num_minuti_note] [smallint] NULL;
END
GO

IF COL_LENGTH('dbo.X_OE_PROD_DICH', 'num_minuti_note') IS NOT NULL
    AND COL_LENGTH('dbo.X_OE_PROD_DICH', 'num_minuti_nota') IS NOT NULL
BEGIN
    EXEC sp_executesql N'
        UPDATE d
        SET [num_minuti_note] = ISNULL(d.[num_minuti_nota], 0)
        FROM [dbo].[X_OE_PROD_DICH] d
        WHERE ISNULL(d.[num_minuti_note], 0) = 0
            AND ISNULL(d.[num_minuti_nota], 0) <> 0;';
END
GO

IF COL_LENGTH('dbo.X_OE_PROD_DICH', 'num_minuti_note') IS NOT NULL
    AND OBJECT_ID('dbo.X_OR_PROD_DICH_NOTE', 'U') IS NOT NULL
    AND COL_LENGTH('dbo.X_OR_PROD_DICH_NOTE', 'num_minuta_nota') IS NOT NULL
BEGIN
    UPDATE d
    SET [num_minuti_note] = totals.num_minuti_note
    FROM [dbo].[X_OE_PROD_DICH] d
    INNER JOIN
    (
        SELECT
            [prg_dichiarazione],
            SUM(ISNULL([num_minuta_nota], 0)) AS num_minuti_note
        FROM [dbo].[X_OR_PROD_DICH_NOTE]
        GROUP BY [prg_dichiarazione]
    ) totals
        ON totals.[prg_dichiarazione] = d.[prg_dichiarazione]
    WHERE ISNULL(d.[num_minuti_note], 0) = 0
        AND totals.num_minuti_note <> 0;
END
GO
