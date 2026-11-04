$ErrorActionPreference='Stop'
$root=(Get-Location).Path
$run=Join-Path $root ('Logs/p246-late-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $run | Out-Null
$marker=Join-Path $run 'sync'
$exe=Join-Path $root 'Builds/P1-Integrated/DeepDiveGame-P1.exe'
$procs=@()
try {
  foreach($slot in 0,1) {
    if ($slot -eq 1) {
      $until=(Get-Date).AddSeconds(180)
      while (-not (Test-Path -LiteralPath ($marker+'.ready'))) {
        if ($procs[0].HasExited) { throw 'Host exited before selection' }
        if ((Get-Date) -gt $until) { throw 'Host did not select shelf item' }
        Start-Sleep -Milliseconds 500
      }
    }
    $role=if($slot -eq 0){'host'}else{'client'}
    $report=Join-Path $run "player$slot.json"
    $log=Join-Path $run "player$slot.log"
    $args=@('-batchmode','-p46-day-role',$role,'-p46-day-count','2','-p46-day-slot',"$slot",'-p46-day-port','25946','-p246-late-shop','1','-p246-sync',('"{0}"' -f $marker),'-p46-mute','1','-p1-report',('"{0}"' -f $report),'-logFile',('"{0}"' -f $log),'-screen-width','960','-screen-height','540','-screen-fullscreen','0')
    $procs+=Start-Process -FilePath $exe -ArgumentList $args -WindowStyle Hidden -PassThru
  }
  foreach($p in $procs) { if (-not $p.WaitForExit(240000)) { throw 'Process timed out' } }
  foreach($slot in 0,1) { $r=Get-Content -Raw -LiteralPath (Join-Path $run "player$slot.json") | ConvertFrom-Json; if(-not $r.passed){throw "Player $slot failed: $($r.failure)"} }
  Write-Output "PASS $run"
} finally { foreach($p in $procs) { if (-not $p.HasExited) { Stop-Process -Id $p.Id -ErrorAction SilentlyContinue } }; Write-Output "Evidence $run" }
