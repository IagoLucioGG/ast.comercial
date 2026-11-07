<#
.SYNOPSIS
    Popula o AST Comercial com dados 100% fictícios de demonstração, pela API.

.DESCRIPTION
    Cria clientes, contatos, produtos, negócios (abertos, ganhos e perdidos), atividades,
    propostas, etiquetas e usuários vendedores, todos identificáveis pelo prefixo "[Demo]"
    no nome/título e, quando a entidade permite, por ChaveExterna "demo-seed:<tipo>:<n>".

    - Login com o usuário admin do seed: e-mail e senha são lidos em tempo de execução de
      Infrastructure/Data/SeedDados.cs e nunca são gravados nem exibidos.
    - Idempotente: registros que já existem (mesma ChaveExterna/título/e-mail) são reaproveitados.
    - -Remover exclui (exclusão lógica da API) apenas os registros "[Demo]", em ordem segura.
    - -Recriar remove e cria tudo de novo.
    - Tolerante à versão da API: se um campo opcional (datas, moeda) não puder ser gravado, o item
      é criado sem ele e isso aparece no resumo na coluna "Sem opcionais".

.EXAMPLE
    .\Seed-Demo.ps1
    .\Seed-Demo.ps1 -Recriar
    .\Seed-Demo.ps1 -Remover
    .\Seed-Demo.ps1 -BaseUrl http://localhost:5080
#>
[CmdletBinding()]
param(
    [string]$BaseUrl = 'http://localhost:5080',
    [switch]$Remover,
    [switch]$Recriar,
    # Pausa entre escritas (ms), para não pressionar o rate limit
    [int]$PausaMs = 40
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
try { [Console]::OutputEncoding = [Text.Encoding]::UTF8 } catch { }

$script:Base = $BaseUrl.TrimEnd('/')
$script:Token = $null
$script:EmpresaId = $null
$script:Cred = $null
$script:Pausas429 = 0
$script:MoedaUSD = $null
$script:Resultado = [ordered]@{}
$script:Avisos = New-Object System.Collections.ArrayList
$script:PtBr = [Globalization.CultureInfo]::GetCultureInfo('pt-BR')
$script:Inv = [Globalization.CultureInfo]::InvariantCulture
$Prefixo = '[Demo]'
$ChaveBase = 'demo-seed'

# Campos que podem ser descartados numa segunda tentativa (a versão antiga da API não grava datas
# vindas do cliente e não lista moedas). Sem eles o registro ainda é útil para a demonstração.
$script:CamposOpcionais = @('DataVencimento', 'PrevisaoFechamento', 'FechadoEm', 'DataExpiracao', 'ConcluidaEm', 'MoedaId')

# ----------------------------------------------------------------------------------------------
# Utilitários
# ----------------------------------------------------------------------------------------------
function Escrever([string]$msg, [string]$cor = 'Gray') { Write-Host $msg -ForegroundColor $cor }
function Aviso([string]$msg) { [void]$script:Avisos.Add($msg); Write-Host "  ! $msg" -ForegroundColor Yellow }

function Registrar([string]$entidade, [string]$campo, [int]$n = 1) {
    if (-not $script:Resultado.Contains($entidade)) {
        $script:Resultado[$entidade] = [ordered]@{ Criados = 0; Existentes = 0; Removidos = 0; Falhas = 0; SemOpcionais = 0 }
    }
    $script:Resultado[$entidade][$campo] += $n
}

function Q([string]$s) { [uri]::EscapeDataString($s) }
function Lit([string]$s) { "'" + $s.Replace("'", "''") + "'" }
function FiltroDemo([string]$campo) { Q ("startswith($campo," + (Lit $Prefixo) + ")") }
function DataApi([datetime]$d) { $d.ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ', $script:Inv) }
function Moeda([decimal]$v, [string]$cod = 'BRL') {
    $simbolo = if ($cod -eq 'USD') { 'US$' } else { 'R$' }
    "$simbolo " + $v.ToString('N2', $script:PtBr)
}
function Prop($obj, [string]$nome) {
    # Lê propriedade sem diferenciar maiúsculas (OData devolve PascalCase; rotas "@" devolvem camelCase)
    if ($null -eq $obj) { return $null }
    $p = $obj.PSObject.Properties[$nome]
    if ($p) { return $p.Value }
    return $null
}
function Pausa { if ($PausaMs -gt 0) { Start-Sleep -Milliseconds $PausaMs } }

function Ler-Texto($resposta) {
    try {
        if ($resposta.RawContentStream) { return [Text.Encoding]::UTF8.GetString($resposta.RawContentStream.ToArray()) }
    } catch { }
    return [string]$resposta.Content
}
function Converter-Json([string]$txt) {
    if ([string]::IsNullOrWhiteSpace($txt)) { return $null }
    try { return ($txt | ConvertFrom-Json) } catch { return $null }
}
function Mensagem($r) {
    $m = Prop $r.Dados 'erro'
    if ($m) { return [string]$m }
    if ($r.Texto) { $t = [string]$r.Texto; return $t.Substring(0, [Math]::Min(160, $t.Length)) }
    return 'sem detalhes'
}

# ----------------------------------------------------------------------------------------------
# Autenticação (credenciais lidas do seed em tempo de execução; nunca exibidas)
# ----------------------------------------------------------------------------------------------
function Ler-CredenciaisSeed {
    $arq = Join-Path $PSScriptRoot '..\Infrastructure\Data\SeedDados.cs'
    if (-not (Test-Path $arq)) { throw "Arquivo do seed não encontrado: $arq" }
    $txt = [IO.File]::ReadAllText((Resolve-Path $arq).Path)
    $m = [regex]::Match($txt, 'new\s+Usuario\s*\{[^}]*?Email\s*=\s*"([^"]+)"[^}]*?HashPassword\("([^"]+)"\)', 'Singleline')
    if (-not $m.Success) { throw 'Não foi possível ler as credenciais do admin em SeedDados.cs.' }
    return @{ Email = $m.Groups[1].Value; Senha = $m.Groups[2].Value }
}

function Entrar {
    if (-not $script:Cred) { $script:Cred = Ler-CredenciaisSeed }
    $corpo = @{ Email = $script:Cred.Email; Senha = $script:Cred.Senha } | ConvertTo-Json -Compress
    try {
        $r = Invoke-WebRequest -Uri "$script:Base/api/Autenticacao/login" -Method Post -UseBasicParsing `
            -ContentType 'application/json; charset=utf-8' -Body ([Text.Encoding]::UTF8.GetBytes($corpo))
    } catch {
        throw "Falha no login do admin do seed em $script:Base (verifique se o backend está no ar)."
    }
    $d = Converter-Json (Ler-Texto $r)
    $script:Token = [string](Prop $d 'token')
    $script:EmpresaId = [long](Prop $d 'empresaId')
    if (-not $script:Token) { throw 'Login não devolveu token.' }
}

# ----------------------------------------------------------------------------------------------
# HTTP com tratamento de 429 (rate limit) e 401 (token expirado)
# ----------------------------------------------------------------------------------------------
function Invoke-Api {
    param([string]$Metodo, [string]$Caminho, $Corpo = $null)
    $uri = if ($Caminho -match '^https?://') { $Caminho } else { "$script:Base/$Caminho" }
    $relogou = $false
    for ($tentativa = 1; $tentativa -le 8; $tentativa++) {
        $p = @{
            Uri = $uri; Method = $Metodo; UseBasicParsing = $true; ErrorAction = 'Stop'
            Headers = @{ Authorization = "Bearer $script:Token"; Accept = 'application/json' }
        }
        if ($null -ne $Corpo) {
            $json = if ($Corpo -is [string]) { $Corpo } else { $Corpo | ConvertTo-Json -Depth 10 -Compress }
            $p.Body = [Text.Encoding]::UTF8.GetBytes($json)
            $p.ContentType = 'application/json; charset=utf-8'
        }
        try {
            $r = Invoke-WebRequest @p
            $txt = Ler-Texto $r
            return [pscustomobject]@{ Ok = $true; Status = [int]$r.StatusCode; Dados = (Converter-Json $txt); Texto = $txt }
        } catch {
            $status = -1; $txt = $null
            $resp = $null
            try { $resp = $_.Exception.Response } catch { }
            if ($resp) { try { $status = [int]$resp.StatusCode } catch { } }
            try { if ($_.ErrorDetails -and $_.ErrorDetails.Message) { $txt = $_.ErrorDetails.Message } } catch { }
            if ($status -eq 429) {
                $script:Pausas429++
                Start-Sleep -Seconds ([Math]::Min(6, 1 + $tentativa))
                continue
            }
            if ($status -eq 401 -and -not $relogou) { $relogou = $true; Entrar; continue }
            if ($status -eq -1 -and $tentativa -lt 3) { Start-Sleep -Seconds 1; continue }
            return [pscustomobject]@{ Ok = $false; Status = $status; Dados = (Converter-Json $txt); Texto = $txt }
        }
    }
    return [pscustomobject]@{ Ok = $false; Status = 429; Dados = $null; Texto = 'Rate limit persistente' }
}

function Obter-Lista([string]$Caminho) {
    $todos = New-Object System.Collections.ArrayList
    $atual = $Caminho
    $paginas = 0
    while ($atual -and $paginas -lt 50) {
        $paginas++
        $r = Invoke-Api 'GET' $atual
        if (-not $r.Ok) { Aviso "Falha ao ler $Caminho (HTTP $($r.Status): $(Mensagem $r))"; break }
        $d = $r.Dados
        if ($null -eq $d) { break }
        if ($d -isnot [array] -and $d.PSObject.Properties['value']) { foreach ($i in @($d.value)) { [void]$todos.Add($i) } }
        else { foreach ($i in @($d)) { [void]$todos.Add($i) } }
        $prox = $null
        if ($d -isnot [array] -and $d.PSObject.Properties['@odata.nextLink']) { $prox = [string]$d.'@odata.nextLink' }
        $atual = $prox
    }
    return $todos.ToArray()
}

function Contar-Api([string]$Conjunto, [string]$Filtro) {
    $r = Invoke-Api 'GET' ("odata/$Conjunto/`$count?`$filter=" + $Filtro)
    if ($r.Ok) { $n = 0; if ([int]::TryParse(([string]$r.Texto).Trim(), [ref]$n)) { return $n } }
    # Plano B: lista e conta localmente
    $l = @(Obter-Lista ("odata/${Conjunto}?`$filter=" + $Filtro + '&$select=Id'))
    return @($l).Count
}

function Buscar-PorChave([string]$Conjunto, [string]$Chave) {
    $l = @(Obter-Lista ("odata/${Conjunto}?`$filter=" + (Q ("ChaveExterna eq " + (Lit $Chave))) + '&$select=Id'))
    if (@($l).Count -gt 0) { return [long](Prop $l[0] 'Id') }
    return $null
}

function Sem-Campos($corpo, [string[]]$campos) {
    $novo = [ordered]@{}
    foreach ($k in @($corpo.Keys)) { if ($campos -notcontains $k) { $novo[$k] = $corpo[$k] } }
    return $novo
}
function Sem-Opcionais($corpo) { return (Sem-Campos $corpo $script:CamposOpcionais) }

# Versões reduzidas do corpo, em ordem: primeiro sem datas, depois também sem moeda.
function Variantes($corpo) {
    $lista = @()
    $removidos = @()
    $tamanho = $corpo.Count
    foreach ($nivel in @(@('DataVencimento', 'PrevisaoFechamento', 'FechadoEm', 'DataExpiracao', 'ConcluidaEm'), @('MoedaId'))) {
        $removidos += $nivel
        $novo = Sem-Campos $corpo $removidos
        if ($novo.Count -lt $tamanho) { $lista += , $novo; $tamanho = $novo.Count }
    }
    return , $lista
}

function Tem-Opcionais($corpo) {
    foreach ($k in @($corpo.Keys)) { if ($script:CamposOpcionais -contains $k) { return $true } }
    return $false
}

<#
  Cria um registro num entity set OData de forma robusta:
  1) se já existe (ChaveExterna), reaproveita;
  2) POST; algumas rotas da versão em execução gravam e respondem 400/500 ao montar a resposta,
     então sempre confirma pela ChaveExterna;
  3) se falhou de fato e havia campos opcionais, tenta de novo sem eles.
