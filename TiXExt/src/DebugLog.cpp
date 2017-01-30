#include "stdafx.h"

#ifdef _DEBUG
#include "DebugLog.h"

#include <windows.h>
#include <cwchar>
#include <memory>

static CRITICAL_SECTION* cs;

void DebugLog(const wchar_t *fmt, ...)
{
    if (cs == 0)
    {
        cs = new CRITICAL_SECTION();
        InitializeCriticalSection(cs);
    }

    EnterCriticalSection(cs);
    
    va_list	args;
    va_start(args, fmt);

    INT len = _scwprintf(L"TiXExt: ") + _vscwprintf(fmt, args) + 1;

    std::unique_ptr<WCHAR[]> str(new WCHAR[len]);

    len = wsprintfW(str.get(), L"TiXExt: ");
    wvsprintfW(str.get() + len, fmt, args);

    va_end(args);

    OutputDebugStringW(str.get());

    LeaveCriticalSection(cs);
}
#endif
