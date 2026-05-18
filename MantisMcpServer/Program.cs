using MantisMcpServer.Services;
using MantisMcpServer.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Serilog;
using Serilog.Events;

namespace MantisMcpServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
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
                Log.Information("Iniciando MantisBT MCP Server...");

                // Importante: Usar EmptyApplicationBuilder para não poluir o STDOUT com logs padrão
                var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings { Args = args });

                // Usa o Serilog como provedor de logs
                builder.Logging.ClearProviders();
                builder.Logging.AddSerilog();

                // Carregamento das configurações via Variáveis de Ambiente
                var mantisUrl = Environment.GetEnvironmentVariable("MANTIS_URL");
                var mantisUser = Environment.GetEnvironmentVariable("MANTIS_USERNAME");
                var mantisToken = Environment.GetEnvironmentVariable("MANTIS_TOKEN");

                if (string.IsNullOrEmpty(mantisUrl) || string.IsNullOrEmpty(mantisUser) || string.IsNullOrEmpty(mantisToken))
                {
                    Log.Fatal("ERRO CRÍTICO: As variáveis de ambiente MANTIS_URL, MANTIS_USERNAME e MANTIS_TOKEN devem estar configuradas.");
                    return;
                }

                // Registro do Wrapper da API Mantis
                builder.Services.AddSingleton<IMantisClient>(new MantisClient(mantisUrl, mantisUser, mantisToken));

                // Configuração do Servidor MCP
                builder.Services.AddMcpServer(options => 
                {
                    options.ServerInfo = new Implementation 
                    { 
                        Name = "MantisBT-SOAP-MCP", 
                        Version = "1.0.0" 
                    };
                })
                .WithStdioServerTransport() 
                .WithToolsFromAssembly(); 

                using var host = builder.Build();
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
    }
}

