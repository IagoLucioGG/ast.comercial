# Mascara dados pessoais (e-mail, CNPJ, CPF, telefone) nos artefatos de texto capturados,
# antes de versionar em repositório público. Usa tempo-limite por regex para não travar em HTML grande.
# Uso: .\Mascarar-Pii.ps1 -Pasta <pasta> [-Log <arquivo>]
param(
    [Parameter(Mandatory = $true)][string]$Pasta,
    [string]$Log = (Join-Path $env:TEMP 'mascara-pii.log')
)
$utf8 = New-Object System.Text.UTF8Encoding($false)
$limite = [TimeSpan]::FromSeconds(10)
$regras = @(
    @{ Nome = 'email';    Rx = [regex]::new('\b[A-Za-z0-9._%+\-]{1,64}@[A-Za-z0-9\-]{1,63}(?:\.[A-Za-z0-9\-]{1,63}){1,4}\b', 'None', $limite); Troca = 'email.mascarado@exemplo.test' }
    @{ Nome = 'cnpj';     Rx = [regex]::new('\b\d{2}\.\d{3}\.\d{3}/\d{4}-\d{2}\b', 'None', $limite); Troca = '00.000.000/0000-00' }
    @{ Nome = 'cpf';      Rx = [regex]::new('\b\d{3}\.\d{3}\.\d{3}-\d{2}\b', 'None', $limite); Troca = '000.000.000-00' }
    @{ Nome = 'telefone'; Rx = [regex]::new('\(\d{2}\)\s?9?\d{4}-\d{4}', 'None', $limite); Troca = '(00) 00000-0000' }
)
Set-Content -Path $Log -Value "inicio $(Get-Date -Format s)" -Encoding UTF8
$tot = @{}; $alterados = 0; $falhas = 0
foreach ($a in Get-ChildItem $Pasta -Recurse -File -Include *.html, *.txt, *.json) {
    try {
        $t = [System.IO.File]::ReadAllText($a.FullName)
        $n = $t
        foreach ($r in $regras) {
            $q = $r.Rx.Matches($n).Count
            if ($q) { $tot[$r.Nome] = [int]$tot[$r.Nome] + $q; $n = $r.Rx.Replace($n, $r.Troca) }
        }
        if ($n -ne $t) { [System.IO.File]::WriteAllText($a.FullName, $n, $utf8); $alterados++ }
    }
    catch {
        $falhas++
        Add-Content -Path $Log -Value "FALHA $($a.FullName): $($_.Exception.Message)" -Encoding UTF8
    }
}
$resumo = "alterados=$alterados falhas=$falhas " + (($tot.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }) -join ' ')
Add-Content -Path $Log -Value $resumo -Encoding UTF8
$resumo
