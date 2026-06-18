# Molina.Bedding.Mvc

Progetto ASP.NET Core 8 per il flusso **Dichiarazione produzione**.

Il flusso principale è ora Blazor Interactive Server. La parte MVC esistente resta disponibile come legacy/fallback.

## Route applicative

- Blazor principale: `/` e `/blazor`
- MVC legacy/fallback: `/mvc/ProductionDeclaration/Start`
- MVC esplicito esistente: `/ProductionDeclaration/Start`

Route Blazor operative:

- `/blazor/operators`
- `/blazor/work-menu`
- `/blazor/launches/{actionId}`
- `/blazor/screen4/{actionId}`

## Schermate

Sono presenti queste schermate sia nel flusso MVC legacy sia nel flusso Blazor principale:

0. **Inizia operazione**
1. **Selezione operatore**
2. **Selezione lavorazione**
3. **Selezione lotto**
4. **Inserimento dichiarazione produzione**

## Collegamento operatori

Gli operatori vengono letti da:

- database SQL Server `U_MOLINA`
- tabella `dbo.X_OE_OPERATORI_BEDDING`
- filtro `bol_annullato = 0`
- ordinamento `des_operatore_bedding`

## Collegamento lotti e dichiarazioni

Origine lotti:

- vista `dbo.X_OE_VW_PROD_LANCIO`
- materiali da `dbo.X_OE_VW_PROD_LANCIO_MP`
- lotti materiale da `dbo.X_OE_VW_LOTTI`
- se vengono trovate più righe MP bedding riempimento, il flusso resta bloccato con messaggio di errore

Inserimento dichiarazioni:

- `dbo.X_OE_PROD_DICH`
- `dbo.X_OE_PROD_DICH_OPERATORI`
- `dbo.X_OE_PROD_DICH_QPR`

## Note operative

- il timing salvato su database viene moltiplicato per il numero di operatori selezionati
- la `qta_dichiarata` resta separata dalla `qta_prodotta`
- per Trapunte viene valorizzato `cod_fase`: `05` in Riempimento, `10` in Macchina
- se esistono dichiarazioni precedenti sul lotto selezionato, è disponibile lo storico
- il salvataggio avviene tramite il pulsante **Inserisci**

## Migrazione Blazor

- Blazor e MVC condividono `DataAccess`, `Models`, `Services`, query SQL e `wwwroot/css/site.css`.
- Lo stato del flusso Blazor viene mantenuto da `BlazorProductionDeclarationState` e salvato in `localStorage`.
- `ProductionDeclarationFlowService` replica l'orchestrazione del controller MVC per operatori, lavorazioni, lotti, barcode, timing, note, `cod_fase`, storico e salvataggio.
- JavaScript dedicato a Blazor limitato a storage locale, focus/scanner barcode, blocco scroll modali e dialog/toast custom: `wwwroot/js/blazor-production-declaration.js`.
- Le differenze visuali residue vanno corrette in Blazor, mantenendo MVC come riferimento legacy.

## Parità MVC/Blazor da verificare

- `/` deve aprire Blazor, non MVC.
- `/mvc/ProductionDeclaration/Start` e `/ProductionDeclaration/Start` devono restare disponibili per confronto legacy.
- La schermata iniziale Blazor deve essere pixel-equivalente a MVC su desktop e tablet.
- Operatori, lavorazioni, lotti, inserimento diretto, Schermata 4, PIN data, timing, note/blocchi, storico e reset devono seguire lo stesso comportamento MVC.
- Barcode/scanner deve mantenere il focus e accettare input anche dopo tocchi fuori dal campo, come nel flusso MVC.

## Script database aggiunti

- `Database/20260407_update_X_OE_PROD_DICH_QPR_add_qta_dichiarata.sql`
- `Database/20260407_update_X_OE_VW_PROD_LANCIO_qta_dichiarata.sql`
- `Database/20260414_update_X_OE_PROD_DICH_add_cod_fase.sql`
- `Database/20260512_update_X_OE_PROD_DICH_add_des_nota.sql`

## Verifica

Comandi consigliati:

```powershell
dotnet restore
dotnet build
dotnet test
```

Se il database non è raggiungibile, non sostituire la logica SQL: verificare build, route e UI, poi completare il test funzionale in ambiente con SQL Server `U_MOLINA`.
