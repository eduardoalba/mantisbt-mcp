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
    # MantisBT costuma declarar ISO-8859-1 mas enviar UTF-8, o que quebra o dotnet-svcutil.
    $response = Invoke-WebRequest -Uri $wsdlUrl -UseBasicParsing
    $wsdlContent = $response.Content
    $wsdlContent = $wsdlContent -replace 'encoding="ISO-8859-1"', 'encoding="UTF-8"'
    
    $absoluteTempPath = Join-Path (Get-Location) $tempWsdl
    [System.IO.File]::WriteAllText($absoluteTempPath, $wsdlContent, [System.Text.Encoding]::UTF8)
    
    Write-Host "Executando: dotnet-svcutil `"$absoluteTempPath`" ..." -ForegroundColor Gray
    $env:DOTNET_SVCUTIL_TELEMETRY_OPTOUT = 1
    
    $absoluteProxyPath = (Get-Item $proxyPath).FullName
    $oldReference = Join-Path $absoluteProxyPath "Reference.cs"
    $oldParams = Join-Path $absoluteProxyPath "dotnet-svcutil.params.json"
    if (Test-Path $oldReference) { Remove-Item $oldReference -Force }
    if (Test-Path $oldParams) { Remove-Item $oldParams -Force }

    dotnet-svcutil "$absoluteTempPath" -o Reference.cs -d "$absoluteProxyPath" -n "*,MantisService" --noLogo
    
    if ($LASTEXITCODE -ne 0) { throw "dotnet-svcutil falhou com código $LASTEXITCODE" }
    
    Remove-Item $absoluteTempPath -ErrorAction SilentlyContinue
    Write-Host "[OK] Proxy SOAP gerado com sucesso." -ForegroundColor Green
} catch {
    Write-Error "Erro detalhado: $_"
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
$exePath = (Get-Item "bin\Release\net10.0\MantisMcpServer.exe").FullName
cd ..

# 4. Coletar Restante das Credenciais
Write-Host "`nConfiguração de Acesso Final" -ForegroundColor Yellow
$mantisUser = Read-Host "Digite seu Usuário"
if ([string]::IsNullOrWhiteSpace($mantisUser)) { Write-Error "Usuário é obrigatório."; exit }

$mantisToken = Read-Host "Digite seu Token de API (Personal Access Token)"
if ([string]::IsNullOrWhiteSpace($mantisToken)) { Write-Error "Token é obrigatório."; exit }

# 5. Opções de Instalação
Write-Host "`nOnde deseja instalar o servidor MCP?" -ForegroundColor Yellow
Write-Host "1. Gemini CLI (Comando 'gemini mcp add')"
Write-Host "2. Claude Code (Comando 'claude mcp add')"
Write-Host "3. Claude Desktop (Configuração via JSON)"
Write-Host "4. Apenas gerar comandos para copiar"

$choice = Read-Host "Escolha uma opção (1-4)"

$mcpName = "mantis"
$mcpCmd = "`"$exePath`""
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
        $fullCmd = "gemini mcp add $mcpName $mcpCmd $envArgs --scope $scopeFlag"
        Invoke-Expression $fullCmd
        Write-Host "[Sucesso] Servidor adicionado ao Gemini CLI!" -ForegroundColor Green
    }
    "2" {
        Write-Host "`nInstalando no Claude Code..." -ForegroundColor Cyan
        try {
            $envArgs = ""
            $mcpEnv.GetEnumerator() | ForEach-Object { $envArgs += " --env $($_.Key)=`"$($_.Value)`"" }
            $fullCmd = "claude mcp add $mcpName -- $mcpCmd $envArgs"
            Invoke-Expression $fullCmd
            Write-Host "[Sucesso] Servidor adicionado ao Claude Code!" -ForegroundColor Green
        } catch {
            Write-Warning "Não foi possível automatizar para o Claude Code. Tente manualmente:"
            Write-Host "Comando: claude mcp add $mcpName -- $mcpCmd $envArgs"
        }
    }
    "3" {
        Write-Host "`nConfigurando Claude Desktop..." -ForegroundColor Cyan
        $claudeConfigPath = "$env:AppData\Claude\claude_desktop_config.json"
        
        $serverObj = @{
            "command" = $exePath.Replace("\", "/")
            "args" = @()
            "env" = $mcpEnv
        }

        try {
            if (Test-Path $claudeConfigPath) {
                $config = Get-Content $claudeConfigPath -Raw | ConvertFrom-Json
            } else {
                $config = New-Object PSObject -Property @{ mcpServers = New-Object PSObject }
            }

            if (-not $config.PSObject.Properties['mcpServers']) {
                $config | Add-Member -MemberType NoteProperty -Name "mcpServers" -Value (New-Object PSObject)
            }

            # Adiciona ou atualiza o nó mantis sem tocar nos outros
            $config.mcpServers | Add-Member -MemberType NoteProperty -Name $mcpName -Value $serverObj -Force
            
            $jsonStr = $config | ConvertTo-Json -Depth 10
            $jsonStr | Set-Content $claudeConfigPath -Encoding UTF8
            Write-Host "[Sucesso] Arquivo de configuração do Claude Desktop atualizado!" -ForegroundColor Green
        } catch {
            Write-Error "Falha ao atualizar o arquivo do Claude Desktop. $_"
            Write-Host "Adicione este bloco manualmente em $claudeConfigPath :" -ForegroundColor Gray
            Write-Host "`"$mcpName`": $($serverObj | ConvertTo-Json -Depth 10)"
        }
    }
    "4" {
        Write-Host "`nComandos para cópia manual:" -ForegroundColor Cyan
        Write-Host "GEMINI: gemini mcp add $mcpName $mcpCmd --env MANTIS_URL=`"$mantisUrl`" --env MANTIS_USERNAME=`"$mantisUser`" --env MANTIS_TOKEN=`"$mantisToken`" --scope project"
        Write-Host "CLAUDE: claude mcp add $mcpName -- $mcpCmd"
    }
}

Write-Host "`n--- Setup Concluído ---" -ForegroundColor Green
Write-Host "Dica: Use '/mcp reload' ou reinicie sua CLI para ativar o servidor."
