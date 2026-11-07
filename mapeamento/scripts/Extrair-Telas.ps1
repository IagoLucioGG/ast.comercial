# Extrai de uma captura REA (capture_browser_scenario) os artefatos de cada passo de navegacao:
#   telas/<step_id>/screenshot.png, acessibilidade.txt, dom.json, info.txt
# Uso: .\Extrair-Telas.ps1 -Arquivo <captura.json> -Destino <pasta-telas>
param(
    [Parameter(Mandatory = $true)][string]$Arquivo,
    [Parameter(Mandatory = $true)][string]$Destino
)
$ProgressPreference = 'SilentlyContinue'
$utf8 = New-Object System.Text.UTF8Encoding($false)
$j = Get-Content $Arquivo -Raw -Encoding UTF8 | ConvertFrom-Json
$steps = @($j.normalized_result.steps)

for ($i = 0; $i -lt $steps.Count; $i++) {
    $s = $steps[$i]
    if ($s.action -eq 'wait_for_timeout') { continue }
    # Os artefatos mais estaveis ficam no passo de espera seguinte (tela ja renderizada)
    $alvo = $s
    if ($i + 1 -lt $steps.Count -and $steps[$i + 1].action -eq 'wait_for_timeout') { $alvo = $steps[$i + 1] }
    $nome = if ($s.step_id -eq 'scenario_start') { 'inicio_' + (([uri]$alvo.after_url.url).AbsolutePath.Trim('/') -replace '[^\w]', '_') } else { $s.step_id }
    $pasta = Join-Path $Destino $nome
    New-Item -ItemType Directory -Force -Path $pasta | Out-Null

    $art = $alvo.artifacts
    if ($art.screenshot.state -eq 'captured') {
        [System.IO.File]::WriteAllBytes((Join-Path $pasta 'screenshot.png'), [Convert]::FromBase64String($art.screenshot.value.data_base64))
    }
    if ($art.accessibility.state -eq 'captured') {
        [System.IO.File]::WriteAllText((Join-Path $pasta 'acessibilidade.txt'), $art.accessibility.value.text, $utf8)
    }
    if ($art.dom.state -eq 'captured') {
        # O DOM renderizado chega como HTML no campo .text
        if ($art.dom.value.text) {
            [System.IO.File]::WriteAllText((Join-Path $pasta 'dom.html'), $art.dom.value.text, $utf8)
        }
        else {
            [System.IO.File]::WriteAllText((Join-Path $pasta 'dom.json'), ($art.dom.value | ConvertTo-Json -Depth 50 -Compress), $utf8)
        }
    }
    $info = "passo: $($s.step_id)`nurl: $($alvo.after_url.url)`nstatus: $($s.status)`nerro: $($s.error)`nscreenshot: $($art.screenshot.state)`nacessibilidade: $($art.accessibility.state)`ndom: $($art.dom.state)"
    [System.IO.File]::WriteAllText((Join-Path $pasta 'info.txt'), $info, $utf8)
    Write-Output ("{0,-22} {1,-60} shot={2} a11y={3} dom={4}" -f $nome, $alvo.after_url.url, $art.screenshot.state, $art.accessibility.state, $art.dom.state)
}
