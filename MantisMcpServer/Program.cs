using MantisMcpServer.Services;
using MantisMcpServer.Tools;
using MantisMcpServer.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Serilog;
using Serilog.Events;
using System.Net.Http.Json;
using System.Reflection;

namespace MantisMcpServer
{
    class Program
    {
        private const string RepoOwner = "eduardoalba";
        private const string RepoName = "mantisbt-mcp";

        static async Task Main(string[] args)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

            // Configuração do Serilog
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "mantisbt-mcp",
                "logs",
                "server.log"
            );

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                // Log para Arquivo (Persistência)
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
                // Log para Console (STDERR para não quebrar o MCP)
                .WriteTo.Console(standardErrorFromLevel: LogEventLevel.Verbose) 
                .CreateLogger();

            try 
            {
                Log.Information("Iniciando MantisBT MCP Server v{Version}...", version);

                // Verificação de atualização em background
                _ = Task.Run(() => CheckForUpdates(version));

                // Importante: Usar EmptyApplicationBuilder para não poluir o STDOUT com logs padrão
                var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings { Args = args });

                // Usa o Serilog como provedor de logs
                builder.Logging.ClearProviders();
                builder.Logging.AddSerilog();
// Carregamento das configurações via Variáveis de Ambiente
var mantisUrl = Environment.GetEnvironmentVariable("MANTIS_URL");
var mantisUser = Environment.GetEnvironmentVariable("MANTIS_USERNAME");
var mantisToken = Environment.GetEnvironmentVariable("MANTIS_TOKEN");
var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

if (string.IsNullOrEmpty(mantisUrl) || string.IsNullOrEmpty(mantisUser) || string.IsNullOrEmpty(mantisToken))
{
    Log.Fatal("ERRO CRÍTICO: As variáveis de ambiente MANTIS_URL, MANTIS_USERNAME e MANTIS_TOKEN devem estar configuradas.");
    return;
}

// Registro do Wrapper da API Mantis
builder.Services.AddSingleton<IMantisClient>(new MantisClient(mantisUrl, mantisUser, mantisToken));

// Registro dos Serviços de Busca e Embedding
builder.Services.AddSingleton<ISearchService, SqliteSearchService>();
if (!string.IsNullOrEmpty(openAiKey))
{
    builder.Services.AddSingleton<IEmbeddingService>(sp => 
        new OpenAIEmbeddingService(openAiKey, "text-embedding-3-small", sp.GetRequiredService<ILogger<OpenAIEmbeddingService>>()));
}

// Configuração do Servidor MCP
builder.Services.AddMcpServer(options => 
{
    options.ServerInfo = new Implementation 
    { 
        Name = "MantisBT-SOAP-MCP", 
        Version = version 
    };
    options.Capabilities ??= new();
    options.Capabilities.Resources = new(); // Habilita suporte a recursos
})
.WithStdioServerTransport() 
.WithToolsFromAssembly()
.WithResourcesFromAssembly(); 

using var host = builder.Build();

// Inicialização do Banco de Dados
using (var scope = host.Services.CreateScope())
{
    var searchService = scope.ServiceProvider.GetRequiredService<ISearchService>();
    await searchService.InitializeAsync();
}

await host.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "O servidor MCP terminou inesperadamente.");
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }

        private static async Task CheckForUpdates(string currentVersion)
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("MantisBT-MCP-Server");
                
                var latestRelease = await client.GetFromJsonAsync<GitHubRelease>(
                    $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest");

                if (latestRelease != null && !string.IsNullOrEmpty(latestRelease.TagName))
                {
                    var latestVersion = latestRelease.TagName.TrimStart('v');
                    if (latestVersion != currentVersion)
                    {
                        var msg = $"UPDATE AVAILABLE: A new version (v{latestVersion}) of MantisBT MCP is available at GitHub! You are currently on v{currentVersion}. Please run the install.ps1 to update.";
                        Log.Warning(msg);
                        // Emitir no STDERR para a IA e usuário verem
                        Console.Error.WriteLine($"\n[UPDATE] {msg}\n");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Falha ao verificar atualizações no GitHub.");
            }
        }

        private record GitHubRelease(string TagName);
    }
}
