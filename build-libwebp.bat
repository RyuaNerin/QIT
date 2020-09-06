set "CDR=%~dp0"
cd "%CDR%\submodules\libwebp\"

if not exist "%CDR%\TiX\libwebp\libwebp.dll" (
	call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvarsall.bat" x64
	nmake /f Makefile.vc CFG=release-static RTLIBCFG=dynamic OBJDIR="%CDR%\TiX\libwebp"
	call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvarsall.bat" -clean_env
	
	copy "%CDR%\TiX\libwebp\release-dynamic\x64\bin\libwebp.dll" "%CDR%\TiX\libwebp\libwebp.dll"
)

REM rmdir /S /Q "%CDR%\TiX\libwebp\release-dynamic"
