rmdir /Q /S ..\Release
mkdir ..\Release

xcopy /E /EXCLUDE:exclude.txt . ..\Release