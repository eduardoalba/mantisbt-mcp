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
        [Description("Recupera todos os detalhes de um chamado específico pelo seu ID, incluindo notas, relacionamentos, tags e anexos.")]
        public async Task<string> GetIssueAsync(
            [Description("O ID numérico do chamado no MantisBT.")] int issue_id)
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                var issue = await client.mc_issue_getAsync(_mantisClient.Username, _mantisClient.Token, issue_id.ToString());
                return JsonSerializer.Serialize(issue, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar chamado {IssueId}", issue_id);
                return $"Erro ao buscar chamado: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Realiza uma busca de chamados em um projeto com paginação.")]
        public async Task<string> SearchIssuesAsync(
            [Description("ID do projeto.")] int project_id,
            [Description("Número da página (padrão: 1).")] int page_number = 1,
            [Description("Registros por página (padrão: 50).")] int per_page = 50)
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
                _logger.LogError(ex, "Erro ao pesquisar chamados no projeto {ProjectId}", project_id);
                return $"Erro ao pesquisar chamados: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Abre um novo chamado no sistema.")]
        public async Task<string> CreateIssueAsync(
            [Description("ID do projeto de destino.")] int project_id,
            [Description("Título/Resumo curto do chamado.")] string summary,
            [Description("Descrição detalhada do problema.")] string description,
            [Description("Nome da categoria associada.")] string category,
            [Description("ID da prioridade.")] int priority_id = 30, // Normal
            [Description("ID da severidade.")] int severity_id = 50) // Major
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
                return $"Chamado criado com sucesso! ID: {issueId}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar chamado no projeto {ProjectId}", project_id);
                return $"Erro ao criar chamado: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Adiciona um comentário a um chamado existente.")]
        public async Task<string> AddNoteAsync(
            [Description("ID do chamado.")] int issue_id,
            [Description("Conteúdo do comentário/nota.")] string text,
            [Description("Se true, a nota será privada.")] bool is_private = false)
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
                return $"Nota adicionada com sucesso! ID: {noteId}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adicionar nota ao chamado {IssueId}", issue_id);
                return $"Erro ao adicionar nota: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Altera o status de um chamado.")]
        public async Task<string> UpdateIssueStatusAsync(
            [Description("ID do chamado.")] int issue_id,
            [Description("ID numérico do novo status.")] int status_id,
            [Description("ID da resolução (opcional).")] int? resolution_id = null,
            [Description("Nota opcional para registrar o motivo da mudança.")] string? note = null)
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                
                // Primeiro buscamos o chamado atual para não perder dados na atualização
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
                return success ? "Status atualizado com sucesso!" : "Falha ao atualizar o status.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar status do chamado {IssueId}", issue_id);
                return $"Erro ao atualizar status: {ex.Message}";
            }
        }
    }
}