#>
function Criar-Registro {
    param([string]$Entidade, [string]$Conjunto, $Corpo, [string]$Chave, [string]$Rotulo)
    $id = Buscar-PorChave $Conjunto $Chave
    if ($id) { Registrar $Entidade 'Existentes'; return $id }

    $r = Invoke-Api 'POST' "odata/$Conjunto" $Corpo
    Pausa
    if ($r.Ok -and (Prop $r.Dados 'Id')) { Registrar $Entidade 'Criados'; return [long](Prop $r.Dados 'Id') }
    $id = Buscar-PorChave $Conjunto $Chave
    if ($id) { Registrar $Entidade 'Criados'; return $id }

    foreach ($variante in (Variantes $Corpo)) {
        $r2 = Invoke-Api 'POST' "odata/$Conjunto" $variante
        Pausa
        $id = $null
        if ($r2.Ok -and (Prop $r2.Dados 'Id')) { $id = [long](Prop $r2.Dados 'Id') } else { $id = Buscar-PorChave $Conjunto $Chave }
        if ($id) { Registrar $Entidade 'Criados'; Registrar $Entidade 'SemOpcionais'; return $id }
        $r = $r2
    }
    Registrar $Entidade 'Falhas'
    Aviso "$Entidade '$Rotulo': HTTP $($r.Status) - $(Mensagem $r)"
    return $null
}

# PATCH com nova tentativa sem campos opcionais
function Atualizar-Registro {
    param([string]$Entidade, [string]$Caminho, $Corpo, [string]$Rotulo)
    $r = Invoke-Api 'PATCH' $Caminho $Corpo
    Pausa
    if ($r.Ok) { return $true }
    foreach ($variante in (Variantes $Corpo)) {
        if ($variante.Count -eq 0) { Registrar $Entidade 'SemOpcionais'; return $false }
        $r = Invoke-Api 'PATCH' $Caminho $variante
        Pausa
        if ($r.Ok) { Registrar $Entidade 'SemOpcionais'; return $true }
    }
    Aviso "$Entidade '$Rotulo' (atualização): HTTP $($r.Status) - $(Mensagem $r)"
    return $false
}

# ----------------------------------------------------------------------------------------------
# Cadastros auxiliares (origem, ramo, porte, motivo de perda, família e grupo de produto)
# A rota odata/AdminCadastros/{tipo} só existe nas versões mais novas da API; sem ela, usa
# os cadastros que já existem (rotas "@") e não cria nada (as rotas "@" antigas gravam vazio).
# ----------------------------------------------------------------------------------------------
$script:RotaArroba = @{
    'origens' = 'Clientes@Origens'; 'ramos' = 'Clientes@RamosAtividade'; 'portes' = 'Clientes@Portes'
    'motivos-perda' = 'Negocios@MotivosPerda'; 'familias-produto' = 'Produtos@Familias'; 'grupos-produto' = 'Produtos@Grupos'
    'status-cliente' = 'Clientes@StatusClientes'; 'status-negocio' = 'Negocios@Status'
}
$script:TemAdminCadastros = $false

function Listar-Cadastro([string]$tipo) {
    if ($script:TemAdminCadastros) { return @(Obter-Lista "odata/AdminCadastros/$tipo") }
    return @(Obter-Lista ("odata/" + $script:RotaArroba[$tipo]))
}

function Garantir-Cadastros([string]$tipo, [object[]]$itens) {
    # $itens: lista de hashtables { nome; ordem; familiaProdutoId? }. Devolve lista de ids utilizáveis.
    $ids = New-Object System.Collections.ArrayList
    $existentes = @(Listar-Cadastro $tipo)
    if ($script:TemAdminCadastros) {
        foreach ($it in $itens) {
            $achado = $existentes | Where-Object { [string](Prop $_ 'nome') -eq $it.nome } | Select-Object -First 1
            if ($achado) { Registrar 'Cadastros auxiliares' 'Existentes'; [void]$ids.Add([long](Prop $achado 'id')); continue }
            $r = Invoke-Api 'POST' "odata/AdminCadastros/$tipo" $it
            Pausa
            if ($r.Ok -and (Prop $r.Dados 'id')) { Registrar 'Cadastros auxiliares' 'Criados'; [void]$ids.Add([long](Prop $r.Dados 'id')) }
            else { Registrar 'Cadastros auxiliares' 'Falhas'; Aviso "Cadastro $tipo '$($it.nome)': HTTP $($r.Status) - $(Mensagem $r)" }
        }
    }
    if ($ids.Count -eq 0) {
        # Reaproveita os cadastros que existem e têm nome
        foreach ($e in $existentes) { if (-not [string]::IsNullOrWhiteSpace([string](Prop $e 'nome'))) { [void]$ids.Add([long](Prop $e 'id')) } }
    }
    return $ids.ToArray()
}

