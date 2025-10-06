$OutputDirectory = [System.IO.Directory]::CreateDirectory([System.IO.Path]::Combine($PSScriptRoot, "chara"))

function Export-GameFile {
    param([String]$Path, [Lumina.Data.FileResource]$File, [System.IO.DirectoryInfo]$OutputDirectory)
    $outPath = [System.IO.Path]::Combine($OutputDirectory.FullName, [System.IO.Path]::GetFileName($Path))
    if ([System.IO.File]::Exists($outPath)) {
        return
    }

    [String]::Format("Exporting {0} to {1}...", $Path, $outPath)
    $File.SaveFileRaw($outPath)
}

function Export-SimpleFile {
    param([String]$Path, [System.IO.DirectoryInfo]$OutputDirectory)
    $mdl = Get-GameFile -Path $Path
    if ($mdl -eq $null) {
        return
    }

    Export-GameFile -Path $Path -File $mdl -OutputDirectory $OutputDirectory
}

function Export-Material {
    param([String]$Path, [System.IO.DirectoryInfo]$OutputDirectory)
    $mtrl = Get-GameFile -Path $Path -AsType Lumina.Data.Files.MtrlFile
    if ($mtrl -eq $null) {
        return
    }

    Export-GameFile -Path $Path -File $mtrl -OutputDirectory $OutputDirectory
    foreach ($entry in $mtrl.TextureOffsets) {
        $end = [Array]::IndexOf($mtrl.Strings, [Byte]0, $entry.Offset)
        $texPath = [System.Text.Encoding]::UTF8.GetString($mtrl.Strings, $entry.Offset, $end - $entry.Offset)
        Export-SimpleFile -Path $texPath -OutputDirectory $OutputDirectory
    }
}

for ($raceCode = 1; $raceCode -le 18; $raceCode++) {
    for ($clan = 0; $clan -le 1; $clan++) {
        for ($face = 1; $face -le 10; $face++) {
            $mdlPath = [String]::Format("chara/human/c{0:D2}01/obj/face/f{1:D2}{2:D2}/model/c{0:D2}01f{1:D2}{2:D2}_fac.mdl", $raceCode, $clan, $face)
            Export-SimpleFile -Path $mdlPath -OutputDirectory $OutputDirectory
            $mtrlPath = [String]::Format("chara/human/c{0:D2}01/obj/face/f{1:D2}{2:D2}/material/mt_c{0:D2}01f{1:D2}{2:D2}_fac_a.mtrl", $raceCode, $clan, $face)
            Export-Material -Path $mtrlPath -OutputDirectory $OutputDirectory
            if (($raceCode -eq 13) -or ($raceCode -eq 14)) {
                $mtrlPath = [String]::Format("chara/human/c{0:D2}01/obj/face/f{1:D2}{2:D2}/material/mt_c{0:D2}01f{1:D2}{2:D2}_fac_b.mtrl", $raceCode, $clan, $face)
                Export-Material -Path $mtrlPath -OutputDirectory $OutputDirectory
                $mtrlPath = [String]::Format("chara/human/c{0:D2}01/obj/face/f{1:D2}{2:D2}/material/mt_c{0:D2}01f{1:D2}{2:D2}_fac_c.mtrl", $raceCode, $clan, $face)
                Export-Material -Path $mtrlPath -OutputDirectory $OutputDirectory
            }
        }
    }
}
