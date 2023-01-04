param(
    [System.String]$PackageDir,
	
    [System.String]$AssemblyVersion
)
$a = Get-Content "$PackageDir\manifest.json" -raw | ConvertFrom-Json
if ( $a.version_number -ne $AssemblyVersion )
{
	Write-Host Changing version from $a.version_number to $AssemblyVersion
	$a.version_number = $AssemblyVersion
	$a | ConvertTo-Json -depth 32| set-content "$PackageDir\manifest.json"
}
else
{
	Write-Host "Version number didn't change."
}