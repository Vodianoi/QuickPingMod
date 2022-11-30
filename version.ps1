param(
    [System.String]$PackageDir,
	
    [System.String]$AssemblyVersion
)
$a = Get-Content "$PackageDir\manifest.json" -raw | ConvertFrom-Json
if ( $a -eq $AssemblyVersion )
{
	Write-Host Changing version to $AssemblyVersion
	$a.version_number = $AssemblyVersion
	$a | ConvertTo-Json -depth 32| set-content "$PackageDir\manifest.json"
}
else
{
	Write-Host "Version number didn't change."
}
# Invoke-Expression "& `"ls`" "
Write-Host "Convert readme to bbcode for Nexus"
Invoke-Expression "& `"md_to_bbcode`" `"-i`" `"../README.md`" `"-o`" `"README.bbcode`" "