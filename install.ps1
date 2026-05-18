# Automatic Installation Script - MantisBT MCP Server
# This script compiles the project and helps register the MCP server in CLIs.

$ErrorActionPreference = "Stop"

$repoOwner = "eduardoalba"
$repoName = "mantisbt-mcp"

Write-Host "--- MantisBT MCP Server Configuration ---" -ForegroundColor Cyan

# 0. Check for pre-compiled binary (User Mode)
$exePath = ""
$binaryInRoot = Join-Path (Get-Location) "MantisMcpServer.exe"
$binaryInBuild = Join-Path (Get-Location) "MantisMcpServer\bin\Release\net10.0\MantisMcpServer.exe"

if (Test-Path $binaryInRoot) {
    $exePath = (Get-Item $binaryInRoot).FullName
    Write-Host "[INFO] Binary found in root. Starting quick configuration mode." -ForegroundColor Green
} elseif (Test-Path $binaryInBuild) {
    $exePath = (Get-Item $binaryInBuild).FullName
    Write-Host "[INFO] Compiled binary detected. Skipping build." -ForegroundColor Green
} else {
    Write-Host "`n[?] Binary not found locally. Would you like to download the latest release from GitHub? (Y/N)" -ForegroundColor Yellow
    $downloadChoice = Read-Host "Default is Y"
    if ($downloadChoice -ne "N" -and $downloadChoice -ne "n") {
        try {
            Write-Host "Fetching latest release info from GitHub..." -ForegroundColor Gray
            $uri = "https://api.github.com/repos/$repoOwner/$repoName/releases/latest"
            $releaseInfo = Invoke-RestMethod -Uri $uri -UseBasicParsing
            $asset = $releaseInfo.assets | Where-Object { $_.name -like "*-Win64.zip" } | Select-Object -First 1
            
            if ($null -eq $asset) { throw "Could not find a valid Win64.zip asset in the latest release." }
            
            $zipPath = Join-Path (Get-Location) "MantisMcpServer-Latest.zip"
            Write-Host "Downloading version $($releaseInfo.tag_name)..." -ForegroundColor Cyan
            Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zipPath -UseBasicParsing
            
            Write-Host "Extracting files..." -ForegroundColor Gray
            Expand-Archive -Path $zipPath -DestinationPath (Get-Location) -Force
            Remove-Item $zipPath
            
            if (Test-Path $binaryInRoot) {
                $exePath = (Get-Item $binaryInRoot).FullName
                Write-Host "[Success] Latest version downloaded and extracted!" -ForegroundColor Green
            }
        } catch {
            Write-Warning "Failed to download from GitHub: $_"
            Write-Host "Falling back to Developer mode (Build required)..." -ForegroundColor Gray
        }
    }
}

