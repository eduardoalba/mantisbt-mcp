using System.ComponentModel;
using System.Text.Json;
using MantisMcpServer.Services;
using MantisService;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;

namespace MantisMcpServer.Tools
{
    [McpServerToolType]
    public class IssueTools
    {
        private readonly IMantisClient _mantisClient;
        private readonly ILogger<IssueTools> _logger;

        public IssueTools(IMantisClient mantisClient, ILogger<IssueTools> logger)
        {
            _mantisClient = mantisClient;
            _logger = logger;
        }

        [McpServerTool]
        [Description("Retrieves all details of a specific MantisBT issue (bug) by its numeric ID. This includes notes (comments), relationships, tags, and attachments.")]
        public async Task<string> GetIssueAsync(
            [Description("The numeric ID of the issue (bug) in MantisBT. Example: 123")] int issue_id)
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                var issue = await client.mc_issue_getAsync(_mantisClient.Username, _mantisClient.Token, issue_id.ToString());
                return JsonSerializer.Serialize(issue, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving issue {IssueId}", issue_id);
                return $"Error retrieving issue {issue_id}: {ex.Message}. Make sure the ID is correct and you have permission to view it.";
            }
        }

        [McpServerTool]
        [Description("Searches for issues within a specific MantisBT project with pagination support. Useful for listing recent bugs or checking project status.")]
        public async Task<string> SearchIssuesAsync(
            [Description("The numeric ID of the project.")] int project_id,
            [Description("The page number to retrieve (starts at 1).")] int page_number = 1,
            [Description("Number of issues to return per page (default is 50).")] int per_page = 50)
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                var issues = await client.mc_project_get_issuesAsync(
                    _mantisClient.Username, 
                    _mantisClient.Token, 
                    project_id.ToString(), 
                    page_number.ToString(), 
                    per_page.ToString());
                
