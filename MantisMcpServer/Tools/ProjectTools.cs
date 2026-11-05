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

        [McpServerTool]
        [Description("Finds the numeric ID of a project by its exact name.")]
        public async Task<string> GetProjectIdByNameAsync(
            [Description("The exact name of the project.")] string project_name)
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                var projectId = await client.mc_project_get_id_from_nameAsync(_mantisClient.Username, _mantisClient.Token, project_name);
                return string.IsNullOrEmpty(projectId) || projectId == "0"
                    ? $"No project found with name: '{project_name}'"
                    : $"Project ID: {projectId}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving project ID by name: {ProjectName}", project_name);
                return $"Error retrieving project ID: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Lists all versions of a specific project.")]
        public async Task<string> GetProjectVersionsAsync(
            [Description("The numeric ID of the project.")] int project_id)
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                var versions = await client.mc_project_get_versionsAsync(_mantisClient.Username, _mantisClient.Token, project_id.ToString());
                return JsonSerializer.Serialize(versions, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving versions for project {ProjectId}", project_id);
                return $"Error retrieving versions: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Lists only the released versions of a specific project.")]
        public async Task<string> GetReleasedVersionsAsync(
            [Description("The numeric ID of the project.")] int project_id)
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                var versions = await client.mc_project_get_released_versionsAsync(_mantisClient.Username, _mantisClient.Token, project_id.ToString());
                return JsonSerializer.Serialize(versions, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving released versions for project {ProjectId}", project_id);
                return $"Error retrieving released versions: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Lists only the unreleased versions of a specific project.")]
        public async Task<string> GetUnreleasedVersionsAsync(
            [Description("The numeric ID of the project.")] int project_id)
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                var versions = await client.mc_project_get_unreleased_versionsAsync(_mantisClient.Username, _mantisClient.Token, project_id.ToString());
                return JsonSerializer.Serialize(versions, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unreleased versions for project {ProjectId}", project_id);
                return $"Error retrieving unreleased versions: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Creates a new version for a project.")]
        public async Task<string> CreateProjectVersionAsync(
            [Description("The numeric ID of the project.")] int project_id,
            [Description("The name of the new version.")] string name,
            [Description("Whether the version is released (default false).")] bool released = false,
            [Description("Whether the version is obsolete (default false).")] bool obsolete = false,
            [Description("An optional description for the version.")] string description = "")
        {
            try
            {
                var version = new ProjectVersionData
                {
                    project_id = project_id.ToString(),
                    name = name,
                    description = description,
                    released = released,
                    releasedSpecified = true,
                    obsolete = obsolete,
                    obsoleteSpecified = true
                };

                using var client = _mantisClient.CreateSoapClient();
                var versionId = await client.mc_project_version_addAsync(_mantisClient.Username, _mantisClient.Token, version);
                return $"Version created successfully! ID: {versionId}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating version '{Name}' for project {ProjectId}", name, project_id);
                return $"Error creating version: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Updates an existing project version. Provide only the fields you want to change; omitted fields keep their current value.")]
        public async Task<string> UpdateProjectVersionAsync(
            [Description("The numeric ID of the version to update.")] int version_id,
            [Description("Optional new name for the version.")] string? name = null,
            [Description("Optional new released flag.")] bool? released = null,
            [Description("Optional new obsolete flag.")] bool? obsolete = null,
            [Description("Optional new description.")] string? description = null)
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                var version = new ProjectVersionData { id = version_id.ToString() };

                if (!string.IsNullOrEmpty(name)) version.name = name;
                if (released.HasValue) { version.released = released.Value; version.releasedSpecified = true; }
                if (obsolete.HasValue) { version.obsolete = obsolete.Value; version.obsoleteSpecified = true; }
                if (description != null) version.description = description;

                var success = await client.mc_project_version_updateAsync(_mantisClient.Username, _mantisClient.Token, version_id.ToString(), version);
                return success ? $"Version {version_id} updated successfully!" : $"Failed to update version {version_id}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating version {VersionId}", version_id);
                return $"Error updating version: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Deletes a project version.")]
        public async Task<string> DeleteProjectVersionAsync(
            [Description("The numeric ID of the version to delete.")] int version_id)
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                var success = await client.mc_project_version_deleteAsync(_mantisClient.Username, _mantisClient.Token, version_id.ToString());
                return success ? $"Version {version_id} deleted successfully!" : $"Failed to delete version {version_id}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting version {VersionId}", version_id);
                return $"Error deleting version: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Creates a new category in a project.")]
        public async Task<string> CreateCategoryAsync(
            [Description("The numeric ID of the project.")] int project_id,
            [Description("The name of the new category.")] string category_name)
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                var categoryId = await client.mc_project_add_categoryAsync(_mantisClient.Username, _mantisClient.Token, project_id.ToString(), category_name);
                return $"Category created successfully! ID: {categoryId}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category '{Name}' for project {ProjectId}", category_name, project_id);
                return $"Error creating category: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Deletes a category from a project.")]
        public async Task<string> DeleteCategoryAsync(
            [Description("The numeric ID of the project.")] int project_id,
            [Description("The name of the category to delete.")] string category_name)
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                var result = await client.mc_project_delete_categoryAsync(_mantisClient.Username, _mantisClient.Token, project_id.ToString(), category_name);
                return $"Category '{category_name}' deleted successfully! Result: {result}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category '{Name}' from project {ProjectId}", category_name, project_id);
                return $"Error deleting category: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Renames a category in a project, optionally reassigning it to a user.")]
        public async Task<string> RenameCategoryAsync(
            [Description("The numeric ID of the project.")] int project_id,
            [Description("The current name of the category.")] string category_name,
            [Description("The new name for the category.")] string new_name,
            [Description("Optional username to assign the category to.")] string assigned_to = "")
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                var result = await client.mc_project_rename_category_by_nameAsync(
                    _mantisClient.Username,
                    _mantisClient.Token,
                    project_id.ToString(),
                    category_name,
                    new_name,
                    assigned_to);
                return $"Category renamed successfully! Result: {result}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error renaming category '{Name}' in project {ProjectId}", category_name, project_id);
                return $"Error renaming category: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Returns the available Projection levels in the MantisBT system.")]
        public async Task<string> GetProjectionsAsync()
        {
            return await GetEnumListAsync("projections");
        }

        [McpServerTool]
        [Description("Returns the available ETA levels in the MantisBT system.")]
        public async Task<string> GetEtasAsync()
        {
            return await GetEnumListAsync("etas");
        }

        [McpServerTool]
        [Description("Returns the available Project View States in the MantisBT system.")]
        public async Task<string> GetProjectViewStatesAsync()
        {
            return await GetEnumListAsync("project_view_states");
        }

        [McpServerTool]
        [Description("Returns the available View States (public/private) in the MantisBT system.")]
        public async Task<string> GetViewStatesAsync()
        {
            return await GetEnumListAsync("view_states");
        }

        [McpServerTool]
        [Description("Returns the available Custom Field Types in the MantisBT system.")]
        public async Task<string> GetCustomFieldTypesAsync()
        {
            return await GetEnumListAsync("custom_field_types");
        }

        [McpServerTool]
        [Description("Returns a raw enumeration from the MantisBT system by its name. Use when a specific enumeration is not exposed as a dedicated tool.")]
        public async Task<string> GetEnumAsync(
            [Description("The name of the enumeration to retrieve (e.g., 'status', 'priority', 'severity').")] string enumeration)
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                var result = await client.mc_enum_getAsync(_mantisClient.Username, _mantisClient.Token, enumeration);
                return string.IsNullOrEmpty(result) ? $"Enumeration '{enumeration}' returned no data." : result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving enumeration '{Enumeration}'", enumeration);
                return $"Error retrieving enumeration: {ex.Message}";
            }
        }

        private async Task<string> GetEnumListAsync(string kind)
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                ObjectRef[] values = kind switch
                {
                    "projections" => await client.mc_enum_projectionsAsync(_mantisClient.Username, _mantisClient.Token),
                    "etas" => await client.mc_enum_etasAsync(_mantisClient.Username, _mantisClient.Token),
                    "project_view_states" => await client.mc_enum_project_view_statesAsync(_mantisClient.Username, _mantisClient.Token),
                    "view_states" => await client.mc_enum_view_statesAsync(_mantisClient.Username, _mantisClient.Token),
                    "custom_field_types" => await client.mc_enum_custom_field_typesAsync(_mantisClient.Username, _mantisClient.Token),
                    _ => Array.Empty<ObjectRef>()
                };
                return JsonSerializer.Serialize(values, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving enumeration '{Kind}'", kind);
                return $"Error retrieving enumeration: {ex.Message}";
            }
        }
    }
}

