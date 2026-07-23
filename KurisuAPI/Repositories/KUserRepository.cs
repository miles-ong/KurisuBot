using KurisuAPI.Factories;
using KurisuAPI.Repositories.Bases;
using KurisuCore.Models;
using KurisuCore.Models.Data;
using Npgsql;
using NpgsqlTypes;

namespace KurisuAPI.Repositories
{
    public sealed class KUserRepository(DbConnectionFactory factory) : KDataBulkRepository<KUserData>(factory)
    {
        protected override string TableName => "userdata";
        protected override string SelectColumns =>
            "id, settings, last_quiz_regen AS LastQuizRegen, quiz_tokens AS QuizTokens, pity, coins, r_dust AS RDust, sr_dust AS SrDust, ssr_dust AS SsrDust, ur_dust AS UrDust";
        protected override string InsertColumns =>
            "id, settings, last_quiz_regen, quiz_tokens, pity, coins, r_dust, sr_dust, ssr_dust, ur_dust";
        protected override string InsertValues =>
            "@Id, @Settings, @LastQuizRegen, @QuizTokens, @Pity, @Coins, @RDust, @SrDust, @SsrDust, @UrDust";
        protected override string UpdateQuery => """
                                                 UPDATE userdata
                                                 SET settings           = @Settings,
                                                     last_quiz_regen    = @LastQuizRegen,
                                                     quiz_tokens        = @QuizTokens,
                                                     pity               = @Pity,
                                                     coins              = @Coins,
                                                     r_dust             = @RDust,
                                                     sr_dust            = @SrDust,
                                                     ssr_dust           = @SsrDust,
                                                     ur_dust            = @UrDust
                                                 WHERE id = @Id
                                                 """;
        protected override string TempTableColumns => """
                                                      id BIGINT PRIMARY KEY,
                                                      settings JSONB,
                                                      last_quiz_regen TIMESTAMPTZ,
                                                      quiz_tokens INT,
                                                      pity INT,
                                                      coins INT,
                                                      r_dust INT,
                                                      sr_dust INT,
                                                      ssr_dust INT,
                                                      ur_dust INT
                                                      """;

        protected override async Task WriteBulkRowAsync(
            NpgsqlBinaryImporter writer, 
            KUserData data,
            CancellationToken token)
        {
            await writer.WriteAsync(data.Id, NpgsqlDbType.Bigint, token);
            await writer.WriteAsync(KSettingsSerializer.Serialize(data.Settings), NpgsqlDbType.Jsonb, token);
            await writer.WriteAsync(DateTime.SpecifyKind(data.LastQuizRegen, DateTimeKind.Utc), NpgsqlDbType.TimestampTz, token);
            await writer.WriteAsync(data.QuizTokens, NpgsqlDbType.Integer, token);
            await writer.WriteAsync(data.Pity, NpgsqlDbType.Integer, token);
            await writer.WriteAsync(data.Coins, NpgsqlDbType.Integer, token);
            await writer.WriteAsync(data.RDust, NpgsqlDbType.Integer, token);
            await writer.WriteAsync(data.SrDust, NpgsqlDbType.Integer, token);
            await writer.WriteAsync(data.SsrDust, NpgsqlDbType.Integer, token);
            await writer.WriteAsync(data.UrDust, NpgsqlDbType.Integer, token);
        }
    }
}
