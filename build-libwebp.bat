set "CDR=%~dp0"
cd "%CDR%\submodules\libwebp\"

if not exist "%CDR%\TiX\libwebp\libwebp_x86.dll" (
	call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvarsall.bat" x86
	nmake /f Makefile.vc CFG=release-dynamic RTLIBCFG=dynamic OBJDIR="%CDR%\TiX\libwebp"
	call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvarsall.bat" -clean_env

	copy "%CDR%\TiX\libwebp\release-dynamic\x86\bin\libwebp.dll" "%CDR%\TiX\libwebp\libwebp_x86.dll"
)

if not exist "%CDR%\TiX\libwebp\libwebp_x64.dll" (
	call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvarsall.bat" x64
	nmake /f Makefile.vc CFG=release-dynamic RTLIBCFG=dynamic OBJDIR="%CDR%\TiX\libwebp"
	call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvarsall.bat" -clean_env
	
	copy "%CDR%\TiX\libwebp\release-dynamic\x64\bin\libwebp.dll" "%CDR%\TiX\libwebp\libwebp_x64.dll"
)

rmdir /S /Q "%CDR%\TiX\libwebp\release-dynamic"
