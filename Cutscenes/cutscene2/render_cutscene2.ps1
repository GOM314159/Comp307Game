# Renders only the frames the cutscene 2 stop-motion cut uses.
# Safe to stop and re-run; existing frames are skipped.
$B = "C:\Program Files (x86)\Steam\steamapps\common\Blender\blender.exe"
$F = Join-Path $PSScriptRoot "cutscene2.blend"

Write-Host 'scene 1 of 5: frames 1..407 step 14'
& $B -b $F -s 1 -e 407 -j 14 -a
Write-Host 'scene 2 of 5: frames 421..711 step 10'
& $B -b $F -s 421 -e 711 -j 10 -a
Write-Host 'scene 3 of 5: frames 721..1069 step 12'
& $B -b $F -s 721 -e 1069 -j 12 -a
Write-Host 'scene 4 of 5: frames 1081..1342 step 9'
& $B -b $F -s 1081 -e 1342 -j 9 -a
Write-Host 'scene 5 of 5: frames 1351..1815 step 16'
& $B -b $F -s 1351 -e 1815 -j 16 -a
Write-Host 'done'
