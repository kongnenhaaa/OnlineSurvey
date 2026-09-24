$ErrorActionPreference = 'Stop'

function Find-Executable {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CommandName,

        [Parameter(Mandatory = $true)]
        [string[]]$SearchPatterns
    )

    $pathCommand = Get-Command $CommandName -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -ne $pathCommand) {
        return $pathCommand.Source
    }

    foreach ($pattern in $SearchPatterns) {
        if ([string]::IsNullOrWhiteSpace($pattern)) {
            continue
        }

        $match = Get-ChildItem -Path $pattern -File -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending |
            Select-Object -First 1

        if ($null -ne $match) {
            return $match.FullName
        }
    }

    return $null
}

function Test-LocalPort {
    param(
        [Parameter(Mandatory = $true)]
        [int]$Port
    )

    $client = [System.Net.Sockets.TcpClient]::new()
    try {
        $connectTask = $client.ConnectAsync('127.0.0.1', $Port)
        if (-not $connectTask.Wait(3000)) {
            return $false
        }

        return $client.Connected
    }
    catch {
        return $false
    }
    finally {
        $client.Dispose()
    }
}

$mongoData = Join-Path $env:LOCALAPPDATA 'MongoDB\data\OnlineSurvey'
$mongoLog = Join-Path $env:LOCALAPPDATA 'MongoDB\logs\OnlineSurvey.log'
$redisData = Join-Path $env:LOCALAPPDATA 'Redis\data\OnlineSurvey'
$redisLog = Join-Path $env:LOCALAPPDATA 'Redis\logs\OnlineSurvey.log'

New-Item -ItemType Directory -Force -Path $mongoData, (Split-Path $mongoLog), $redisData, (Split-Path $redisLog) | Out-Null

if (-not (Get-Process mongod -ErrorAction SilentlyContinue)) {
    $mongoPatterns = @(
        (Join-Path $env:LOCALAPPDATA 'MongoDB\*\mongodb-win32-*\bin\mongod.exe'),
        (Join-Path $env:LOCALAPPDATA 'MongoDB\*\bin\mongod.exe'),
        (Join-Path $env:ProgramFiles 'MongoDB\Server\*\bin\mongod.exe')
    )

    if (${env:ProgramFiles(x86)}) {
        $mongoPatterns += Join-Path ${env:ProgramFiles(x86)} 'MongoDB\Server\*\bin\mongod.exe'
    }

    $mongoExe = Find-Executable -CommandName 'mongod.exe' -SearchPatterns $mongoPatterns
    if (-not $mongoExe) {
        throw 'Khong tim thay mongod.exe. Hay cai MongoDB Community Server roi chay lai script.'
    }

    Start-Process -FilePath $mongoExe `
        -ArgumentList @('--dbpath', "`"$mongoData`"", '--logpath', "`"$mongoLog`"", '--logappend', '--bind_ip', '127.0.0.1', '--port', '27017') `
        -WindowStyle Hidden | Out-Null
}

if (-not (Get-Process redis-server -ErrorAction SilentlyContinue)) {
    $redisPatterns = @(
        (Join-Path $env:LOCALAPPDATA 'Redis\*\redis-server.exe'),
        (Join-Path $env:LOCALAPPDATA 'Redis\redis-server.exe'),
        (Join-Path $env:ProgramFiles 'Redis\redis-server.exe')
    )

    if (${env:ProgramFiles(x86)}) {
        $redisPatterns += Join-Path ${env:ProgramFiles(x86)} 'Redis\redis-server.exe'
    }

    $redisExe = Find-Executable -CommandName 'redis-server.exe' -SearchPatterns $redisPatterns
    if (-not $redisExe) {
        throw 'Khong tim thay redis-server.exe. Hay cai Redis tuong thich Windows roi chay lai script.'
    }

    Start-Process -FilePath $redisExe `
        -ArgumentList @('--bind', '127.0.0.1', '--port', '6379', '--dir', "`"$redisData`"", '--loglevel', 'notice', '--logfile', "`"$redisLog`"") `
        -WorkingDirectory (Split-Path -Parent $redisExe) -WindowStyle Hidden | Out-Null
}

Start-Sleep -Seconds 2

if (-not (Test-LocalPort -Port 27017)) {
    throw "MongoDB chua san sang tai 127.0.0.1:27017. Kiem tra log: $mongoLog"
}

if (-not (Test-LocalPort -Port 6379)) {
    throw "Redis chua san sang tai 127.0.0.1:6379. Kiem tra log: $redisLog"
}

Write-Host 'MongoDB: running at 127.0.0.1:27017'
Write-Host 'Redis:   running at 127.0.0.1:6379'
