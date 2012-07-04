@echo off
set src=D:\OpenCASCADE6.5.1\ros
set target=D:\SRL\xBIM\XbimFramework
echo Copying from %src% to %target%
echo Win32 Library Release files
pause
mkdir %target%
mkdir %target%\bin
mkdir %target%\bin\x86
mkdir %target%\bin\x64
mkdir %target%\bin\x86\Release
mkdir %target%\bin\x86\Debug
mkdir %target%\bin\x64\Release
mkdir %target%\bin\x64\Debug
mkdir %target%\Xbim.ModelGeometry\OpenCascade
mkdir %target%\Xbim.ModelGeometry\OpenCascade\Win32
mkdir %target%\Xbim.ModelGeometry\OpenCascade\Win64
mkdir %target%\Xbim.ModelGeometry\OpenCascade\Win32\lib
mkdir %target%\Xbim.ModelGeometry\OpenCascade\Win32\libd
mkdir %target%\Xbim.ModelGeometry\OpenCascade\Win64\lib
mkdir %target%\Xbim.ModelGeometry\OpenCascade\Win64\libd
copy %src%\win32\vc10\lib\TKBool.lib %target%\Xbim.ModelGeometry\OpenCascade\Win32\lib
copy %src%\win32\vc10\lib\TKernel.lib %target%\Xbim.ModelGeometry\OpenCascade\Win32\lib
copy %src%\win32\vc10\lib\TKMath.lib %target%\Xbim.ModelGeometry\OpenCascade\Win32\lib
copy %src%\win32\vc10\lib\TKTopAlgo.lib %target%\Xbim.ModelGeometry\OpenCascade\Win32\lib
copy %src%\win32\vc10\lib\TKGeomAlgo.lib %target%\Xbim.ModelGeometry\OpenCascade\Win32\lib
copy %src%\win32\vc10\lib\TKBRep.lib %target%\Xbim.ModelGeometry\OpenCascade\Win32\lib
copy %src%\win32\vc10\lib\TKMesh.lib %target%\Xbim.ModelGeometry\OpenCascade\Win32\lib
copy %src%\win32\vc10\lib\TKPrim.lib %target%\Xbim.ModelGeometry\OpenCascade\Win32\lib
copy %src%\win32\vc10\lib\TKBO.lib %target%\Xbim.ModelGeometry\OpenCascade\Win32\lib
copy %src%\win32\vc10\lib\TKG3D.lib %target%\Xbim.ModelGeometry\OpenCascade\Win32\lib
copy %src%\win32\vc10\lib\TKG2D.lib %target%\Xbim.ModelGeometry\OpenCascade\Win32\lib
copy %src%\win32\vc10\lib\TKGeomBase.lib %target%\Xbim.ModelGeometry\OpenCascade\Win32\lib
copy %src%\win32\vc10\lib\TKShHealing.lib %target%\Xbim.ModelGeometry\OpenCascade\Win32\lib

Echo Win32 Library Debug files
copy %src%\win32\vc10\libd\TKBool.* %target%\Xbim.ModelGeometry\OpenCascade\Win32\libd
copy %src%\win32\vc10\libd\TKernel.* %target%\Xbim.ModelGeometry\OpenCascade\Win32\libd
copy %src%\win32\vc10\libd\TKMath.* %target%\Xbim.ModelGeometry\OpenCascade\Win32\libd
copy %src%\win32\vc10\libd\TKTopAlgo.* %target%\Xbim.ModelGeometry\OpenCascade\Win32\libd
copy %src%\win32\vc10\libd\TKGeomAlgo.* %target%\Xbim.ModelGeometry\OpenCascade\Win32\libd
copy %src%\win32\vc10\libd\TKBRep.* %target%\Xbim.ModelGeometry\OpenCascade\Win32\libd
copy %src%\win32\vc10\libd\TKMesh.* %target%\Xbim.ModelGeometry\OpenCascade\Win32\libd
copy %src%\win32\vc10\libd\TKPrim.* %target%\Xbim.ModelGeometry\OpenCascade\Win32\libd
copy %src%\win32\vc10\libd\TKBO.* %target%\Xbim.ModelGeometry\OpenCascade\Win32\libd
copy %src%\win32\vc10\libd\TKG3D.* %target%\Xbim.ModelGeometry\OpenCascade\Win32\libd
copy %src%\win32\vc10\libd\TKG2D.* %target%\Xbim.ModelGeometry\OpenCascade\Win32\libd
copy %src%\win32\vc10\libd\TKGeomBase.* %target%\Xbim.ModelGeometry\OpenCascade\Win32\libd
copy %src%\win32\vc10\libd\TKShHealing.* %target%\Xbim.ModelGeometry\OpenCascade\Win32\libd

