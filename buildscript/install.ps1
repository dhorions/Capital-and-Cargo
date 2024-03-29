# Create the target directory if it doesn't exist
$targetDirectory = "C:\Games\CapitalAndCargo"
if (-not (Test-Path -Path $targetDirectory)) {
    New-Item -ItemType Directory -Path $targetDirectory
}

# Define the source directory where ZIP files are located
$sourceDirectory = "."

# Get all ZIP files matching the pattern
$zipFiles = Get-ChildItem -Path $sourceDirectory -Filter "CapitalAndCargo_*.zip"

# Sort the files by date and index, assuming the format CapitalAndCargo_yyyymmdd_Index.zip
$sortedFiles = $zipFiles | Sort-Object -Property @{
    Expression = { [regex]::Match($_.Name, "(\d{8})_(\d+)").Groups[1].Value }
    Descending = $true
}, @{
    Expression = { [regex]::Match($_.Name, "_(\d+).zip").Groups[1].Value -as [int] }
    Descending = $true
}

# Select the most recent file
$mostRecentFile = $sortedFiles | Select-Object -First 1

# Check if a file was found
if ($mostRecentFile -ne $null) {
    # Extract the most recent ZIP file
    Expand-Archive -Path $mostRecentFile.FullName -DestinationPath $targetDirectory -Force
    Write-Host "Extracted the most recent ZIP file ($($mostRecentFile.Name)) to $targetDirectory"
} else {
    Write-Host "No ZIP files found matching the pattern."
}



# Ensure the target directory exists
if (-not (Test-Path -Path $targetDirectory)) {
    New-Item -ItemType Directory -Path $targetDirectory
}

# Define the path for the new PowerShell script
$newScriptPath = Join-Path -Path $targetDirectory -ChildPath "CapitalAndCargo.ps1"

# Define the command to include in the new script
$command = 'Start-Process -FilePath "./net8.0/CapitalAndCargo.exe" -WindowStyle Maximized'

# Create the new PowerShell script with the command
Set-Content -Path $newScriptPath -Value $command

Write-Host "The script CapitalAndCargo.ps1 has been created in $targetDirectory."