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

$ProjectPath  = Resolve-Path (Join-Path $PSScriptRoot $Project)
$PublishPath  = Join-Path $PSScriptRoot $PublishDir

$functionAppName = ""

if ($CodeOnly -or $FunctionAppNameOverride) {
    if (-not $FunctionAppNameOverride) {
        throw "When using -CodeOnly you must pass -FunctionAppNameOverride with your existing Function App name."
    }
    $functionAppName = $FunctionAppNameOverride
    Write-Host "Code-only update: targeting existing Function App '$functionAppName'"
    Write-Host "Ensuring resource group exists..."
    az group create --name $ResourceGroup --location $Location | Out-Null
} else {
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

Write-Host "Publishing project..."
if (Test-Path $PublishPath) { Remove-Item $PublishPath -Recurse -Force }
dotnet publish $ProjectPath --configuration Release --output $PublishPath
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

if (-not (Test-Path (Join-Path $PublishPath "host.json"))) {
    throw "host.json missing. Wrong project?"
}

$azureFuncDir = Join-Path $PublishPath ".azurefunctions"
New-Item -ItemType Directory -Path $azureFuncDir -Force | Out-Null

$zipPath = Join-Path $PSScriptRoot "functionapp.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

Write-Host "Creating zip package..."
Push-Location $PublishPath
$itemsToZip = Get-ChildItem -Force
Compress-Archive -Path $itemsToZip -DestinationPath $zipPath
Pop-Location

Write-Host "Deploying zip..."
az functionapp deployment source config-zip `
    --resource-group $ResourceGroup `
    --name $functionAppName `
    --src "$zipPath"

Write-Host "Deployment complete."
