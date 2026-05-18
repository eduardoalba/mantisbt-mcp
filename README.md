# MantisBT MCP Server (.NET)

A [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) server that enables AI agents (like Claude, Gemini, and Codex) to interact with the [Mantis Bug Tracker](https://www.mantisbt.org/) via its SOAP API (MantisConnect).

## 🚀 Features

- **Issue Management:** Read details, search within projects, create new issues, and add notes.
- **Workflow Control:** Update status and resolution of issues.
- **Metadata Discovery:** List accessible projects, categories, and system enumeration levels (Status, Priority, Severity, etc.).
- **Smart DX:** High-quality tool descriptions and error handling tailored for LLM consumption.
- **Persistent Logging:** Detailed logs stored in `%AppData%\mantisbt-mcp\logs` for easy troubleshooting.

## 🛠️ Prerequisites

- **.NET 10 SDK** (or later) installed.
- **dotnet-svcutil** tool: `dotnet tool install -g dotnet-svcutil`
- **MantisBT Access:** A valid URL, Username, and Personal Access Token (API Token).

## 📦 Fast Installation (Recommended)

The easiest way to set up the server is using the automated PowerShell script. It handles proxy generation, compilation, and registration across multiple platforms.

```powershell
.\install.ps1
```

The script will:
1. Generate the SOAP Proxy (fixing common MantisBT encoding issues).
2. Compile the project in Release mode.
3. Securely collect your credentials.
4. **Automate installation** for:
   - **Gemini CLI** (`gemini mcp add`)
   - **Claude Code** (`claude mcp add`)
   - **Claude Desktop** (`config.json`)
   - **Codex CLI** (`codex mcp add`)
   - **Codex Desktop** (`config.toml`)

## 🔧 Manual Configuration

If you prefer to configure the server manually, use the following environment variables:

- `MANTIS_URL`: The base URL of your MantisBT instance (e.g., `https://mantis.yourcompany.com/`).
- `MANTIS_USERNAME`: Your login username.
- `MANTIS_TOKEN`: Your Personal Access Token.

### Claude Desktop Example (`claude_desktop_config.json`)

```json
{
  "mcpServers": {
    "mantis": {
      "command": "C:/path/to/MantisMcpServer.exe",
      "env": {
        "MANTIS_URL": "https://your-mantis.com/",
        "MANTIS_USERNAME": "your_user",
        "MANTIS_TOKEN": "your_api_token"
      }
    }
  }
}
```

## 🔍 Troubleshooting

- **Logs:** If the tools are not appearing or failing, check the logs at `%AppData%\mantisbt-mcp\logs\server.log`.
- **SOAP Errors:** The server logs raw SOAP exceptions. Ensure your API Token has sufficient permissions for the requested project/action.
- **Encoding:** Some MantisBT versions declare ISO-8859-1 but send UTF-8. The `install.ps1` script fixes this during proxy generation.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request or open an issue for bugs and feature requests.

## 📄 License

This project is licensed under the [MIT License](LICENSE).
