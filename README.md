# MantisBT MCP Server (.NET Core)

Este é um servidor [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) que permite que LLMs (como Claude) interajam com o Mantis Bug Tracker através da sua API SOAP (MantisConnect).

## Funcionalidades (Fase 1)
- **Leitura:** Obter detalhes de chamados por ID.
- **Busca:** Pesquisar chamados em projetos.
- **Escrita:** Criar novos chamados e adicionar notas/comentários.
- **Gestão:** Alterar status de chamados.
- **Metadados:** Listar projetos, categorias e mapeamentos do sistema (Status, Prioridades, etc).

## Pré-requisitos
- .NET 10 SDK instalado.
- Credenciais de acesso ao MantisBT (Usuário e Senha).

## Instalação Rápida (Recomendado)
Para facilitar o build e a configuração em diferentes CLIs, execute o script de setup no PowerShell:

```powershell
.\install.ps1
```
O script irá:
1. Compilar o projeto automaticamente.
2. Solicitar suas credenciais de forma segura.
3. Oferecer opções para instalar no **Gemini CLI**, **Claude Code** ou gerar o JSON para o **Claude Desktop**.

---

## Configuração Manual
### 1. Variáveis de Ambiente
O servidor utiliza as seguintes variáveis para se conectar:

- `MANTIS_URL`: URL base do seu Mantis (ex: `https://seu-mantis.com/`)
- `MANTIS_USERNAME`: Seu usuário de login.
- `MANTIS_TOKEN`: Seu Token de API (Personal Access Token) gerado no MantisBT.

### 2. Configuração no Claude Desktop
Para usar este servidor no Claude Desktop, adicione o seguinte ao seu arquivo `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "mantis": {
      "command": "dotnet",
      "args": [
        "run", 
        "--project", "C:/source/personal/mpcs/mantisbt-mcp/MantisMcpServer/MantisMcpServer.csproj", 
        "--no-build"
      ],
      "env": {
        "MANTIS_URL": "https://seu-mantis.com/",
        "MANTIS_USERNAME": "seu_usuario",
        "MANTIS_TOKEN": "seu_token_aqui"
      }
    }
  }
}
```

## Desenvolvimento
Para compilar o projeto manualmente:
```powershell
cd MantisMcpServer
dotnet build
```

Para rodar em modo debug (verificando saída de erro no console):
```powershell
$env:MANTIS_USERNAME="user"; $env:MANTIS_TOKEN="token"; dotnet run
```
