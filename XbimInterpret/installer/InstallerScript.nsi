; Script generated by the HM NIS Edit Script Wizard. 
!include "FileAssociation.nsh"

; HM NIS Edit Wizard helper defines
!define PRODUCT_NAME "XbimInterpret"
!define PRODUCT_VERSION "1.0"
!define PRODUCT_PUBLISHER "Martin Cerny"
!define PRODUCT_WEB_SITE "http://xbim.codeplex.com"
!define PRODUCT_DIR_REGKEY "Software\Microsoft\Windows\CurrentVersion\App Paths\XbimInterpret.exe"
!define PRODUCT_UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}"
!define PRODUCT_UNINST_ROOT_KEY "HKLM"

; MUI 1.67 compatible ------
!include "MUI.nsh"

; MUI Settings
!define MUI_ABORTWARNING
!define MUI_ICON "${NSISDIR}\Contrib\Graphics\Icons\modern-install.ico"
!define MUI_UNICON "${NSISDIR}\Contrib\Graphics\Icons\modern-uninstall.ico"

; Welcome page
!insertmacro MUI_PAGE_WELCOME
; License page
!insertmacro MUI_PAGE_LICENSE "..\..\Licences\MS Permissive Licence.txt"
; Directory page
!insertmacro MUI_PAGE_DIRECTORY
; Instfiles page
!insertmacro MUI_PAGE_INSTFILES
; Finish page
!define MUI_FINISHPAGE_RUN "$INSTDIR\XbimInterpret.exe"
!insertmacro MUI_PAGE_FINISH

; Uninstaller pages
!insertmacro MUI_UNPAGE_INSTFILES

; Language files
!insertmacro MUI_LANGUAGE "English"

; MUI end ------

Name "${PRODUCT_NAME} ${PRODUCT_VERSION}"
OutFile "Setup.exe"
InstallDir "$PROGRAMFILES\XbimInterpret"
InstallDirRegKey HKLM "${PRODUCT_DIR_REGKEY}" ""
ShowInstDetails show
ShowUnInstDetails show

Section "MainSection" SEC01
  SetOutPath "$INSTDIR"
  SetOverwrite try
  File "..\bin\Release\Esent.Interop.dll"
  File "..\bin\Release\ICSharpCode.SharpZipLib.dll"
  File "..\bin\Release\log4net.dll"
  File "..\bin\Release\log4net.xml"
  SetOutPath "$INSTDIR\Classifications"
  File "..\bin\Release\Classifications\NRM.csv"
  File "..\bin\Release\Classifications\Uniclass 1.4.csv"
  File "..\bin\Release\Classifications\Uniclass2-Ac.csv"
  File "..\bin\Release\Classifications\Uniclass2-Co.csv"
  File "..\bin\Release\Classifications\Uniclass2-Ee.csv"
  File "..\bin\Release\Classifications\Uniclass2-Ef.csv"
  File "..\bin\Release\Classifications\Uniclass2-En.csv"
  File "..\bin\Release\Classifications\Uniclass2-PP.csv"
  File "..\bin\Release\Classifications\Uniclass2-Pr.csv"
  File "..\bin\Release\Classifications\Uniclass2-Sp.csv"
  File "..\bin\Release\Classifications\Uniclass2-Ss.csv"
  File "..\bin\Release\Classifications\Uniclass2-WR.csv"
  File "..\bin\Release\Classifications\Uniclass2-Zz.csv"
  SetOutPath "$INSTDIR\x64"
  File "..\bin\Release\x64\SQLite.Interop.dll"
  SetOutPath "$INSTDIR\x86"
  File "..\bin\Release\x86\SQLite.Interop.dll"
  SetOutPath "$INSTDIR"
  File "..\bin\Release\Xbim.Common.dll"
  File "..\bin\Release\Xbim.Ifc.Extensions.dll"
  File "..\bin\Release\Xbim.Ifc2x3.dll"
  File "..\bin\Release\Xbim.IO.dll"
  File "..\bin\Release\Xbim.Script.dll"
  File "..\bin\Release\XbimInterpret.exe"
  File "..\bin\Release\BQL_documentation.pdf"
  CreateDirectory "$SMPROGRAMS\XbimInterpret"
  CreateShortCut "$SMPROGRAMS\XbimInterpret\XbimInterpret.lnk" "$INSTDIR\XbimInterpret.exe"
  CreateShortCut "$SMPROGRAMS\XbimInterpret\Documentation.lnk" "$INSTDIR\BQL_documentation.pdf"
  CreateShortCut "$DESKTOP\XbimInterpret.lnk" "$INSTDIR\XbimInterpret.exe"
  File "..\bin\Release\XbimInterpret.exe.config"
  File "..\bin\Release\XbimInterpret.vshost.exe"
  File "..\bin\Release\XbimInterpret.vshost.exe.config"
  File "..\bin\Release\XbimInterpret.vshost.exe.manifest"
  
  ${registerExtension} "$INSTDIR\XbimInterpret.exe" ".bql" "BQL File"