if ([string]::IsNullOrWhiteSpace($exePath)) {
    Write-Host "`n[DEBUG] Proceeding to Developer mode (Build required)..." -ForegroundColor Gray
    
    # 1. Check .NET SDK and dotnet-svcutil
    try {
        $dotnetVersion = dotnet --version
        Write-Host "[OK] .NET SDK detected (v$dotnetVersion)" -ForegroundColor Green
    } catch {
        Write-Error ".NET SDK not found. Please install the .NET 10 SDK or ensure you are running this script in a folder with MantisMcpServer.exe."
        exit
    }


    # 2. Collect URL for Proxy Generation
    Write-Host "`n[1/3] Initial Configuration" -ForegroundColor Yellow
    $mantisUrl = Read-Host "Enter your Mantis URL (e.g., https://your-mantis.com/)"
    if ([string]::IsNullOrWhiteSpace($mantisUrl)) { 
        Write-Error "Mantis URL is required to generate the SOAP proxy."
        exit 
    }

    # 2.5 Generate SOAP Proxy
    Write-Host "`n[2/3] Generating SOAP Proxy from WSDL..." -ForegroundColor Yellow
    $wsdlUrl = $mantisUrl.TrimEnd('/') + "/api/soap/mantisconnect.php?wsdl"
    $proxyPath = "MantisMcpServer\ServiceProxy"
    $tempWsdl = "temp_mantis.wsdl"

    if (-not (Test-Path $proxyPath)) { New-Item -ItemType Directory -Path $proxyPath }

    try {
        Write-Host "Downloading and fixing WSDL encoding..." -ForegroundColor Gray
        $response = Invoke-WebRequest -Uri $wsdlUrl -UseBasicParsing
        $wsdlContent = $response.Content
        $wsdlContent = $wsdlContent -replace 'encoding="ISO-8859-1"', 'encoding="UTF-8"'
        
        $absoluteTempPath = Join-Path (Get-Location) $tempWsdl
        [System.IO.File]::WriteAllText($absoluteTempPath, $wsdlContent, [System.Text.Encoding]::UTF8)
        
        Write-Host "Executing: dotnet-svcutil `"$absoluteTempPath`" ..." -ForegroundColor Gray
        $env:DOTNET_SVCUTIL_TELEMETRY_OPTOUT = 1
        
        $absoluteProxyPath = (Get-Item $proxyPath).FullName
        dotnet-svcutil "$absoluteTempPath" -o Reference.cs -d "$absoluteProxyPath" -n "*,MantisService" --noLogo
        
        if ($LASTEXITCODE -ne 0) { throw "dotnet-svcutil failed with exit code $LASTEXITCODE" }
        Remove-Item $absoluteTempPath -ErrorAction SilentlyContinue
    } catch {
        Write-Error "Error generating proxy: $_"
        exit
    }

    # 3. Project Build
    Write-Host "`n[3/3] Compiling the project..." -ForegroundColor Yellow
    cd MantisMcpServer
    dotnet build -c Release
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Project compilation failed."
        exit
    }
    $exePath = (Get-Item "bin\Release\net10.0\MantisMcpServer.exe").FullName
    cd ..
}

# 4. Collect Credentials
Write-Host "`nMantis Access Configuration" -ForegroundColor Yellow
if ([string]::IsNullOrWhiteSpace($mantisUrl)) {
    $mantisUrl = Read-Host "Enter your Mantis URL (e.g., https://your-mantis.com/)"
}
$mantisUser = Read-Host "Enter your Username"
$mantisToken = Read-Host "Enter your API Token (Personal Access Token)"

if ([string]::IsNullOrWhiteSpace($mantisUrl) -or [string]::IsNullOrWhiteSpace($mantisUser) -or [string]::IsNullOrWhiteSpace($mantisToken)) {
    Write-Error "All fields are required."; exit
}

# 5. Installation Options
Write-Host "`nWhere would you like to install the MCP server?" -ForegroundColor Yellow
Write-Host "You can select multiple options separated by commas (e.g., 1,2,4)" -ForegroundColor Gray
Write-Host "1. Gemini CLI (Automatic via 'gemini mcp add')"
Write-Host "2. Claude Code (Automatic via 'claude mcp add')"
Write-Host "3. Codex (Automatic via 'codex mcp add' - Works for CLI & Desktop)"
Write-Host "4. Claude Desktop (Manual instructions)"
Write-Host "5. Generate all copy-paste commands"

$choicesInput = Read-Host "Select options (1-5)"
$selectedChoices = $choicesInput.Split(',') | ForEach-Object { $_.Trim() }

