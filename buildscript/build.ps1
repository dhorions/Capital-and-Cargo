# Define the source folder
$sourceFolder = "../bin/Debug/"

# Define the base path for the ZIP file
$originalDirectory = Get-Location
$baseZipPath = $originalDirectory.Path +"/builds/CapitalAndCargo_"
 Write-Host $originalDirectory
$buildsDirectory = "./builds"

# Get the current date in YYYYMMDD format
$currentDate = Get-Date -Format "yyyyMMdd"

# Initialize the index
$index = 1

# Construct the initial part of the filename
$fileName = $baseZipPath + $currentDate + "_"

# Find the first available index for which a file doesn't exist
while (Test-Path -Path ("$fileName" + $index + ".zip")) {
    $index++
}

# Complete the filename with the first available index
$destinationZipFile = "$fileName$index.zip"

# Use Compress-Archive to create the ZIP file

Set-Location ($sourceFolder )
Compress-Archive -Path "./net8.0" -DestinationPath $destinationZipFile
Set-Location $originalDirectory
# Output a message indicating completion
Write-Host "The folder $sourceFolder has been compressed into $destinationZipFile."

# remove older builds
# Delete ZIP files in the builds directory older than 1 week
Get-ChildItem -Path $buildsDirectory -Filter "*.zip" | Where-Object {
    $_.LastWriteTime -lt (Get-Date).AddDays(-7)
} | Remove-Item -Force


# Move the build to the Kids share for play testing if possible
# Check if the target directory exists
$targetDirectory = "D:\kids_share\Capital and Cargo"
$installScriptPath = "./install.ps1"
if (Test-Path -Path $targetDirectory) {
    # Move the ZIP file to the target directory
    Copy-Item -Path $destinationZipFile -Destination $targetDirectory
    Copy-Item -Path $installScriptPath -Destination $targetDirectory
    Write-Host "Local Build created and moved to kids share."
} else {
    Write-Host "Local build created."
}