SectionEnd

Section -AdditionalIcons
  WriteIniStr "$INSTDIR\${PRODUCT_NAME}.url" "InternetShortcut" "URL" "${PRODUCT_WEB_SITE}"
  CreateShortCut "$SMPROGRAMS\XbimInterpret\Website.lnk" "$INSTDIR\${PRODUCT_NAME}.url"
  CreateShortCut "$SMPROGRAMS\XbimInterpret\Uninstall.lnk" "$INSTDIR\uninst.exe"
SectionEnd

Section -Post
  WriteUninstaller "$INSTDIR\uninst.exe"
  WriteRegStr HKLM "${PRODUCT_DIR_REGKEY}" "" "$INSTDIR\XbimInterpret.exe"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayName" "$(^Name)"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "UninstallString" "$INSTDIR\uninst.exe"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayIcon" "$INSTDIR\XbimInterpret.exe"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayVersion" "${PRODUCT_VERSION}"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "URLInfoAbout" "${PRODUCT_WEB_SITE}"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "Publisher" "${PRODUCT_PUBLISHER}"
SectionEnd


Function un.onUninstSuccess
  HideWindow
  MessageBox MB_ICONINFORMATION|MB_OK "$(^Name) was successfully removed from your computer."
FunctionEnd

Function un.onInit
  MessageBox MB_ICONQUESTION|MB_YESNO|MB_DEFBUTTON2 "Are you sure you want to completely remove $(^Name) and all of its components?" IDYES +2
  Abort
FunctionEnd

Section Uninstall
  ${unregisterExtension} ".bql" "BQL File"
    
  Delete "$INSTDIR\${PRODUCT_NAME}.url"
  Delete "$INSTDIR\uninst.exe"
  Delete "$INSTDIR\XbimInterpret.vshost.exe.manifest"
  Delete "$INSTDIR\XbimInterpret.vshost.exe.config"
  Delete "$INSTDIR\XbimInterpret.vshost.exe"
  Delete "$INSTDIR\XbimInterpret.exe.config"
  Delete "$INSTDIR\XbimInterpret.exe"
  Delete "$INSTDIR\Xbim.Script.dll"
  Delete "$INSTDIR\Xbim.IO.dll"
  Delete "$INSTDIR\Xbim.Ifc2x3.dll"
  Delete "$INSTDIR\Xbim.Ifc.Extensions.dll"
  Delete "$INSTDIR\Xbim.Common.dll"
  Delete "$INSTDIR\x86\SQLite.Interop.dll"
  Delete "$INSTDIR\x64\SQLite.Interop.dll"
  Delete "$INSTDIR\Classifications\NRM.csv"
  Delete "$INSTDIR\Classifications\Uniclass 1.4.csv"
  Delete "$INSTDIR\Classifications\Uniclass2-Ac.csv"
  Delete "$INSTDIR\Classifications\Uniclass2-Co.csv"
  Delete "$INSTDIR\Classifications\Uniclass2-Ee.csv"
  Delete "$INSTDIR\Classifications\Uniclass2-Ef.csv"
  Delete "$INSTDIR\Classifications\Uniclass2-En.csv"
  Delete "$INSTDIR\Classifications\Uniclass2-PP.csv"
  Delete "$INSTDIR\Classifications\Uniclass2-Pr.csv"
  Delete "$INSTDIR\Classifications\Uniclass2-Sp.csv"
  Delete "$INSTDIR\Classifications\Uniclass2-Ss.csv"
  Delete "$INSTDIR\Classifications\Uniclass2-WR.csv"
  Delete "$INSTDIR\Classifications\Uniclass2-Zz.csv"
  Delete "$INSTDIR\log4net.xml"
  Delete "$INSTDIR\log4net.dll"
  Delete "$INSTDIR\ICSharpCode.SharpZipLib.dll"
  Delete "$INSTDIR\Esent.Interop.dll"
  Delete "$INSTDIR\BQL_documentation.pdf"

  Delete "$SMPROGRAMS\XbimInterpret\Uninstall.lnk"
  Delete "$SMPROGRAMS\XbimInterpret\Website.lnk"
  Delete "$SMPROGRAMS\XbimInterpret\Documentation.lnk"
  Delete "$DESKTOP\XbimInterpret.lnk"
  Delete "$SMPROGRAMS\XbimInterpret\XbimInterpret.lnk"

  RMDir "$SMPROGRAMS\XbimInterpret"
  RMDir "$INSTDIR\x86"
  RMDir "$INSTDIR\x64"
  RMDir "$INSTDIR\Classifications"
  RMDir "$INSTDIR"

  DeleteRegKey ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}"
  DeleteRegKey HKLM "${PRODUCT_DIR_REGKEY}"
  SetAutoClose true
SectionEnd