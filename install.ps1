# Script de Instalação Automática - MantisBT MCP Server
# Este script compila o projeto e ajuda a registrar o servidor MCP nas CLIs.

$ErrorActionPreference = "Stop"

Write-Host "--- Configuração do MantisBT MCP Server ---" -ForegroundColor Cyan

# 1. Verificar .NET SDK e dotnet-svcutil
try {
    $dotnetVersion = dotnet --version
    Write-Host "[OK] .NET SDK detectado (v$dotnetVersion)" -ForegroundColor Green
} catch {
    Write-Error "O .NET SDK não foi encontrado. Por favor, instale o .NET 10 para continuar."
    exit
}

# 2. Coletar URL para Gerar o Proxy
Write-Host "`n[1/3] Configuração Inicial" -ForegroundColor Yellow
$mantisUrl = Read-Host "Digite a URL do Mantis (Ex: https://seu-mantis.com/)"
if ([string]::IsNullOrWhiteSpace($mantisUrl)) { 
    Write-Error "A URL do Mantis é necessária para gerar o proxy SOAP."
    exit 
}

# 2.5 Gerar Proxy SOAP
Write-Host "`n[2/3] Gerando Proxy SOAP a partir do WSDL..." -ForegroundColor Yellow
$wsdlUrl = $mantisUrl.TrimEnd('/') + "/api/soap/mantisconnect.php?wsdl"
$proxyPath = "MantisMcpServer\ServiceProxy"
$tempWsdl = "temp_mantis.wsdl"

if (-not (Test-Path $proxyPath)) { New-Item -ItemType Directory -Path $proxyPath }

try {
    Write-Host "Baixando e corrigindo encoding do WSDL..." -ForegroundColor Gray
    $response = Invoke-WebRequest -Uri $wsdlUrl -UseBasicParsing
    $wsdlContent = $response.Content
    
    # Uso do operador -replace (case-insensitive) para garantir que pegue 'ISO-8859-1' ou 'iso-8859-1'
    $wsdlContent = $wsdlContent -replace 'encoding="ISO-8859-1"', 'encoding="UTF-8"'
    
    # Salva usando UTF8 sem BOM para evitar problemas com o parser
    $absoluteTempPath = Join-Path (Get-Location) $tempWsdl
    [System.IO.File]::WriteAllText($absoluteTempPath, $wsdlContent, [System.Text.Encoding]::UTF8)
    
    Write-Host "Executando: dotnet-svcutil `"$absoluteTempPath`" ..." -ForegroundColor Gray
    $env:DOTNET_SVCUTIL_TELEMETRY_OPTOUT = 1
    
    # Resolve o caminho absoluto da pasta de destino
    $absoluteProxyPath = (Get-Item $proxyPath).FullName

    # Remove arquivos anteriores se existirem para evitar erro de 'file already exists'
    $oldReference = Join-Path $absoluteProxyPath "Reference.cs"
    $oldParams = Join-Path $absoluteProxyPath "dotnet-svcutil.params.json"
    if (Test-Path $oldReference) { Remove-Item $oldReference -Force }
    if (Test-Path $oldParams) { Remove-Item $oldParams -Force }

    # Executa e captura saída completa
    dotnet-svcutil "$absoluteTempPath" -o Reference.cs -d "$absoluteProxyPath" -n "*,MantisService" --noLogo
    
    if ($LASTEXITCODE -ne 0) { 
        Write-Error "O comando dotnet-svcutil falhou."
        throw "dotnet-svcutil retornou código de erro $LASTEXITCODE" 
    }
    
    Remove-Item $absoluteTempPath -ErrorAction SilentlyContinue
    Write-Host "[OK] Proxy SOAP gerado com sucesso." -ForegroundColor Green
} catch {
    Write-Error "Erro detalhado: $_"
    if (Test-Path $tempWsdl) { 
        Write-Host "`n--- Conteúdo do WSDL baixado (primeiras 5 linhas) ---" -ForegroundColor Gray
        Get-Content $tempWsdl -TotalCount 5
    }
    Remove-Item $tempWsdl -ErrorAction SilentlyContinue
    exit
}

