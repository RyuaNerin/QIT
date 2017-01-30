#ifndef __DEBUGLOG__H__
#define __DEBUGLOG__H__

#ifdef _DEBUG
#include <string>

void DebugLog(const wchar_t *fmt, ...);
#else
#define DebugLog
#endif

#endif
