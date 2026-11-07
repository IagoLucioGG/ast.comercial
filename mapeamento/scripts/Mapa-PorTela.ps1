# Gera o mapa "tela -> chamadas de API" a partir de uma captura do REA.
# Uso: .\Mapa-PorTela.ps1 -Arquivo <captura.json> [-SaidaMd <arquivo.md>]
# Observacao: ids numericos sao normalizados para {id}; query strings e valores nao sao exibidos.
param(
    [Parameter(Mandatory = $true)][string]$Arquivo,
    [string]$SaidaMd = ""
)
$ProgressPreference = 'SilentlyContinue'
$terceiros = 'google|gstatic|intercom|birdie|cloudflare|googletagmanager|cloudfront|blob\.core\.windows'
$j = Get-Content $Arquivo -Raw -Encoding UTF8 | ConvertFrom-Json
$n = $j.normalized_result
$itens = $n.events.items
$status = @{}
$itens | Where-Object kind -eq 'response' | ForEach-Object { $status[$_.transaction_id] = $_.status }

$sb = New-Object System.Text.StringBuilder
foreach ($s in $n.steps) {
    if ($s.action -eq 'wait_for_timeout') { continue }
    $reqs = $itens | Where-Object {
        $_.kind -eq 'request' -and $_.step_index -in ($s.step_index, ($s.step_index + 1)) -and
        $_.resource_type -in 'xhr', 'fetch' -and $_.url.origin -notmatch $terceiros
    }
    [void]$sb.AppendLine("### $($s.step_id): $($s.after_url.url)")
    $g = $reqs | ForEach-Object {
        $u = [uri]$_.url.url
        $p = $u.AbsolutePath -replace '\(\d+\)', '({id})' -replace '/\d{3,}', '/{id}'
        "{0} {1}{2} [{3}]" -f $_.method, $u.Host, $p, $status[$_.transaction_id]
    } | Group-Object | Sort-Object Name
    if (-not $g) { [void]$sb.AppendLine("- (sem chamadas XHR/fetch proprias capturadas)") }
    foreach ($x in $g) { [void]$sb.AppendLine("- ``$($x.Name)`` x$($x.Count)") }
    [void]$sb.AppendLine()
}
if ($SaidaMd) { [System.IO.File]::WriteAllText($SaidaMd, $sb.ToString(), (New-Object System.Text.UTF8Encoding($false))) }
$sb.ToString()
