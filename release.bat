rmdir /Q /S ..\Release
mkdir ..\Release
mkdir ..\Release\ECGToolkit

xcopy /E /EXCLUDE:exclude.txt . ..\Release\ECGToolkit