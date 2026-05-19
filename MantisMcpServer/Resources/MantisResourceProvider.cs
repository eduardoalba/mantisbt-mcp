using System.ComponentModel;
using System.Text.Json;
using MantisMcpServer.Services;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;

namespace MantisMcpServer.Resources
{
    [McpServerResourceType]
    public class MantisResourceProvider
    {
        private readonly IMantisClient _mantisClient;
        private readonly ILogger<MantisResourceProvider> _logger;
        private readonly string _logPath;

        public MantisResourceProvider(IMantisClient mantisClient, ILogger<MantisResourceProvider> logger)
        {
            _mantisClient = mantisClient;
            _logger = logger;
            
            _logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "mantisbt-mcp",
                "logs",
                "server.log"
            );
        }

        [McpServerResource(UriTemplate = "logs://server")]
        [Description("Current server operation logs for debugging and monitoring.")]
        public async Task<string> GetLogsAsync()
        {
            try
            {
                if (!File.Exists(_logPath))
                {
                    return "Log file not found.";
                }

                // Read with sharing enabled to avoid locking issues with Serilog
                using var fileStream = new FileStream(_logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fileStream);
                
                // Get the last 100 lines for efficiency
                var lines = new List<string>();
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    lines.Add(line);
                    if (lines.Count > 100) lines.RemoveAt(0);
                }

                return string.Join(Environment.NewLine, lines);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading log resource");
                return $"Error reading logs: {ex.Message}";
            }
        }

        [McpServerResource(UriTemplate = "mantis://projects/stats")]
        [Description("A summary of issues and statistics across accessible projects.")]
        public async Task<string> GetProjectStatsAsync()
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                var projects = await client.mc_projects_get_user_accessibleAsync(_mantisClient.Username, _mantisClient.Token);
                
                var statsList = new List<object>();

                foreach (var project in projects)
                {
                    var issues = await client.mc_project_get_issuesAsync(
                        _mantisClient.Username, 
                        _mantisClient.Token, 
                        project.id, 
                        "1", 
                        "1"); 
                    
                    statsList.Add(new
                    {
                        ProjectId = project.id,
                        ProjectName = project.name,
                        IssueCount = issues?.Length ?? 0,
                        Status = project.status?.name,
                        Description = project.description
                    });
                }

                return JsonSerializer.Serialize(new
                {
                    Timestamp = DateTime.UtcNow,
                    Projects = statsList
                }, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating project stats resource");
                return $"Error: {ex.Message}";
            }
        }
    }
}