Echo Win64 Library Release files
copy %src%\Win64\vc10\lib\TKBool.lib %target%\Xbim.ModelGeometry\OpenCascade\Win64\lib
copy %src%\Win64\vc10\lib\TKernel.lib %target%\Xbim.ModelGeometry\OpenCascade\Win64\lib
copy %src%\Win64\vc10\lib\TKMath.lib %target%\Xbim.ModelGeometry\OpenCascade\Win64\lib
copy %src%\Win64\vc10\lib\TKTopAlgo.lib %target%\Xbim.ModelGeometry\OpenCascade\Win64\lib
copy %src%\Win64\vc10\lib\TKGeomAlgo.lib %target%\Xbim.ModelGeometry\OpenCascade\Win64\lib
copy %src%\Win64\vc10\lib\TKBRep.lib %target%\Xbim.ModelGeometry\OpenCascade\Win64\lib
copy %src%\Win64\vc10\lib\TKMesh.lib %target%\Xbim.ModelGeometry\OpenCascade\Win64\lib
copy %src%\Win64\vc10\lib\TKPrim.lib %target%\Xbim.ModelGeometry\OpenCascade\Win64\lib
copy %src%\Win64\vc10\lib\TKBO.lib %target%\Xbim.ModelGeometry\OpenCascade\Win64\lib
copy %src%\Win64\vc10\lib\TKG3D.lib %target%\Xbim.ModelGeometry\OpenCascade\Win64\lib
copy %src%\Win64\vc10\lib\TKG2D.lib %target%\Xbim.ModelGeometry\OpenCascade\Win64\lib
copy %src%\Win64\vc10\lib\TKGeomBase.lib %target%\Xbim.ModelGeometry\OpenCascade\Win64\lib
copy %src%\Win64\vc10\lib\TKShHealing.lib %target%\Xbim.ModelGeometry\OpenCascade\Win64\lib

Echo Win64 Library Debug files
copy %src%\Win64\vc10\libd\TKBool.* %target%\Xbim.ModelGeometry\OpenCascade\Win64\libd
copy %src%\Win64\vc10\libd\TKernel.* %target%\Xbim.ModelGeometry\OpenCascade\Win64\libd
copy %src%\Win64\vc10\libd\TKMath.* %target%\Xbim.ModelGeometry\OpenCascade\Win64\libd
copy %src%\Win64\vc10\libd\TKTopAlgo.* %target%\Xbim.ModelGeometry\OpenCascade\Win64\libd
copy %src%\Win64\vc10\libd\TKGeomAlgo.* %target%\Xbim.ModelGeometry\OpenCascade\Win64\libd
copy %src%\Win64\vc10\libd\TKBRep.* %target%\Xbim.ModelGeometry\OpenCascade\Win64\libd
copy %src%\Win64\vc10\libd\TKMesh.* %target%\Xbim.ModelGeometry\OpenCascade\Win64\libd
copy %src%\Win64\vc10\libd\TKPrim.* %target%\Xbim.ModelGeometry\OpenCascade\Win64\libd
copy %src%\Win64\vc10\libd\TKBO.* %target%\Xbim.ModelGeometry\OpenCascade\Win64\libd
copy %src%\Win64\vc10\libd\TKG3D.* %target%\Xbim.ModelGeometry\OpenCascade\Win64\libd
copy %src%\Win64\vc10\libd\TKG2D.* %target%\Xbim.ModelGeometry\OpenCascade\Win64\libd
copy %src%\Win64\vc10\libd\TKGeomBase.* %target%\Xbim.ModelGeometry\OpenCascade\Win64\libd
copy %src%\Win64\vc10\libd\TKShHealing.* %target%\Xbim.ModelGeometry\OpenCascade\Win64\libd