# ----------------------------------------------------------------------------------------------
# Remoção dos registros [Demo] (ordem: atividades > itens/parcelas > propostas > negócios >
# contatos > clientes > produtos > grupos/famílias > etiquetas > funis > cadastros > usuários)
# ----------------------------------------------------------------------------------------------
function Remover-Ids([string]$Entidade, [string]$PrefixoRota, [object[]]$ids) {
    foreach ($id in $ids) {
        $r = Invoke-Api 'DELETE' "odata/$PrefixoRota($id)"
        Pausa
        if ($r.Ok -or $r.Status -eq 404) { Registrar $Entidade 'Removidos' } else { Registrar $Entidade 'Falhas'; Aviso "Remover $Entidade $id`: HTTP $($r.Status) - $(Mensagem $r)" }
    }
}

function Ids-Demo([string]$Conjunto, [string]$Campo) {
    $l = @(Obter-Lista ("odata/${Conjunto}?`$filter=" + (FiltroDemo $Campo) + '&$select=Id'))
    return @($l | ForEach-Object { [long](Prop $_ 'Id') })
}

function Remover-Demo {
    Escrever "`n== Removendo registros $Prefixo ==" 'Cyan'
    $clientes = @(Ids-Demo 'Clientes' 'Nome')
    $negocios = @(Ids-Demo 'Negocios' 'Titulo')
    $contatos = @(Ids-Demo 'PessoasContato' 'Nome')
    $propostas = @(Ids-Demo 'Propostas' 'Titulo')

    # Atividades: o GET devolve DTO e não aceita $filter nesta versão; filtra localmente.
    # Inclui atividades geradas por automações sobre registros demo (senão ficariam órfãs).
    $ativ = Obter-Lista 'odata/Atividades'
    $idsAtiv = @($ativ | Where-Object {
            ([string](Prop $_ 'titulo')).StartsWith($Prefixo) -or
            ((Prop $_ 'clienteId') -and $clientes -contains [long](Prop $_ 'clienteId')) -or
            ((Prop $_ 'negocioId') -and $negocios -contains [long](Prop $_ 'negocioId')) -or
            ((Prop $_ 'pessoaContatoId') -and $contatos -contains [long](Prop $_ 'pessoaContatoId'))
        } | ForEach-Object { [long](Prop $_ 'id') })
    Remover-Ids 'Atividades' 'Atividades' $idsAtiv

    foreach ($propId in $propostas) {
        $itens = Obter-Lista ("odata/Propostas@Produtos?`$filter=" + (Q "PropostaId eq $propId"))
        Remover-Ids 'Itens de proposta' 'Propostas@Produtos' @($itens | ForEach-Object { [long](Prop $_ 'id') })
        $parc = Obter-Lista ("odata/Propostas@Parcelas?`$filter=" + (Q "PropostaId eq $propId"))
        Remover-Ids 'Parcelas de proposta' 'Propostas@Parcelas' @($parc | ForEach-Object { [long](Prop $_ 'id') })
    }
    Remover-Ids 'Propostas' 'Propostas' $propostas
    Remover-Ids 'Negócios' 'Negocios' $negocios
    Remover-Ids 'Contatos' 'PessoasContato' $contatos
    Remover-Ids 'Clientes' 'Clientes' $clientes
    Remover-Ids 'Produtos' 'Produtos' (Ids-Demo 'Produtos' 'Nome')
    Remover-Ids 'Etiquetas' 'Etiquetas' (Ids-Demo 'Etiquetas' 'Nome')

    # Funis [Demo] (se uma execução anterior criou)
    $funis = Obter-Lista 'odata/Negocios@Funis'
    Remover-Ids 'Funis' 'Funis' @($funis | Where-Object { ([string](Prop $_ 'nome')).StartsWith($Prefixo) } | ForEach-Object { [long](Prop $_ 'id') })

    # Cadastros auxiliares [Demo]: grupos antes de famílias
    foreach ($tipo in @('grupos-produto', 'familias-produto', 'origens', 'ramos', 'portes', 'motivos-perda')) {
        $lista = Listar-Cadastro $tipo
        foreach ($c in @($lista | Where-Object { ([string](Prop $_ 'nome')).StartsWith($Prefixo) })) {
            $id = [long](Prop $c 'id')
            $r = if ($script:TemAdminCadastros) { Invoke-Api 'DELETE' "odata/AdminCadastros/$tipo/$id" } else { Invoke-Api 'DELETE' ("odata/" + $script:RotaArroba[$tipo] + "($id)") }
            Pausa
            if ($r.Ok -or $r.Status -eq 404) { Registrar 'Cadastros auxiliares' 'Removidos' } else { Registrar 'Cadastros auxiliares' 'Falhas'; Aviso "Remover cadastro $tipo $id`: HTTP $($r.Status)" }
        }
    }

    # Usuários [Demo] com e-mail @exemplo.test (DELETE da API = desativar)
    # Antes de desativar, troca o e-mail por um único "removido": o usuário inativo continua no banco
    # e, sem isso, o e-mail original ficaria preso e não poderia ser recriado depois.
    $us = @(Obter-Lista ("odata/Usuarios?`$filter=" + (FiltroDemo 'Nome') + '&$select=Id,Email'))
    $idsUs = @($us | Where-Object { ([string](Prop $_ 'Email')).EndsWith('@exemplo.test') } | ForEach-Object { [long](Prop $_ 'Id') })
    foreach ($uid in $idsUs) {
        $novoEmail = "removido.$uid.$([DateTime]::UtcNow.Ticks)@exemplo.test"
        [void](Invoke-Api 'PATCH' "odata/Usuarios($uid)" ([ordered]@{ Email = $novoEmail }))
        Pausa
    }
    Remover-Ids 'Usuários' 'Usuarios' $idsUs
}

# ----------------------------------------------------------------------------------------------
# Dados fictícios (listas fixas)
# ----------------------------------------------------------------------------------------------
$EmpresasDemo = @(
    @{ N = 'Alfa Logística'; R = 'Alfa Logística Demo Ltda'; C = 'São Paulo'; E = 'SP' }
    @{ N = 'Beta Varejo'; R = 'Beta Varejo Demo S.A.'; C = 'Campinas'; E = 'SP' }
    @{ N = 'Gama Indústria'; R = 'Gama Indústria Demo Ltda'; C = 'Joinville'; E = 'SC' }
    @{ N = 'Delta Tecnologia'; R = 'Delta Tecnologia Demo ME'; C = 'Recife'; E = 'PE' }
    @{ N = 'Épsilon Saúde'; R = 'Épsilon Saúde Demo Ltda'; C = 'Belo Horizonte'; E = 'MG' }
    @{ N = 'Zeta Alimentos'; R = 'Zeta Alimentos Demo Ltda'; C = 'Chapecó'; E = 'SC' }
    @{ N = 'Eta Construções'; R = 'Eta Construções Demo Ltda'; C = 'Goiânia'; E = 'GO' }
    @{ N = 'Teta Educação'; R = 'Teta Educação Demo Ltda'; C = 'Curitiba'; E = 'PR' }
    @{ N = 'Iota Transportes'; R = 'Iota Transportes Demo Ltda'; C = 'Porto Alegre'; E = 'RS' }
    @{ N = 'Kappa Energia'; R = 'Kappa Energia Demo S.A.'; C = 'Rio de Janeiro'; E = 'RJ' }
    @{ N = 'Lambda Consultoria'; R = 'Lambda Consultoria Demo Ltda'; C = 'Florianópolis'; E = 'SC' }
    @{ N = 'Mi Farmacêutica'; R = 'Mi Farmacêutica Demo Ltda'; C = 'Ribeirão Preto'; E = 'SP' }
    @{ N = 'Ni Agro'; R = 'Ni Agro Demo Ltda'; C = 'Cuiabá'; E = 'MT' }
    @{ N = 'Xi Têxtil'; R = 'Xi Têxtil Demo Ltda'; C = 'Blumenau'; E = 'SC' }
    @{ N = 'Ômicron Software'; R = 'Ômicron Software Demo Ltda'; C = 'São José dos Campos'; E = 'SP' }
    @{ N = 'Pi Engenharia'; R = 'Pi Engenharia Demo Ltda'; C = 'Salvador'; E = 'BA' }
    @{ N = 'Rô Comunicação'; R = 'Rô Comunicação Demo ME'; C = 'Fortaleza'; E = 'CE' }
    @{ N = 'Sigma Metalúrgica'; R = 'Sigma Metalúrgica Demo Ltda'; C = 'Caxias do Sul'; E = 'RS' }
    @{ N = 'Tau Turismo'; R = 'Tau Turismo Demo Ltda'; C = 'Natal'; E = 'RN' }
    @{ N = 'Ípsilon Seguros'; R = 'Ípsilon Seguros Demo S.A.'; C = 'São Paulo'; E = 'SP' }
    @{ N = 'Fi Móveis'; R = 'Fi Móveis Demo Ltda'; C = 'Bento Gonçalves'; E = 'RS' }
    @{ N = 'Chi Química'; R = 'Chi Química Demo Ltda'; C = 'Camaçari'; E = 'BA' }
    @{ N = 'Psi Clínicas'; R = 'Psi Clínicas Demo Ltda'; C = 'Vitória'; E = 'ES' }
    @{ N = 'Ômega Distribuidora'; R = 'Ômega Distribuidora Demo Ltda'; C = 'Manaus'; E = 'AM' }
    @{ N = 'Aurora Serviços'; R = 'Aurora Serviços Demo Ltda'; C = 'Brasília'; E = 'DF' }
)

