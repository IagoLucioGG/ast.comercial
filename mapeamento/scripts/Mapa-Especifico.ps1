# Lista, por passo (goto/click) de uma captura REA, as chamadas XHR/fetch proprias da aplicacao,
# removendo o "bootstrap" (chamadas repetidas em toda carga) e trackers de terceiros.
# Uso: .\Mapa-Especifico.ps1 -Arquivo <captura.json> [-SaidaMd <arquivo.md>]
param(
    [Parameter(Mandatory = $true)][string]$Arquivo,
    [string]$SaidaMd = ""
)
$ProgressPreference = 'SilentlyContinue'
$terceiros = 'google|gstatic|intercom|birdie|cloudflare|googletagmanager|cloudfront|blob\.core\.windows'
$boot = '/Account$|Account@Integrations|SupportAccesses|/Approvals|UserPreferencesLastUpdateDate|Fields@Entities|/Operations$|RelativeDates|/Selectors|Self@Counters|Tasks@EmailReminders|Tasks@RepeatIntervalUnits|Tasks@Types|Timeline@Entities|FeatureFlags|Exportations|gateway-api2|identity-provider|importation2|/Self\(|messageshub|mailmapie|Self/LogPage|/Self$|cdn-cgi'

$j = Get-Content $Arquivo -Raw -Encoding UTF8 | ConvertFrom-Json
$n = $j.normalized_result
$it = $n.events.items
$st = @{}
$it | Where-Object kind -eq 'response' | ForEach-Object { $st[$_.transaction_id] = $_.status }
$acoes = @($n.steps | Where-Object { $_.action -ne 'wait_for_timeout' })

$sb = New-Object System.Text.StringBuilder
foreach ($s in $acoes) {
    [void]$sb.AppendLine("### $($s.step_id): $($s.before_url.url) -> $($s.after_url.url) [$($s.status)]")
    $prox = $acoes | Where-Object { $_.step_index -gt $s.step_index } | Select-Object -First 1
    $fim = if ($prox) { $prox.event_sequence_start - 1 } else { 999999 }
    $linhas = $it | Where-Object {
        $_.kind -eq 'request' -and $_.sequence -ge $s.event_sequence_start -and $_.sequence -le $fim -and
        $_.resource_type -in 'xhr', 'fetch' -and $_.url.origin -notmatch $terceiros
    } | ForEach-Object {
        $u = [uri]$_.url.url
        $p = $u.AbsolutePath -replace '\(\d+\)', '({id})' -replace '/\d{3,}', '/{id}'
        "{0} {1}{2} [{3}]" -f $_.method, $u.Host, $p, $st[$_.transaction_id]
    } | Where-Object { $_ -notmatch $boot } | Sort-Object -Unique
    if (-not $linhas) { [void]$sb.AppendLine("- (nenhuma chamada especifica)") }
    foreach ($l in $linhas) { [void]$sb.AppendLine("- ``$l``") }
    [void]$sb.AppendLine()
}
if ($SaidaMd) { [System.IO.File]::WriteAllText($SaidaMd, $sb.ToString(), (New-Object System.Text.UTF8Encoding($false))) }
$sb.ToString()
