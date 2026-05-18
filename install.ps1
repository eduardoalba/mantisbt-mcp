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
Write-Host "You can select multiple options separated by commas (e.g., 1,3,5)" -ForegroundColor Gray
Write-Host "1. Gemini CLI (Command 'gemini mcp add')"
Write-Host "2. Claude Code (Command 'claude mcp add')"
Write-Host "3. Claude Desktop (Config via JSON)"
Write-Host "4. Codex CLI (Command 'codex mcp add')"
Write-Host "5. Codex Desktop (Config via TOML)"
Write-Host "6. Generate copy-paste commands only"

$choicesInput = Read-Host "Select options (1-6)"
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
            try {
                $envArgs = ""
                $mcpEnv.GetEnumerator() | ForEach-Object { $envArgs += " --env $($_.Key)=`"$($_.Value)`"" }
                $fullCmd = "claude mcp add $mcpName -- $mcpCmd $envArgs"
                Invoke-Expression $fullCmd
                Write-Host "[Success] Server added to Claude Code!" -ForegroundColor Green
            } catch {
                Write-Warning "Failed to automate Claude Code installation. Try manually:"
                Write-Host "Command: claude mcp add $mcpName -- $mcpCmd $envArgs"
            }
        }
        "3" {
            Write-Host "`n--- Configuring Claude Desktop ---" -ForegroundColor Cyan
            $claudeConfigPath = "$env:AppData\Claude\claude_desktop_config.json"
            $claudeDir = Split-Path $claudeConfigPath
            
            if (-not (Test-Path $claudeDir)) {
                Write-Warning "Claude Desktop directory not found at $claudeDir. Skipping this installation target."
                continue
            }

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

                $config.mcpServers | Add-Member -MemberType NoteProperty -Name $mcpName -Value $serverObj -Force
                
                $jsonStr = $config | ConvertTo-Json -Depth 10
                $jsonStr | Set-Content $claudeConfigPath -Encoding UTF8
                Write-Host "[Success] Claude Desktop configuration updated!" -ForegroundColor Green
            } catch {
                Write-Error "Failed to update Claude Desktop config. $_"
                Write-Host "Add this block manually in $claudeConfigPath :" -ForegroundColor Gray
                Write-Host "`"$mcpName`": $($serverObj | ConvertTo-Json -Depth 10)"
            }
        }
        "4" {
            Write-Host "`n--- Installing in Codex CLI ---" -ForegroundColor Cyan
            try {
                $envArgs = ""
                $mcpEnv.GetEnumerator() | ForEach-Object { $envArgs += " --env $($_.Key)=`"$($_.Value)`"" }
                $fullCmd = "codex mcp add $mcpName $envArgs -- $mcpCmd"
                Invoke-Expression $fullCmd
                Write-Host "[Success] Server added to Codex CLI!" -ForegroundColor Green
            } catch {
                Write-Warning "Failed to automate Codex CLI installation. Try manually:"
                Write-Host "Command: codex mcp add $mcpName $envArgs -- $mcpCmd"
            }
        }
        "5" {
            Write-Host "`n--- Configuring Codex Desktop ---" -ForegroundColor Cyan
            $codexConfigPath = "$env:USERPROFILE\.codex\config.toml"
            
            $tomlBlock = "[mcp_servers.$mcpName]`n"
            $tomlBlock += "command = `"$($exePath.Replace('\', '\\'))`"`n"
            $tomlBlock += "args = []`n"
            $tomlBlock += "[mcp_servers.$mcpName.env]`n"
            $mcpEnv.GetEnumerator() | ForEach-Object { $tomlBlock += "$($_.Key) = `"$($_.Value)`"`n" }

            try {
                if (-not (Test-Path (Split-Path $codexConfigPath))) {
                    New-Item -ItemType Directory -Path (Split-Path $codexConfigPath) -Force | Out-Null
                }
                
                if (Test-Path $codexConfigPath) {
                    $content = Get-Content $codexConfigPath -Raw
                    $pattern = "(?s)\[mcp_servers\.$mcpName\].*?(?=\n\[mcp_servers|\z)"
                    
                    if ($content -match "\[mcp_servers\.$mcpName\]") {
                        Write-Host "Updating existing '$mcpName' configuration in config.toml..." -ForegroundColor Gray
                        $newContent = $content -replace $pattern, $tomlBlock.Trim()
                        $newContent | Set-Content $codexConfigPath -Encoding UTF8
                    } else {
                        Add-Content -Path $codexConfigPath -Value "`n$tomlBlock" -Encoding UTF8
                    }
                } else {
                    Set-Content -Path $codexConfigPath -Value $tomlBlock -Encoding UTF8
                }
                
                Write-Host "[Success] Codex configuration updated!" -ForegroundColor Green
            } catch {
                Write-Error "Failed to update Codex configuration. $_"
                Write-Host "Add this block manually in $codexConfigPath :" -ForegroundColor Gray
                Write-Host $tomlBlock
            }
        }
        "6" {
            Write-Host "`n--- Manual Copy Commands ---" -ForegroundColor Cyan
            Write-Host "GEMINI: gemini mcp add $mcpName $mcpCmd --env MANTIS_URL=`"$mantisUrl`" --env MANTIS_USERNAME=`"$mantisUser`" --env MANTIS_TOKEN=`"$mantisToken`" --scope project"
            Write-Host "CLAUDE: claude mcp add $mcpName -- $mcpCmd"
            Write-Host "CODEX:  codex mcp add $mcpName --env MANTIS_URL=`"$mantisUrl`" --env MANTIS_USERNAME=`"$mantisUser`" --env MANTIS_TOKEN=`"$mantisToken`" -- $mcpCmd"
        }
    }
}

Write-Host "`n--- Setup Completed ---" -ForegroundColor Green
Write-Host "Tip: Use '/mcp reload' or restart your AI tool to activate the server."