$PessoasDemo = @(
    'Maria Exemplo', 'João Fictício', 'Ana Teste', 'Pedro Exemplo', 'Juliana Fictícia', 'Carlos Teste',
    'Fernanda Exemplo', 'Rafael Fictício', 'Beatriz Teste', 'Lucas Exemplo', 'Camila Fictícia', 'Bruno Teste',
    'Larissa Exemplo', 'Thiago Fictício', 'Patrícia Teste', 'Gustavo Exemplo', 'Renata Fictícia', 'Diego Teste',
    'Aline Exemplo', 'Marcelo Fictício'
)
$CargosDemo = @('Diretora Comercial', 'Gerente de Compras', 'Analista de TI', 'CEO', 'Coordenadora Financeira',
    'Gerente de Operações', 'Compradora', 'Diretor de TI', 'Supervisora de Logística', 'Sócio-diretor')
# índice do cliente de cada contato (alguns clientes com 2 contatos, alguns sem)
$ClienteDoContato = @(0, 0, 1, 2, 3, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 14, 15, 17, 19, 22)

$ProdutosDemo = @(
    @{ N = 'Licença CRM Essencial (usuário/mês)'; Cod = 'DEMO-001'; P = 89.90; Cu = 20; U = 'usuário'; Cat = 'Assinatura'; G = 0 }
    @{ N = 'Licença CRM Profissional (usuário/mês)'; Cod = 'DEMO-002'; P = 149.90; Cu = 35; U = 'usuário'; Cat = 'Assinatura'; G = 0 }
    @{ N = 'Licença CRM Enterprise (usuário/mês)'; Cod = 'DEMO-003'; P = 249.90; Cu = 60; U = 'usuário'; Cat = 'Assinatura'; G = 0 }
    @{ N = 'Módulo de Propostas'; Cod = 'DEMO-004'; P = 1200.00; Cu = 300; U = 'un'; Cat = 'Produto'; G = 0 }
    @{ N = 'Módulo de Automações'; Cod = 'DEMO-005'; P = 1800.00; Cu = 450; U = 'un'; Cat = 'Produto'; G = 0 }
    @{ N = 'Implantação Padrão'; Cod = 'DEMO-006'; P = 4500.00; Cu = 2000; U = 'projeto'; Cat = 'Servico'; G = 1 }
    @{ N = 'Implantação Avançada'; Cod = 'DEMO-007'; P = 12000.00; Cu = 5500; U = 'projeto'; Cat = 'Servico'; G = 1 }
    @{ N = 'Migração de Dados'; Cod = 'DEMO-008'; P = 3200.00; Cu = 1400; U = 'projeto'; Cat = 'Servico'; G = 1 }
    @{ N = 'Treinamento In Company (turma)'; Cod = 'DEMO-009'; P = 2400.00; Cu = 900; U = 'turma'; Cat = 'Servico'; G = 1 }
    @{ N = 'Consultoria de Processos (hora)'; Cod = 'DEMO-010'; P = 280.00; Cu = 110; U = 'h'; Cat = 'Servico'; G = 1 }
    @{ N = 'Suporte Premium (mês)'; Cod = 'DEMO-011'; P = 990.00; Cu = 300; U = 'mês'; Cat = 'Assinatura'; G = 2 }
    @{ N = 'Integração via API (pacote)'; Cod = 'DEMO-012'; P = 5600.00; Cu = 2300; U = 'pacote'; Cat = 'Servico'; G = 2 }
)

$OportunidadesDemo = @('Implantação de CRM', 'Licenças anuais', 'Consultoria de processos', 'Renovação de contrato',
    'Expansão de equipe', 'Treinamento in company', 'Suporte premium', 'Integração de sistemas', 'Projeto piloto', 'Upgrade de plano')

$VendedoresDemo = @(
    @{ Nome = "$Prefixo Carla Vendedora"; Email = 'vendas.carla.demo@exemplo.test' }
    @{ Nome = "$Prefixo Bruno Vendedor"; Email = 'vendas.bruno.demo@exemplo.test' }
    @{ Nome = "$Prefixo Diego Vendedor"; Email = 'vendas.diego.demo@exemplo.test' }
)

$EtiquetasDemo = @(
    @{ Nome = "$Prefixo VIP"; Cor = '#8e44ad' }, @{ Nome = "$Prefixo Urgente"; Cor = '#e74c3c' },
    @{ Nome = "$Prefixo Indicação"; Cor = '#27ae60' }, @{ Nome = "$Prefixo Renovação"; Cor = '#2980b9' },
    @{ Nome = "$Prefixo Follow-up"; Cor = '#f39c12' }, @{ Nome = "$Prefixo Estratégico"; Cor = '#16a085' }
)

function Slug([string]$s) {
    $n = $s.Normalize([Text.NormalizationForm]::FormD)
    $sb = New-Object Text.StringBuilder
    foreach ($ch in $n.ToCharArray()) {
        if ([Globalization.CharUnicodeInfo]::GetUnicodeCategory($ch) -ne [Globalization.UnicodeCategory]::NonSpacingMark) { [void]$sb.Append($ch) }
    }
    return (($sb.ToString().ToLowerInvariant()) -replace '[^a-z0-9]+', '.').Trim('.')
}

function Nova-Senha {
    # Senha aleatória forte; nunca é exibida nem gravada (os usuários demo não são usados para login)
    $b = New-Object byte[] 24
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    $rng.GetBytes($b)
    return 'Dm#' + ([Convert]::ToBase64String($b) -replace '[+/=]', 'x') + '7a'
}

