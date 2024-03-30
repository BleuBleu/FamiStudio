function Build-Args([string]$Str)
{
	return [regex]::Split($Str, ' (?=(?:[^"]|"[^"]*")*$)' )
}

function Count-Folder-LineOfCodes([string]$Folder)
{
	$LocalAppData = $env:LOCALAPPDATA
	$CodeFiles = Get-ChildItem -Path $Folder -Recurse -File -Include @("*.cs", "*.ini", "*.s", "*.asm", "*.h", "*.cfg", "*.bat", "*.cpp", "*.c")

	$UserAndLineCount = @{}

	foreach ($File in $CodeFiles)
	{
		if ((-not $File.ToString().Contains("\bin\")) -and 
		    (-not $File.ToString().Contains("\obj\")) -and 
		    (-not $File.ToString().Contains("DemoSource\song_")))
		{
			$AbsolutePath = [System.IO.Path]::GetFullPath($File)

			# Write-Host $AbsolutePath

			$BlameOutput = (& $LocalAppData\GitHubDesktop\app-3.3.5\resources\app\git\mingw64\bin\git blame $AbsolutePath)
			if ($LASTEXITCODE -eq 0) 
			{
				$BlameLines = $BlameOutput.Split("`n");

				foreach ($BlameLine in $BlameLines)
				{
					$Idx0 = $BlameLine.IndexOf("(");

					if ($Idx0 -ge 0)
					{
						$Idx1 = $BlameLine.IndexOf(" ", $Idx0 + 1);
						
						if ($Idx1 -ge 0)
						{
							$User = $BlameLine.Substring($Idx0 + 1, $Idx1 - $Idx0 - 1);
							
							if (-not $UserAndLineCount.ContainsKey($User))
							{
								$UserAndLineCount[$User] = 1
							}
							else
							{
								$UserAndLineCount[$User] = $UserAndLineCount[$User] + 1
							}
						}
					}
				}
			}
		}
	}

	"==============================================="
	"Contributors for folder $Folder"
	$UserAndLineCount
}

cd ..

Count-Folder-LineOfCodes ".\FamiStudio"
Count-Folder-LineOfCodes ".\SoundEngine"
Count-Folder-LineOfCodes ".\ThirdParty"
