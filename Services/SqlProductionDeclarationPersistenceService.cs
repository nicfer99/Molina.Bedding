using Microsoft.Data.SqlClient;
using Molina.Bedding.Mvc.DataAccess;
using Molina.Bedding.Mvc.Models;

namespace Molina.Bedding.Mvc.Services;

public class SqlProductionDeclarationPersistenceService : IProductionDeclarationPersistenceService
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public SqlProductionDeclarationPersistenceService(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public IReadOnlyList<DeclarationHistoryItemViewModel> GetPreviousDeclarationsByOrderIds(string lineCode, string? phaseCode, IReadOnlyList<int> orderIds)
    {
        var normalizedIds = orderIds
            .Distinct()
            .OrderBy(static value => value)
            .ToList();

        if (normalizedIds.Count == 0)
        {
            return [];
        }

        var parameterNames = new List<string>();
        for (var index = 0; index < normalizedIds.Count; index++)
        {
            parameterNames.Add($"@orderId{index}");
        }

        var items = new List<DeclarationHistoryItemViewModel>();

        using var connection = _dbConnectionFactory.CreateConnection();
        connection.Open();

        var hasDeclaredQuantityColumn = HasQtaDichiarataColumn(connection, null);
        var hasPhaseCodeColumn = HasPhaseCodeColumn(connection, null);
        var normalizedLineCode = (lineCode ?? string.Empty).Trim();
        var normalizedPhaseCode = (phaseCode ?? string.Empty).Trim();
        var applyPhaseFilter = hasPhaseCodeColumn && !string.IsNullOrWhiteSpace(normalizedPhaseCode);
        var baseWhereClause = $"d.cod_linea_produzione = @lineCode AND q.prg_ordine IN ({string.Join(", ", parameterNames)})";

        if (applyPhaseFilter)
        {
            baseWhereClause += " AND ISNULL(d.cod_fase, '') = @phaseCode";
        }

        var declaredQuantityExpression = hasDeclaredQuantityColumn
            ? "CAST(CASE WHEN ISNULL(q.qta_dichiarata, 0) <> 0 THEN q.qta_dichiarata ELSE q.qta_lavorata END AS decimal(10, 2))"
            : "CAST(ISNULL(q.qta_lavorata, 0) AS decimal(10, 2))";

        var sql = $"""
            SELECT
                q.prg_ordine,
                d.prg_dichiarazione,
                d.dat_dichiarazione_time,
                {declaredQuantityExpression} AS qta_dichiarata,
                ISNULL(q.num_minuti_lavorati, 0) AS num_minuti_lavorati,
                ISNULL(notes.des_nota, '') AS des_anomalia,
                ISNULL(notes.num_minuta_nota, 0) AS num_minuti_anomalia,
                ISNULL(ops.des_operatori, '') AS des_operatori
            FROM [dbo].[X_OE_PROD_DICH_QPR] q
            INNER JOIN [dbo].[X_OE_PROD_DICH] d
                ON d.prg_dichiarazione = q.prg_dichiarazione
            OUTER APPLY (
                SELECT
                    STUFF((
                        SELECT ', ' + o.des_operatore_bedding
                        FROM [dbo].[X_OE_PROD_DICH_OPERATORI] dopeInner
                        INNER JOIN [dbo].[X_OE_OPERATORI_BEDDING] o
                            ON o.prg_operatore_bedding = dopeInner.prg_operatore_bedding
                        WHERE dopeInner.prg_dichiarazione = d.prg_dichiarazione
                        ORDER BY dopeInner.prg_operatore_bedding
                        FOR XML PATH(''), TYPE
                    ).value('.', 'nvarchar(max)'), 1, 2, '') AS des_operatori
            ) ops
            OUTER APPLY (
                SELECT
                    STUFF((
                        SELECT '; ' + COALESCE(NULLIF(noteInner.des_nota, ''), noteTypeInner.des_dichiarazione_tipo_nota, '')
                        FROM [dbo].[X_OR_PROD_DICH_NOTE] noteInner
                        LEFT JOIN [dbo].[X_OE_PROD_DICH_TIPI_NOTE] noteTypeInner
                            ON noteTypeInner.prg_dichiarazione_tipo_nota = noteInner.prg_dichiarazione_tipo_nota
                        WHERE noteInner.prg_dichiarazione = d.prg_dichiarazione
                            AND COALESCE(NULLIF(noteInner.des_nota, ''), noteTypeInner.des_dichiarazione_tipo_nota, '') <> ''
                        ORDER BY noteInner.prg_dichiarazione_tipo_nota
                        FOR XML PATH(''), TYPE
                    ).value('.', 'nvarchar(max)'), 1, 2, '') AS des_nota,
                    (
                        SELECT SUM(ISNULL(noteMinutesInner.num_minuta_nota, 0))
                        FROM [dbo].[X_OR_PROD_DICH_NOTE] noteMinutesInner
                        WHERE noteMinutesInner.prg_dichiarazione = d.prg_dichiarazione
                    ) AS num_minuta_nota
            ) notes
            WHERE {baseWhereClause}
            ORDER BY q.prg_ordine, d.dat_dichiarazione_time DESC, d.prg_dichiarazione DESC
            """;

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new SqlParameter("@lineCode", normalizedLineCode));

        if (applyPhaseFilter)
        {
            command.Parameters.Add(new SqlParameter("@phaseCode", normalizedPhaseCode));
        }

        for (var index = 0; index < normalizedIds.Count; index++)
        {
            command.Parameters.Add(new SqlParameter(parameterNames[index], normalizedIds[index]));
        }

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new DeclarationHistoryItemViewModel
            {
                OrderId = Convert.ToInt32(reader["prg_ordine"]),
                DeclarationId = Convert.ToInt32(reader["prg_dichiarazione"]),
                DeclarationDateTime = reader["dat_dichiarazione_time"] == DBNull.Value
                    ? DateTime.MinValue
                    : Convert.ToDateTime(reader["dat_dichiarazione_time"]),
                DeclaredQuantity = reader["qta_dichiarata"] == DBNull.Value
                    ? 0m
                    : Convert.ToDecimal(reader["qta_dichiarata"]),
                WorkedMinutes = reader["num_minuti_lavorati"] == DBNull.Value
                    ? 0
                    : Convert.ToInt32(reader["num_minuti_lavorati"]),
                AnomalyDescription = reader["des_anomalia"] == DBNull.Value
                    ? string.Empty
                    : Convert.ToString(reader["des_anomalia"]) ?? string.Empty,
                AnomalyMinutes = reader["num_minuti_anomalia"] == DBNull.Value
                    ? 0
                    : Convert.ToInt32(reader["num_minuti_anomalia"]),
                OperatorsSummary = reader["des_operatori"] == DBNull.Value
                    ? string.Empty
                    : Convert.ToString(reader["des_operatori"]) ?? string.Empty
            });
        }

        return items;
    }

    public int InsertDeclaration(ProductionDeclarationInsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.LineCode))
        {
            throw new InvalidOperationException("La linea di produzione è obbligatoria per inserire la dichiarazione.");
        }

        if (request.OperatorIds.Count == 0)
        {
            throw new InvalidOperationException("Seleziona almeno un operatore prima di inserire la dichiarazione.");
        }

        if (request.Rows.Count == 0)
        {
            throw new InvalidOperationException("Inserisci almeno una qta dichiarata prima di procedere.");
        }

        using var connection = _dbConnectionFactory.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();
        try
        {
            var now = DateTime.Now;
            var declarationDate = request.DeclarationDate.Date;
            var timestamp = declarationDate + now.TimeOfDay;
            var hasPhaseCodeColumn = HasPhaseCodeColumn(connection, transaction);
            var declarationId = InsertDeclarationHeader(
                connection,
                transaction,
                request,
                timestamp,
                hasPhaseCodeColumn);
            InsertDeclarationNote(
                connection,
                transaction,
                declarationId,
                request.NoteTypeId,
                request.AnomalyDescription,
                request.AnomalyMinutes);
            InsertDeclarationOperators(connection, transaction, declarationId, request.OperatorIds);
            var hasDeclaredQuantityColumn = HasQtaDichiarataColumn(connection, transaction);
            InsertDeclarationRows(connection, transaction, declarationId, request.Rows, request.TimingMinutes, hasDeclaredQuantityColumn);
            InsertDeclarationMaterialLotRows(connection, transaction, declarationId, request.Rows);
            transaction.Commit();
            return declarationId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public int InsertDirectDeclaration(ProductionDeclarationInsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.LineCode))
        {
            throw new InvalidOperationException("La linea di produzione e obbligatoria per inserire la dichiarazione.");
        }

        if (request.OperatorIds.Count == 0)
        {
            throw new InvalidOperationException("Seleziona almeno un operatore prima di inserire la dichiarazione.");
        }

        if (request.TimingMinutes <= 0)
        {
            throw new InvalidOperationException("Inserisci il timing prima di procedere.");
        }

        using var connection = _dbConnectionFactory.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();
        try
        {
            var now = DateTime.Now;
            var declarationDate = request.DeclarationDate.Date;
            var timestamp = declarationDate + now.TimeOfDay;
            var hasPhaseCodeColumn = HasPhaseCodeColumn(connection, transaction);
            var hasGenericDeclarationColumn = HasGenericDeclarationColumn(connection, transaction);
            var declarationId = InsertDirectDeclarationHeader(
                connection,
                transaction,
                request,
                timestamp,
                hasPhaseCodeColumn,
                hasGenericDeclarationColumn);
            InsertDeclarationNote(
                connection,
                transaction,
                declarationId,
                request.NoteTypeId,
                request.NoteDescription,
                request.NoteMinutes);
            InsertDeclarationOperators(connection, transaction, declarationId, request.OperatorIds);
            transaction.Commit();
            return declarationId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static int InsertDirectDeclarationHeader(
        SqlConnection connection,
        SqlTransaction transaction,
        ProductionDeclarationInsertRequest request,
        DateTime timestamp,
        bool hasPhaseCodeColumn,
        bool hasGenericDeclarationColumn)
    {
        var columns = new List<string>
        {
            "[cod_linea_produzione]",
            "[dat_dichiarazione]",
            "[dat_dichiarazione_time]",
            "[num_minuti_lavorati]"
        };
        var values = new List<string>
        {
            "@lineCode",
            "@declarationDate",
            "@declarationDateTime",
            "@workedMinutes"
        };

        if (hasPhaseCodeColumn)
        {
            columns.Insert(1, "[cod_fase]");
            values.Insert(1, "@phaseCode");
        }

        if (hasGenericDeclarationColumn)
        {
            columns.Add("[bol_dichiarazione_generica]");
            values.Add("@isGenericDeclaration");
        }

        var sql = $"""
            INSERT INTO [dbo].[X_OE_PROD_DICH]
            (
                {string.Join(",\n                ", columns)}
            )
            VALUES
            (
                {string.Join(",\n                ", values)}
            );
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.Parameters.Add(new SqlParameter("@lineCode", request.LineCode.Trim()));
        if (hasPhaseCodeColumn)
        {
            command.Parameters.Add(new SqlParameter("@phaseCode", string.IsNullOrWhiteSpace(request.PhaseCode) ? DBNull.Value : request.PhaseCode.Trim()));
        }
        command.Parameters.Add(new SqlParameter("@declarationDate", timestamp.Date));
        command.Parameters.Add(new SqlParameter("@declarationDateTime", timestamp));
        command.Parameters.Add(new SqlParameter("@workedMinutes", request.TimingMinutes));
        if (hasGenericDeclarationColumn)
        {
            command.Parameters.Add(new SqlParameter("@isGenericDeclaration", System.Data.SqlDbType.Bit) { Value = true });
        }

        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static int InsertDeclarationHeader(
        SqlConnection connection,
        SqlTransaction transaction,
        ProductionDeclarationInsertRequest request,
        DateTime timestamp,
        bool hasPhaseCodeColumn)
    {
        var columns = new List<string>
        {
            "[cod_linea_produzione]",
            "[dat_dichiarazione]",
            "[dat_dichiarazione_time]",
            "[num_minuti_lavorati]"
        };
        var values = new List<string>
        {
            "@lineCode",
            "@declarationDate",
            "@declarationDateTime",
            "@workedMinutes"
        };

        if (hasPhaseCodeColumn)
        {
            columns.Insert(1, "[cod_fase]");
            values.Insert(1, "@phaseCode");
        }

        var sql = $"""
            INSERT INTO [dbo].[X_OE_PROD_DICH]
            (
                {string.Join(",\n                ", columns)}
            )
            VALUES
            (
                {string.Join(",\n                ", values)}
            );
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.Parameters.Add(new SqlParameter("@lineCode", request.LineCode));
        if (hasPhaseCodeColumn)
        {
            command.Parameters.Add(new SqlParameter("@phaseCode", string.IsNullOrWhiteSpace(request.PhaseCode) ? DBNull.Value : request.PhaseCode.Trim()));
        }
        command.Parameters.Add(new SqlParameter("@declarationDate", timestamp.Date));
        command.Parameters.Add(new SqlParameter("@declarationDateTime", timestamp));
        command.Parameters.Add(new SqlParameter("@workedMinutes", request.TimingMinutes));

        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static void InsertDeclarationNote(
        SqlConnection connection,
        SqlTransaction transaction,
        int declarationId,
        int? noteTypeId,
        string? noteDescription,
        int noteMinutes)
    {
        var normalizedDescription = noteDescription?.Trim();
        var normalizedMinutes = Math.Max(0, noteMinutes);
        var hasNoteData = noteTypeId.GetValueOrDefault() > 0
            || !string.IsNullOrWhiteSpace(normalizedDescription)
            || normalizedMinutes > 0;

        if (!hasNoteData)
        {
            return;
        }

        if (!noteTypeId.HasValue || noteTypeId.Value <= 0)
        {
            throw new InvalidOperationException("Il tipo nota e obbligatorio per inserire blocchi o anomalie.");
        }

        const string sql = """
            INSERT INTO [dbo].[X_OR_PROD_DICH_NOTE]
            (
                [prg_dichiarazione],
                [prg_dichiarazione_tipo_nota],
                [des_nota],
                [num_minuta_nota]
            )
            VALUES
            (
                @declarationId,
                @noteTypeId,
                @noteDescription,
                @noteMinutes
            )
            """;

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.Parameters.Add(new SqlParameter("@declarationId", declarationId));
        command.Parameters.Add(new SqlParameter("@noteTypeId", noteTypeId.Value));
        command.Parameters.Add(new SqlParameter("@noteDescription", (object?)normalizedDescription ?? DBNull.Value));
        command.Parameters.Add(new SqlParameter("@noteMinutes", normalizedMinutes));
        command.ExecuteNonQuery();
    }

    private static void InsertDeclarationOperators(SqlConnection connection, SqlTransaction transaction, int declarationId, IReadOnlyList<int> operatorIds)
    {
        const string sql = """
            INSERT INTO [dbo].[X_OE_PROD_DICH_OPERATORI]
            (
                [prg_dichiarazione],
                [prg_operatore_bedding]
            )
            VALUES
            (
                @declarationId,
                @operatorId
            )
            """;

        foreach (var operatorId in operatorIds.Distinct().OrderBy(static value => value))
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.Add(new SqlParameter("@declarationId", declarationId));
            command.Parameters.Add(new SqlParameter("@operatorId", operatorId));
            command.ExecuteNonQuery();
        }
    }

    private static void InsertDeclarationMaterialLotRows(SqlConnection connection, SqlTransaction transaction, int declarationId, IReadOnlyList<ProductionDeclarationInsertRowRequest> rows)
    {
        var normalizedRows = rows
            .Where(static row => row.OrderId > 0
                && row.DeclaredQuantity > 0
                && !string.IsNullOrWhiteSpace(row.ArticleCode)
                && !string.IsNullOrWhiteSpace(row.SelectedMaterialLotCode))
            .OrderBy(static row => row.OrderId)
            .ToList();

        if (normalizedRows.Count == 0)
        {
            return;
        }

        const string sql = """
            INSERT INTO [dbo].[X_OE_PROD_DICH_QPR_LOTTI]
            (
                [prg_dichiarazione],
                [prg_ordine],
                [prg_riga],
                [cod_art],
                [cod_lotto],
                [qta_lotto]
            )
            VALUES
            (
                @declarationId,
                @orderId,
                @rowSequence,
                @articleCode,
                @lotCode,
                @lotQuantity
            )
            """;

        for (var index = 0; index < normalizedRows.Count; index++)
        {
            var row = normalizedRows[index];

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.Add(new SqlParameter("@declarationId", declarationId));
            command.Parameters.Add(new SqlParameter("@orderId", row.OrderId));
            command.Parameters.Add(new SqlParameter("@rowSequence", index + 1));
            command.Parameters.Add(new SqlParameter("@articleCode", row.ArticleCode.Trim()));
            command.Parameters.Add(new SqlParameter("@lotCode", row.SelectedMaterialLotCode.Trim()));
            command.Parameters.Add(new SqlParameter("@lotQuantity", row.DeclaredQuantity));
            command.ExecuteNonQuery();
        }
    }

    private static void InsertDeclarationRows(SqlConnection connection, SqlTransaction transaction, int declarationId, IReadOnlyList<ProductionDeclarationInsertRowRequest> rows, int workedMinutes, bool hasDeclaredQuantityColumn)
    {
        var sql = hasDeclaredQuantityColumn
            ? """
                INSERT INTO [dbo].[X_OE_PROD_DICH_QPR]
                (
                    [prg_dichiarazione],
                    [prg_ordine],
                    [num_minuti_lavorati],
                    [qta_dichiarata]
                )
                VALUES
                (
                    @declarationId,
                    @orderId,
                    @workedMinutes,
                    @declaredQuantity
                )
                """
            : """
                INSERT INTO [dbo].[X_OE_PROD_DICH_QPR]
                (
                    [prg_dichiarazione],
                    [prg_ordine],
                    [num_minuti_lavorati],
                    [qta_lavorata]
                )
                VALUES
                (
                    @declarationId,
                    @orderId,
                    @workedMinutes,
                    @declaredQuantity
                )
                """;

        foreach (var row in rows)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.Add(new SqlParameter("@declarationId", declarationId));
            command.Parameters.Add(new SqlParameter("@orderId", row.OrderId));
            command.Parameters.Add(new SqlParameter("@workedMinutes", workedMinutes));
            command.Parameters.Add(new SqlParameter("@declaredQuantity", row.DeclaredQuantity));
            command.ExecuteNonQuery();
        }
    }

    private static bool HasQtaDichiarataColumn(SqlConnection connection, SqlTransaction? transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT CASE WHEN COL_LENGTH('dbo.X_OE_PROD_DICH_QPR', 'qta_dichiarata') IS NULL THEN 0 ELSE 1 END";
        return Convert.ToInt32(command.ExecuteScalar()) == 1;
    }

    private static bool HasPhaseCodeColumn(SqlConnection connection, SqlTransaction? transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT CASE WHEN COL_LENGTH('dbo.X_OE_PROD_DICH', 'cod_fase') IS NULL THEN 0 ELSE 1 END";
        return Convert.ToInt32(command.ExecuteScalar()) == 1;
    }

    private static bool HasGenericDeclarationColumn(SqlConnection connection, SqlTransaction? transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT CASE WHEN COL_LENGTH('dbo.X_OE_PROD_DICH', 'bol_dichiarazione_generica') IS NULL THEN 0 ELSE 1 END";
        return Convert.ToInt32(command.ExecuteScalar()) == 1;
    }
}
