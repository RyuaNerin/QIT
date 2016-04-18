#include "stdafx.h"

#include <windows.h>
#include <Olectl.h>

#include "ClassFactory.h"
#include "DebugLog.h"
#include "resource.h"

HINSTANCE g_hInst;
UINT g_ref;

HANDLE m_menuImage = NULL;
WCHAR m_exePath[MAX_PATH] = { 0 };
DWORD m_option = FALSE;

// {9CE5906A-DFBB-4A5A-9EBF-9D262E5D29B9}
#define SHELLEXT_GUID   { 0x9ce5906a, 0xdfbb, 0x4a5a, { 0x9e, 0xbf, 0x9d, 0x26, 0x2e, 0x5d, 0x29, 0xb9 } }

BOOL APIENTRY DllMain(HMODULE hModule, DWORD dwReason, LPVOID lpReserved)
{
	switch (dwReason)
	{
	case DLL_PROCESS_ATTACH:
        g_hInst = hModule;

        if (m_menuImage == NULL)
            m_menuImage = LoadImageW(g_hInst, MAKEINTRESOURCEW(IDB_ICON), IMAGE_BITMAP, 0, 0, LR_DEFAULTSIZE | LR_LOADTRANSPARENT | LR_LOADMAP3DCOLORS);

        if (m_exePath[0] == 0)
        {
            HKEY hKey;
            if (RegOpenKeyExW(HKEY_LOCAL_MACHINE, L"Software\\RyuaNerin", NULL, KEY_READ, &hKey) == NO_ERROR)
            {
                DWORD len = sizeof(m_exePath);
                RegGetValueW(hKey, NULL, L"TiX", REG_SZ, NULL, reinterpret_cast<LPBYTE>(&m_exePath), &len);

                DWORD type = REG_DWORD;
                len = sizeof(m_option);                
                RegQueryValueExW(hKey, L"TiX-Option", NULL, &type, (LPBYTE)&m_option, &len);

                DebugLog(L"Get ExePath [%d] %s", len, m_exePath);
                DebugLog(L"Get Option [%d]", m_option);

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
    *ppReturn = 0;
    if (!IsEqualCLSID(SHELLEXT_GUID, rclsid))
        return CLASS_E_CLASSNOTAVAILABLE;

    ClassFactory *fac = new ClassFactory();
    if (fac == 0)
        return E_OUTOFMEMORY;

    HRESULT result = fac->QueryInterface(riid, ppReturn);
    fac->Release();
    return result;
}

STDAPI DllCanUnloadNow(void)
{
    return g_ref > 0 ? S_FALSE : S_OK;
}

BOOL Regist_Key(HKEY rootKey, LPWSTR subKey, LPWSTR keyName, LPWSTR keyData)
{
    HKEY hKey;
    DWORD dwDisp;

    if (RegCreateKeyExW(HKEY_CLASSES_ROOT, subKey, 0, NULL, REG_OPTION_NON_VOLATILE, KEY_WRITE, NULL, &hKey, &dwDisp) != NOERROR)
        return FALSE;

    RegSetValueExW(hKey, keyName, 0, REG_SZ, (LPBYTE)keyData, (lstrlen(keyData) + 1) * sizeof(TCHAR));
    RegCloseKey(hKey);
    return TRUE;
}
BOOL Regist_CLSID(LPWSTR clsid)
{
    TCHAR dllPath[MAX_PATH];
    GetModuleFileNameW(g_hInst, dllPath, ARRAYSIZE(dllPath));

    TCHAR subKey[MAX_PATH];
    wsprintf(subKey, L"CLSID\\%s", clsid);
    if (!Regist_Key(HKEY_CLASSES_ROOT, subKey, NULL, L"Shell Extension for TiX"))
        return FALSE;
    
    wsprintf(subKey, L"CLSID\\%s\\InprocServer32", clsid);
    if (!Regist_Key(HKEY_CLASSES_ROOT, subKey, NULL, dllPath))
        return FALSE;

    if (!Regist_Key(HKEY_CLASSES_ROOT, subKey, L"ThreadingModel", L"Apartment"))
        return FALSE;

    return TRUE;
}

STDAPI DllRegisterServer(void)
{
    DebugLog(L"Registering Server");
    
    TCHAR clsid[MAX_PATH];
    if (StringFromGUID2(SHELLEXT_GUID, clsid, ARRAYSIZE(clsid)) == 0)
        return SELFREG_E_CLASS;

    if (!Regist_CLSID(clsid))
        return SELFREG_E_CLASS;

    if (!Regist_Key(HKEY_CLASSES_ROOT, L"*\\ShellEx\\ContextMenuHandlers\\00TiXExt", NULL, clsid))
        return SELFREG_E_CLASS;

    if (!Regist_Key(HKEY_CLASSES_ROOT, L"Directory\\ShellEx\\ContextMenuHandlers\\00TiXExt", NULL, clsid))
        return SELFREG_E_CLASS;

    if (!Regist_Key(HKEY_LOCAL_MACHINE, L"Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Approved", NULL, L"00TiXExt"))
        return SELFREG_E_CLASS;

    return S_OK;
}

void Unregist_CLSID(LPWSTR clsid)
{
    TCHAR subKey[MAX_PATH];
    wsprintf(subKey, L"CLSID\\%s\\InprocServer32", clsid);
    RegDeleteKeyW(HKEY_CLASSES_ROOT, subKey);

    wsprintf(subKey, L"CLSID\\%s", clsid);
    RegDeleteKeyW(HKEY_CLASSES_ROOT, subKey);
}

STDAPI DllUnregisterServer(void)
{
    DebugLog(L"Unregistering server");

    TCHAR clsid[MAX_PATH];
    StringFromGUID2(SHELLEXT_GUID, clsid, ARRAYSIZE(clsid));
    
    Unregist_CLSID(clsid);
    
    RegDeleteKeyW(HKEY_CLASSES_ROOT, L"*\\ShellEx\\ContextMenuHandlers\\00TiXExt");

    RegDeleteKeyW(HKEY_CLASSES_ROOT, L"Directory\\ShellEx\\ContextMenuHandlers\\00TiXExt");

    HKEY hTmpKey;
    if (RegOpenKeyW(HKEY_LOCAL_MACHINE, L"Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Approved", &hTmpKey) == ERROR_SUCCESS)
    {
        RegDeleteValueW(hTmpKey, clsid);
        RegCloseKey(hTmpKey);
    }

    if (RegOpenKeyW(HKEY_LOCAL_MACHINE, L"Software\\RyuaNerin", &hTmpKey) == ERROR_SUCCESS)
    {
        RegDeleteValueW(hTmpKey, L"TiX");
        RegDeleteValueW(hTmpKey, L"TiX-Option");
        RegCloseKey(hTmpKey);
    }


    return S_OK;
}
