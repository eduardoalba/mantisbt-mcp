using System.ComponentModel;
using System.Text.Json;
using MantisMcpServer.Services;
using MantisService;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;

namespace MantisMcpServer.Tools
{
    [McpServerToolType]
    public class ProjectTools
    {
        private readonly IMantisClient _mantisClient;
        private readonly ILogger<ProjectTools> _logger;

        public ProjectTools(IMantisClient mantisClient, ILogger<ProjectTools> logger)
        {
            _mantisClient = mantisClient;
            _logger = logger;
        }

        [McpServerTool]
        [Description("Lists all projects that the current user has access to. Use this first to find project IDs.")]
        public async Task<string> GetMyProjectsAsync()
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                var projects = await client.mc_projects_get_user_accessibleAsync(_mantisClient.Username, _mantisClient.Token);
                return JsonSerializer.Serialize(projects, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing projects");
                return $"Error listing projects: {ex.Message}. Check your credentials and URL.";
            }
        }

        [McpServerTool]
        [Description("Retrieves all available categories for a specific project. Categories are required when creating new issues.")]
        public async Task<string> GetCategoriesAsync(
            [Description("The numeric ID of the project.")] int project_id)
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                var categories = await client.mc_project_get_categoriesAsync(_mantisClient.Username, _mantisClient.Token, project_id.ToString());
                return JsonSerializer.Serialize(categories, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching categories for project {ProjectId}", project_id);
                return $"Error fetching categories for project {project_id}: {ex.Message}. Make sure the project ID is correct.";
            }
        }

        [McpServerTool]
        [Description("Returns common system metadata: valid Status, Priority, Severity, and Resolution levels with their corresponding numeric IDs.")]
        public async Task<string> GetSystemEnumsAsync()
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                var status = await client.mc_enum_statusAsync(_mantisClient.Username, _mantisClient.Token);
                var priorities = await client.mc_enum_prioritiesAsync(_mantisClient.Username, _mantisClient.Token);
                var severities = await client.mc_enum_severitiesAsync(_mantisClient.Username, _mantisClient.Token);
                var resolutions = await client.mc_enum_resolutionsAsync(_mantisClient.Username, _mantisClient.Token);

                var result = new
                {
                    Status = status,
                    Priorities = priorities,
                    Severities = severities,
                    Resolutions = resolutions
                };

                return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching system enums");
                return $"Error fetching system metadata: {ex.Message}.";
            }
        }

        [McpServerTool]
        [Description("Lists all saved filters available for the user in a specific project.")]
        public async Task<string> GetFiltersAsync(
            [Description("The numeric ID of the project.")] int project_id)
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                var filters = await client.mc_filter_getAsync(_mantisClient.Username, _mantisClient.Token, project_id.ToString());
                return JsonSerializer.Serialize(filters, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving filters for project {ProjectId}", project_id);
                return $"Error retrieving filters: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Retrieves issues matching a specific saved filter ID.")]
        public async Task<string> GetIssuesByFilterAsync(
            [Description("The numeric ID of the project.")] int project_id,
            [Description("The numeric ID of the saved filter.")] int filter_id,
            [Description("The page number to retrieve (starts at 1).")] int page_number = 1,
            [Description("Number of issues to return per page (default is 50).")] int per_page = 50)
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                var issues = await client.mc_filter_get_issuesAsync(
                    _mantisClient.Username,
                    _mantisClient.Token,
                    project_id.ToString(),
                    filter_id.ToString(),
                    page_number.ToString(),
                    per_page.ToString());

                return JsonSerializer.Serialize(issues, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving issues for filter {FilterId} in project {ProjectId}", filter_id, project_id);
                return $"Error retrieving issues for filter: {ex.Message}";
            }
        }
    }
}

