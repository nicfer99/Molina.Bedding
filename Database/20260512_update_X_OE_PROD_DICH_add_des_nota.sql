USE [U_MOLINA]
GO

IF OBJECT_ID('dbo.X_OE_PROD_DICH_TIPI_NOTE', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[X_OE_PROD_DICH_TIPI_NOTE]
    (
        [prg_dichiarazione_tipo_nota] [int] IDENTITY(1,1) NOT NULL,
        [des_dichiarazione_tipo_nota] [varchar](100) NOT NULL,
        [bol_testo_annotazioni] [bit] NOT NULL
            CONSTRAINT [DF_X_OE_PROD_DICH_TIPI_NOTE_bol_testo_annotazioni] DEFAULT ((0)),
        [bol_dichiarazione_generica] [bit] NOT NULL
            CONSTRAINT [DF_X_OE_PROD_DICH_TIPI_NOTE_bol_dichiarazione_generica] DEFAULT ((0)),
        CONSTRAINT [PK_X_OE_PROD_DICH_TIPI_NOTE] PRIMARY KEY CLUSTERED
        (
            [prg_dichiarazione_tipo_nota] ASC
        )
    );
END
GO

IF COL_LENGTH('dbo.X_OE_PROD_DICH_TIPI_NOTE', 'bol_dichiarazione_generica') IS NULL
BEGIN
    ALTER TABLE [dbo].[X_OE_PROD_DICH_TIPI_NOTE]
        ADD [bol_dichiarazione_generica] [bit] NOT NULL
            CONSTRAINT [DF_X_OE_PROD_DICH_TIPI_NOTE_bol_dichiarazione_generica] DEFAULT ((0));
END
GO

IF COL_LENGTH('dbo.X_OE_PROD_DICH', 'prg_dichiarazione_tipo_nota') IS NULL
BEGIN
    ALTER TABLE [dbo].[X_OE_PROD_DICH]
        ADD [prg_dichiarazione_tipo_nota] [int] NULL;
END
GO

IF COL_LENGTH('dbo.X_OE_PROD_DICH', 'des_nota') IS NULL
BEGIN
    ALTER TABLE [dbo].[X_OE_PROD_DICH]
        ADD [des_nota] [varchar](255) NULL;
END
GO

IF COL_LENGTH('dbo.X_OE_PROD_DICH', 'num_minuti_nota') IS NULL
BEGIN
    ALTER TABLE [dbo].[X_OE_PROD_DICH]
        ADD [num_minuti_nota] [int] NULL;
END
GO

IF COL_LENGTH('dbo.X_OE_PROD_DICH', 'bol_dichiarazione_generica') IS NULL
BEGIN
    ALTER TABLE [dbo].[X_OE_PROD_DICH]
        ADD [bol_dichiarazione_generica] [bit] NOT NULL
            CONSTRAINT [DF_X_OE_PROD_DICH_bol_dichiarazione_generica] DEFAULT ((0));
END
GO
