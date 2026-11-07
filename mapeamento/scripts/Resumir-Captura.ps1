# Resume uma captura do REA (capture_browser_scenario) salva em arquivo JSON.
# Uso: .\Resumir-Captura.ps1 -Arquivo <caminho.json> [-Saida <pasta>]
param(
    [Parameter(Mandatory = $true)][string]$Arquivo,
    [string]$Saida = ""
)
$ProgressPreference = 'SilentlyContinue'
$j = Get-Content $Arquivo -Raw -Encoding UTF8 | ConvertFrom-Json
$n = $j.normalized_result
$itens = $n.events.items

Write-Output "== PASSOS (URL antes -> depois) =="
foreach ($s in $n.steps) {
    Write-Output ("[{0}] {1} ({2}) {3} -> {4} | status={5}" -f $s.step_index, $s.step_id, $s.action, $s.before_url.url, $s.after_url.url, $s.status)
}

Write-Output "`n== HOSTS (requests por origem) =="
$itens | Where-Object kind -eq 'request' | Group-Object { $_.url.origin } | Sort-Object Count -Descending |
    ForEach-Object { "{0,5}  {1}" -f $_.Count, $_.Name }

Write-Output "`n== ENDPOINTS XHR/FETCH (metodo, path sem query, qtd, status) =="
$resp = @{}
$itens | Where-Object kind -eq 'response' | ForEach-Object { $resp[$_.transaction_id] = $_.status }
$itens | Where-Object { $_.kind -eq 'request' -and $_.resource_type -in 'xhr', 'fetch' } | ForEach-Object {
    $u = [uri]$_.url.url
    $p = $u.AbsolutePath -replace '\(\d+\)', '({id})' -replace '/\d{3,}', '/{id}'
    [pscustomobject]@{ Metodo = $_.method; Origem = $u.Host; Path = $p; Status = $resp[$_.transaction_id]; Step = $_.step_index }
} | Group-Object Metodo, Origem, Path | Sort-Object Name | ForEach-Object {
    $f = $_.Group[0]
    "{0,-6} {1}{2}  x{3}  status={4}  1o_passo={5}" -f $f.Metodo, $f.Origem, $f.Path, $_.Count, (($_.Group.Status | Sort-Object -Unique) -join ','), $f.Step
}

Write-Output "`n== WEBSOCKETS =="
$itens | Where-Object { $_.kind -like 'websocket*' } | Group-Object kind | ForEach-Object { "{0}: {1}" -f $_.Name, $_.Count }

Write-Output "`n== ERROS DE PAGINA =="
$itens | Where-Object kind -eq 'page-error' | Select-Object -First 10 | ForEach-Object { $_.text }
