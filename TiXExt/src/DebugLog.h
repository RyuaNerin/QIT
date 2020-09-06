#pragma once

#ifdef _DEBUG
void DebugLog(const wchar_t *fmt, ...);
#else
#define DebugLog
#endif
