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
        [Description("Returns common system metadata: valid Status, Priority, Severity, Resolution, Access Levels, Project Status, and Reproducibility levels.")]
        public async Task<string> GetSystemEnumsAsync()
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                var status = await client.mc_enum_statusAsync(_mantisClient.Username, _mantisClient.Token);
                var priorities = await client.mc_enum_prioritiesAsync(_mantisClient.Username, _mantisClient.Token);
                var severities = await client.mc_enum_severitiesAsync(_mantisClient.Username, _mantisClient.Token);
                var resolutions = await client.mc_enum_resolutionsAsync(_mantisClient.Username, _mantisClient.Token);
                var accessLevels = await client.mc_enum_access_levelsAsync(_mantisClient.Username, _mantisClient.Token);
                var projectStatus = await client.mc_enum_project_statusAsync(_mantisClient.Username, _mantisClient.Token);
                var reproducibilities = await client.mc_enum_reproducibilitiesAsync(_mantisClient.Username, _mantisClient.Token);

                var result = new
                {
                    Status = status,
                    Priorities = priorities,
                    Severities = severities,
                    Resolutions = resolutions,
                    AccessLevels = accessLevels,
                    ProjectStatus = projectStatus,
                    Reproducibilities = reproducibilities
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
        [Description("Lists all subprojects for a specific project.")]
        public async Task<string> GetSubprojectsAsync(
            [Description("The numeric ID of the parent project.")] int project_id)
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                var subprojects = await client.mc_project_get_all_subprojectsAsync(_mantisClient.Username, _mantisClient.Token, project_id.ToString());
                return JsonSerializer.Serialize(subprojects, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subprojects for project {ProjectId}", project_id);
                return $"Error retrieving subprojects: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Lists all users who have access to a specific project.")]
        public async Task<string> GetProjectUsersAsync(
            [Description("The numeric ID of the project.")] int project_id,
            [Description("Minimum access level ID (e.g., 10=viewer, 40=updater, 70=manager). Default is 0 (all).")] int access_level = 0)
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                var users = await client.mc_project_get_usersAsync(_mantisClient.Username, _mantisClient.Token, project_id.ToString(), access_level.ToString());
                return JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users for project {ProjectId}", project_id);
                return $"Error retrieving users: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Retrieves all custom field definitions for a specific project.")]
        public async Task<string> GetProjectCustomFieldsAsync(
            [Description("The numeric ID of the project.")] int project_id)
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                var customFields = await client.mc_project_get_custom_fieldsAsync(_mantisClient.Username, _mantisClient.Token, project_id.ToString());
                return JsonSerializer.Serialize(customFields, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving custom fields for project {ProjectId}", project_id);
                return $"Error retrieving custom fields: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Lists all attachments (documents) associated with a project.")]
        public async Task<string> GetProjectAttachmentsAsync(
            [Description("The numeric ID of the project.")] int project_id)
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                var attachments = await client.mc_project_get_attachmentsAsync(_mantisClient.Username, _mantisClient.Token, project_id.ToString());
                return JsonSerializer.Serialize(attachments, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving project attachments for project {ProjectId}", project_id);
                return $"Error retrieving project attachments: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Uploads a file as a project-level attachment (document).")]
        public async Task<string> CreateProjectAttachmentAsync(
            [Description("The numeric ID of the project.")] int project_id,
            [Description("The filename (e.g., 'manual.pdf').")] string name,
            [Description("A title for the attachment.")] string title,
            [Description("A brief description of the file.")] string description,
            [Description("The MIME type (e.g., 'application/pdf').")] string file_type,
            [Description("The file content encoded in Base64.")] string content_base64)
        {
            try
            {
                var content = Convert.FromBase64String(content_base64);
                using var client = _mantisClient.CreateSoapClient();
                var attachmentId = await client.mc_project_attachment_addAsync(
                    _mantisClient.Username,
                    _mantisClient.Token,
                    project_id.ToString(),
                    name,
                    title,
                    description,
                    file_type,
                    content);

                return $"Project attachment uploaded successfully! ID: {attachmentId}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading project attachment for project {ProjectId}", project_id);
                return $"Error uploading project attachment: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Downloads the content of a project-level attachment as a Base64 string.")]
        public async Task<string> GetProjectAttachmentAsync(
            [Description("The numeric ID of the project attachment.")] int attachment_id)
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                var content = await client.mc_project_attachment_getAsync(_mantisClient.Username, _mantisClient.Token, attachment_id.ToString());
                return Convert.ToBase64String(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving project attachment {AttachmentId}", attachment_id);
                return $"Error retrieving project attachment: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Deletes a project-level attachment.")]
        public async Task<string> DeleteProjectAttachmentAsync(
            [Description("The numeric ID of the project attachment to delete.")] int attachment_id)
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                var success = await client.mc_project_attachment_deleteAsync(_mantisClient.Username, _mantisClient.Token, attachment_id.ToString());
                return success ? "Project attachment deleted successfully!" : "Failed to delete project attachment.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting project attachment {AttachmentId}", attachment_id);
                return $"Error deleting project attachment: {ex.Message}";
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

