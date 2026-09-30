using System.Data;
using System.Data.Common;
using System.Globalization;
using Npgsql;

namespace Sipitex.DataMigration;

static class Migrator
{
    private const int BatchSize = 400;

    public static int Run(SourceConnection source, string destinationConnectionString, bool dryRun)
    {
        var destinationBuilder = new NpgsqlConnectionStringBuilder(destinationConnectionString)
        {
            IncludeErrorDetail = false
        };
        using var destination = new NpgsqlConnection(destinationBuilder.ConnectionString);
        destination.Open();

        Console.WriteLine("Origen: " + source.Description);
        Console.WriteLine("Destino: PostgreSQL host=" + destinationBuilder.Host + " base=" + destinationBuilder.Database);
        Console.WriteLine(dryRun ? "Modo: dry-run (no se escribe nada)" : "Modo: copia");

        var destinationTables = LoadPostgresTables(destination);
        var sourceTables = source.IsSqlite
            ? LoadSqliteTables(source.Connection)
            : LoadPostgresTables(source.Connection).ToDictionary(
                t => t.Key,
                t => (IReadOnlyList<string>)t.Value.Columns.Select(c => c.Name).ToList(),
                StringComparer.OrdinalIgnoreCase);

        sourceTables.Remove("__EFMigrationsHistory");
        destinationTables.Remove("__EFMigrationsHistory");

        var missing = sourceTables.Keys
            .Where(name => !destinationTables.ContainsKey(name))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (missing.Count > 0)
        {
            Console.Error.WriteLine(
                "El destino no tiene estas tablas del origen: " + string.Join(", ", missing));
            return 1;
        }

        var onlyDestination = destinationTables.Keys
            .Where(name => !sourceTables.ContainsKey(name))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (onlyDestination.Count > 0)
            Console.WriteLine("Solo en destino (no se copian): " + string.Join(", ", onlyDestination));

        var selected = destinationTables.Values
            .Where(table => sourceTables.ContainsKey(table.Name))
            .ToList();
        var order = OrderByForeignKeys(selected);
        var plans = order.Select(name =>
        {
            var sourceName = sourceTables.Keys.First(key => key.Equals(name, StringComparison.OrdinalIgnoreCase));
            return BuildPlan(destinationTables[name], sourceName, sourceTables[sourceName], order);
        }).ToList();

        if (dryRun)
        {
            var counts = plans.Select(plan => (
                plan.DestinationTable,
                Source: Count(source.Connection, plan.SourceTable),
                Destination: Count(destination, plan.DestinationTable))).ToList();
            PrintSummary(counts);
            Console.WriteLine("dry-run terminado. No se comparan los conteos como error porque el destino puede estar vacío.");
            return 0;
        }

        var deferred = new List<PendingUpdate>();
        foreach (var plan in plans)
        {
            var copied = CopyTable(source.Connection, destination, plan, deferred);
            Console.WriteLine($"Copiada {plan.DestinationTable}: {copied} filas leídas");
        }

        ApplyDeferred(destination, deferred);
        ResetSequences(destination, plans);

        var compared = plans.Select(plan => (
            plan.DestinationTable,
            Source: Count(source.Connection, plan.SourceTable),
            Destination: Count(destination, plan.DestinationTable))).ToList();
        PrintSummary(compared);

        var mismatches = compared.Where(row => row.Source != row.Destination).Select(row => row.DestinationTable).ToList();
        if (mismatches.Count > 0)
        {
            Console.Error.WriteLine("Los conteos no coinciden: " + string.Join(", ", mismatches));
            return 1;
        }

        Console.WriteLine("Origen y destino coinciden en todas las tablas copiadas.");
        Console.WriteLine("Roles: se copió la columna Rol de Users y las tablas de asignación, sin imprimir correos ni hashes.");
        return 0;
    }

