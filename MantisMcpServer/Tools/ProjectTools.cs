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
        private readonly MantisClient _mantisClient;
        private readonly ILogger<ProjectTools> _logger;

        public ProjectTools(MantisClient mantisClient, ILogger<ProjectTools> logger)
        {
            _mantisClient = mantisClient;
            _logger = logger;
        }

        [McpServerTool]
        [Description("Retorna a lista de projetos acessíveis pelo usuário.")]
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
                _logger.LogError(ex, "Erro ao listar projetos");
                return $"Erro ao listar projetos: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Lista as categorias disponíveis para um determinado projeto.")]
        public async Task<string> GetCategoriesAsync(
            [Description("ID do projeto.")] int project_id)
        {
            try
            {
                using var client = _mantisClient.CreateSoapClient();
                var categories = await client.mc_project_get_categoriesAsync(_mantisClient.Username, _mantisClient.Token, project_id.ToString());
                return JsonSerializer.Serialize(categories, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar categorias do projeto {ProjectId}", project_id);
                return $"Erro ao buscar categorias: {ex.Message}";
            }
        }

        [McpServerTool]
        [Description("Retorna os mapeamentos de Status, Prioridades, Severidades e Resoluções.")]
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
                _logger.LogError(ex, "Erro ao buscar enums do sistema");
                return $"Erro ao buscar metadados: {ex.Message}";
            }
        }
    }
}
