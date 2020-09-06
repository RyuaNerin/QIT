set "CDR=%~dp0"
cd "%CDR%\libwebp\"

if not exist "%CDR%\libwebp-bin" (
	call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvarsall.bat" x64
	nmake /f Makefile.vc CFG=release-static RTLIBCFG=static OBJDIR="%CDR%\libwebp-bin"
	call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvarsall.bat" -clean_env
)
