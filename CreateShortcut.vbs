Set oWS = WScript.CreateObject("WScript.Shell")
sLinkFile = oWS.SpecialFolders("Desktop") & "\HepsiEmlak.lnk"
Set oLink = oWS.CreateShortcut(sLinkFile)
oLink.TargetPath = CreateObject("WScript.Shell").CurrentDirectory & "\start.bat"
oLink.WorkingDirectory = CreateObject("WScript.Shell").CurrentDirectory
oLink.Description = "HepsiEmlak İlan Toplayıcı"
oLink.Save 