using KurisuBot.Core.Models;
using Npgsql;

namespace KurisuBot.Core.Infrastructure.Repositories
{
    internal sealed class KAnimeRepository
    {
        private readonly string _connectionString;

        public KAnimeRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<KAnime> InsertAsync(string title, string? description)
        {
            const string sql = """
                INSERT INTO animeinfo (title, description)
                VALUES (@title, @description)
                RETURNING id
            """;

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
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

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
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

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@id", id);

            int rows = await command.ExecuteNonQueryAsync();

            if (rows <= 0) throw new KeyNotFoundException($"Failed to find anime (ID: {id}) in database.");
        }

        public async Task<List<KAnime>> GetAllAsync()
        {
            const string sql = "SELECT id, title, description FROM animeinfo";

            List<KAnime> result = new();

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

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
    }
}