# 3. Build do Projeto
Write-Host "`n[3/3] Compilando o projeto..." -ForegroundColor Yellow
cd MantisMcpServer
dotnet build -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Error "Falha na compilação do projeto."
    exit
}
# Localiza o executável gerado
$exePath = (Get-Item "bin\Release\net10.0\MantisMcpServer.exe").FullName
cd ..

# 4. Coletar Restante das Credenciais
Write-Host "`nConfiguração de Acesso Final" -ForegroundColor Yellow
$mantisUser = Read-Host "Digite seu Usuário"
if ([string]::IsNullOrWhiteSpace($mantisUser)) { Write-Error "Usuário é obrigatório."; exit }

$mantisToken = Read-Host "Digite seu Token de API (Personal Access Token)"
if ([string]::IsNullOrWhiteSpace($mantisToken)) { Write-Error "Token é obrigatório."; exit }

# 5. Opções de Instalação
Write-Host "`n[3/3] Onde deseja instalar o servidor MCP?" -ForegroundColor Yellow
Write-Host "1. Gemini CLI (Comando 'gemini mcp add')"
Write-Host "2. Claude Code (Comando 'claude mcp add')"
Write-Host "3. Claude Desktop (Configuração via JSON)"
Write-Host "4. Apenas gerar comandos para copiar"

$choice = Read-Host "Escolha uma opção (1-4)"

$mcpName = "mantis"
$mcpCmd = "`"$exePath`""
$mcpArgs = ""
$mcpEnv = @{
    "MANTIS_URL" = $mantisUrl
    "MANTIS_USERNAME" = $mantisUser
    "MANTIS_TOKEN" = $mantisToken
}

switch ($choice) {
    "1" {
        Write-Host "`nConfiguração do Gemini CLI" -ForegroundColor Cyan
        $scope = Read-Host "Deseja instalar de forma (1) Local [Projeto] ou (2) Global [Usuário]? (Padrão: 1)"
        $scopeFlag = if ($scope -eq "2") { "user" } else { "project" }
        
        Write-Host "Instalando no Gemini CLI (Escopo: $scopeFlag)..." -ForegroundColor Cyan
        $envArgs = ""
        $mcpEnv.GetEnumerator() | ForEach-Object { $envArgs += " --env $($_.Key)=`"$($_.Value)`"" }
        $fullCmd = "gemini mcp add $mcpName $mcpCmd --scope $scopeFlag -- $envArgs"
        Invoke-Expression $fullCmd
        Write-Host "[Sucesso] Servidor adicionado ao Gemini CLI ($scopeFlag)!" -ForegroundColor Green
    }
    "2" {
        Write-Host "`nInstalando no Claude Code..." -ForegroundColor Cyan
        Write-Host "Nota: O Claude Code requer que as variáveis de ambiente estejam acessíveis."
        $fullCmd = "claude mcp add $mcpName -- $mcpCmd"
        Write-Host "Comando sugerido: $fullCmd"
        Write-Host "Certifique-se de configurar as variáveis MANTIS_URL, MANTIS_USERNAME e MANTIS_TOKEN no seu ambiente."
    }
    "3" {
        Write-Host "`nGerando JSON para Claude Desktop..." -ForegroundColor Cyan
        $jsonObj = @{
            "mcpServers" = @{
                "mantis" = @{
                    "command" = $exePath.Replace("\", "/")
                    "args" = @()
                    "env" = $mcpEnv
                }
            }
        }
        $jsonStr = $jsonObj | ConvertTo-Json -Depth 10
        Write-Host "Adicione este bloco ao seu arquivo claude_desktop_config.json:" -ForegroundColor White
        Write-Host $jsonStr -ForegroundColor Gray
    }
    "4" {
        Write-Host "`nComandos para cópia manual:" -ForegroundColor Cyan
        Write-Host "GEMINI: gemini mcp add $mcpName $mcpCmd --env MANTIS_URL=`"$mantisUrl`" --env MANTIS_USERNAME=`"$mantisUser`" --env MANTIS_TOKEN=`"$mantisToken`""
        Write-Host "CLAUDE: claude mcp add $mcpName -- $mcpCmd"
    }
}

Write-Host "`n--- Setup Concluído ---" -ForegroundColor Green
Write-Host "Dica: Use '/mcp reload' ou reinicie sua CLI para ativar o servidor."