Echo Win32 Binary Release files
copy %src%\win32\vc10\bin\TKBool.dll %target%\bin\x86\Release
copy %src%\win32\vc10\bin\TKernel.dll %target%\bin\x86\Release
copy %src%\win32\vc10\bin\TKMath.dll %target%\bin\x86\Release
copy %src%\win32\vc10\bin\TKTopAlgo.dll %target%\bin\x86\Release
copy %src%\win32\vc10\bin\TKGeomAlgo.dll %target%\bin\x86\Release
copy %src%\win32\vc10\bin\TKBRep.dll %target%\bin\x86\Release
copy %src%\win32\vc10\bin\TKMesh.dll %target%\bin\x86\Release
copy %src%\win32\vc10\bin\TKPrim.dll %target%\bin\x86\Release
copy %src%\win32\vc10\bin\TKBO.dll %target%\bin\x86\Release
copy %src%\win32\vc10\bin\TKG3D.dll %target%\bin\x86\Release
copy %src%\win32\vc10\bin\TKG2D.dll %target%\bin\x86\Release
copy %src%\win32\vc10\bin\TKGeomBase.dll %target%\bin\x86\Release
copy %src%\win32\vc10\bin\TKShHealing.dll %target%\bin\x86\Release

Echo Win32 Binary Debug files
copy %src%\win32\vc10\bind\TKBool.* %target%\bin\x86\Debug
copy %src%\win32\vc10\bind\TKernel.* %target%\bin\x86\Debug
copy %src%\win32\vc10\bind\TKMath.* %target%\bin\x86\Debug
copy %src%\win32\vc10\bind\TKTopAlgo.* %target%\bin\x86\Debug
copy %src%\win32\vc10\bind\TKGeomAlgo.* %target%\bin\x86\Debug
copy %src%\win32\vc10\bind\TKBRep.* %target%\bin\x86\Debug
copy %src%\win32\vc10\bind\TKMesh.* %target%\bin\x86\Debug
copy %src%\win32\vc10\bind\TKPrim.* %target%\bin\x86\Debug
copy %src%\win32\vc10\bind\TKBO.* %target%\bin\x86\Debug
copy %src%\win32\vc10\bind\TKG3D.* %target%\bin\x86\Debug
copy %src%\win32\vc10\bind\TKG2D.* %target%\bin\x86\Debug
copy %src%\win32\vc10\bind\TKGeomBase.* %target%\bin\x86\Debug
copy %src%\win32\vc10\bind\TKShHealing.* %target%\bin\x86\Debug

Echo Win64 Binary Release files
copy %src%\Win64\vc10\bin\TKBool.dll %target%\bin\x64\Release
copy %src%\Win64\vc10\bin\TKernel.dll %target%\bin\x64\Release
copy %src%\Win64\vc10\bin\TKMath.dll %target%\bin\x64\Release
copy %src%\Win64\vc10\bin\TKTopAlgo.dll %target%\bin\x64\Release
copy %src%\Win64\vc10\bin\TKGeomAlgo.dll %target%\bin\x64\Release
copy %src%\Win64\vc10\bin\TKBRep.dll %target%\bin\x64\Release
copy %src%\Win64\vc10\bin\TKMesh.dll %target%\bin\x64\Release
copy %src%\Win64\vc10\bin\TKPrim.dll %target%\bin\x64\Release
copy %src%\Win64\vc10\bin\TKBO.dll %target%\bin\x64\Release
copy %src%\Win64\vc10\bin\TKG3D.dll %target%\bin\x64\Release
copy %src%\Win64\vc10\bin\TKG2D.dll %target%\bin\x64\Release
copy %src%\Win64\vc10\bin\TKGeomBase.dll %target%\bin\x64\Release
copy %src%\Win64\vc10\bin\TKShHealing.dll %target%\bin\x64\Release

Echo Win64 Binary Debug files
copy %src%\Win64\vc10\bind\TKBool.* %target%\bin\x64\Debug
copy %src%\Win64\vc10\bind\TKernel.* %target%\bin\x64\Debug
copy %src%\Win64\vc10\bind\TKMath.* %target%\bin\x64\Debug
copy %src%\Win64\vc10\bind\TKTopAlgo.* %target%\bin\x64\Debug
copy %src%\Win64\vc10\bind\TKGeomAlgo.* %target%\bin\x64\Debug
copy %src%\Win64\vc10\bind\TKBRep.* %target%\bin\x64\Debug
copy %src%\Win64\vc10\bind\TKMesh.* %target%\bin\x64\Debug
copy %src%\Win64\vc10\bind\TKPrim.* %target%\bin\x64\Debug
copy %src%\Win64\vc10\bind\TKBO.* %target%\bin\x64\Debug
copy %src%\Win64\vc10\bind\TKG3D.* %target%\bin\x64\Debug
copy %src%\Win64\vc10\bind\TKG2D.* %target%\bin\x64\Debug
copy %src%\Win64\vc10\bind\TKGeomBase.* %target%\bin\x64\Debug
copy %src%\Win64\vc10\bind\TKShHealing.* %target%\bin\x64\Debug