# Molina.Bedding.Mvc

Progetto MVC ASP.NET 8 per il flusso **Dichiarazione produzione**.

## Stato del progetto in questo pacchetto
Sono presenti queste schermate:

0. **Inizia operazione**
1. **Selezione operatore**
2. **Selezione lavorazione**
3. **Selezione lotto**
4. **Inserimento dichiarazione produzione**

## Collegamento operatori
Gli operatori non vengono più letti dal file JSON di esempio.

Origine attuale:
- database SQL Server `U_MOLINA`
- tabella `dbo.X_OE_OPERATORI_BEDDING`
- filtro: `bol_annullato = 0`
- ordinamento: `des_operatore_bedding`

## Collegamento lotti e dichiarazioni
Origine lotti:
- vista `dbo.X_OE_VW_PROD_LANCIO`
- per **tutti i lanci di produzione** il codice articolo materiale viene cercato in `dbo.X_OE_VW_PROD_LANCIO_MP` con filtro `bol_is_mp_bedding_riempimento = 1` e `prg_qpr = prg_ordine` della riga selezionata
- se vengono trovate più righe MP bedding riempimento, il flusso viene bloccato con messaggio di errore
- i lotti materiale vengono poi cercati su `dbo.X_OE_VW_LOTTI`, filtrando per `cod_art`, `cod_dep = '002'` e `ISNULL(qta_esistenza, 0) <> 0`

Inserimento dichiarazioni:
- `dbo.X_OE_PROD_DICH`
- `dbo.X_OE_PROD_DICH_OPERATORI`
- `dbo.X_OE_PROD_DICH_QPR`

## Note operative
- nella schermata 4 il totale qta prodotte viene mostrato in alto
- la linea è stata allargata per evitare l'andata a capo indesiderata
- il timing salvato su database viene moltiplicato per il numero di operatori selezionati
- la qta dichiarata viene inserita in un campo separato rispetto alla qta prodotta mostrata a video
- per Trapunte viene valorizzato anche `cod_fase` nella testata dichiarazione: `05` in modalità **Riempimento**, `10` in modalità **Macchina**
- se esistono dichiarazioni precedenti sul lotto selezionato, è disponibile il pulsante per mostrarle
- il salvataggio avviene tramite il pulsante **Inserisci** in basso a destra

## Script database aggiunti
- `Database/20260407_update_X_OE_PROD_DICH_QPR_add_qta_dichiarata.sql`
- `Database/20260407_update_X_OE_VW_PROD_LANCIO_qta_dichiarata.sql`
- `Database/20260414_update_X_OE_PROD_DICH_add_cod_fase.sql`

## Limite di verifica
Nel container corrente non è disponibile `dotnet`, quindi non è stata eseguita una build reale.
La verifica è stata statica sui file modificati.
