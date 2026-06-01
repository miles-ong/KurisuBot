using Npgsql;

namespace KurisuBot.Core.Infrastructure.Repositories
{
    internal sealed class KCharacterAnimeLinkRepository
    {
        private readonly string _connectionString;

        public KCharacterAnimeLinkRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task InsertAsync(int cid, int aid)
        {
            const string sql = """
                INSERT INTO character_anime (character_id, anime_id)
                VALUES (@cid, @aid)
                ON CONFLICT DO NOTHING
            """;

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
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

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@cid", cid);
            command.Parameters.AddWithValue("@aid", aid);

            int rows = await command.ExecuteNonQueryAsync();

            if (rows <= 0) throw new KeyNotFoundException($"Failed to find link between character (ID: {cid}) and anime (ID: {aid}) in database.");
        }

        public async Task<List<(int cid, int aid)>> GetAllAsync()
        {
            const string sql = "SELECT character_id, anime_id FROM character_anime";

            List<(int, int)> links = new();

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                links.Add((
                    reader.GetInt32(0),
                    reader.GetInt32(1)
                ));
            }

            return links;
        }
    }
}
