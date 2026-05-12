USE [U_MOLINA]
GO

IF OBJECT_ID('[dbo].[X_OE_VW_PROD_LANCIO]', 'V') IS NOT NULL
    DROP VIEW [dbo].[X_OE_VW_PROD_LANCIO]
GO

CREATE VIEW [dbo].[X_OE_VW_PROD_LANCIO]
AS
SELECT TOP (100) PERCENT
    dbo.OR_ORDINIT.prg_ordine,
    dbo.OR_ORDINIT.ind_stato_evas,
    dbo.OR_ORDINIT.num_doc,
    dbo.OR_ORDINIT.sig_serie_doc,
    dbo.OR_ORDINIT.dat_doc,
    dbo.OR_ORDINIT.des_campo_libero3 AS cod_art,
    dbo.MG_ARTBASE.des_articolo,
    CAST(SUM(dbo.OR_ORDINIR.qta_merce) AS decimal(10, 2)) AS qta_merce,
    CAST(CASE WHEN
        (SELECT SUM(qta_merce_netto)
         FROM dbo.X_OE_MAGAZZINO_MATRICOLE_T
         WHERE prg_ordine_qpr = dbo.OR_ORDINIT.prg_ordine
           AND ind_tipo_matricola = '1') IS NULL THEN 0 ELSE
        (SELECT SUM(qta_merce_netto)
         FROM dbo.X_OE_MAGAZZINO_MATRICOLE_T
         WHERE prg_ordine_qpr = dbo.OR_ORDINIT.prg_ordine
           AND ind_tipo_matricola = '1') END AS decimal(10, 2)) AS qta_evasa,
    dbo.OR_ORDINIT.cod_dep_mov,
    dbo.X_OE_RIFPROD.prg_imp,
    dbo.X_OE_RIFPROD.prg_produzione_carico,
    dbo.X_OE_RIFPROD.prg_produzione_scarico,
    dbo.X_OE_PROD_LINEE.des_linea_produzione,
    dbo.X_OE_PROD_IMPIANTI.des_impianto,
    SUM(dbo.OR_ORDINIR.qta_campo_libero14) AS qta_campo_libero14,
    dbo.OR_ORDINIT.num_anno,
    dbo.OR_ORDINIT.des_campo_libero2,
    dbo.OR_ORDINIT.des_campo_libero6,
    dbo.X_OE_PROD_LINEE.bol_mix,
    (SELECT COUNT(prg_qpr)
     FROM dbo.X_OE_RIFPROD_CLIENTI
     WHERE prg_qpr = dbo.OR_ORDINIT.prg_ordine
       AND bol_close_produzione <> 1) AS num_dettaglio_aperto,
    dbo.X_OE_PROD_LINEE.cod_linea_produzione,
    (SELECT CASE WHEN SUM(CASE WHEN ISNULL(qta_dichiarata, 0) <> 0 THEN qta_dichiarata ELSE qta_lavorata END) IS NULL THEN 0 ELSE SUM(CASE WHEN ISNULL(qta_dichiarata, 0) <> 0 THEN qta_dichiarata ELSE qta_lavorata END) END
     FROM dbo.X_OE_PROD_DICH_QPR
     WHERE dbo.OR_ORDINIT.prg_ordine = prg_ordine) AS qta_dichiarata
FROM dbo.X_OE_PROD_LINEE
RIGHT OUTER JOIN dbo.X_OE_PROD_IMPIANTI
RIGHT OUTER JOIN dbo.MG_ARTBASE
RIGHT OUTER JOIN dbo.X_OE_RIFPROD
INNER JOIN dbo.OR_ORDINIT
    ON dbo.X_OE_RIFPROD.prg_qpr = dbo.OR_ORDINIT.prg_ordine
LEFT OUTER JOIN dbo.OR_ORDINIR
    ON dbo.OR_ORDINIT.prg_ordine = dbo.OR_ORDINIR.prg_ordine
    ON dbo.MG_ARTBASE.cod_art = dbo.OR_ORDINIT.des_campo_libero3
    ON dbo.X_OE_PROD_IMPIANTI.prg_impianto = dbo.OR_ORDINIT.val_campo_libero7
    ON dbo.X_OE_PROD_LINEE.cod_linea_produzione = dbo.OR_ORDINIT.des_campo_libero1
WHERE dbo.OR_ORDINIR.val_campo_libero11 = 1
GROUP BY
    dbo.OR_ORDINIT.prg_ordine,
    dbo.OR_ORDINIT.ind_stato_evas,
    dbo.OR_ORDINIT.num_doc,
    dbo.OR_ORDINIT.sig_serie_doc,
    dbo.OR_ORDINIT.dat_doc,
    dbo.OR_ORDINIT.des_campo_libero3,
    dbo.MG_ARTBASE.des_articolo,
    dbo.OR_ORDINIT.cod_dep_mov,
    dbo.X_OE_RIFPROD.prg_imp,
    dbo.X_OE_RIFPROD.prg_produzione_carico,
    dbo.X_OE_RIFPROD.prg_produzione_scarico,
    dbo.X_OE_PROD_LINEE.des_linea_produzione,
    dbo.X_OE_PROD_IMPIANTI.des_impianto,
    dbo.OR_ORDINIT.num_anno,
    dbo.OR_ORDINIT.des_campo_libero2,
    dbo.OR_ORDINIT.des_campo_libero6,
    dbo.X_OE_PROD_LINEE.bol_mix,
    dbo.X_OE_PROD_LINEE.cod_linea_produzione
HAVING dbo.X_OE_RIFPROD.prg_imp <> 0;
GO
