using KurisuAPI.Factories;
using Npgsql;

namespace KurisuAPI.Repositories
{
    public sealed class KCharacterAnimeLinkRepository(DbConnectionFactory factory)
    {
        public async Task<List<int>> GetByAnimeIdAsync(int id)
        {
            const string sql = """
                SELECT character_id
                FROM character_anime
                WHERE anime_id = @id
            """;

            await using var connection = await factory.CreateAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@id", id);

            List<int> cids = new();
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                cids.Add(reader.GetInt32(0));
            }

            return cids;
        }

        public async Task<List<int>> GetByCharacterIdAsync(int id)
        {
            const string sql = """
                SELECT anime_id
                FROM character_anime
                WHERE character_id = @id
            """;

            await using var connection = await factory.CreateAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@id", id);

            List<int> aids = new();
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                aids.Add(reader.GetInt32(0));
            }

            return aids;
        }

        public async Task InsertAsync(int cid, int aid)
        {
            const string sql = """
                INSERT INTO character_anime (character_id, anime_id)
                VALUES (@cid, @aid)
                ON CONFLICT DO NOTHING
            """;

            await using var connection = await factory.CreateAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@cid", cid);
            command.Parameters.AddWithValue("@aid", aid);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(int cid, int aid)
        {
            const string sql = """
                DELETE FROM character_anime
                WHERE character_id = @cid AND anime_id = @aid
            """;

            await using var connection = await factory.CreateAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@cid", cid);
            command.Parameters.AddWithValue("@aid", aid);

            int rows = await command.ExecuteNonQueryAsync();

            if (rows <= 0) throw new KeyNotFoundException($"Failed to find link between character (ID: {cid}) and anime (ID: {aid}) in database.");
        }
    }
}
