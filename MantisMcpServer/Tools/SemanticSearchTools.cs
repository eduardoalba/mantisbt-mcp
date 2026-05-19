using System.ComponentModel;
using System.Text.Json;
using MantisMcpServer.Services;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;

namespace MantisMcpServer.Tools
{
    [McpServerToolType]
    public class SemanticSearchTools
    {
        private readonly IMantisClient _mantisClient;
        private readonly ISearchService _searchService;
        private readonly IEmbeddingService? _embeddingService;
        private readonly ILogger<SemanticSearchTools> _logger;

        public SemanticSearchTools(
            IMantisClient mantisClient, 
            ISearchService searchService, 
            ILogger<SemanticSearchTools> logger,
            IEmbeddingService? embeddingService = null)
        {
            _mantisClient = mantisClient;
            _searchService = searchService;
            _embeddingService = embeddingService;
            _logger = logger;
        }

        [McpServerTool]
        [Description("Synchronizes and indexes issues from a project for local semantic search.")]
        public async Task<string> SyncProjectAsync(
            [Description("The numeric ID of the project to sync.")] int project_id)
        {
            try
            {
                using var soapClient = _mantisClient.CreateSoapClient();
                // Get all issues for the project (paging if necessary, but starting simple)
                var issues = await soapClient.mc_project_get_issuesAsync(
                    _mantisClient.Username, 
                    _mantisClient.Token, 
                    project_id.ToString(), 
                    "1", 
                    "500");

                int indexedCount = 0;
                int embeddingCount = 0;

                foreach (var issue in issues)
                {
                    float[]? embedding = null;
                    if (_embeddingService != null)
                    {
                        var textToEmbed = $"{issue.summary} {issue.description}";
                        embedding = await _embeddingService.GetEmbeddingAsync(textToEmbed);
                        embeddingCount++;
                    }

                    await _searchService.IndexIssueAsync(
                        project_id, 
                        int.Parse(issue.id), 
                        issue.summary, 
                        issue.description, 
                        embedding);
                    
                    indexedCount++;
                }

                return $"Sync complete. Indexed {indexedCount} issues ({embeddingCount} with embeddings) for project {project_id}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing project {ProjectId}", project_id);
                return $"Error syncing project: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Performs a semantic search for issues based on a natural language query.")]
        public async Task<string> SearchSemanticAsync(
            [Description("The search query in natural language.")] string query,
            [Description("Optional project ID to filter the search.")] int? project_id = null,
            [Description("Maximum number of results to return.")] int limit = 10)
        {
            try
            {
                if (_embeddingService == null)
                {
                    return "Semantic search is not available. Please configure OPENAI_API_KEY.";
                }

                var queryEmbedding = await _embeddingService.GetEmbeddingAsync(query);
                var results = await _searchService.SearchSemanticAsync(project_id, queryEmbedding, limit);

                return JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during semantic search");
                return $"Error during semantic search: {ex.Message}";
            }
        }
    }
}
