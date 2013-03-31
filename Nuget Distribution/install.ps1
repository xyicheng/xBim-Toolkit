param($installPath, $toolsPath, $package, $project)

$platformNames = "x86", "x64"
$fileNames = "TKBool.dll", "TKernel.dll","TKMath.dll","TKTopAlgo.dll","TKGeomAlgo.dll","TKBRep.dll","TKMesh.dll","TKPrim.dll","TKBO.dll","TKG3D.dll","TKG2D.dll","TKGeomBase.dll","TKShHealing.dll","TKOffset.dll", "Xbim.ModelGeometry.OCC.dll"
$propertyName = "CopyToOutputDirectory"

$project.Object.References | Where-Object { $_.Identity -eq 'Xbim.ModelGeometry.OCC' } | ForEach-Object { $_.CopyLocal=$false} 

foreach($platformName in $platformNames) {
    $folder = $project.ProjectItems.Item($platformName)
    if ($folder -eq $null) { continue }
    foreach($fileName in $fileNames) {
      $item = $folder.ProjectItems.Item($fileName)
      if ($item -eq $null) { continue }
      $property = $item.Properties.Item($propertyName)
      if ($property -eq $null) { continue }
      $property.Value = 2
    }
}