$OutputDirectory = [System.IO.Directory]::CreateDirectory([System.IO.Path]::Combine($PSScriptRoot, "chara"))

function Export-GameFile {
    param([String]$Path, [Lumina.Data.FileResource]$File, [System.IO.DirectoryInfo]$OutputDirectory)
    $outPath = [System.IO.Path]::Combine($OutputDirectory.FullName, [System.IO.Path]::GetFileName($Path))
    if (![System.IO.File]::Exists($outPath)) {
        [String]::Format("Exporting {0} to {1}...", $Path, $outPath)
        $File.SaveFileRaw($outPath)
    }
}

for ($raceCode = 1; $raceCode -le 18; $raceCode++) {
    for ($clan = 0; $clan -le 1; $clan++) {
        for ($face = 1; $face -le 10; $face++) {
            $mdlPath = [String]::Format("chara/human/c{0:D2}01/obj/face/f{1:D2}{2:D2}/model/c{0:D2}01f{1:D2}{2:D2}_fac.mdl", $raceCode, $clan, $face)
            $mdl = Get-GameFile -Path $mdlPath
            if ($mdl -ne $null) {
                Export-GameFile -Path $mdlPath -File $mdl -OutputDirectory $OutputDirectory
            }
            $mtrlPath = [String]::Format("chara/human/c{0:D2}01/obj/face/f{1:D2}{2:D2}/material/mt_c{0:D2}01f{1:D2}{2:D2}_fac_a.mtrl", $raceCode, $clan, $face)
            $mtrl = Get-GameFile -Path $mtrlPath -AsType Lumina.Data.Files.MtrlFile
            if ($mtrl -ne $null) {
                Export-GameFile -Path $mtrlPath -File $mtrl -OutputDirectory $OutputDirectory
                foreach ($entry in $mtrl.TextureOffsets) {
                    $end = [Array]::IndexOf($mtrl.Strings, [Byte]0, $entry.Offset)
                    $texPath = [System.Text.Encoding]::UTF8.GetString($mtrl.Strings, $entry.Offset, $end - $entry.Offset)
                    $tex = Get-GameFile -Path $texPath
                    if ($tex -ne $null) {
                        Export-GameFile -Path $texPath -File $tex -OutputDirectory $OutputDirectory
                    }
                }
            }
        }
    }
}
