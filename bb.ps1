# param(
    # [System.String]$PackageDir,
	
    # [System.String]$AssemblyVersion
# )


# # Determine the package's local installation location.
# # If it isn't installed, install it first, in the current user's scope.
# while (-not ($installDir = (Get-Package -ErrorAction Ignore -ProviderName NuGet Tomlyn).Source)) {
  # $null = Install-Package -Scope CurrentUser -ErrorAction Stop -ProviderName NuGet Tomlyn
# }
# Add-Type -ErrorAction Stop -LiteralPath (Join-Path $installDir '../lib/netstandard2.0/Tomlyn.dll')

# $tomlstr = (Get-Content -Path "./QuickPing/thunderstore.toml")
# # Parse the TOML string into an object mod)el (nested dictionaries).
# $tomlTable = [Tomlyn.Toml]::ToModel($tomlstr)

# # Output the '[my_table]' section's 'list' value.
# #  -> 4, 5, 6
# # IMPORTANT: Use ['<key>'] syntax; .<key> syntax does NOT work.
# $tomlTable['package']['versionNumber'] = $AssemblyVersion

# $a = Get-Content "$PackageDir\manifest.json" -raw | ConvertFrom-Json
# if ( $a -ne $AssemblyVersion )
# {
	# Write-Host Changing version to $AssemblyVersion
	# $a.version_number = $AssemblyVersion
	# $a | ConvertTo-Json -depth 32| set-content "$PackageDir\manifest.json"
# }
# else
# {
	# Write-Host "Version number didn't change."
# }
# Invoke-Expression "& `"ls`" "
Write-Host "Convert readme to bbcode for Nexus"
Invoke-Expression "& `"md_to_bbcode`" `"-i`" `"../README.md`" `"-o`" `"README.bbcode`" "