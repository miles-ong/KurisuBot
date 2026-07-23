using KurisuAPI.Factories;
using KurisuAPI.Repositories.Bases;
using KurisuCore.Models.Data;
using Npgsql;
using NpgsqlTypes;

namespace KurisuAPI.Repositories
{
    public sealed class KCardRepository(DbConnectionFactory factory) : KDataBulkRepository<KCardData>(factory)
    {
        protected override string TableName => "carddata";
        protected override string SelectColumns =>
            "id, owner_id AS OwnerId, variant_id AS VariantId, time_obtained AS TimeObtained, border_id AS BorderId, overlay_id AS OverlayId, xp";
        protected override string InsertColumns =>
            "id, owner_id, variant_id, time_obtained, border_id, overlay_id, xp";
        protected override string InsertValues =>
            "@Id, @OwnerId, @VariantId, @TimeObtained, @BorderId, @OverlayId, @Xp";
        protected override string UpdateQuery => """
                                                 UPDATE carddata
                                                 SET owner_id       = @OwnerId,
                                                     variant_id     = @VariantId,
                                                     time_obtained  = @TimeObtained,
                                                     border_id      = @BorderId,
                                                     overlay_id     = @OverlayId,
                                                     xp             = @Xp
                                                 WHERE id = @Id
                                                 """;
        protected override string TempTableColumns => """
                                                      id BIGINT PRIMARY KEY,
                                                      owner_id BIGINT,
                                                      variant_id INT,
                                                      time_obtained TIMESTAMPTZ,
                                                      border_id INT,
                                                      overlay_id INT,
                                                      xp INT
                                                      """;

        protected override async Task WriteBulkRowAsync(
            NpgsqlBinaryImporter writer,
            KCardData data,
            CancellationToken token)
        {
            await writer.WriteAsync(data.Id, NpgsqlDbType.Bigint, token);

            if (data.OwnerId is not null) await writer.WriteAsync(data.OwnerId, NpgsqlDbType.Bigint, token);
            else await writer.WriteNullAsync(token);

            await writer.WriteAsync(data.VariantId, NpgsqlDbType.Integer, token);
            await writer.WriteAsync(DateTime.SpecifyKind(data.TimeObtained, DateTimeKind.Utc), NpgsqlDbType.TimestampTz, token);

            if (data.BorderId is not null) await writer.WriteAsync(data.BorderId, NpgsqlDbType.Integer, token);
            else await writer.WriteNullAsync(token);
            
            if (data.OverlayId is not null) await writer.WriteAsync(data.OverlayId, NpgsqlDbType.Integer, token);
            else await writer.WriteNullAsync(token);

            await writer.WriteAsync(data.Xp, NpgsqlDbType.Integer, token);
        }
    }
}
