#include "stdafx.h"

#include <windows.h>
#include <Olectl.h>

#include "ClassFactory.h"
#include "DebugLog.h"
#include "resource.h"

HINSTANCE g_hInst;
UINT g_ref;

HBITMAP m_menuImage = NULL;
WCHAR   m_exePath[MAX_PATH] = { 0 };
bool    m_option_with_text;
bool    m_option_without_text;

// {9CE5906A-DFBB-4A5A-9EBF-9D262E5D29B9}
#define SHELLEXT_GUID   { 0x9ce5906a, 0xdfbb, 0x4a5a, { 0x9e, 0xbf, 0x9d, 0x26, 0x2e, 0x5d, 0x29, 0xb9 } }

BOOL APIENTRY DllMain(HMODULE hModule, DWORD dwReason, LPVOID lpReserved)
{
	switch (dwReason)
	{
	case DLL_PROCESS_ATTACH:
        g_hInst = hModule;

        if (m_menuImage == NULL)
            m_menuImage = static_cast<HBITMAP>(LoadImageW(g_hInst, MAKEINTRESOURCEW(IDB_ICON), IMAGE_BITMAP, 0, 0, LR_DEFAULTSIZE | LR_LOADTRANSPARENT | LR_LOADMAP3DCOLORS));

        if (m_exePath[0] == 0)
        {
            HKEY hKey;
            if (RegOpenKeyExW(HKEY_CURRENT_USER, L"Software\\RyuaNerin", NULL, KEY_READ, &hKey) == NO_ERROR)
            {
                DWORD len, dword;

                len = sizeof(m_exePath);
                RegGetValueW(hKey, NULL, L"TiX",     REG_SZ,    NULL, (LPBYTE)&m_exePath, &len);
                len = sizeof(dword);
                RegGetValueW(hKey, NULL, L"TiX-wt",  REG_DWORD, NULL, (LPBYTE)&dword,     &len); m_option_with_text    = dword;
                RegGetValueW(hKey, NULL, L"TiX-wot", REG_DWORD, NULL, (LPBYTE)&dword,     &len); m_option_without_text = dword;
                
                DebugLog(L"Get ExePath [%d] %s", len, m_exePath);
                DebugLog(L"WithText    [%d]",    m_option_with_text);
                DebugLog(L"WithoutText [%d]",    m_option_without_text);

                RegCloseKey(hKey);
            }
        }

        DisableThreadLibraryCalls(hModule);
        break;
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
	return TRUE;
}

STDAPI DllGetClassObject(REFCLSID rclsid, REFIID riid, void **ppReturn)
{    
    *ppReturn = nullptr;
    if (!IsEqualCLSID(SHELLEXT_GUID, rclsid))
        return CLASS_E_CLASSNOTAVAILABLE;

    ClassFactory *fac = new ClassFactory();
    if (fac == nullptr)
        return E_OUTOFMEMORY;

    HRESULT result = fac->QueryInterface(riid, ppReturn);
    fac->Release();
    return result;
}

STDAPI DllCanUnloadNow(void)
{
    return g_ref > 0 ? S_FALSE : S_OK;
}

bool Regist_Key(HKEY rootKey, LPWSTR subKey, LPWSTR keyName, LPWSTR keyData)
{
    HKEY hKey;
    DWORD dwDisp;

    if (RegCreateKeyExW(rootKey, subKey, 0, NULL, REG_OPTION_NON_VOLATILE, KEY_WRITE, NULL, &hKey, &dwDisp) != NOERROR)
        return false;

    LSTATUS lstatus = RegSetValueExW(hKey, keyName, 0, REG_SZ, (LPBYTE)keyData, (int)(wcslen(keyData) + 1) * sizeof(TCHAR));
    RegCloseKey(hKey);

    return lstatus == ERROR_SUCCESS;
}
bool Regist_CLSID(LPWSTR clsid)
{
    TCHAR dllPath[MAX_PATH];
    GetModuleFileNameW(g_hInst, dllPath, sizeof(dllPath));

    TCHAR subKey[MAX_PATH];
    wsprintf(subKey, L"Software\\Classes\\CLSID\\%s", clsid);
    DebugLog(subKey);
    if (!Regist_Key(HKEY_CURRENT_USER, subKey, NULL, L"Shell Extension for TiX"))
        return false;
    
    wsprintf(subKey, L"Software\\Classes\\CLSID\\%s\\InprocServer32", clsid);
    DebugLog(subKey);
    if (!Regist_Key(HKEY_CURRENT_USER, subKey, NULL, dllPath))
        return false;

    if (!Regist_Key(HKEY_CURRENT_USER, subKey, L"ThreadingModel", L"Apartment"))
        return false;

    return true;
}

STDAPI DllRegisterServer(void)
{
    DebugLog(L"Registering Server");
    
    WCHAR clsid[MAX_PATH];
    if (StringFromGUID2(SHELLEXT_GUID, clsid, ARRAYSIZE(clsid)) == 0)
        return SELFREG_E_CLASS;
    DebugLog(clsid);

    if (!Regist_CLSID(clsid))
        return SELFREG_E_CLASS;

    if (!Regist_Key(HKEY_CURRENT_USER, L"Software\\Classes\\*\\ShellEx\\ContextMenuHandlers\\TiXExt", NULL, clsid))
        return SELFREG_E_CLASS;

    if (!Regist_Key(HKEY_CURRENT_USER, L"Software\\Classes\\Directory\\ShellEx\\ContextMenuHandlers\\TiXExt", NULL, clsid))
        return SELFREG_E_CLASS;

    return S_OK;
}

void Unregist_CLSID(LPWSTR clsid)
{
    TCHAR subKey[MAX_PATH];
    wsprintf(subKey, L"Software\\Classes\\CLSID\\%s\\InprocServer32", clsid);
    RegDeleteKeyW(HKEY_CURRENT_USER, subKey);

    wsprintf(subKey, L"Software\\Classes\\CLSID\\%s", clsid);
    RegDeleteKeyW(HKEY_CURRENT_USER, subKey);
}

STDAPI DllUnregisterServer(void)
{
    DebugLog(L"Unregistering server");

    TCHAR clsid[MAX_PATH];
    StringFromGUID2(SHELLEXT_GUID, clsid, ARRAYSIZE(clsid));
    
    Unregist_CLSID(clsid);
    
    RegDeleteKeyW(HKEY_CLASSES_ROOT, L"Software\\Classes\\*\\ShellEx\\ContextMenuHandlers\\TiXExt");

    RegDeleteKeyW(HKEY_CLASSES_ROOT, L"Software\\Classes\\Directory\\ShellEx\\ContextMenuHandlers\\TiXExt");

    return S_OK;
}
