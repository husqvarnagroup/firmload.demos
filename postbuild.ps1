param(
    [string]$workingDir,
    [string]$bundleName,
    [string]$bundleDir = "$($workingDir)\Bundle"
)

$tempDir = "$($workingDir)\temp"
$binDir = "$($workingDir).\bin"
$outputFile = "$($bundleName).zip";
$firmloadDir = "c:\production-tools\bundles"


# Create or empty temp dir
if(!(Test-Path $tempDir)) 
{
	New-Item $tempDir -ItemType Directory
} else 
{
    Get-ChildItem -Path $tempDir -Include *.* -Recurse | foreach { $_.Delete()}
}

# Copy bundle files
Copy-Item -Path "$($bundleDir)\*.*" -Destination $tempDir -Force

# Create output dir
if(!(Test-Path $binDir)) 
{
	New-Item $binDir -ItemType Directory
}

Compress-Archive -Path "$($tempDir)\*.*" -DestinationPath "$($binDir)\$($outputFile)" -Force

# Clean up build
Remove-Item -Path $tempDir -Recurse -Force

# Copy to Firmload bundle path
if(!(Test-Path $firmloadDir))
{
    New-Item $firmloadDir -ItemType Directory
}

Copy-Item -Path "$($binDir)\$($outputFile)" -Destination "$($firmloadDir)\$($outputFile)"