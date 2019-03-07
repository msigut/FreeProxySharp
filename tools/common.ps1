function Exec
{
    # source: http://joshua.poehls.me/2012/powershell-script-module-boilerplate/
    [CmdletBinding()]
    param (
        [Parameter(Position=0, Mandatory=1)]
        [scriptblock]$Command
    )
    $LastExitCode = 0
    & $Command
    if ($LastExitCode -ne 0) {
        throw "Execution failed:`n`tError code: $LastExitCode`n`tCommand: $Command"
    }
    return
}
function Exec-In-Folder
{
    [CmdletBinding()]
    param (
        [Parameter(Position=0, Mandatory=1)]
        [string]$Folder,
        [Parameter(Position=1, Mandatory=1)]
        [scriptblock]$Command
    )
    
    Push-Location $Folder
    try {
        Exec $Command
    }
    finally {
        Pop-Location
    }
    return
}
function GetTimestamp()
{
    $date1 = Get-Date -Date "01/01/2010"
    $date2 = Get-Date
    [math]::Round((New-TimeSpan -Start $date1 -End $date2).TotalSeconds)
}
function SetAppStdOutLogging($webConfigPath, $enabled)
{
    $document = (Get-Content $webConfigPath) -as [Xml]
    $webServer = $document["configuration"]["system.webServer"]
    If ($webServer -eq $null) {
        $webServer = $document["configuration"]["location"]["system.webServer"]
    }
    $aspNetCore = $webServer["aspNetCore"]
    
    $aspNetCore.SetAttribute("stdoutLogEnabled", $enabled)
    $document.Save($webConfigPath)
    return
}
function SetAppEnvVariable($webConfigPath, $name, $value)
{
    $document = (Get-Content $webConfigPath) -as [Xml]
    $webServer = $document["configuration"]["system.webServer"]
    If ($webServer -eq $null) {
        $webServer = $document["configuration"]["location"]["system.webServer"]
    }
    $aspNetCore = $webServer["aspNetCore"]
    
    $variables = $aspNetCore["environmentVariables"]
    if (!$variables) {
        $variables = $document.CreateElement("environmentVariables");
        $_ = $aspNetCore.AppendChild($variables)
    }
    $variable = $variables["environmentVariable"] | Where-Object {$_.Name -eq $name}
    if (!$variable) {
        $variable = $document.CreateElement("environmentVariable")
        $variable.SetAttribute("name", $name)
        $variable.SetAttribute("value", $value)
        $_ = $variables.AppendChild($variable)
    }
    $document.Save($webConfigPath)
    return
}
function ZipFiles($sourceDir, $zipFileName)
{
    $_ = [Reflection.Assembly]::LoadWithPartialName("System.IO.Compression.FileSystem")
    $compressionLevel = [System.IO.Compression.CompressionLevel]::Optimal
    [System.IO.Compression.ZipFile]::CreateFromDirectory($sourceDir, $zipFileName, $compressionLevel, $false)
    return
}
function UnzipFiles($sourceDir, $zipFileName)
{
    $_ = [Reflection.Assembly]::LoadWithPartialName("System.IO.Compression.FileSystem")
    [System.IO.Compression.ZipFile]::ExtractToDirectory($sourceDir, $zipFileName)
    return
}