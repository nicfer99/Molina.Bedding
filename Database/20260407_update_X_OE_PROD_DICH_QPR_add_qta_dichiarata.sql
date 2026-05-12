USE [U_MOLINA]
GO

IF COL_LENGTH('dbo.X_OE_PROD_DICH_QPR', 'qta_dichiarata') IS NULL
BEGIN
    ALTER TABLE [dbo].[X_OE_PROD_DICH_QPR]
        ADD [qta_dichiarata] [float] NOT NULL
            CONSTRAINT [DF_X_OE_PROD_DICH_QPR_qta_dichiarata] DEFAULT ((0));
END
GO
