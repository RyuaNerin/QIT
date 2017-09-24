
set "CDR=%~dp0"

set "BUILD="

if not exist "%CDR%\TiXInstaller\libwebp\release-dynamic\x86\bin\libwebpdecoder.dll" set "BUILD=1"
if not exist "%CDR%\TiXInstaller\libwebp\release-dynamic\x86\bin\libwebp.dll"        set "BUILD=1"
if defined BUILD (
	"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\VC\Auxiliary\Build\vcvarsall.bat" x86
	cd "%CDR%\submodules\libwebp\"
	nmake /f Makefile.vc CFG=release-dynamic OBJDIR="%CDR%\TiXInstaller\libwebp"
	"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\VC\Auxiliary\Build\vcvarsall.bat" -clean_env
)

if not exist "%CDR%\TiXInstaller\libwebp\release-dynamic\x64\bin\libwebpdecoder.dll" set "BUILD=1"
if not exist "%CDR%\TiXInstaller\libwebp\release-dynamic\x64\bin\libwebp.dll"        set "BUILD=1"
if defined BUILD (
	"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\VC\Auxiliary\Build\vcvarsall.bat" x64
	cd "%CDR%\submodules\libwebp\"
	nmake /f Makefile.vc CFG=release-dynamic OBJDIR="%CDR%\TiXInstaller\libwebp"
	"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\VC\Auxiliary\Build\vcvarsall.bat" -clean_env
)