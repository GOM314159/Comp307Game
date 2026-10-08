# Renders only the frames the cutscene 1 stop-motion cut uses.
# Safe to stop and re-run; existing frames are skipped.
$B = "C:\Program Files (x86)\Steam\steamapps\common\Blender\blender.exe"
$F = Join-Path $PSScriptRoot "cutscene1.blend"

Write-Host 'scene 1 of 10: frames 1..291 step 10'
& $B -b $F -s 1 -e 291 -j 10 -a
Write-Host 'scene 2 of 10: frames 301..504 step 7'
& $B -b $F -s 301 -e 504 -j 7 -a
Write-Host 'scene 3 of 10: frames 511..772 step 9'
& $B -b $F -s 511 -e 772 -j 9 -a
Write-Host 'scene 4 of 10: frames 781..1100 step 11'
& $B -b $F -s 781 -e 1100 -j 11 -a
Write-Host 'scene 5 of 10: frames 1111..1372 step 9'
& $B -b $F -s 1111 -e 1372 -j 9 -a
Write-Host 'scene 6 of 10: frames 1381..1758 step 13'
& $B -b $F -s 1381 -e 1758 -j 13 -a
Write-Host 'scene 7 of 10: frames 1771..1887 step 4'
& $B -b $F -s 1771 -e 1887 -j 4 -a
Write-Host 'scene 8 of 10: frames 1891..2152 step 9'
& $B -b $F -s 1891 -e 2152 -j 9 -a
Write-Host 'scene 9 of 10: frames 2161..2364 step 7'
& $B -b $F -s 2161 -e 2364 -j 7 -a
Write-Host 'scene 10 of 10: frames 2371..2719 step 12'
& $B -b $F -s 2371 -e 2719 -j 12 -a
Write-Host 'done'
