using KurisuAPI.Factories;
using KurisuAPI.Records;
using KurisuCore.Models;
using Npgsql;
using NpgsqlTypes;

namespace KurisuAPI.Repositories
{
    public sealed class KCardRepository(DbConnectionFactory factory)
    {
        public async Task<List<KCardRow>> GetByOwnerAsync(long ownerId)
        {
            const string sql = """
                SELECT id, variantid, timeobtained, borderid, overlayid
                FROM carddata
                WHERE ownerid = @ownerid
            """;

            await using var connection = await factory.CreateAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@ownerid", ownerId);

            List<KCardRow> cards = new();
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                KCardRow row = new(
                    reader.GetInt64(0),
                    reader.GetInt32(1),
                    DateTime.SpecifyKind(reader.GetDateTime(2), DateTimeKind.Utc),
                    reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    reader.IsDBNull(4) ? null : reader.GetInt32(4)
                );

                cards.Add(row);
            }

            return cards;
        }

        public async Task BulkUpsertAsync(IEnumerable<KCard> cards, CancellationToken token)
        {
            await using var connection = await factory.CreateAsync(token);
            await using var transaction = await connection.BeginTransactionAsync(token);

            const string tempSql = """
                CREATE TEMP TABLE temp_carddata (
                    id BIGINT PRIMARY KEY,
                    ownerid BIGINT,
                    variantid INT,
                    timeobtained TIMESTAMPTZ,
                    borderid INT,
                    overlayid INT
                ) ON COMMIT DROP
            """;

            await using var tempCommand = new NpgsqlCommand(tempSql, connection, transaction);
            await tempCommand.ExecuteNonQueryAsync(token);
            NpgsqlBinaryImporter? writer = null;

            try
            {
                writer = await connection.BeginBinaryImportAsync("""
                    COPY temp_carddata (id, ownerid, variantid, timeobtained, borderid, overlayid)
                    FROM STDIN (FORMAT BINARY)
                """);

                foreach (KCard card in cards)
                {
                    await writer.StartRowAsync();
                    await writer.WriteAsync(card.Id, NpgsqlDbType.Bigint);
                    await writer.WriteAsync(card.Owner!.Id, NpgsqlDbType.Bigint);
                    await writer.WriteAsync(card.Variant.Id, NpgsqlDbType.Integer);
                    await writer.WriteAsync(card.TimeObtained, NpgsqlDbType.TimestampTz);
                    await writer.WriteAsync(card.BorderId, NpgsqlDbType.Integer);
                    await writer.WriteAsync(card.OverlayId, NpgsqlDbType.Integer);
                }

                await writer.CompleteAsync(token);
            }
            finally
            {
                writer?.Dispose();
            }

            const string upsertSql = """
                INSERT INTO carddata (id, ownerid, variantid, timeobtained, borderid, overlayid)
                SELECT id, ownerid, variantid, timeobtained, borderid, overlayid
                FROM temp_carddata
                ON CONFLICT (id) DO UPDATE SET
                    ownerid         = EXCLUDED.ownerid,
                    variantid       = EXCLUDED.variantid,
                    timeobtained    = EXCLUDED.timeobtained,
                    borderid        = EXCLUDED.borderid,
                    overlayid       = EXCLUDED.overlayid
            """;

            await using var upsertCommand = new NpgsqlCommand(upsertSql, connection, transaction);
            await upsertCommand.ExecuteNonQueryAsync(token);
            await transaction.CommitAsync(token);
        }
    }
}
