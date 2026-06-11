using KurisuAPI.Factories;
using KurisuAPI.Records;
using KurisuCore.Enums;
using KurisuCore.Models;
using Npgsql;

namespace KurisuAPI.Repositories
{
    public sealed class KVariantRepository(string connectionString)
    {
        public async Task<KVariantRow?> GetByIdAsync(int id)
        {
            const string sql = "SELECT id, character_id, anime_id, rarity, title FROM variantinfo WHERE id = @id";

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@id", id);

            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync()) return null;

            return new KVariantRow(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetInt32(2),
                Enum.Parse<KRarity>(reader.GetString(3)),
                reader.GetString(4)
            );
        }

        public async Task<int> InsertAsync(int cid, int aid, KRarity rarity, string title)
        {
            const string sql = """
                INSERT INTO variantinfo (character_id, anime_id, rarity, title)
                VALUES (@cid, @aid, @rarity::cardrarity, @title)
                RETURNING id
            """;

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@cid", cid);
            command.Parameters.AddWithValue("@aid", aid);
            command.Parameters.AddWithValue("@rarity", rarity.ToString());
            command.Parameters.AddWithValue("@title", title);

            return (int)(await command.ExecuteScalarAsync())!;
        }

        public async Task UpdateAsync(KVariant variant)
        {
            const string sql = """
                UPDATE variantinfo
                SET rarity  = @rarity::cardrarity,
                    title   = @title
                WHERE id = @id
            """;

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@id", variant.Id);
            command.Parameters.AddWithValue("@rarity", variant.Rarity.ToString());
            command.Parameters.AddWithValue("@title", variant.Title);

            int rows = await command.ExecuteNonQueryAsync();

            if (rows <= 0) throw new KeyNotFoundException($"Failed to find variant {variant} in database.");
        }

        public async Task DeleteAsync(int id)
        {
            const string sql = "DELETE FROM variantinfo WHERE id = @id";

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@id", id);

            int rows = await command.ExecuteNonQueryAsync();

            if (rows <= 0) throw new KeyNotFoundException($"Failed to find variant (ID: {id}) in database.");
        }
    }
}