    private static CopyPlan BuildPlan(
        TableSchema destination,
        string sourceTable,
        IReadOnlyList<string> sourceColumns,
        IReadOnlyList<string> order)
    {
        var sourceByName = sourceColumns.ToDictionary(name => name, name => name, StringComparer.OrdinalIgnoreCase);
        var columns = new List<ColumnMap>();
        foreach (var column in destination.Columns)
        {
            if (!sourceByName.TryGetValue(column.Name, out var sourceName))
            {
                if (column.NotNull)
                    throw new InvalidOperationException(
                        $"La tabla {destination.Name} exige la columna {column.Name} y el origen no la tiene.");
                continue;
            }

            columns.Add(new ColumnMap(column.Name, sourceName, column.PgType));
        }

        if (destination.PrimaryKey.Count == 0)
            throw new InvalidOperationException($"La tabla {destination.Name} no tiene llave primaria en el destino.");

        foreach (var key in destination.PrimaryKey)
        {
            if (columns.All(column => !column.DestinationName.Equals(key, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException(
                    $"La llave primaria {key} de {destination.Name} no está en el origen.");
        }

        var index = order
            .Select((name, position) => (name, position))
            .ToDictionary(item => item.name, item => item.position, StringComparer.OrdinalIgnoreCase);
        var deferred = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var foreignKey in destination.ForeignKeys)
        {
            if (!foreignKey.IsNullable)
                continue;
            if (!columns.Any(column => column.DestinationName.Equals(foreignKey.Column, StringComparison.OrdinalIgnoreCase)))
                continue;

            var self = foreignKey.ReferencedTable.Equals(destination.Name, StringComparison.OrdinalIgnoreCase);
            var referencedLater = index.TryGetValue(foreignKey.ReferencedTable, out var referencedAt)
                && referencedAt > index[destination.Name];
            if (self || referencedLater)
                deferred.Add(foreignKey.Column);
        }

        return new CopyPlan(
            sourceTable,
            destination.Name,
            columns,
            destination.PrimaryKey,
            deferred);
    }

    private static int CopyTable(DbConnection source, NpgsqlConnection destination, CopyPlan plan, List<PendingUpdate> deferred)
    {
        var selectList = string.Join(", ", plan.Columns.Select(column => Quote(column.SourceName)));
        var orderList = string.Join(", ", plan.PrimaryKey.Select(key =>
        {
            var sourceName = plan.Columns.First(column =>
                column.DestinationName.Equals(key, StringComparison.OrdinalIgnoreCase)).SourceName;
            return Quote(sourceName);
        }));
        using var command = source.CreateCommand();
        command.CommandText = $"SELECT {selectList} FROM {Quote(plan.SourceTable)} ORDER BY {orderList}";
        using var reader = command.ExecuteReader();

        var insertColumns = string.Join(", ", plan.Columns.Select(column => Quote(column.DestinationName)));
        var insertValues = string.Join(", ", plan.Columns.Select((_, index) => "@p" + index));
        var conflict = string.Join(", ", plan.PrimaryKey.Select(Quote));
        var insertSql =
            $"INSERT INTO {Quote(plan.DestinationTable)} ({insertColumns}) VALUES ({insertValues}) ON CONFLICT ({conflict}) DO NOTHING";

        var read = 0;
        var batch = new List<object[]>();
        while (reader.Read())
        {
            var values = new object[plan.Columns.Count];
            for (var i = 0; i < values.Length; i++)
                values[i] = Coerce(reader.GetValue(i), plan.Columns[i].PgType);
            batch.Add(values);
            read++;
            if (batch.Count >= BatchSize)
            {
                Flush(destination, plan, insertSql, batch, deferred);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
            Flush(destination, plan, insertSql, batch, deferred);

        return read;
    }

    private static void Flush(
        NpgsqlConnection destination,
        CopyPlan plan,
        string insertSql,
        List<object[]> batch,
        List<PendingUpdate> deferred)
    {
        using var transaction = destination.BeginTransaction();
        foreach (var row in batch)
        {
            using var insert = new NpgsqlCommand(insertSql, destination, transaction);
            for (var i = 0; i < row.Length; i++)
            {
                var deferredColumn = plan.DeferredColumns.Contains(plan.Columns[i].DestinationName);
                insert.Parameters.AddWithValue("p" + i, deferredColumn ? DBNull.Value : row[i]);
            }

            insert.ExecuteNonQuery();

            foreach (var column in plan.DeferredColumns)
            {
                var ordinal = IndexOf(plan.Columns, column);
                if (row[ordinal] is DBNull)
                    continue;

                var keyValues = plan.PrimaryKey
                    .Select(key => row[IndexOf(plan.Columns, key)])
                    .ToArray();
                deferred.Add(new PendingUpdate(plan.DestinationTable, plan.PrimaryKey.ToArray(), keyValues, column, row[ordinal]));
            }
        }

        transaction.Commit();
    }

    private static void ApplyDeferred(NpgsqlConnection destination, List<PendingUpdate> pending)
    {
        for (var offset = 0; offset < pending.Count; offset += BatchSize)
        {
            var slice = pending.Skip(offset).Take(BatchSize).ToList();
            using var transaction = destination.BeginTransaction();
            foreach (var update in slice)
            {
                var assignments = Quote(update.Column) + " = @v";
                var where = string.Join(" AND ", update.PrimaryKeyColumns.Select((column, index) => Quote(column) + " = @k" + index));
                using var command = new NpgsqlCommand(
                    $"UPDATE {Quote(update.Table)} SET {assignments} WHERE {where}",
                    destination,
                    transaction);
                command.Parameters.AddWithValue("v", update.Value);
                for (var i = 0; i < update.KeyValues.Length; i++)
                    command.Parameters.AddWithValue("k" + i, update.KeyValues[i]);
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        if (pending.Count > 0)
            Console.WriteLine($"Referencias diferidas actualizadas: {pending.Count}");
    }

    private static void ResetSequences(NpgsqlConnection destination, IReadOnlyList<CopyPlan> plans)
    {
        var updated = 0;
        using var transaction = destination.BeginTransaction();
        foreach (var plan in plans)
        {
            if (plan.PrimaryKey.Count != 1)
                continue;

            var key = plan.PrimaryKey[0];
            var column = plan.Columns.First(item => item.DestinationName.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (column.PgType is not ("int2" or "int4" or "int8"))
                continue;

            using var sequence = new NpgsqlCommand("SELECT pg_get_serial_sequence(@t, @c)", destination, transaction);
            sequence.Parameters.AddWithValue("t", "public." + Quote(plan.DestinationTable));
            sequence.Parameters.AddWithValue("c", key);
            if (sequence.ExecuteScalar() is null or DBNull)
                continue;

            var regclass = "public." + Quote(plan.DestinationTable);
            using var setval = new NpgsqlCommand(
                $"""
                SELECT setval(
                    pg_get_serial_sequence('{regclass}', '{key}'),
                    COALESCE((SELECT MAX({Quote(key)}) FROM {Quote(plan.DestinationTable)}), 1),
                    (SELECT COUNT(*) FROM {Quote(plan.DestinationTable)}) > 0)
                """,
                destination,
                transaction);
            setval.ExecuteScalar();
            updated++;
        }

        transaction.Commit();
        Console.WriteLine("Secuencias reiniciadas: " + updated);
    }

    private static List<string> OrderByForeignKeys(IReadOnlyList<TableSchema> tables)
    {
        var names = tables.Select(table => table.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var dependencies = tables.ToDictionary(
            table => table.Name,
            _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);

        foreach (var table in tables)
        {
            foreach (var foreignKey in table.ForeignKeys)
            {
                if (foreignKey.IsNullable)
                    continue;
                if (foreignKey.ReferencedTable.Equals(table.Name, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!names.Contains(foreignKey.ReferencedTable))
                    continue;

                var referenced = tables.First(item =>
                    item.Name.Equals(foreignKey.ReferencedTable, StringComparison.OrdinalIgnoreCase)).Name;
                dependencies[table.Name].Add(referenced);
            }
        }

        var ordered = new List<string>();
        var ready = new SortedSet<string>(
            dependencies.Where(item => item.Value.Count == 0).Select(item => item.Key),
            StringComparer.OrdinalIgnoreCase);

        while (ready.Count > 0)
        {
            var next = ready.Min!;
            ready.Remove(next);
            ordered.Add(next);
            foreach (var item in dependencies)
            {
                if (!item.Value.Remove(next) || item.Value.Count > 0 || ordered.Contains(item.Key))
                    continue;
                ready.Add(item.Key);
            }
        }

        if (ordered.Count != tables.Count)
        {
            var pending = tables.Select(table => table.Name).Except(ordered, StringComparer.OrdinalIgnoreCase);
            throw new InvalidOperationException(
                "Hay un ciclo de llaves foráneas obligatorias: " + string.Join(", ", pending));
        }

        return ordered;
    }

    private static Dictionary<string, TableSchema> LoadPostgresTables(DbConnection connection)
    {
        var names = new List<string>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                SELECT c.relname
                FROM pg_class c
                JOIN pg_namespace n ON n.oid = c.relnamespace
                WHERE n.nspname = 'public' AND c.relkind = 'r'
                ORDER BY c.relname
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
                names.Add(reader.GetString(0));
        }

        var tables = new Dictionary<string, TableSchema>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
            tables[name] = LoadPostgresTable(connection, name);
        return tables;
    }

    private static TableSchema LoadPostgresTable(DbConnection connection, string name)
    {
        var columns = new List<ColumnInfo>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                SELECT a.attname, t.typname, a.attnotnull
                FROM pg_attribute a
                JOIN pg_class c ON c.oid = a.attrelid
                JOIN pg_namespace n ON n.oid = c.relnamespace
                JOIN pg_type t ON t.oid = a.atttypid
                WHERE n.nspname = 'public' AND c.relname = @table AND a.attnum > 0 AND NOT a.attisdropped
                ORDER BY a.attnum
                """;
            var parameter = command.CreateParameter();
            parameter.ParameterName = "table";
            parameter.Value = name;
            command.Parameters.Add(parameter);
            using var reader = command.ExecuteReader();
            while (reader.Read())
                columns.Add(new ColumnInfo(reader.GetString(0), reader.GetString(1), reader.GetBoolean(2)));
        }

        var primaryKey = new List<string>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                SELECT a.attname
                FROM pg_index i
                JOIN pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = ANY (i.indkey)
                JOIN pg_class c ON c.oid = i.indrelid
                JOIN pg_namespace n ON n.oid = c.relnamespace
                WHERE i.indisprimary AND n.nspname = 'public' AND c.relname = @table
                ORDER BY array_position(i.indkey, a.attnum)
                """;
            var parameter = command.CreateParameter();
            parameter.ParameterName = "table";
            parameter.Value = name;
            command.Parameters.Add(parameter);
            using var reader = command.ExecuteReader();
            while (reader.Read())
                primaryKey.Add(reader.GetString(0));
        }

        var foreignKeys = new List<ForeignKeyInfo>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                SELECT att.attname, dst.relname, NOT att.attnotnull
                FROM pg_constraint con
                JOIN pg_class src ON src.oid = con.conrelid
                JOIN pg_namespace nsp ON nsp.oid = src.relnamespace
                JOIN pg_class dst ON dst.oid = con.confrelid
                JOIN LATERAL unnest(con.conkey) WITH ORDINALITY AS sk(attnum, ord) ON true
                JOIN LATERAL unnest(con.confkey) WITH ORDINALITY AS dk(attnum, ord) ON dk.ord = sk.ord
                JOIN pg_attribute att ON att.attrelid = src.oid AND att.attnum = sk.attnum
                WHERE con.contype = 'f' AND nsp.nspname = 'public' AND src.relname = @table
                """;
            var parameter = command.CreateParameter();
            parameter.ParameterName = "table";
            parameter.Value = name;
            command.Parameters.Add(parameter);
            using var reader = command.ExecuteReader();
            while (reader.Read())
                foreignKeys.Add(new ForeignKeyInfo(reader.GetString(0), reader.GetString(1), reader.GetBoolean(2)));
        }

        return new TableSchema(name, columns, primaryKey, foreignKeys);
    }

    private static Dictionary<string, IReadOnlyList<string>> LoadSqliteTables(DbConnection connection)
    {
        var names = new List<string>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                SELECT name
                FROM sqlite_master
                WHERE type = 'table' AND name NOT LIKE 'sqlite_%'
                ORDER BY name
                """;
            using var reader = command.ExecuteReader();
            while (reader.Read())
                names.Add(reader.GetString(0));
        }

        var tables = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
        {
            Quote(name);
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA table_info(" + Quote(name) + ")";
            using var reader = command.ExecuteReader();
            var columns = new List<string>();
            while (reader.Read())
                columns.Add(reader.GetString(1));
            tables[name] = columns;
        }

        return tables;
    }

    private static long Count(DbConnection connection, string table)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM " + Quote(table);
        var value = command.ExecuteScalar();
        return Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }

    private static void PrintSummary(IReadOnlyList<(string Table, long Source, long Destination)> rows)
    {
        Console.WriteLine();
        Console.WriteLine($"{"Tabla",-48} {"Origen",8} {"Destino",8}");
        foreach (var row in rows)
        {
            var mark = row.Source == row.Destination ? "" : " *";
            Console.WriteLine($"{row.Table,-48} {row.Source,8} {row.Destination,8}{mark}");
        }

        Console.WriteLine();
    }

    private static object Coerce(object value, string pgType)
    {
        if (value is DBNull)
            return DBNull.Value;

        switch (pgType)
        {
            case "bool":
                if (value is bool)
                    return value;
                if (value is long or int or short or byte)
                    return Convert.ToInt64(value, CultureInfo.InvariantCulture) != 0;
                if (value is string text)
                    return text is "1" or "t" or "T" or "true" or "TRUE" or "True";
                break;
            case "int2":
            case "int4":
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            case "int8":
                return Convert.ToInt64(value, CultureInfo.InvariantCulture);
            case "numeric":
                return value is decimal ? value : Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            case "float4":
                return Convert.ToSingle(value, CultureInfo.InvariantCulture);
            case "float8":
                return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            case "timestamptz":
            case "timestamp":
                var time = value switch
                {
                    DateTime dateTime => dateTime,
                    DateTimeOffset offset => offset.UtcDateTime,
                    string raw when DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed) => parsed,
                    _ => (DateTime?)null
                };
                if (time is DateTime resolved)
                {
                    if (pgType == "timestamptz" && resolved.Kind == DateTimeKind.Unspecified)
                        resolved = DateTime.SpecifyKind(resolved, DateTimeKind.Utc);
                    if (pgType == "timestamptz" && resolved.Kind == DateTimeKind.Local)
                        resolved = resolved.ToUniversalTime();
                    return resolved;
                }

                break;
            case "date":
                if (value is DateOnly)
                    return value;
                if (value is DateTime date)
                    return DateOnly.FromDateTime(date);
                if (value is string dateText && DateOnly.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnly))
                    return dateOnly;
                break;
            case "uuid":
                if (value is Guid)
                    return value;
                if (value is string guidText && Guid.TryParse(guidText, out var guid))
                    return guid;
                break;
            case "bytea":
                if (value is byte[])
                    return value;
                break;
        }

        return value;
    }

    private static int IndexOf(IReadOnlyList<ColumnMap> columns, string name)
    {
        for (var i = 0; i < columns.Count; i++)
        {
            if (columns[i].DestinationName.Equals(name, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        throw new InvalidOperationException("Columna no incluida en la copia: " + name);
    }

    private static string Quote(string identifier)
    {
        foreach (var character in identifier)
        {
            var allowed = character is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_';
            if (!allowed)
                throw new InvalidOperationException("Identificador no permitido.");
        }

        return "\"" + identifier + "\"";
    }

    private sealed record ColumnInfo(string Name, string PgType, bool NotNull);

    private sealed record ForeignKeyInfo(string Column, string ReferencedTable, bool IsNullable);

    private sealed record TableSchema(
        string Name,
        IReadOnlyList<ColumnInfo> Columns,
        IReadOnlyList<string> PrimaryKey,
        IReadOnlyList<ForeignKeyInfo> ForeignKeys);

    private sealed record ColumnMap(string DestinationName, string SourceName, string PgType);

    private sealed record CopyPlan(
        string SourceTable,
        string DestinationTable,
        IReadOnlyList<ColumnMap> Columns,
        IReadOnlyList<string> PrimaryKey,
        IReadOnlySet<string> DeferredColumns);

    private sealed record PendingUpdate(
        string Table,
        string[] PrimaryKeyColumns,
        object[] KeyValues,
        string Column,
        object Value);
}
