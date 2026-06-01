using KurisuBot.Core.Models;
using Npgsql;

namespace KurisuBot.Core.Infrastructure.Repositories
{
    internal sealed class KCharacterRepository
    {
        private readonly string _connectionString;

        public KCharacterRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<KCharacter> InsertAsync(string name)
        {
            const string sql = """
                INSERT INTO characterinfo (name) 
                VALUES (@name)
                RETURNING id
            """;

            await using var connection = new NpgsqlConnection(_connectionString);
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

            await using var connection = new NpgsqlConnection(_connectionString);
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

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            command.Parameters.AddWithValue("@id", id);

            int rows = await command.ExecuteNonQueryAsync();

            if (rows <= 0) throw new KeyNotFoundException($"Failed to find character (ID: {id}) in database.");
        }

        public async Task<List<KCharacter>> GetAllAsync()
        {
            const string sql = "SELECT id, name FROM characterinfo";

            List<KCharacter> characters = new();

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                characters.Add(new KCharacter(
                    reader.GetInt32(0),
                    reader.GetString(1)
                ));
            }

            return characters;
        }
    }
}
