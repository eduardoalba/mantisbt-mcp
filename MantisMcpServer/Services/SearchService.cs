using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MantisMcpServer.Services
{
    public interface ISearchService
    {
        Task InitializeAsync();
        Task IndexIssueAsync(int projectId, int issueId, string summary, string description, float[]? embedding = null);
        Task<List<SearchResult>> SearchSemanticAsync(int? projectId, float[] queryEmbedding, int limit = 10);
        Task<List<SearchResult>> SearchTextAsync(int? projectId, string text, int limit = 10);
    }

    public record SearchResult(int IssueId, string Summary, float Score);

    public class SqliteSearchService : ISearchService
    {
        private readonly string _dbPath;
        private readonly ILogger<SqliteSearchService> _logger;

        public SqliteSearchService(ILogger<SqliteSearchService> logger)
        {
            _logger = logger;
            _dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "mantisbt-mcp",
                "data",
                "search.db"
            );
            
            var dir = Path.GetDirectoryName(_dbPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        public async Task InitializeAsync()
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Issues (
                    IssueId INTEGER PRIMARY KEY,
                    ProjectId INTEGER,
                    Summary TEXT,
                    Description TEXT,
                    Embedding BLOB,
                    LastUpdated DATETIME DEFAULT CURRENT_TIMESTAMP
                );
                CREATE INDEX IF NOT EXISTS idx_issues_project ON Issues(ProjectId);
            ";
            await command.ExecuteNonQueryAsync();
        }

        public async Task IndexIssueAsync(int projectId, int issueId, string summary, string description, float[]? embedding = null)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO Issues (IssueId, ProjectId, Summary, Description, Embedding, LastUpdated)
                VALUES ($issueId, $projectId, $summary, $description, $embedding, CURRENT_TIMESTAMP)
            ";
            command.Parameters.AddWithValue("$issueId", issueId);
            command.Parameters.AddWithValue("$projectId", projectId);
            command.Parameters.AddWithValue("$summary", summary);
            command.Parameters.AddWithValue("$description", description);
            
            if (embedding != null)
            {
                var bytes = new byte[embedding.Length * sizeof(float)];
                Buffer.BlockCopy(embedding, 0, bytes, 0, bytes.Length);
                command.Parameters.AddWithValue("$embedding", bytes);
            }
            else
            {
                command.Parameters.AddWithValue("$embedding", DBNull.Value);
            }

            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<SearchResult>> SearchSemanticAsync(int? projectId, float[] queryEmbedding, int limit = 10)
        {
            // Note: SQLite doesn't have native vector search unless using an extension like sqlite-vss.
            // For now, we'll do a simple (and slower) brute-force cosine similarity in C# after fetching candidates.
            // In a production scenario with many issues, we'd use a vector-optimized DB or extension.
            
            var results = new List<(int IssueId, string Summary, float[] Embedding)>();

            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT IssueId, Summary, Embedding FROM Issues WHERE Embedding IS NOT NULL";
            if (projectId.HasValue)
            {
                command.CommandText += " AND ProjectId = $projectId";
                command.Parameters.AddWithValue("$projectId", projectId.Value);
            }

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var bytes = (byte[])reader["Embedding"];
                var embedding = new float[bytes.Length / sizeof(float)];
                Buffer.BlockCopy(bytes, 0, embedding, 0, bytes.Length);
                
                results.Add((reader.GetInt32(0), reader.GetString(1), embedding));
            }

            return results
                .Select(r => new SearchResult(r.IssueId, r.Summary, CosineSimilarity(queryEmbedding, r.Embedding)))
                .OrderByDescending(r => r.Score)
                .Take(limit)
                .ToList();
        }

        public async Task<List<SearchResult>> SearchTextAsync(int? projectId, string text, int limit = 10)
        {
            var results = new List<SearchResult>();

            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT IssueId, Summary FROM Issues WHERE (Summary LIKE $text OR Description LIKE $text)";
            if (projectId.HasValue)
            {
                command.CommandText += " AND ProjectId = $projectId";
                command.Parameters.AddWithValue("$projectId", projectId.Value);
            }
            command.CommandText += " LIMIT $limit";
            command.Parameters.AddWithValue("$text", $"%{text}%");
            command.Parameters.AddWithValue("$limit", limit);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new SearchResult(reader.GetInt32(0), reader.GetString(1), 1.0f));
            }

            return results;
        }

        private float CosineSimilarity(float[] vector1, float[] vector2)
        {
            float dotProduct = 0;
            float magnitude1 = 0;
            float magnitude2 = 0;

            for (int i = 0; i < vector1.Length; i++)
            {
                dotProduct += vector1[i] * vector2[i];
                magnitude1 += vector1[i] * vector1[i];
                magnitude2 += vector2[i] * vector2[i];
            }

            return dotProduct / (MathF.Sqrt(magnitude1) * MathF.Sqrt(magnitude2));
        }
    }
}
