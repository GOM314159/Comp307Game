@echo off
cd /d "%~dp0"

echo Encoding PNG frames to MP4...
echo.

ffmpeg -y ^
  -framerate 30 ^
  -pattern_type glob ^
  -i "frames/f_*.png" ^
  -i vo_intro.wav ^
  -c:v libx264 ^
  -preset fast ^
  -crf 18 ^
  -c:a aac ^
  -b:a 192k ^
  -shortest ^
  intro_cutscene.mp4

echo.
echo Encode complete! Check intro_cutscene.mp4
pause