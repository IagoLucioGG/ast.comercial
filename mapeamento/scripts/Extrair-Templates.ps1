# Extrai cada template HTML de um ng-templates.js ($templateCache.put('<caminho>','<html>')) para arquivos.
# Uso: .\Extrair-Templates.ps1 -Arquivo <ng-templates.js> -Destino <pasta>
param(
    [Parameter(Mandatory = $true)][string]$Arquivo,
    [Parameter(Mandatory = $true)][string]$Destino
)
$utf8 = New-Object System.Text.UTF8Encoding($false)
$t = [System.IO.File]::ReadAllText((Resolve-Path $Arquivo), [System.Text.Encoding]::UTF8)

# Decodifica o literal de string JS em aspas simples
$script:rxEsc = [regex]'\\(u[0-9a-fA-F]{4}|x[0-9a-fA-F]{2}|[\s\S])'
function Decode-JsString([string]$s) {
    $script:rxEsc.Replace($s, {
        param($m)
        $g = $m.Groups[1].Value
        if ($g.Length -eq 5 -and $g[0] -eq 'u') { return [string][char][Convert]::ToInt32($g.Substring(1), 16) }
        if ($g.Length -eq 3 -and $g[0] -eq 'x') { return [string][char][Convert]::ToInt32($g.Substring(1), 16) }
        if ($g -eq 'n') { return "`n" }
        if ($g -eq 'r') { return '' }
        if ($g -eq 't') { return "`t" }
        if ($g -eq 'b' -or $g -eq 'f' -or $g -eq 'v') { return '' }
        if ($g -eq '0') { return '' }
        return $g
    })
}

$rx = [regex]"\`$templateCache\.put\('((?:[^'\\]|\\.)*)','((?:[^'\\]|\\.)*)'\);"
$ms = $rx.Matches($t)
$indice = New-Object System.Collections.Generic.List[string]
$n = 0
foreach ($m in $ms) {
    $caminho = $m.Groups[1].Value
    $html = Decode-JsString $m.Groups[2].Value
    $rel = $caminho -replace '[:*?"<>|]', '_'
    $dst = Join-Path $Destino $rel
    New-Item -ItemType Directory -Force -Path (Split-Path $dst) | Out-Null
    [System.IO.File]::WriteAllText($dst, $html, $utf8)
    $indice.Add(("{0}`t{1}" -f $html.Length, $caminho))
    $n++
}
[System.IO.File]::WriteAllLines((Join-Path $Destino '_indice.tsv'), $indice, $utf8)
"templates extraidos: $n de $($ms.Count) | destino: $Destino"
