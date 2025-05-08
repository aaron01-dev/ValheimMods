param(
    [Parameter(Mandatory)]
    [ValidateSet('Debug','Release')]
    [System.String]$Target,
    
    [Parameter(Mandatory)]
    [System.String]$TargetPath,
    
    [Parameter(Mandatory)]
    [System.String]$TargetAssembly,

    [Parameter(Mandatory)]
    [System.String]$ValheimPath,

    [Parameter(Mandatory)]
    [System.String]$ProjectPath,

    [Parameter(Mandatory)]
    [System.String]$Version,
    
    [System.String]$DeployPath
)

# Make sure Get-Location is the script path
Push-Location -Path (Split-Path -Parent $MyInvocation.MyCommand.Path)

# Test some preliminaries
("$TargetPath",
 "$ValheimPath",
 "$(Get-Location)\libraries"
) | % {
    if (!(Test-Path "$_")) {Write-Error -ErrorAction Stop -Message "$_ folder is missing"}
}

# Plugin name without ".dll"
$name = "$TargetAssembly" -Replace('.dll')

# Create the mdb file
$pdb = "$TargetPath\$name.pdb"
if (Test-Path -Path "$pdb") {
    Write-Host "Create mdb file for plugin $name"
    Invoke-Expression "& `"$(Get-Location)\libraries\Debug\pdb2mdb.exe`" `"$TargetPath\$TargetAssembly`""
}

# Main Script
Write-Host "Publishing for $Target from $TargetPath"

if ($Target.Equals("Debug")) {
    if ($DeployPath.Equals("")){
        $DeployPath = "$ValheimPath\BepInEx\plugins"
    }
    
    if (!$DeployPath.Contains("scripts")) {
        $plug = New-Item -Type Directory -Path "$DeployPath\$name" -Force
    } else {
        $plug = New-Item -Type Directory -Path "$DeployPath" -Force
    }
        
    Write-Host "Copy $TargetAssembly to $plug"
    Copy-Item -Path "$TargetPath\$name.dll" -Destination "$plug" -Force
    Copy-Item -Path "$TargetPath\$name.pdb" -Destination "$plug" -Force
    Copy-Item -Path "$TargetPath\$name.dll.mdb" -Destination "$plug" -Force
}

if($Target.Equals("Release")) {
    Write-Host "Packaging for ThunderStore..."
    $Package="Package"
    $PackagePath="$ProjectPath\$Package"
    $BuildPath="$ProjectPath\Builds"

    Write-Host "$PackagePath\$TargetAssembly"
    New-Item -Type Directory -Path "$PackagePath\plugins" -Force
    Copy-Item -Path "$TargetPath\$TargetAssembly" -Destination "$PackagePath\plugins\$TargetAssembly" -Force
    Copy-Item -Path "$ProjectPath\README.md" -Destination "$PackagePath\README.md" -Force
    Copy-Item -Path "$ProjectPath\CHANGELOG.md" -Destination "$PackagePath\CHANGELOG.md" -Force
    Compress-Archive -Path "$PackagePath\*" -DestinationPath "$BuildPath\$name v$Version.zip" -Force
}

# Pop Location
Pop-Location