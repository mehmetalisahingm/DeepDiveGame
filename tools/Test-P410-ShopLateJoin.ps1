param(
  [string]$BuildPath = '',
  [ValidateRange(0,65535)][int]$Port = 0
)
$ErrorActionPreference='Stop'
# Locate the project even if the tester invokes the script outside the repo root.
$root=Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($BuildPath)) { $BuildPath=Join-Path $root 'Builds/P1-Integrated/DeepDiveGame-P1.exe' }
elseif (-not [System.IO.Path]::IsPathRooted($BuildPath)) { $BuildPath=Join-Path $root $BuildPath }
if (-not (Test-Path -LiteralPath $BuildPath -PathType Leaf)) { throw "Missing integrated Windows build: $BuildPath" }
if ($Port -ne 0 -and $Port -lt 1024) { throw 'Port must be 0 (automatic) or 1024..65535.' }
if ($Port -eq 0) {
  # Two independent runs must not share the old fixed 25946/UDP port.
  $probe=[System.Net.Sockets.UdpClient]::new(0)
  try { $Port=[int]$probe.Client.LocalEndPoint.Port }
  finally { $probe.Dispose() }
}
$run=Join-Path $root ('Logs/p246-late-'+(Get-Date -Format 'yyyyMMdd-HHmmss-fff')+'-'+[Guid]::NewGuid().ToString('N').Substring(0,6))
New-Item -ItemType Directory -Path $run -Force | Out-Null
$marker=Join-Path $run 'sync'
$procs=@()
try {
  foreach($slot in 0,1) {
    if ($slot -eq 1) {
      $until=(Get-Date).AddSeconds(180)
      while (-not (Test-Path -LiteralPath ($marker+'.ready'))) {
        if ($procs[0].HasExited) { throw 'Host exited before selecting shelf item' }
        if ((Get-Date) -gt $until) { throw 'Host did not select shelf item' }
        Start-Sleep -Milliseconds 500
      }
    }
    $role=if($slot -eq 0){'host'}else{'client'}
    $report=Join-Path $run "player$slot.json"
    $log=Join-Path $run "player$slot.log"
    $args=@('-batchmode','-p46-day-role',$role,'-p46-day-count','2',
      '-p46-day-slot',"$slot",'-p46-day-port',"$Port",
      '-p246-late-shop','1','-p246-sync',('"{0}"' -f $marker),
      '-p46-mute','1','-p1-report',('"{0}"' -f $report),
      '-logFile',('"{0}"' -f $log),
      '-screen-width','960','-screen-height','540','-screen-fullscreen','0')
    $procs+=Start-Process -FilePath $BuildPath -ArgumentList $args -WindowStyle Hidden -PassThru
  }
  foreach($p in $procs) { if (-not $p.WaitForExit(240000)) { throw "Process timed out: $($p.Id)" } }
  foreach($slot in 0,1) {
    $report=Join-Path $run "player$slot.json"
    if (-not (Test-Path -LiteralPath $report -PathType Leaf)) { throw "Player $slot produced no report" }
    $r=Get-Content -Raw -LiteralPath $report | ConvertFrom-Json
    if (-not $r.passed -or -not $r.lateShopJoined -or -not $r.lateShopSawSelection -or -not $r.lateShopSawBasket) {
      throw "Player $slot failed late-shop selection/basket/visibility proof: $($r.failure)"
    }
  }
  if (-not (Test-Path -LiteralPath ($marker+'.observed'))) { throw 'Guest observation proof was not written' }
  $guestShot=Join-Path $run 'player1.json.late-shop.png'
  if (-not (Test-Path -LiteralPath $guestShot -PathType Leaf)) { throw 'Guest basket image missing' }
  Write-Output "PASS: host selected camera before guest joined; guest mirrored the choice and viewed basket. UDP port=$Port"
} finally {
  foreach($p in $procs) { if ($p -ne $null -and -not $p.HasExited) { Stop-Process -Id $p.Id -ErrorAction SilentlyContinue } }
  Write-Output "Evidence: $run"
}
