using KurisuAPI.Factories;
using KurisuCore.Enums;
using KurisuCore.Models;
using Npgsql;
using NpgsqlTypes;

namespace KurisuAPI.Repositories
{
    public sealed class KUserRepository(string connectionString)
    {
        public async Task<KUser?> GetByIdAsync(long id)
        {
            const string sql = """
                SELECT id, settings, quiztokens, lastquizregen, pity, coins, rdust, srdust, ssrdust, urdust
                FROM userdata
                WHERE id = @id
            """;

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@id", id);

            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync()) return null;

            int settingsOrdinal = reader.GetOrdinal("settings");
            KSettings settings = reader.IsDBNull(settingsOrdinal)
                ? new KSettings()
                : KSettingsSerializer.Deserialize(reader.GetString(settingsOrdinal));

            DateTime lastQuizRegen = DateTime.SpecifyKind(
                reader.GetDateTime(reader.GetOrdinal("lastquizregen")),
                DateTimeKind.Utc);

            KUser user = new KUser(id, "", settings, lastQuizRegen);

            user.SetBalance(KBalance.Pity, reader.GetInt32(reader.GetOrdinal("pity")));
            user.SetBalance(KBalance.QuizTokens, reader.GetInt32(reader.GetOrdinal("quiztokens")));
            user.SetBalance(KBalance.Coins, reader.GetInt32(reader.GetOrdinal("coins")));
            user.SetBalance(KBalance.RDust, reader.GetInt32(reader.GetOrdinal("rdust")));
            user.SetBalance(KBalance.SRDust, reader.GetInt32(reader.GetOrdinal("srdust")));
            user.SetBalance(KBalance.SSRDust, reader.GetInt32(reader.GetOrdinal("ssrdust")));
            user.SetBalance(KBalance.URDust, reader.GetInt32(reader.GetOrdinal("urdust")));

            return user;
        }

        public async Task BulkUpsertAsync(IEnumerable<KUser> users, CancellationToken token)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(token);
            await using var transaction = await connection.BeginTransactionAsync(token);

            const string tempTableSql = """
                CREATE TEMP TABLE temp_userdata (
                    id BIGINT PRIMARY KEY,
                    settings JSONB,
                    quiztokens INT,
                    lastquizregen TIMESTAMPTZ,
                    pity INT,
                    coins INT,
                    rdust INT,
                    srdust INT,
                    ssrdust INT,
                    urdust INT
                ) ON COMMIT DROP
            """;

            await using var tempCommand = new NpgsqlCommand(tempTableSql, connection, transaction);
            await tempCommand.ExecuteNonQueryAsync(token);
            NpgsqlBinaryImporter? writer = null;

            try
            {
                writer = await connection.BeginBinaryImportAsync("""
                    COPY temp_userdata (id, settings, quiztokens, lastquizregen, pity, coins, rdust, srdust, ssrdust, urdust)
                    FROM STDIN (FORMAT BINARY)
                """);

                foreach(KUser user in users)
                {
                    await writer.StartRowAsync();
                    await writer.WriteAsync(user.Id, NpgsqlDbType.Bigint);
                    await writer.WriteAsync(KSettingsSerializer.Serialize(user.Settings), NpgsqlDbType.Jsonb);
                    await writer.WriteAsync(user.GetBalance(KBalance.QuizTokens), NpgsqlDbType.Integer);
                    await writer.WriteAsync(DateTime.SpecifyKind(user.LastQuizRegen, DateTimeKind.Utc), NpgsqlDbType.TimestampTz);
                    await writer.WriteAsync(user.GetBalance(KBalance.Pity), NpgsqlDbType.Integer);
                    await writer.WriteAsync(user.GetBalance(KBalance.Coins), NpgsqlDbType.Integer);
                    await writer.WriteAsync(user.GetBalance(KBalance.RDust), NpgsqlDbType.Integer);
                    await writer.WriteAsync(user.GetBalance(KBalance.SRDust), NpgsqlDbType.Integer);
                    await writer.WriteAsync(user.GetBalance(KBalance.SSRDust), NpgsqlDbType.Integer);
                    await writer.WriteAsync(user.GetBalance(KBalance.URDust), NpgsqlDbType.Integer);
                }

                await writer.CompleteAsync(token);
            }
            finally
            {
                writer?.Dispose();
            }

            const string upsertSql = """
                INSERT INTO userdata (id, settings, quiztokens, lastquizregen, pity, coins, rdust, srdust, ssrdust, urdust)
                SELECT id, settings, quiztokens, lastquizregen, pity, coins, rdust, srdust, ssrdust, urdust
                FROM temp_userdata
                ON CONFLICT (id) DO UPDATE SET
                    settings        = EXCLUDED.settings,
                    quiztokens      = EXCLUDED.quiztokens,
                    lastquizregen   = EXCLUDED.lastquizregen,
                    pity            = EXCLUDED.pity,
                    coins           = EXCLUDED.coins,
                    rdust           = EXCLUDED.rdust,
                    srdust          = EXCLUDED.srdust,
                    ssrdust         = EXCLUDED.ssrdust,
                    urdust          = EXCLUDED.urdust
            """;

            await using var upsertCommand = new NpgsqlCommand(upsertSql, connection, transaction);
            await upsertCommand.ExecuteNonQueryAsync(token);
            await transaction.CommitAsync(token);
        }
    }
}