# ----------------------------------------------------------------------------------------------
# Criação
# ----------------------------------------------------------------------------------------------
function Criar-Demo {
    $rnd = New-Object System.Random 20261106
    $hoje = (Get-Date).Date
    $inicioMes = New-Object DateTime $hoje.Year, $hoje.Month, 1

    # ---- Referências existentes ----
    Escrever "`n== Lendo cadastros existentes ==" 'Cyan'
    $statusCli = @(Obter-Lista 'odata/Clientes@StatusClientes' | Where-Object { -not [string]::IsNullOrWhiteSpace([string](Prop $_ 'nome')) })
    $statusNeg = @(Obter-Lista 'odata/Negocios@Status')
    function IdStatusNeg($tipoNum, $tipoNome) {
        $s = $statusNeg | Where-Object { [string](Prop $_ 'tipoBase') -eq [string]$tipoNum -or [string](Prop $_ 'tipoBase') -eq $tipoNome } |
            Sort-Object { -[int][bool](Prop $_ 'padrao') }, { [int](Prop $_ 'ordem') } | Select-Object -First 1
        if ($s) { return [long](Prop $s 'id') } ; return $null
    }
    $stAberto = IdStatusNeg 0 'Aberto'; $stGanho = IdStatusNeg 1 'Ganho'; $stPerdido = IdStatusNeg 2 'Perdido'

    $moedas = @(Obter-Lista 'odata/Moedas')
    $mBRL = $null; $mUSD = $null
    foreach ($m in $moedas) {
        if ([string](Prop $m 'Codigo') -eq 'BRL') { $mBRL = [long](Prop $m 'Id') }
        if ([string](Prop $m 'Codigo') -eq 'USD') { $mUSD = [long](Prop $m 'Id') }
    }
    if (-not $mUSD) {
        # As moedas do seed são globais (EmpresaId 0) e não aparecem na listagem filtrada por empresa.
        # O seed insere BRL, USD, EUR nessa ordem; se o id presumido não existir, o item é gravado sem moeda.
        $mBRL = 1; $mUSD = 2
        Aviso 'Moedas não listadas pela API; usando ids presumidos do seed (BRL=1, USD=2).'
    }
    $script:MoedaUSD = $mUSD

    $script:TemAdminCadastros = (Invoke-Api 'GET' 'odata/AdminCadastros/tipos').Ok
    if (-not $script:TemAdminCadastros) { Aviso 'Rota AdminCadastros indisponível nesta versão da API: origens/ramos/portes/motivos/famílias/grupos não serão criados (usa os existentes com nome, se houver).' }
    $origens = Garantir-Cadastros 'origens' @(
        @{ nome = "$Prefixo Indicação"; ordem = 1 }, @{ nome = "$Prefixo Site"; ordem = 2 },
        @{ nome = "$Prefixo Evento"; ordem = 3 }, @{ nome = "$Prefixo Prospecção ativa"; ordem = 4 })
    $ramos = Garantir-Cadastros 'ramos' @(
        @{ nome = "$Prefixo Logística"; ordem = 1 }, @{ nome = "$Prefixo Varejo"; ordem = 2 }, @{ nome = "$Prefixo Indústria"; ordem = 3 },
        @{ nome = "$Prefixo Tecnologia"; ordem = 4 }, @{ nome = "$Prefixo Saúde"; ordem = 5 }, @{ nome = "$Prefixo Serviços"; ordem = 6 })
    $portes = Garantir-Cadastros 'portes' @(
        @{ nome = "$Prefixo Pequena"; ordem = 1 }, @{ nome = "$Prefixo Média"; ordem = 2 }, @{ nome = "$Prefixo Grande"; ordem = 3 })
    $motivos = Garantir-Cadastros 'motivos-perda' @(
        @{ nome = "$Prefixo Preço acima do orçamento"; ordem = 1 }, @{ nome = "$Prefixo Escolheu concorrente"; ordem = 2 },
        @{ nome = "$Prefixo Projeto adiado"; ordem = 3 })
    $familias = Garantir-Cadastros 'familias-produto' @(@{ nome = "$Prefixo Software"; ordem = 1 }, @{ nome = "$Prefixo Serviços"; ordem = 2 })
    $origens = @($origens); $ramos = @($ramos); $portes = @($portes); $motivos = @($motivos); $familias = @($familias)
    $grupos = @()
    if ($script:TemAdminCadastros -and $familias.Count -ge 2) {
        $grupos = Garantir-Cadastros 'grupos-produto' @(
            @{ nome = "$Prefixo Licenças e módulos"; ordem = 1; familiaProdutoId = $familias[0] },
            @{ nome = "$Prefixo Implantação e treinamento"; ordem = 2; familiaProdutoId = $familias[1] },
            @{ nome = "$Prefixo Suporte e integrações"; ordem = 3; familiaProdutoId = $familias[1] })
        $grupos = @($grupos)
    }

    # ---- Funil: reaproveita "[Demo] Funil de Vendas" se existir com etapas; senão o funil do seed ----
    $funis = @(Obter-Lista 'odata/Negocios@Funis')
    $etapasTodas = @(Obter-Lista 'odata/Negocios@Etapas')
    function EtapasDo($fid) { @($etapasTodas | Where-Object { [long](Prop $_ 'funilId') -eq $fid } | Sort-Object { [int](Prop $_ 'ordem') }) }
    $funil = $funis | Where-Object { [string](Prop $_ 'nome') -eq "$Prefixo Funil de Vendas" -and @(EtapasDo ([long](Prop $_ 'id'))).Count -ge 3 } | Select-Object -First 1
    if (-not $funil) { $funil = $funis | Where-Object { [string](Prop $_ 'nome') -eq 'Funil Padrão' -and @(EtapasDo ([long](Prop $_ 'id'))).Count -ge 3 } | Select-Object -First 1 }
    if (-not $funil) { $funil = $funis | Where-Object { -not ([string](Prop $_ 'nome')).StartsWith('[') -and @(EtapasDo ([long](Prop $_ 'id'))).Count -ge 3 } | Sort-Object { [int](Prop $_ 'ordem') } | Select-Object -First 1 }
    if (-not $funil) { throw 'Nenhum funil com pelo menos 3 etapas encontrado; não é possível criar negócios.' }
    $funilId = [long](Prop $funil 'id')
    $etapas = @(EtapasDo $funilId)
    Escrever ("Funil usado: '{0}' (id {1}) com {2} etapas: {3}" -f (Prop $funil 'nome'), $funilId, $etapas.Count, (($etapas | ForEach-Object { Prop $_ 'nome' }) -join ' > '))
    $etapaIds = @($etapas | ForEach-Object { [long](Prop $_ 'id') })
    $ultimaEtapa = $etapaIds[$etapaIds.Count - 1]

    # ---- Usuários vendedores ----
    Escrever "`n== Usuários vendedores ==" 'Cyan'
    $vendedores = New-Object System.Collections.ArrayList
    foreach ($v in $VendedoresDemo) {
        $filtro = '?$filter=' + (Q ("Email eq " + (Lit $v.Email))) + '&$select=Id'
        $ex = @(Obter-Lista ("odata/Usuarios" + $filtro))
        if ($ex.Count -gt 0) { Registrar 'Usuários' 'Existentes'; [void]$vendedores.Add([long](Prop $ex[0] 'Id')); continue }
        $senha = Nova-Senha
        # Versão nova: corpo NovoUsuarioRequest (Senha). Versão antiga: Delta<Usuario> (SenhaHash recebe a senha e é hasheada).
        $r = Invoke-Api 'POST' 'odata/Usuarios' ([ordered]@{ Nome = $v.Nome; Email = $v.Email; Senha = $senha; Perfil = 'Vendedor' })
        Pausa
        $ex = @(Obter-Lista ("odata/Usuarios" + $filtro))
        if ($ex.Count -eq 0) {
            $r = Invoke-Api 'POST' 'odata/Usuarios' ([ordered]@{ Nome = $v.Nome; Email = $v.Email; SenhaHash = $senha; Perfil = 'Vendedor'; Tipo = 'Normal' })
            Pausa
            $ex = @(Obter-Lista ("odata/Usuarios" + $filtro))
        }
        $senha = $null
        if ($ex.Count -gt 0) { Registrar 'Usuários' 'Criados'; [void]$vendedores.Add([long](Prop $ex[0] 'Id')) }
        else { Registrar 'Usuários' 'Falhas'; Aviso "Usuário '$($v.Nome)': HTTP $($r.Status) - $(Mensagem $r)" }
    }

    # ---- Etiquetas ----
    Escrever "`n== Etiquetas ==" 'Cyan'
    $i = 0
    foreach ($e in $EtiquetasDemo) {
        $i++
        [void](Criar-Registro 'Etiquetas' 'Etiquetas' ([ordered]@{ Nome = $e.Nome; Cor = $e.Cor; ChaveExterna = "${ChaveBase}:etq:$i" }) "${ChaveBase}:etq:$i" $e.Nome)
    }

    # ---- Clientes ----
    Escrever "`n== Clientes ==" 'Cyan'
    $clientes = New-Object System.Collections.ArrayList   # @{ Id; Nome; Curto }
    for ($i = 0; $i -lt $EmpresasDemo.Count; $i++) {
        $emp = $EmpresasDemo[$i]
        $n = $i + 1
        $slug = Slug $emp.N
        $corpo = [ordered]@{
            Nome = "$Prefixo $($emp.N)"; RazaoSocial = $emp.R
            Email = "contato.$slug@exemplo.test"; Telefone = ('(00) 0000-{0:0000}' -f $n)
            Documento = ('11.111.111/1111-{0:00}' -f $n)   # CNPJ inválido (dígitos repetidos), só para demo
            Cidade = $emp.C; Estado = $emp.E; Endereco = "Rua Exemplo, $($n * 10)"; Cep = '00000-000'
            Site = "https://$slug.exemplo.test"
            Observacoes = 'Cliente fictício criado pelo script de demonstração (scripts/Seed-Demo.ps1).'
            ChaveExterna = "${ChaveBase}:cli:$n"
        }
        if ($statusCli.Count -gt 0) { $corpo.StatusClienteId = [long](Prop $statusCli[$i % [Math]::Min(3, $statusCli.Count)] 'id') }
        if ($origens.Count -gt 0) { $corpo.OrigemId = $origens[$i % $origens.Count] }
        if ($ramos.Count -gt 0) { $corpo.RamoAtividadeId = $ramos[($i * 7) % $ramos.Count] }
        if ($portes.Count -gt 0) { $corpo.PorteEmpresaId = $portes[($i * 5) % $portes.Count] }
        $id = Criar-Registro 'Clientes' 'Clientes' $corpo $corpo.ChaveExterna $corpo.Nome
        [void]$clientes.Add(@{ Id = $id; Nome = $corpo.Nome; Curto = $emp.N })
    }

    # ---- Contatos ----
    Escrever "`n== Contatos ==" 'Cyan'
    $contatoDoCliente = @{}
    for ($i = 0; $i -lt $PessoasDemo.Count; $i++) {
        $cli = $clientes[$ClienteDoContato[$i]]
        if (-not $cli.Id) { Registrar 'Contatos' 'Falhas'; continue }
        $n = $i + 1
        $nome = $PessoasDemo[$i]
        $corpo = [ordered]@{
            Nome = "$Prefixo $nome"; Email = ((Slug $nome) + '@exemplo.test'); Telefone = ('(00) 90000-{0:0000}' -f $n)
            Cargo = $CargosDemo[$i % $CargosDemo.Count]; Decisor = ($i % 3 -eq 0)
            Documento = ('111.111.111-{0:00}' -f (20 + $n))   # CPF inválido, só para demo
            Observacoes = 'Contato fictício de demonstração.'; ClienteId = $cli.Id; ChaveExterna = "${ChaveBase}:ctt:$n"
        }
        $id = Criar-Registro 'Contatos' 'PessoasContato' $corpo $corpo.ChaveExterna $corpo.Nome
        if ($id -and -not $contatoDoCliente.ContainsKey($cli.Id)) { $contatoDoCliente[$cli.Id] = $id }
    }

    # ---- Produtos ----
    Escrever "`n== Produtos ==" 'Cyan'
    $produtos = New-Object System.Collections.ArrayList
    for ($i = 0; $i -lt $ProdutosDemo.Count; $i++) {
        $p = $ProdutosDemo[$i]
        $corpo = [ordered]@{
            Nome = "$Prefixo $($p.N)"; Codigo = $p.Cod; Descricao = 'Produto fictício de demonstração.'
            Preco = [decimal]$p.P; PrecoCusto = [decimal]$p.Cu; Unidade = $p.U; Categoria = $p.Cat; Disponivel = $true
            ChaveExterna = "${ChaveBase}:prd:$($i + 1)"; MoedaId = $mBRL
        }
        if ($grupos.Count -ge 3) { $corpo.GrupoProdutoId = $grupos[$p.G]; $corpo.FamiliaProdutoId = $familias[[Math]::Min(1, $p.G)] }
        elseif ($familias.Count -ge 2) { $corpo.FamiliaProdutoId = $familias[[Math]::Min(1, $p.G)] }
        $id = Criar-Registro 'Produtos' 'Produtos' $corpo $corpo.ChaveExterna $corpo.Nome
        [void]$produtos.Add(@{ Id = $id; Nome = $corpo.Nome; Codigo = $p.Cod; Preco = [decimal]$p.P })
    }

    # ---- Negócios ----
    Escrever "`n== Negócios ==" 'Cyan'
    $negocios = New-Object System.Collections.ArrayList  # @{ Id; Titulo; ClienteIdx; Situacao; Etapa; Moeda; Valor }
    $usdIdx = @(3, 11, 19, 27, 35, 42)
    $diasGanho = @(-2, -6, -11, -33, -45)     # este mês e o anterior
    $diasPerda = @(-4, -29, -38)
    for ($i = 0; $i -lt 45; $i++) {
        $n = $i + 1
        $ci = ($i * 7) % $clientes.Count
        $cli = $clientes[$ci]
        if (-not $cli.Id) { Registrar 'Negócios' 'Falhas'; continue }
        $situacao = if ($i -lt 5) { 'Ganho' } elseif ($i -lt 8) { 'Perdido' } else { 'Aberto' }
        $etapaIdx = if ($situacao -eq 'Ganho') { $etapaIds.Count - 1 } elseif ($situacao -eq 'Perdido') { $i % [Math]::Max(1, $etapaIds.Count - 1) } else { $i % $etapaIds.Count }
        $ehUsd = $usdIdx -contains $i
        $valor = [decimal]([Math]::Round(($rnd.Next(30, 1800) * 100) / 50.0) * 50)
        if ($ehUsd) { $valor = [decimal]([Math]::Round($valor / 5 / 10) * 10) }
        $titulo = "$Prefixo $($OportunidadesDemo[$i % $OportunidadesDemo.Count]) - $($cli.Curto)"
        $corpo = [ordered]@{
            Titulo = $titulo; Valor = $valor; ClienteId = $cli.Id; FunilId = $funilId; EtapaId = $etapaIds[$etapaIdx]
            EmpresaId = $script:EmpresaId   # a versão em execução não preenche o tenant sozinha no POST de negócio
            ChaveExterna = "${ChaveBase}:neg:$n"
            PrevisaoFechamento = (DataApi ($hoje.AddDays(7 + $rnd.Next(0, 80)).AddHours(12)))
        }
        if ($stAberto) { $corpo.StatusId = $stAberto }
        if ($contatoDoCliente.ContainsKey($cli.Id)) { $corpo.PessoaContatoId = $contatoDoCliente[$cli.Id] }
        $corpo.MoedaId = if ($ehUsd) { $mUSD } else { $mBRL }
        $existia = [bool](Buscar-PorChave 'Negocios' $corpo.ChaveExterna)
        $id = Criar-Registro 'Negócios' 'Negocios' $corpo $corpo.ChaveExterna $titulo
        if (-not $id) { continue }
        [void]$negocios.Add(@{ Id = $id; Titulo = $titulo; ClienteIdx = $ci; ClienteId = $cli.Id; Situacao = $situacao; EtapaIdx = $etapaIdx; Moeda = $(if ($ehUsd) { 'USD' } else { 'BRL' }); Valor = $valor })
        if ($existia) { continue }

        if ($situacao -eq 'Ganho') {
            $rg = Invoke-Api 'POST' "odata/Negocios($id)/Ganhar"
            Pausa
            if (-not $rg.Ok) { Aviso "Ganhar negócio $id`: HTTP $($rg.Status) - $(Mensagem $rg)" }
            $patch = [ordered]@{ FechadoEm = (DataApi ($hoje.AddDays($diasGanho[$i]).AddHours(15))) }
            if ($stGanho) { $patch.StatusId = $stGanho }
            [void](Atualizar-Registro 'Negócios' "odata/Negocios($id)" $patch $titulo)
        } elseif ($situacao -eq 'Perdido') {
            $k = $i - 5
            $motivoId = if ($motivos.Count -gt 0) { $motivos[$k % $motivos.Count] } else { $null }
            $rp = Invoke-Api 'POST' "odata/Negocios($id)/Perder" @{ MotivoPerdaId = $motivoId }
            Pausa
            if (-not $rp.Ok) { Aviso "Perder negócio $id`: HTTP $($rp.Status) - $(Mensagem $rp)" }
            $patch = [ordered]@{ FechadoEm = (DataApi ($hoje.AddDays($diasPerda[$k]).AddHours(11))) }
            if ($stPerdido) { $patch.StatusId = $stPerdido }
            if ($motivoId) { $patch.MotivoPerdaId = $motivoId }
            [void](Atualizar-Registro 'Negócios' "odata/Negocios($id)" $patch $titulo)
        }
    }

    # ---- Atividades ----
    Escrever "`n== Atividades ==" 'Cyan'
    $cacheAtiv = @(Obter-Lista 'odata/Atividades')
    $titulosUsados = @{}
    function Criar-Atividade($corpo, [bool]$concluir) {
        $t = $corpo.Titulo
        $ex = $cacheAtiv | Where-Object { [string](Prop $_ 'titulo') -eq $t } | Select-Object -First 1
        if ($ex) { Registrar 'Atividades' 'Existentes'; return }
        $r = Invoke-Api 'POST' 'odata/Atividades' $corpo
        Pausa
        $id = if ($r.Ok) { Prop $r.Dados 'id' } else { $null }
        $semOpc = $false
        if (-not $id) {
            $lista = @(Obter-Lista 'odata/Atividades')
            $ex = $lista | Where-Object { [string](Prop $_ 'titulo') -eq $t } | Select-Object -First 1
            if ($ex) { $id = Prop $ex 'id' }
        }
        if (-not $id) {
            foreach ($variante in (Variantes $corpo)) {
                $r = Invoke-Api 'POST' 'odata/Atividades' $variante
                Pausa
                $id = if ($r.Ok) { Prop $r.Dados 'id' } else { $null }
                if (-not $id) {
                    $lista = @(Obter-Lista 'odata/Atividades')
                    $ex = $lista | Where-Object { [string](Prop $_ 'titulo') -eq $t } | Select-Object -First 1
                    if ($ex) { $id = Prop $ex 'id' }
                }
                if ($id) { $semOpc = $true; break }
            }
        }
        if (-not $id) { Registrar 'Atividades' 'Falhas'; Aviso "Atividade '$t': HTTP $($r.Status) - $(Mensagem $r)"; return }
        Registrar 'Atividades' 'Criados'
        if ($semOpc) { Registrar 'Atividades' 'SemOpcionais' }
        if ($concluir) {
            $rc = Invoke-Api 'POST' "odata/Atividades($id)/Concluir"
            Pausa
            if (-not $rc.Ok) { Aviso "Concluir atividade $id`: HTTP $($rc.Status)" }
        }
    }

    $modelos = @(
        @{ T = 'Ligacao'; A = 'Ligar para retorno' }, @{ T = 'Email'; A = 'Enviar material técnico' },
        @{ T = 'Reuniao'; A = 'Reunião de alinhamento' }, @{ T = 'Tarefa'; A = 'Preparar proposta' },
        @{ T = 'Ligacao'; A = 'Confirmar orçamento' }, @{ T = 'Reuniao'; A = 'Demonstração do produto' },
        @{ T = 'Email'; A = 'Enviar contrato para revisão' }, @{ T = 'Tarefa'; A = 'Atualizar cadastro' }
    )
    $plano = @()
    foreach ($d in @(-1, -2, -3, -4, -6, -8, -10, -12)) { $plano += @{ G = 'atrasada'; D = $hoje.AddDays($d).AddHours(10) } }
    foreach ($h in @(9, 10, 11, 14, 16, 17)) { $plano += @{ G = 'hoje'; D = $hoje.AddHours($h) } }
    foreach ($d in @(1, 2, 3, 4, 5, 7, 8, 10, 12, 14)) { $plano += @{ G = 'futura'; D = $hoje.AddDays($d).AddHours(9 + ($d % 7)) } }
    foreach ($d in @(-1, -3, -5, -9, -14, -20)) { $plano += @{ G = 'concluida'; D = $hoje.AddDays($d).AddHours(14) } }
    $abertos = @($negocios | Where-Object { $_.Situacao -eq 'Aberto' })
    for ($i = 0; $i -lt $plano.Count; $i++) {
        $pl = $plano[$i]
        $mo = $modelos[$i % $modelos.Count]
        $neg = if ($abertos.Count -gt 0) { $abertos[($i * 3) % $abertos.Count] } else { $null }
        $cli = if ($neg) { $clientes[$neg.ClienteIdx] } else { $clientes[$i % $clientes.Count] }
        if (-not $cli.Id) { Registrar 'Atividades' 'Falhas'; continue }
        $titulo = "$Prefixo $($mo.A) - $($cli.Curto)"
        if ($titulosUsados.ContainsKey($titulo)) { $titulosUsados[$titulo]++; $titulo = "$titulo ($($titulosUsados[$titulo]))" } else { $titulosUsados[$titulo] = 1 }
        $corpo = [ordered]@{
            Titulo = $titulo; Tipo = $mo.T
            Descricao = ("Atividade fictícia de demonstração ({0}). Prazo planejado: {1}." -f $pl.G, $pl.D.ToString('dd/MM/yyyy HH:mm', $script:PtBr))
            ClienteId = $cli.Id; DataVencimento = (DataApi $pl.D)
        }
        if ($neg -and ($i % 4 -ne 3)) { $corpo.NegocioId = $neg.Id }
        if ($contatoDoCliente.ContainsKey($cli.Id)) { $corpo.PessoaContatoId = $contatoDoCliente[$cli.Id] }
        Criar-Atividade $corpo ($pl.G -eq 'concluida')
    }
    # Notas com o motivo de cada negócio perdido (registro visível mesmo sem cadastro de motivos)
    $motivosTexto = @('Preço acima do orçamento do cliente', 'Cliente escolheu um concorrente', 'Projeto adiado para o próximo ano')
    $k = 0
    foreach ($neg in @($negocios | Where-Object { $_.Situacao -eq 'Perdido' })) {
        $corpo = [ordered]@{
            Titulo = "$Prefixo Motivo da perda - $($clientes[$neg.ClienteIdx].Curto)"; Tipo = 'Nota'
            Descricao = "Negócio perdido: $($motivosTexto[$k % 3])."; ClienteId = $neg.ClienteId; NegocioId = $neg.Id
        }
        Criar-Atividade $corpo $true
        $k++
    }

    # ---- Propostas ----
    Escrever "`n== Propostas ==" 'Cyan'
    $alvos = @()
    $alvos += @($negocios | Where-Object { $_.Situacao -eq 'Aberto' -and $_.EtapaIdx -ge [Math]::Max(0, $etapaIds.Count - 3) } | Select-Object -First 6)
    $alvos += @($negocios | Where-Object { $_.Situacao -eq 'Ganho' } | Select-Object -First (8 - $alvos.Count))
    $statusProp = @('Pendente', 'Aprovada', 'Rejeitada', 'EmRevisao', 'Pendente', 'Aprovada', 'EmRevisao', 'Pendente')
    $formas = @('Boleto bancário', 'PIX', 'Cartão de crédito', 'Transferência bancária')
    $qtdItens = @(2, 3, 4, 5, 2, 3, 4, 5)
    $qtdParcelas = @(1, 3, 2, 6, 1, 4, 2, 3)
    $script:ItensSuportados = $true
    $script:ParcelasSuportadas = $true
    for ($i = 0; $i -lt $alvos.Count; $i++) {
        $neg = $alvos[$i]
        $n = $i + 1
        $chave = "${ChaveBase}:prp:$n"
        $cli = $clientes[$neg.ClienteIdx]
        # Itens (calculados aqui; total = qtd x preço x (1 - desconto%))
        $itens = @()
        for ($j = 0; $j -lt $qtdItens[$i]; $j++) {
            $pr = $produtos[($i * 3 + $j * 5) % $produtos.Count]
            $qtd = [double](1 + (($i + $j) % 4) * ($(if ($pr.Preco -lt 300) { 5 } else { 1 })))
            $desc = [double]((($i + $j) % 3) * 5)
            $preco = $pr.Preco
            if ($neg.Moeda -eq 'USD') { $preco = [decimal]([Math]::Round([double]$preco / 5.0, 2)) }
            $total = [decimal]([Math]::Round([double]$preco * $qtd * (1 - $desc / 100), 2))
            $itens += @{ P = $pr; Qtd = $qtd; Desc = $desc; Preco = $preco; Total = $total; Ordem = $j + 1 }
        }
        $subtotal = [decimal]0; foreach ($it in $itens) { $subtotal += $it.Total }
        $descontoProp = [decimal]([Math]::Round([double]$subtotal * (($i % 3) * 0.03), 2))
        $frete = if ($i % 2 -eq 0) { [decimal]0 } else { [decimal](150 + $i * 25) }
        $valor = $subtotal - $descontoProp + $frete
        $np = $qtdParcelas[$i]
        $parcelas = @()
        $acum = [decimal]0
        for ($p = 1; $p -le $np; $p++) {
            $v = if ($p -lt $np) { [decimal]([Math]::Round([double]$valor / $np, 2)) } else { $valor - $acum }
            $acum += $v
            $parcelas += @{ Numero = $p; Venc = $hoje.AddDays(30 * $p); Valor = $v; Pct = [decimal]([Math]::Round(100.0 / $np, 2)) }
        }
        $txtItens = ($itens | ForEach-Object { "- {0} x {1} ({2} cada, desc. {3}%) = {4}" -f $_.Qtd, $_.P.Nome.Replace("$Prefixo ", ''), (Moeda $_.Preco $neg.Moeda), $_.Desc, (Moeda $_.Total $neg.Moeda) }) -join "`n"
        $txtParc = ($parcelas | ForEach-Object { "{0}/{1}: {2} em {3}" -f $_.Numero, $np, (Moeda $_.Valor $neg.Moeda), $_.Venc.ToString('dd/MM/yyyy', $script:PtBr) }) -join "; "
        $st = $statusProp[$i]
        $corpo = [ordered]@{
            Titulo = "$Prefixo Proposta comercial - $($cli.Curto)"
            Descricao = "Proposta fictícia de demonstração.`nItens:`n$txtItens"
            Observacoes = "Parcelas: $txtParc"
            Numero = 9000 + $n; Revisao = 1; ClienteId = $neg.ClienteId; NegocioId = $neg.Id
            Valor = $valor; Desconto = $descontoProp; CustoFrete = $frete; ModalFrete = $(if ($frete -gt 0) { 'CIF' } else { 'Sem frete' })
            NumeroParcelas = $np; FormaPagamento = $formas[$i % $formas.Count]; PrazoEntrega = "$(10 + $i * 5) dias úteis"
            ChaveExterna = $chave; DataExpiracao = (DataApi ($hoje.AddDays(15 + $i * 3).AddHours(18)))
            MoedaId = $(if ($neg.Moeda -eq 'USD') { $mUSD } else { $mBRL })
        }
        if ($st -eq 'Pendente' -or $st -eq 'EmRevisao') { $corpo.StatusAprovacao = $st }
        if ($vendedores.Count -gt 0) { $corpo.ResponsavelId = $vendedores[$i % $vendedores.Count] }
        $existia = [bool](Buscar-PorChave 'Propostas' $chave)
        $propId = Criar-Registro 'Propostas' 'Propostas' $corpo $chave $corpo.Titulo
        if (-not $propId -or $existia) { continue }

        # Itens
        if ($script:ItensSuportados) {
            foreach ($it in $itens) {
                $ci = [ordered]@{
                    PropostaId = $propId; ProdutoId = $it.P.Id; ProdutoNome = $it.P.Nome; ProdutoCodigo = $it.P.Codigo
                    Quantidade = $it.Qtd; PrecoUnitario = $it.Preco; Desconto = $it.Desc; Total = $it.Total; Ordem = $it.Ordem
                }
                $r = Invoke-Api 'POST' 'odata/Propostas@Produtos' $ci
                Pausa
                if (-not $r.Ok) {
                    $conf = @(Obter-Lista ("odata/Propostas@Produtos?`$filter=" + (Q "PropostaId eq $propId")))
                    if ($conf.Count -lt $it.Ordem) {
                        $script:ItensSuportados = $false
                        Aviso "Itens de proposta não puderam ser gravados (HTTP $($r.Status) - $(Mensagem $r)); os itens ficam descritos no campo Descrição."
                        break
                    }
                }
                Registrar 'Itens de proposta' 'Criados'
            }
        }
        # Parcelas
        if ($script:ParcelasSuportadas) {
            foreach ($pa in $parcelas) {
                $cp = [ordered]@{ PropostaId = $propId; Numero = $pa.Numero; Valor = $pa.Valor; Percentual = $pa.Pct; Descricao = "Parcela $($pa.Numero)/$np"; DataVencimento = (DataApi $pa.Venc.AddHours(12)) }
                $r = Invoke-Api 'POST' 'odata/Propostas@Parcelas' $cp
                Pausa
                if (-not $r.Ok) { $r = Invoke-Api 'POST' 'odata/Propostas@Parcelas' (Sem-Opcionais $cp); Pausa; if ($r.Ok) { Registrar 'Parcelas de proposta' 'SemOpcionais' } }
                if (-not $r.Ok) {
                    $conf = @(Obter-Lista ("odata/Propostas@Parcelas?`$filter=" + (Q "PropostaId eq $propId")))
                    if ($conf.Count -lt $pa.Numero) {
                        $script:ParcelasSuportadas = $false
                        Aviso "Parcelas de proposta não puderam ser gravadas (HTTP $($r.Status) - $(Mensagem $r)); as parcelas ficam descritas em Observações."
                        break
                    }
                }
                Registrar 'Parcelas de proposta' 'Criados'
            }
        }
        if ($script:ItensSuportados) {
            $rr = Invoke-Api 'POST' "odata/Propostas($propId)/Recalcular"
            Pausa
        }
        # Status de aprovação
        if ($st -eq 'Aprovada' -or $st -eq 'Rejeitada') {
            $acao = if ($st -eq 'Aprovada') { 'Aprovar' } else { 'Rejeitar' }
            $ra = Invoke-Api 'POST' "odata/Propostas($propId)/$acao"
            Pausa
            if (-not $ra.Ok) {
                if (-not (Atualizar-Registro 'Propostas' "odata/Propostas($propId)" ([ordered]@{ StatusAprovacao = $st }) $corpo.Titulo)) { Aviso "Status '$st' da proposta $propId não aplicado." }
            }
        }
    }
}

