# Baixa os bundles JS proprios da aplicacao Ploomes (CloudFront PROD/stable) para analise estatica.
# Uso: .\Baixar-Bundles.ps1 [-Destino <pasta>]
param([string]$Destino = ".\mapeamento\fontes\bundles")
$ProgressPreference = 'SilentlyContinue'
$base = "https://d3hvjm3wv4q03y.cloudfront.net/PROD/stable"
$arquivos = @(
    "assets/js/angular-app.min.js",
    "assets/js/react-app.min.js",
    "assets/js/vendor.min.js",
    "assets/js/ng-templates.js",
    "assets/js/utils/intercom.js",
    "assets/js/utils/widgetCkEditor.js",
    "assets/js/ckeditor/ckeditor.js",
    "assets/js/fullcalendar/fullcalendar.min.js",
    "assets/js/microsoft-signalr/signalr.min.js",
    "chunks/chunk.0.js", "chunks/chunk.1.js", "chunks/chunk.2.js",
    "chunks/chunk.3.js", "chunks/chunk.4.js", "chunks/chunk.5.js"
)
New-Item -ItemType Directory -Force -Path $Destino | Out-Null
foreach ($a in $arquivos) {
    $url = "$base/$a"
    $dst = Join-Path $Destino ($a -replace '/', '__')
    try {
        Invoke-WebRequest -Uri $url -OutFile $dst -UseBasicParsing -TimeoutSec 60
        "{0,-45} {1,10:N0} bytes" -f $a, (Get-Item $dst).Length
    }
    catch { "{0,-45} ERRO: {1}" -f $a, $_.Exception.Message }
}
