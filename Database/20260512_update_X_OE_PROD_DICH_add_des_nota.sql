USE [U_MOLINA]
GO

IF OBJECT_ID('dbo.X_OE_PROD_DICH_TIPI_NOTE', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[X_OE_PROD_DICH_TIPI_NOTE]
    (
        [prg_dichiarazione_tipo_nota] [int] IDENTITY(1,1) NOT NULL,
        [des_dichiarazione_tipo_nota] [varchar](25) NOT NULL,
        [bol_testo_annotazioni] [bit] NOT NULL
            CONSTRAINT [DF_X_OE_PROD_DICH_TIPI_NOTE_bol_testo_annotazioni] DEFAULT ((0)),
        CONSTRAINT [X_OE_PROD_DICH_TIPI_NOTE_PrimaryKey] PRIMARY KEY CLUSTERED
        (
            [prg_dichiarazione_tipo_nota] ASC
        )
    );
END
GO

IF COL_LENGTH('dbo.X_OE_PROD_DICH_TIPI_NOTE', 'bol_testo_annotazioni') IS NULL
BEGIN
    ALTER TABLE [dbo].[X_OE_PROD_DICH_TIPI_NOTE]
        ADD [bol_testo_annotazioni] [bit] NOT NULL
            CONSTRAINT [DF_X_OE_PROD_DICH_TIPI_NOTE_bol_testo_annotazioni] DEFAULT ((0));
END
GO

IF OBJECT_ID('dbo.X_OR_PROD_DICH_NOTE', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[X_OR_PROD_DICH_NOTE]
    (
        [prg_dichiarazione] [int] NOT NULL,
        [prg_dichiarazione_tipo_nota] [int] NOT NULL,
        [des_nota] [varchar](255) NULL,
        [num_minuta_nota] [smallint] NOT NULL,
        CONSTRAINT [X_OR_PROD_DICH_NOTE_PrimaryKey] PRIMARY KEY CLUSTERED
        (
            [prg_dichiarazione] ASC,
            [prg_dichiarazione_tipo_nota] ASC
        )
    );
END
GO

IF OBJECT_ID('dbo.X_OR_PROD_DICH_NOTE', 'U') IS NOT NULL
    AND COL_LENGTH('dbo.X_OR_PROD_DICH_NOTE', 'num_minuta_nota') IS NULL
BEGIN
    ALTER TABLE [dbo].[X_OR_PROD_DICH_NOTE]
        ADD [num_minuta_nota] [smallint] NOT NULL
            CONSTRAINT [DF_X_OR_PROD_DICH_NOTE_num_minuta_nota] DEFAULT ((0));
END
GO

IF COL_LENGTH('dbo.X_OE_PROD_DICH', 'prg_dichiarazione_tipo_nota') IS NOT NULL
BEGIN
    DECLARE @descriptionExpression nvarchar(max) =
        CASE
            WHEN COL_LENGTH('dbo.X_OE_PROD_DICH', 'des_nota') IS NULL THEN N'NULL'
            ELSE N'd.[des_nota]'
        END;
    DECLARE @minutesExpression nvarchar(max) =
        CASE
            WHEN COL_LENGTH('dbo.X_OE_PROD_DICH', 'num_minuti_nota') IS NULL THEN N'0'
            ELSE N'ISNULL(d.[num_minuti_nota], 0)'
        END;
    DECLARE @migrationSql nvarchar(max) = N'
        INSERT INTO [dbo].[X_OR_PROD_DICH_NOTE]
        (
            [prg_dichiarazione],
            [prg_dichiarazione_tipo_nota],
            [des_nota],
            [num_minuta_nota]
        )
        SELECT
            d.[prg_dichiarazione],
            d.[prg_dichiarazione_tipo_nota],
            ' + @descriptionExpression + N',
            ' + @minutesExpression + N'
        FROM [dbo].[X_OE_PROD_DICH] d
        WHERE d.[prg_dichiarazione_tipo_nota] IS NOT NULL
            AND NOT EXISTS
            (
                SELECT 1
                FROM [dbo].[X_OR_PROD_DICH_NOTE] existing
                WHERE existing.[prg_dichiarazione] = d.[prg_dichiarazione]
                    AND existing.[prg_dichiarazione_tipo_nota] = d.[prg_dichiarazione_tipo_nota]
            );';

    EXEC sp_executesql @migrationSql;
END
GO