# ----------------------------------------------------------------------------------------------
# Resumo lido de volta da API
# ----------------------------------------------------------------------------------------------
function Mostrar-Resumo {
    Escrever "`n== Resumo da execução ==" 'Cyan'
    $linhas = foreach ($k in $script:Resultado.Keys) {
        $v = $script:Resultado[$k]
        [pscustomobject]@{ Entidade = $k; Criados = $v.Criados; 'Já existiam' = $v.Existentes; Removidos = $v.Removidos; Falhas = $v.Falhas; 'Sem opcionais' = $v.SemOpcionais }
    }
    if ($linhas) { $linhas | Format-Table -AutoSize | Out-String -Width 160 | Write-Host }

    Escrever "== Registros $Prefixo na API (lido de volta) ==" 'Cyan'
    $statusNeg = @(Obter-Lista 'odata/Negocios@Status')
    $g = $statusNeg | Where-Object { [string](Prop $_ 'tipoBase') -in @('1', 'Ganho') } | Select-Object -First 1
    $p = $statusNeg | Where-Object { [string](Prop $_ 'tipoBase') -in @('2', 'Perdido') } | Select-Object -First 1
    $fNeg = "startswith(Titulo," + (Lit $Prefixo) + ")"
    $propostas = @(Ids-Demo 'Propostas' 'Titulo')
    $nItens = 0; $nParc = 0
    foreach ($propId in $propostas) {
        $nItens += @(Obter-Lista ("odata/Propostas@Produtos?`$filter=" + (Q "PropostaId eq $propId"))).Count
        $nParc += @(Obter-Lista ("odata/Propostas@Parcelas?`$filter=" + (Q "PropostaId eq $propId"))).Count
    }
    $ativ = @(Obter-Lista 'odata/Atividades' | Where-Object { ([string](Prop $_ 'titulo')).StartsWith($Prefixo) })
    $agora = Get-Date
    $comData = @($ativ | Where-Object { Prop $_ 'dataVencimento' })
    $dados = [ordered]@{
        'Clientes'                 = Contar-Api 'Clientes' (FiltroDemo 'Nome')
        'Contatos'                 = Contar-Api 'PessoasContato' (FiltroDemo 'Nome')
        'Produtos'                 = Contar-Api 'Produtos' (FiltroDemo 'Nome')
        'Negócios (total)'         = Contar-Api 'Negocios' (FiltroDemo 'Titulo')
        'Negócios ganhos'          = $(if ($g) { Contar-Api 'Negocios' (Q ("$fNeg and StatusId eq " + (Prop $g 'id'))) } else { 'n/d' })
        'Negócios perdidos'        = $(if ($p) { Contar-Api 'Negocios' (Q ("$fNeg and StatusId eq " + (Prop $p 'id'))) } else { 'n/d' })
        'Negócios com FechadoEm'   = Contar-Api 'Negocios' (Q "$fNeg and FechadoEm ne null")
        'Negócios em USD'          = $(if ($script:MoedaUSD) { Contar-Api 'Negocios' (Q "$fNeg and MoedaId eq $($script:MoedaUSD)") } else { 'n/d' })
        'Atividades'               = $ativ.Count
        'Atividades concluídas'    = @($ativ | Where-Object { Prop $_ 'concluida' }).Count
        'Atividades com prazo'     = $comData.Count
        'Propostas'                = @($propostas).Count
        'Itens de proposta'        = $nItens
        'Parcelas de proposta'     = $nParc
        'Etiquetas'                = Contar-Api 'Etiquetas' (FiltroDemo 'Nome')
        'Usuários vendedores'      = Contar-Api 'Usuarios' (FiltroDemo 'Nome')
    }
    foreach ($k in $dados.Keys) { Write-Host ("  {0,-26} {1}" -f $k, $dados[$k]) }
    if ($script:Pausas429 -gt 0) { Escrever "  (pausas por rate limit: $script:Pausas429)" 'DarkGray' }
    if ($script:Avisos.Count -gt 0) { Escrever "`n$($script:Avisos.Count) aviso(s) durante a execução (ver acima)." 'Yellow' }
    Escrever "`nPara remover os dados demo: .\scripts\Seed-Demo.ps1 -Remover" 'DarkGray'
}

# ----------------------------------------------------------------------------------------------
# Execução
# ----------------------------------------------------------------------------------------------
Escrever "Seed de demonstração AST Comercial -> $script:Base" 'Cyan'
Entrar
Escrever 'Login do admin do seed: ok.'
$script:TemAdminCadastros = (Invoke-Api 'GET' 'odata/AdminCadastros/tipos').Ok

if ($Remover -or $Recriar) { Remover-Demo }
if (-not $Remover) { Criar-Demo }
Mostrar-Resumo
$script:Cred = $null
$script:Token = $null
