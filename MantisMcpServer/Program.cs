using MantisMcpServer.Services;
using MantisMcpServer.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MantisMcpServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Importante: Usar EmptyApplicationBuilder para não poluir o STDOUT com logs padrão
            var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings { Args = args });

            // Redireciona Logs para o STDERR (obrigatório para transporte STDIO)
            builder.Logging.AddConsole(options =>
            {
                options.LogToStandardErrorThreshold = LogLevel.Trace;
            });

            // Carregamento das configurações via Variáveis de Ambiente
            var mantisUrl = Environment.GetEnvironmentVariable("MANTIS_URL");
            var mantisUser = Environment.GetEnvironmentVariable("MANTIS_USERNAME");
            var mantisToken = Environment.GetEnvironmentVariable("MANTIS_TOKEN");

            if (string.IsNullOrEmpty(mantisUrl) || string.IsNullOrEmpty(mantisUser) || string.IsNullOrEmpty(mantisToken))
            {
                Console.Error.WriteLine("ERRO CRÍTICO: As variáveis de ambiente MANTIS_URL, MANTIS_USERNAME e MANTIS_TOKEN devem estar configuradas.");
                return;
            }

            // Registro do Wrapper da API Mantis
            builder.Services.AddSingleton(new MantisClient(mantisUrl, mantisUser, mantisToken));

            // Configuração do Servidor MCP
            builder.Services.AddMcpServer(options => 
            {
                options.ServerInfo = new Implementation 
                { 
                    Name = "MantisBT-SOAP-MCP", 
                    Version = "1.0.0" 
                };
            })
            .WithStdioServerTransport() // Nome correto do método
            .WithToolsFromAssembly(); 

            using var host = builder.Build();
            await host.RunAsync();
        }
    }
}
