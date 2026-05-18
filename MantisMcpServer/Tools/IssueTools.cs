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
        private readonly MantisClient _mantisClient;
        private readonly ILogger<IssueTools> _logger;

        public IssueTools(MantisClient mantisClient, ILogger<IssueTools> logger)
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
    }
}



