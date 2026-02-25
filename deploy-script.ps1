# Full deploy (first time): .\deploy-script.ps1
# Update existing Function App (code only): .\deploy-script.ps1 -FunctionAppNameOverride "your-existing-func-app-name"
param(
    [string]$ResourceGroup = "assignmentfortrippleaz",
    [string]$Location = "germanywestcentral",
    [string]$TemplateFile = "infra/main.bicep",
    [string]$PublishDir = "publish",
    [string]$BaseName = "fanoutimg",
    [string]$FunctionAppNameOverride = "",
    [string]$TableName = "statustable",
    [string]$BlobContainerName = "blob-for-tripple",
    [string]$Project = "triple-assignment-azure.csproj",
    [switch]$CodeOnly
)

$ErrorActionPreference = "Stop"

Write-Host "Checking Azure CLI login..."
try {
    az account show | Out-Null
} catch {
    az login | Out-Null
}

# Resolve paths
$ProjectPath  = Resolve-Path (Join-Path $PSScriptRoot $Project)
$PublishPath  = Join-Path $PSScriptRoot $PublishDir

$functionAppName = ""

if ($CodeOnly -or $FunctionAppNameOverride) {
    # Update existing Function App: skip Bicep, deploy code only
    if (-not $FunctionAppNameOverride) {
        throw "When using -CodeOnly you must pass -FunctionAppNameOverride with your existing Function App name."
    }
    $functionAppName = $FunctionAppNameOverride
    Write-Host "Code-only update: targeting existing Function App '$functionAppName'"
    Write-Host "Ensuring resource group exists..."
    az group create --name $ResourceGroup --location $Location | Out-Null
} else {
    # Full deploy: Bicep + code
    Write-Host "Creating resource group..."
    az group create --name $ResourceGroup --location $Location | Out-Null

    $TemplatePath = Resolve-Path (Join-Path $PSScriptRoot $TemplateFile)
    Write-Host "Deploying Bicep template..."
    $bicepParamList = @(
        "baseName=$BaseName",
        "tableName=$TableName",
        "blobContainerName=$BlobContainerName"
    )
    if ($FunctionAppNameOverride) {
        $bicepParamList += "functionAppNameOverride=$FunctionAppNameOverride"
    }
    $deploymentOutputs = az deployment group create `
        --resource-group $ResourceGroup `
        --template-file $TemplatePath `
        --parameters $bicepParamList `
        --query "properties.outputs" `
        -o json | ConvertFrom-Json

    if (-not $deploymentOutputs.functionAppName.value) {
        throw "Bicep must output: output functionAppName string = functionApp.name"
    }
    $functionAppName = $deploymentOutputs.functionAppName.value
}

Write-Host "Function App Name: $functionAppName"

# Publish .NET project
Write-Host "Publishing project..."
if (Test-Path $PublishPath) { Remove-Item $PublishPath -Recurse -Force }
dotnet publish $ProjectPath --configuration Release --output $PublishPath
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

# Verify host.json exists
if (-not (Test-Path (Join-Path $PublishPath "host.json"))) {
    throw "host.json missing. Wrong project?"
}

# Azure expects .azurefunctions at zip root for package validation
$azureFuncDir = Join-Path $PublishPath ".azurefunctions"
New-Item -ItemType Directory -Path $azureFuncDir -Force | Out-Null

# Zip contents (include hidden items like .azurefunctions - "*" can skip them on Windows)
$zipPath = Join-Path $PSScriptRoot "functionapp.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

Write-Host "Creating zip package..."
Push-Location $PublishPath
$itemsToZip = Get-ChildItem -Force
Compress-Archive -Path $itemsToZip -DestinationPath $zipPath
Pop-Location

# Deploy zip
Write-Host "Deploying zip..."
az functionapp deployment source config-zip `
    --resource-group $ResourceGroup `
    --name $functionAppName `
    --src "$zipPath"

Write-Host "Deployment complete."