                return JsonSerializer.Serialize(issues, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching issues in project {ProjectId}", project_id);
                return $"Error searching issues in project {project_id}: {ex.Message}. Check if the project ID exists.";
            }
        }

        [McpServerTool]
        [Description("Searches for issues across projects or within a specific project using a text search string.")]
        public async Task<string> SearchIssuesByTextAsync(
            [Description("The text to search for in issues (summary, description, notes, etc.).")] string search_text,
            [Description("Optional numeric ID of the project to limit the search.")] int? project_id = null,
            [Description("The page number to retrieve (starts at 1).")] int page_number = 1,
            [Description("Number of issues to return per page (default is 50).")] int per_page = 50)
        {
            try
            {
                var filter = new FilterSearchData
                {
                    search = search_text
                };

                if (project_id.HasValue)
                {
                    filter.project_id = new[] { project_id.Value.ToString() };
                }

                using var client = _mantisClient.CreateSoapClient();
                var issues = await client.mc_filter_search_issuesAsync(
                    _mantisClient.Username,
                    _mantisClient.Token,
                    filter,
                    page_number.ToString(),
                    per_page.ToString());

                return JsonSerializer.Serialize(issues, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching issues for text: {SearchText}", search_text);
                return $"Error searching issues: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Retrieves all issues assigned to the current user in a specific project or all accessible projects.")]
        public async Task<string> GetMyIssuesAsync(
            [Description("Optional numeric ID of the project. If not provided, it will search across all accessible projects.")] int? project_id = null,
            [Description("The page number to retrieve (starts at 1).")] int page_number = 1,
            [Description("Number of issues to return per page (default is 50).")] int per_page = 50)
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                
                // Get current user info to get the ID
                var userData = await client.mc_loginAsync(_mantisClient.Username, _mantisClient.Token);
                var currentUser = userData.account_data;

                // filter_type: "assigned" means issues assigned to the user
                // Other options in Mantis: "reporter", "monitored", "unassigned"
                string filterType = "assigned";
                string projectIdStr = project_id?.ToString() ?? "0"; // 0 often means all projects in Mantis SOAP

                var issues = await client.mc_project_get_issues_for_userAsync(
                    _mantisClient.Username,
                    _mantisClient.Token,
                    projectIdStr,
                    filterType,
                    currentUser,
                    page_number.ToString(),
                    per_page.ToString());

                return JsonSerializer.Serialize(issues, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving assigned issues for user {Username}", _mantisClient.Username);
                return $"Error retrieving issues: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Creates a new issue (bug report) in a specific MantisBT project.")]
        public async Task<string> CreateIssueAsync(
            [Description("The numeric ID of the target project.")] int project_id,
            [Description("A brief title or summary of the issue.")] string summary,
            [Description("A detailed description of the problem, including steps to reproduce.")] string description,
            [Description("The category name for the issue (must exist in the project). Use GetCategories to find valid names.")] string category,
            [Description("Priority ID (e.g., 10=none, 20=low, 30=normal, 40=high, 50=urgent). Default is 30.")] int priority_id = 30,
            [Description("Severity ID (e.g., 10=feature, 20=trivial, 30=text, 40=tweak, 50=minor, 60=major, 70=crash, 80=block). Default is 50.")] int severity_id = 50)
        {
            try
            {
                var issue = new IssueData
                {
                    project = new ObjectRef { id = project_id.ToString() },
                    summary = summary,
                    description = description,
                    category = category,
                    priority = new ObjectRef { id = priority_id.ToString() },
                    severity = new ObjectRef { id = severity_id.ToString() }
                };

                using var client = _mantisClient.CreateSoapClient();
                var issueId = await client.mc_issue_addAsync(_mantisClient.Username, _mantisClient.Token, issue);
                return $"Issue created successfully! ID: {issueId}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating issue in project {ProjectId}", project_id);
                return $"Error creating issue: {ex.Message}. Ensure the category exists and project ID is valid.";
            }
        }

        [McpServerTool]
        [Description("Adds a new note (comment) to an existing MantisBT issue.")]
        public async Task<string> AddNoteAsync(
            [Description("The numeric ID of the issue.")] int issue_id,
            [Description("The text content of the note/comment.")] string text,
            [Description("Whether the note should be private (only visible to authorized users).")] bool is_private = false)
        {
            try
            {
                var note = new IssueNoteData
                {
                    text = text,
                    view_state = new ObjectRef { id = is_private ? "50" : "10" } // 10=public, 50=private
                };

                using var client = _mantisClient.CreateSoapClient();
                var noteId = await client.mc_issue_note_addAsync(_mantisClient.Username, _mantisClient.Token, issue_id.ToString(), note);
                return $"Note added successfully! ID: {noteId}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding note to issue {IssueId}", issue_id);
                return $"Error adding note: {ex.Message}. Check if the issue ID is valid.";
            }
        }

        [McpServerTool]
        [Description("Updates the status, resolution, or adds a note to an existing MantisBT issue. Useful for progressing workflows.")]
        public async Task<string> UpdateIssueStatusAsync(
            [Description("The numeric ID of the issue.")] int issue_id,
            [Description("The numeric ID of the new status (e.g., 10=new, 20=feedback, 30=acknowledged, 40=confirmed, 50=assigned, 80=resolved, 90=closed).")] int status_id,
            [Description("Optional numeric ID for the resolution (e.g., 10=open, 20=fixed, 30=reopened, 40=unable to duplicate, 50=not fixable, 60=duplicate, 70=not a bug, 80=suspended, 90=won't fix).")] int? resolution_id = null,
            [Description("Optional note to explain the status change.")] string? note = null)
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                
                // Fetch the issue to maintain existing data
                var issue = await client.mc_issue_getAsync(_mantisClient.Username, _mantisClient.Token, issue_id.ToString());
                
                issue.status = new ObjectRef { id = status_id.ToString() };
                if (resolution_id.HasValue)
                {
                    issue.resolution = new ObjectRef { id = resolution_id.Value.ToString() };
                }

                if (!string.IsNullOrEmpty(note))
                {
                    var noteList = issue.notes?.ToList() ?? new List<IssueNoteData>();
                    noteList.Add(new IssueNoteData { text = note });
                    issue.notes = noteList.ToArray();
                }

                var success = await client.mc_issue_updateAsync(_mantisClient.Username, _mantisClient.Token, issue_id.ToString(), issue);
                return success ? "Issue status updated successfully!" : "Failed to update issue status.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for issue {IssueId}", issue_id);
                return $"Error updating status: {ex.Message}. Ensure the status ID and issue ID are valid.";
            }
        }

        [McpServerTool]
        [Description("Retrieves the full history of changes for a specific issue (auditing).")]
        public async Task<string> GetIssueHistoryAsync(
            [Description("The numeric ID of the issue.")] int issue_id)
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                var history = await client.mc_issue_get_historyAsync(_mantisClient.Username, _mantisClient.Token, issue_id.ToString());
                return JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving history for issue {IssueId}", issue_id);
                return $"Error retrieving history: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Adds a relationship between two issues (e.g., child of, parent of, related to, duplicate of).")]
        public async Task<string> AddIssueRelationshipAsync(
            [Description("The source issue ID.")] int issue_id,
            [Description("The target issue ID.")] int target_issue_id,
            [Description("The relationship type ID (1=duplicate, 2=related, 3=parent, 4=child).")] int type_id)
        {
            try
            {
                var relationship = new RelationshipData
                {
                    target_id = target_issue_id.ToString(),
                    type = new ObjectRef { id = type_id.ToString() }
                };

                using var client = _mantisClient.CreateSoapClient();
                var relationshipId = await client.mc_issue_relationship_addAsync(_mantisClient.Username, _mantisClient.Token, issue_id.ToString(), relationship);
                return $"Relationship added successfully! ID: {relationshipId}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding relationship to issue {IssueId}", issue_id);
                return $"Error adding relationship: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Removes an existing relationship from an issue.")]
        public async Task<string> DeleteIssueRelationshipAsync(
            [Description("The numeric ID of the issue.")] int issue_id,
            [Description("The numeric ID of the relationship to delete.")] int relationship_id)
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                var success = await client.mc_issue_relationship_deleteAsync(_mantisClient.Username, _mantisClient.Token, issue_id.ToString(), relationship_id.ToString());
                return success ? "Relationship deleted successfully!" : "Failed to delete relationship.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting relationship {RelId} from issue {IssueId}", relationship_id, issue_id);
                return $"Error deleting relationship: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Lists all available tags in the MantisBT system with pagination.")]
        public async Task<string> GetTagsAsync(
            [Description("The page number to retrieve (starts at 1).")] int page_number = 1,
            [Description("Number of tags per page (default 50).")] int per_page = 50)
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                var result = await client.mc_tag_get_allAsync(_mantisClient.Username, _mantisClient.Token, page_number.ToString(), per_page.ToString());
                return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tags");
                return $"Error retrieving tags: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Sets the tags for a specific issue. This overwrites existing tags on the issue.")]
        public async Task<string> SetIssueTagsAsync(
            [Description("The numeric ID of the issue.")] int issue_id,
            [Description("Comma-separated list of tag names to set.")] string tag_names)
        {
            try
            {
                var tags = tag_names.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => new TagData { name = t.Trim() })
                    .ToArray();

                using var client = _mantisClient.CreateSoapClient();
                var success = await client.mc_issue_set_tagsAsync(_mantisClient.Username, _mantisClient.Token, issue_id.ToString(), tags);
                return success ? "Issue tags updated successfully!" : "Failed to update issue tags.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting tags for issue {IssueId}", issue_id);
                return $"Error setting tags: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Downloads the content of an attachment as a Base64 string.")]
        public async Task<string> GetAttachmentAsync(
            [Description("The numeric ID of the attachment.")] int attachment_id)
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                var content = await client.mc_issue_attachment_getAsync(_mantisClient.Username, _mantisClient.Token, attachment_id.ToString());
                return Convert.ToBase64String(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving attachment {AttachmentId}", attachment_id);
                return $"Error retrieving attachment: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Uploads a file as an attachment to a specific issue.")]
        public async Task<string> CreateAttachmentAsync(
            [Description("The numeric ID of the issue.")] int issue_id,
            [Description("The filename (e.g., 'screenshot.png').")] string name,
            [Description("The MIME type (e.g., 'image/png').")] string file_type,
            [Description("The file content encoded in Base64.")] string content_base64)
        {
            try
            {
                var content = Convert.FromBase64String(content_base64);
                using var client = _mantisClient.CreateSoapClient();
                var attachmentId = await client.mc_issue_attachment_addAsync(
                    _mantisClient.Username, 
                    _mantisClient.Token, 
                    issue_id.ToString(), 
                    name, 
                    file_type, 
                    content);
                
                return $"Attachment uploaded successfully! ID: {attachmentId}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading attachment for issue {IssueId}", issue_id);
                return $"Error uploading attachment: {ex.Message}";
            }
        }
    }
}