$mcpName = "mantis"
$mcpCmd = "`"$exePath`""
$mcpEnv = @{
    "MANTIS_URL" = $mantisUrl
    "MANTIS_USERNAME" = $mantisUser
    "MANTIS_TOKEN" = $mantisToken
}

foreach ($choice in $selectedChoices) {
    switch ($choice) {
        "1" {
            Write-Host "`n--- Gemini CLI Configuration ---" -ForegroundColor Cyan
            $scope = Read-Host "Install as (1) Local [Project] or (2) Global [User]? (Default: 1)"
            $scopeFlag = if ($scope -eq "2") { "user" } else { "project" }
            
            Write-Host "Installing in Gemini CLI (Scope: $scopeFlag)..." -ForegroundColor Cyan
            $envArgs = ""
            $mcpEnv.GetEnumerator() | ForEach-Object { $envArgs += " --env $($_.Key)=`"$($_.Value)`"" }
            $fullCmd = "gemini mcp add $mcpName $mcpCmd $envArgs --scope $scopeFlag"
            Invoke-Expression $fullCmd
            Write-Host "[Success] Server added to Gemini CLI!" -ForegroundColor Green
        }
        "2" {
            Write-Host "`n--- Installing in Claude Code ---" -ForegroundColor Cyan
            $envArgs = ""
            $mcpEnv.GetEnumerator() | ForEach-Object { $envArgs += " --env $($_.Key)=`"$($_.Value)`"" }
            $fullCmd = "claude mcp add $mcpName -- $mcpCmd $envArgs"
            try {
                Invoke-Expression $fullCmd
                Write-Host "[Success] Server added to Claude Code!" -ForegroundColor Green
            } catch {
                Write-Warning "Failed to run 'claude mcp add' automatically."
                Write-Host "Please run this manually: $fullCmd" -ForegroundColor Gray
            }
        }
        "3" {
            Write-Host "`n--- Installing in Codex ---" -ForegroundColor Cyan
            $envArgs = ""
            $mcpEnv.GetEnumerator() | ForEach-Object { $envArgs += " --env $($_.Key)=`"$($_.Value)`"" }
            $fullCmd = "codex mcp add $mcpName $envArgs -- $mcpCmd"
            try {
                Invoke-Expression $fullCmd
                Write-Host "[Success] Server added to Codex (Config file updated)!" -ForegroundColor Green
            } catch {
                Write-Warning "Failed to run 'codex mcp add' automatically. Is Codex CLI installed?"
                Write-Host "Please run this manually: $fullCmd" -ForegroundColor Gray
            }
        }
        "4" {
            Write-Host "`n--- Claude Desktop Manual Instructions ---" -ForegroundColor Cyan
            $claudeConfigPath = "$env:AppData\Claude\claude_desktop_config.json"
            
            $jsonBlock = @{
                "mantis" = @{
                    "command" = $exePath.Replace("\", "/")
                    "args" = @()
                    "env" = $mcpEnv
                }
            } | ConvertTo-Json -Depth 10

            Write-Host "Claude Desktop does not have an automatic setup command. Please follow these steps:" -ForegroundColor White
            Write-Host "1. Open the configuration file at:" -ForegroundColor Gray
            Write-Host "   $claudeConfigPath" -ForegroundColor White
            Write-Host "2. Add the following block inside the 'mcpServers' section:" -ForegroundColor Gray
            Write-Host $jsonBlock -ForegroundColor Green
            Write-Host "3. Save the file and restart Claude Desktop." -ForegroundColor Gray
        }
        "5" {
            Write-Host "`n--- Manual Copy Commands ---" -ForegroundColor Cyan
            Write-Host "GEMINI: gemini mcp add $mcpName $mcpCmd --env MANTIS_URL=`"$mantisUrl`" --env MANTIS_USERNAME=`"$mantisUser`" --env MANTIS_TOKEN=`"$mantisToken`" --scope project"
            Write-Host "CLAUDE: claude mcp add $mcpName -- $mcpCmd"
            Write-Host "CODEX:  codex mcp add $mcpName --env MANTIS_URL=`"$mantisUrl`" --env MANTIS_USERNAME=`"$mantisUser`" --env MANTIS_TOKEN=`"$mantisToken`" -- $mcpCmd"
        }
    }
}

Write-Host "`n--- Setup Completed ---" -ForegroundColor Green
Write-Host "Tip: Use '/mcp reload' or restart your AI tool to activate the server."

