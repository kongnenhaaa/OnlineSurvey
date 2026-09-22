$ErrorActionPreference = 'Stop'

$mongoBin = Join-Path $env:LOCALAPPDATA 'MongoDB\8.0.32\mongodb-win32-x86_64-windows-8.0.32\bin'
$mongoData = Join-Path $env:LOCALAPPDATA 'MongoDB\data\OnlineSurvey'
$mongoLog = Join-Path $env:LOCALAPPDATA 'MongoDB\logs\OnlineSurvey.log'
$redisRoot = Join-Path $env:LOCALAPPDATA 'Redis\3.0.504'
$redisData = Join-Path $redisRoot 'data'
$redisLog = Join-Path $redisRoot 'redis.log'

New-Item -ItemType Directory -Force -Path $mongoData, (Split-Path $mongoLog), $redisData | Out-Null

if (-not (Get-Process mongod -ErrorAction SilentlyContinue)) {
    Start-Process -FilePath (Join-Path $mongoBin 'mongod.exe') `
        -ArgumentList @('--dbpath', $mongoData, '--logpath', $mongoLog, '--logappend', '--bind_ip', '127.0.0.1', '--port', '27017') `
        -WindowStyle Hidden | Out-Null
}

if (-not (Get-Process redis-server -ErrorAction SilentlyContinue)) {
    Start-Process -FilePath (Join-Path $redisRoot 'redis-server.exe') `
        -ArgumentList @('--bind', '127.0.0.1', '--port', '6379', '--dir', $redisData, '--loglevel', 'notice', '--logfile', $redisLog) `
        -WorkingDirectory $redisRoot -WindowStyle Hidden | Out-Null
}

Start-Sleep -Seconds 2
Write-Host 'MongoDB: 127.0.0.1:27017'
Write-Host 'Redis:   127.0.0.1:6379'
