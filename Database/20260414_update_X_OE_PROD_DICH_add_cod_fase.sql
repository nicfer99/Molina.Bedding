USE [U_MOLINA]
GO

IF COL_LENGTH('dbo.X_OE_PROD_DICH', 'cod_fase') IS NULL
BEGIN
    ALTER TABLE [dbo].[X_OE_PROD_DICH]
        ADD [cod_fase] [varchar](2) NULL;
END
GO
