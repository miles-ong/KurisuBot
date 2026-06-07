using KurisuAPI.Factories;
using KurisuCore.Models;
using Npgsql;

namespace KurisuAPI.Repositories
{
    public sealed class KAnimeRepository(DbConnectionFactory factory)
    {
        public async Task<KAnime?> GetByIdAsync(int id)
        {
            const string sql = "SELECT id, title, description FROM animeinfo WHERE id = @id";

            await using var connection = await factory.CreateAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@id", id);

            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync()) return null;

            return new KAnime(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2)
            );
        }

        public async Task<List<KAnime>> SearchAsync(string query, int limit)
        {
            const string sql = """
                SELECT id, title, description
                FROM animeinfo
                WHERE title ILIKE @query
                LIMIT @limit
            """;

            await using var connection = await factory.CreateAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@query", $"%{query}%");
            command.Parameters.AddWithValue("@limit", limit);

            await using var reader = await command.ExecuteReaderAsync();
            List<KAnime> result = new();

            while (await reader.ReadAsync())
            {
                result.Add(new KAnime(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2)
                ));
            }

            return result;
        }

        public async Task<KAnime> InsertAsync(string title, string? description)
        {
            const string sql = """
                INSERT INTO animeinfo (title, description)
                VALUES (@title, @description)
                RETURNING id
            """;

            await using var connection = await factory.CreateAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@title", title);
            command.Parameters.AddWithValue("@description", (object?)description ?? DBNull.Value);

            int id = (int)(await command.ExecuteScalarAsync())!;

            return new KAnime(id, title, description ?? "");
        }

        public async Task UpdateAsync(KAnime anime)
        {
            const string sql = """
                UPDATE animeinfo
                SET title = @title,
                    description = @description
                WHERE id = @id
            """;

            await using var connection = await factory.CreateAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@id", anime.Id);
            command.Parameters.AddWithValue("@title", anime.Title);
            command.Parameters.AddWithValue("@description", anime.Description);

            int rows = await command.ExecuteNonQueryAsync();

            if (rows <= 0) throw new KeyNotFoundException($"Failed to find anime {anime} in database.");
        }

        public async Task DeleteAsync(int id)
        {
            const string sql = "DELETE FROM animeinfo WHERE id = @id";

            await using var connection = await factory.CreateAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@id", id);

            int rows = await command.ExecuteNonQueryAsync();

            if (rows <= 0) throw new KeyNotFoundException($"Failed to find anime (ID: {id}) in database.");
        }
    }
}
