using KurisuAPI.Factories;
using KurisuCore.Models;
using Npgsql;

namespace KurisuAPI.Repositories
{
    public sealed class KCharacterRepository(string connectionString)
    {
        public async Task<KCharacter?> GetByIdAsync(int id)
        {
            const string sql = "SELECT id, name FROM characterinfo WHERE id = @id";

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@id", id);

            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync()) return null;

            return new KCharacter(
                reader.GetInt32(0),
                reader.GetString(1)
            );
        }

        public async Task<List<KCharacter>> SearchAsync(string query, int limit)
        {
            const string sql = """
                SELECT id, name
                FROM characterinfo
                WHERE name ILIKE @query
                LIMIT @limit
            """;

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@query", $"%{query}%");
            command.Parameters.AddWithValue("@limit", limit);

            await using var reader = await command.ExecuteReaderAsync();
            List<KCharacter> result = new();

            while (await reader.ReadAsync())
            {
                result.Add(new KCharacter(
                    reader.GetInt32(0),
                    reader.GetString(1)
                ));
            }

            return result;
        }

        public async Task<KCharacter> InsertAsync(string name)
        {
            const string sql = """
                INSERT INTO characterinfo (name) 
                VALUES (@name)
                RETURNING id
            """;

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@name", name);

            int id = (int)(await command.ExecuteScalarAsync())!;

            return new KCharacter(id, name);
        }

        public async Task UpdateAsync(KCharacter character)
        {
            const string sql = """
                UPDATE characterinfo
                SET name = @name
                WHERE id = @id
            """;

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@id", character.Id);
            command.Parameters.AddWithValue("@name", character.Name);

            int rows = await command.ExecuteNonQueryAsync();

            if (rows <= 0) throw new KeyNotFoundException($"Failed to find character {character} in database.");
        }

        public async Task DeleteAsync(int id)
        {
            const string sql = "DELETE FROM characterinfo WHERE id = @id";

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@id", id);

            int rows = await command.ExecuteNonQueryAsync();

            if (rows <= 0) throw new KeyNotFoundException($"Failed to find character (ID: {id}) in database.");
        }
    }
}
