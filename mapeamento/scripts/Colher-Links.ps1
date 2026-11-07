# Colhe rotas internas (links /url: da arvore de acessibilidade e URLs finais de cada passo)
# de todas as capturas REA salvas numa pasta. Ids numericos viram {id}.
# Uso: .\Colher-Links.ps1 -Pasta <pasta-com-capturas> [-Saida <arquivo.txt>]
param(
    [Parameter(Mandatory = $true)][string]$Pasta,
    [string]$Saida = ""
)
$ProgressPreference = 'SilentlyContinue'
$urls = @{}
Get-ChildItem $Pasta -Filter 'mcp_rea_capture_browser_scenario-*.txt' | ForEach-Object {
    $j = Get-Content $_.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($s in $j.normalized_result.steps) {
        $a = $s.artifacts.accessibility
        if ($a.state -eq 'captured') {
            foreach ($m in [regex]::Matches($a.value.text, '/url:\s*"?(/[^\s"#]*)"?')) {
                $u = $m.Groups[1].Value
                if ($u.Length -gt 1) { $urls[($u -replace '/\d{3,}', '/{id}')] = 1 }
            }
        }
        if ($s.after_url.url) {
            $p = ([uri]$s.after_url.url).AbsolutePath
            $urls[($p -replace '/\d{3,}', '/{id}')] = 1
        }
    }
}
$lista = $urls.Keys | Sort-Object
if ($Saida) { Set-Content -Path $Saida -Value $lista -Encoding UTF8 }
$lista
