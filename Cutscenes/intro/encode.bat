@echo off
cd /d "C:\Users\imaha\source\repos\Comp307Game\Cutscenes\intro"

echo Encoding PNG frames + audio to MP4...
echo.

ffmpeg -y -framerate 30 -pattern_type glob -i "frames\f_*.png" -i "intro_VO.wav" -c:v libx264 -preset fast -crf 18 -c:a aac -b:a 192k -shortest "..\..\..\Assets\Cutscene Videos\intro_cutscene.mp4"

echo.
if errorlevel 1 (
    echo Error: FFmpeg encoding failed
    pause
) else (
    echo Encoding complete!
    echo Output: C:\Users\imaha\source\repos\Comp307Game\Assets\Cutscene Videos\intro_cutscene.mp4
    pause
)
