@echo off
set src=C:\OpenCASCADE6.5.3\ros
set target=C:\Users\pgnj4\Documents\xBIM\Head
echo Copying from %src% to %target%
echo Win32 Library Release files
pause
mkdir %target%

mkdir %target%\Xbim.ModelGeometry\OpenCascade
mkdir %target%\Xbim.ModelGeometry\OpenCascade\Win32
mkdir %target%\Xbim.ModelGeometry\OpenCascade\Win64
mkdir %target%\Xbim.ModelGeometry\OpenCascade\Win32\lib 
mkdir %target%\Xbim.ModelGeometry\OpenCascade\Win64\lib 
mkdir %target%\Xbim.ModelGeometry\OpenCascade\Win64\bin 
mkdir %target%\Xbim.ModelGeometry\OpenCascade\Win64\bin 
xcopy %src%\win32\vc10\lib\TKBool.lib %target%\Xbim.ModelGeometry\OpenCascade\Win32\lib  /Y /R
xcopy %src%\win32\vc10\lib\TKernel.lib %target%\Xbim.ModelGeometry\OpenCascade\Win32\lib  /Y /R
xcopy %src%\Win32\vc10\lib\TKAdvTools.lib %target%\Xbim.ModelGeometry\OpenCascade\Win32\lib  /Y /R
xcopy %src%\win32\vc10\lib\TKMath.lib %target%\Xbim.ModelGeometry\OpenCascade\Win32\lib  /Y /R
xcopy %src%\win32\vc10\lib\TKTopAlgo.lib %target%\Xbim.ModelGeometry\OpenCascade\Win32\lib  /Y /R
xcopy %src%\win32\vc10\lib\TKGeomAlgo.lib %target%\Xbim.ModelGeometry\OpenCascade\Win32\lib  /Y /R
xcopy %src%\win32\vc10\lib\TKBRep.lib %target%\Xbim.ModelGeometry\OpenCascade\Win32\lib  /Y /R
xcopy %src%\win32\vc10\lib\TKMesh.lib %target%\Xbim.ModelGeometry\OpenCascade\Win32\lib  /Y /R
xcopy %src%\win32\vc10\lib\TKPrim.lib %target%\Xbim.ModelGeometry\OpenCascade\Win32\lib  /Y /R
xcopy %src%\win32\vc10\lib\TKBO.lib %target%\Xbim.ModelGeometry\OpenCascade\Win32\lib  /Y /R
xcopy %src%\win32\vc10\lib\TKG3D.lib %target%\Xbim.ModelGeometry\OpenCascade\Win32\lib  /Y /R
xcopy %src%\win32\vc10\lib\TKG2D.lib %target%\Xbim.ModelGeometry\OpenCascade\Win32\lib  /Y /R
xcopy %src%\win32\vc10\lib\TKGeomBase.lib %target%\Xbim.ModelGeometry\OpenCascade\Win32\lib  /Y /R
xcopy %src%\win32\vc10\lib\TKShHealing.lib %target%\Xbim.ModelGeometry\OpenCascade\Win32\lib  /Y /R
xcopy %src%\win32\vc10\lib\TKFillet.lib %target%\Xbim.ModelGeometry\OpenCascade\Win32\lib  /Y /R
xcopy %src%\win32\vc10\lib\TKOffset.lib %target%\Xbim.ModelGeometry\OpenCascade\Win32\lib  /Y /R
Echo Win64 Library Release files
xcopy %src%\Win64\vc10\lib\TKBool.lib %target%\Xbim.ModelGeometry\OpenCascade\Win64\lib  /Y /R
xcopy %src%\Win64\vc10\lib\TKernel.lib %target%\Xbim.ModelGeometry\OpenCascade\Win64\lib  /Y /R
xcopy %src%\Win64\vc10\lib\TKAdvTools.lib %target%\Xbim.ModelGeometry\OpenCascade\Win64\lib  /Y /R
xcopy %src%\Win64\vc10\lib\TKMath.lib %target%\Xbim.ModelGeometry\OpenCascade\Win64\lib  /Y /R
xcopy %src%\Win64\vc10\lib\TKTopAlgo.lib %target%\Xbim.ModelGeometry\OpenCascade\Win64\lib  /Y /R
xcopy %src%\Win64\vc10\lib\TKGeomAlgo.lib %target%\Xbim.ModelGeometry\OpenCascade\Win64\lib  /Y /R
xcopy %src%\Win64\vc10\lib\TKBRep.lib %target%\Xbim.ModelGeometry\OpenCascade\Win64\lib  /Y /R
xcopy %src%\Win64\vc10\lib\TKMesh.lib %target%\Xbim.ModelGeometry\OpenCascade\Win64\lib  /Y /R
xcopy %src%\Win64\vc10\lib\TKPrim.lib %target%\Xbim.ModelGeometry\OpenCascade\Win64\lib  /Y /R
xcopy %src%\Win64\vc10\lib\TKBO.lib %target%\Xbim.ModelGeometry\OpenCascade\Win64\lib  /Y /R
xcopy %src%\Win64\vc10\lib\TKG3D.lib %target%\Xbim.ModelGeometry\OpenCascade\Win64\lib  /Y /R
xcopy %src%\Win64\vc10\lib\TKG2D.lib %target%\Xbim.ModelGeometry\OpenCascade\Win64\lib  /Y /R
xcopy %src%\Win64\vc10\lib\TKGeomBase.lib %target%\Xbim.ModelGeometry\OpenCascade\Win64\lib  /Y /R
xcopy %src%\Win64\vc10\lib\TKShHealing.lib %target%\Xbim.ModelGeometry\OpenCascade\Win64\lib  /Y /R
xcopy %src%\Win64\vc10\lib\TKFillet.lib %target%\Xbim.ModelGeometry\OpenCascade\Win64\lib  /Y /R
xcopy %src%\Win64\vc10\lib\TKOffset.lib %target%\Xbim.ModelGeometry\OpenCascade\Win64\lib  /Y /R
Echo Win32 Binary Release files
xcopy %src%\win32\vc10\bin\TKBool.dll %target%\Xbim.ModelGeometry\OpenCascade\Win32\bin  /Y /R
xcopy %src%\win32\vc10\bin\TKernel.dll %target%\Xbim.ModelGeometry\OpenCascade\Win32\bin  /Y /R
xcopy %src%\win32\vc10\bin\TKMath.dll %target%\Xbim.ModelGeometry\OpenCascade\Win32\bin  /Y /R
xcopy %src%\win32\vc10\bin\TKTopAlgo.dll %target%\Xbim.ModelGeometry\OpenCascade\Win32\bin  /Y /R
xcopy %src%\win32\vc10\bin\TKGeomAlgo.dll %target%\Xbim.ModelGeometry\OpenCascade\Win32\bin  /Y /R
xcopy %src%\win32\vc10\bin\TKBRep.dll %target%\Xbim.ModelGeometry\OpenCascade\Win32\bin  /Y /R
xcopy %src%\win32\vc10\bin\TKMesh.dll %target%\Xbim.ModelGeometry\OpenCascade\Win32\bin  /Y /R
xcopy %src%\win32\vc10\bin\TKPrim.dll %target%\Xbim.ModelGeometry\OpenCascade\Win32\bin  /Y /R
xcopy %src%\win32\vc10\bin\TKBO.dll %target%\Xbim.ModelGeometry\OpenCascade\Win32\bin  /Y /R
xcopy %src%\win32\vc10\bin\TKG3D.dll %target%\Xbim.ModelGeometry\OpenCascade\Win32\bin  /Y /R
xcopy %src%\win32\vc10\bin\TKG2D.dll %target%\Xbim.ModelGeometry\OpenCascade\Win32\bin  /Y /R
xcopy %src%\win32\vc10\bin\TKGeomBase.dll %target%\Xbim.ModelGeometry\OpenCascade\Win32\bin  /Y /R
xcopy %src%\win32\vc10\bin\TKShHealing.dll %target%\Xbim.ModelGeometry\OpenCascade\Win32\bin  /Y /R
xcopy %src%\win32\vc10\bin\TKFillet.dll %target%\Xbim.ModelGeometry\OpenCascade\Win32\bin  /Y /R
xcopy %src%\win32\vc10\bin\TKOffset.dll %target%\Xbim.ModelGeometry\OpenCascade\Win32\bin  /Y /R
Echo Win64 Binary Release files
xcopy %src%\Win64\vc10\bin\TKBool.dll %target%\Xbim.ModelGeometry\OpenCascade\Win64\bin  /Y /R
xcopy %src%\Win64\vc10\bin\TKernel.dll %target%\Xbim.ModelGeometry\OpenCascade\Win64\bin  /Y /R
xcopy %src%\Win64\vc10\bin\TKMath.dll %target%\Xbim.ModelGeometry\OpenCascade\Win64\bin  /Y /R
xcopy %src%\Win64\vc10\bin\TKTopAlgo.dll %target%\Xbim.ModelGeometry\OpenCascade\Win64\bin  /Y /R
xcopy %src%\Win64\vc10\bin\TKGeomAlgo.dll %target%\Xbim.ModelGeometry\OpenCascade\Win64\bin  /Y /R
xcopy %src%\Win64\vc10\bin\TKBRep.dll %target%\Xbim.ModelGeometry\OpenCascade\Win64\bin  /Y /R
xcopy %src%\Win64\vc10\bin\TKMesh.dll %target%\Xbim.ModelGeometry\OpenCascade\Win64\bin  /Y /R
xcopy %src%\Win64\vc10\bin\TKPrim.dll %target%\Xbim.ModelGeometry\OpenCascade\Win64\bin  /Y /R
xcopy %src%\Win64\vc10\bin\TKBO.dll %target%\Xbim.ModelGeometry\OpenCascade\Win64\bin  /Y /R
xcopy %src%\Win64\vc10\bin\TKG3D.dll %target%\Xbim.ModelGeometry\OpenCascade\Win64\bin  /Y /R
xcopy %src%\Win64\vc10\bin\TKG2D.dll %target%\Xbim.ModelGeometry\OpenCascade\Win64\bin  /Y /R
xcopy %src%\Win64\vc10\bin\TKGeomBase.dll %target%\Xbim.ModelGeometry\OpenCascade\Win64\bin  /Y /R
xcopy %src%\Win64\vc10\bin\TKShHealing.dll %target%\Xbim.ModelGeometry\OpenCascade\Win64\bin  /Y /R
xcopy %src%\Win64\vc10\bin\TKFillet.dll %target%\Xbim.ModelGeometry\OpenCascade\Win64\bin  /Y /R
xcopy %src%\Win64\vc10\bin\TKOffset.dll %target%\Xbim.ModelGeometry\OpenCascade\Win64\bin  /Y /R