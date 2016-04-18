#include "stdafx.h"
#include "ContextMenu.h"

#include <Shellapi.h>

#include "resource.h"
#include "DebugLog.h"

#define MENU_WITH_TEXT      0
#define MENU_WITHOUT_TEXT   1

// Dll Main
extern HINSTANCE g_hInst;
extern UINT g_ref;

extern HANDLE m_menuImage;
extern WCHAR m_exePath[MAX_PATH];
extern DWORD m_option;

#define VECTOR_RESERVE  32

ContextMenu::ContextMenu()
{
    DebugLog(L"ContextMenu");

    m_files.reserve(VECTOR_RESERVE);
    m_refCnt = 1;

    InterlockedIncrement(&g_ref);
}

ContextMenu::~ContextMenu()
{
    InterlockedDecrement(&g_ref);
}

#pragma region IUnknown
STDMETHODIMP ContextMenu::QueryInterface(REFIID riid, LPVOID *ppvObject)
{
    *ppvObject = 0;

    if (IsEqualIID(riid, IID_IUnknown))
        *ppvObject = this;
    else if (IsEqualIID(riid, IID_IShellExtInit))
        *ppvObject = (IShellExtInit*)this;
    else if (IsEqualIID(riid, IID_IContextMenu))
        *ppvObject = (IContextMenu*)this;

    if (*ppvObject)
    {
        ((LPUNKNOWN)(*ppvObject))->AddRef();
        return S_OK;
    }

    return E_NOINTERFACE;
}

STDMETHODIMP_(ULONG) ContextMenu::AddRef()
{
    return InterlockedIncrement(&m_refCnt);
}

STDMETHODIMP_(ULONG) ContextMenu::Release()
{
    ULONG newRef = InterlockedDecrement(&m_refCnt);
    if (newRef == 0)
        delete this;
    return newRef;
}
#pragma endregion IUnknown

#pragma region IShellExtInit
STDMETHODIMP ContextMenu::Initialize(LPCITEMIDLIST pidlFolder, LPDATAOBJECT pdtobj, HKEY hkeyProgID)
{
    ClearFiles();

    if (pdtobj == NULL)
        return E_INVALIDARG;

    FORMATETC fmt = { CF_HDROP, NULL, DVASPECT_CONTENT, -1, TYMED_HGLOBAL };
    STGMEDIUM stg = { TYMED_HGLOBAL };
    if (FAILED(pdtobj->GetData(&fmt, &stg)))
        return E_INVALIDARG;
    
    HDROP hDrop = (HDROP)GlobalLock(stg.hGlobal);

    if (hDrop == NULL)
        return E_INVALIDARG;

    UINT files = DragQueryFileW(hDrop, 0xFFFFFFFF, NULL, 0);

    HRESULT hr = E_INVALIDARG;
    if (files > 0)
    {
        hr = S_OK;

        DebugLog(L"Found %d files", files);

        UINT len;
        LPWSTR path;
        for (UINT i = 0; i < files; i++)
        {
            len = DragQueryFileW(hDrop, i, NULL, 0);
            if (len == 0)
            {
                ClearFiles();
                hr = E_INVALIDARG;
                break;
            }

            ++len;

            path = (LPWSTR)new WCHAR[len];
            DragQueryFileW(hDrop, i, path, len);
            m_files.push_back(path);
            DebugLog(path);
            path = NULL;
        }
    }
    
    GlobalUnlock(stg.hGlobal);
    ReleaseStgMedium(&stg);

    return hr;
}
#pragma endregion IShellExtInit

#pragma region IContextMenu
STDMETHODIMP ContextMenu::QueryContextMenu(HMENU hMenu, UINT uMenuIndex, UINT uidFirstCmd, UINT uidLastCmd, UINT uFlags)
{
    if (m_exePath[0] == 0)
        return MAKE_HRESULT(SEVERITY_SUCCESS, 0, 0);

    if (uFlags & CMF_DEFAULTONLY)
        return MAKE_HRESULT(SEVERITY_SUCCESS, 0, 0);

    DWORD items = 0;

    DebugLog(L"QueryContextMenu / Option : [%d]", m_option);

    if (m_option & 1)
    {
        MENUITEMINFO mii;

        mii = { sizeof(MENUITEMINFO) };
        mii.fMask = MIIM_BITMAP | MIIM_STRING | MIIM_FTYPE | MIIM_ID | MIIM_STATE;
        mii.wID = uidFirstCmd + items;
        mii.fType = MFT_STRING;
        mii.dwTypeData = L"TiX 로 트윗하기 (&8)";
        mii.fState = MFS_ENABLED;
        mii.hbmpItem = static_cast<HBITMAP>(m_menuImage);

        if (!InsertMenuItem(hMenu, uMenuIndex + items, TRUE, &mii))
            return HRESULT_FROM_WIN32(GetLastError());
        items++;
    }

    if (m_option & 2)
    {
        MENUITEMINFO mii;

        mii = { sizeof(MENUITEMINFO) };
        mii.fMask = MIIM_BITMAP | MIIM_STRING | MIIM_FTYPE | MIIM_ID | MIIM_STATE;
        mii.wID = uidFirstCmd + items;
        mii.fType = MFT_STRING;
        mii.dwTypeData = L"TiX 로 바로 트윗하기 (&9)";
        mii.fState = MFS_ENABLED;
        mii.hbmpItem = static_cast<HBITMAP>(m_menuImage);

        if (!InsertMenuItem(hMenu, uMenuIndex + items, TRUE, &mii))
            return HRESULT_FROM_WIN32(GetLastError());
        items++;
    }

    return MAKE_HRESULT(SEVERITY_SUCCESS, 0, items);
}

STDMETHODIMP ContextMenu::GetCommandString(UINT_PTR idCmd, UINT uFlags, UINT* pwReserved, LPSTR pszName, UINT cchMax)
{
    if (idCmd != 0)
        return E_INVALIDARG;

    if (uFlags & GCS_HELPTEXT)
    {
        if (uFlags & GCS_UNICODE)
            wcscpy_s((wchar_t*)pszName, cchMax, L"TiX 로 트윗하기");
        else
            lstrcpynA(pszName, "Tweet with TiX", cchMax);

        return S_OK;
    }

    return E_INVALIDARG;
}

STDMETHODIMP ContextMenu::InvokeCommand(LPCMINVOKECOMMANDINFO pCmdInfo)
{
    if (m_exePath[0] == 0)
        return S_OK;

    WCHAR wbuff[MAX_PATH + 40];
    char buff[MAX_PATH * 4];

    DebugLog(L"Process");

    HANDLE hRead, hWrite;
    SECURITY_ATTRIBUTES sa = { sizeof(SECURITY_ATTRIBUTES), 0, TRUE };

    HANDLE proc = GetCurrentProcess();
    CreatePipe(&hRead, &hWrite, &sa, 0);
    DuplicateHandle(proc, hWrite, proc, &hWrite, 0, FALSE, DUPLICATE_CLOSE_SOURCE | DUPLICATE_SAME_ACCESS);

    PROCESS_INFORMATION	procInfo = { 0 };
    STARTUPINFO startInfo = { sizeof(STARTUPINFO), 0 };
    startInfo.dwFlags = STARTF_USESTDHANDLES;
    startInfo.hStdInput = hRead;
    startInfo.hStdOutput = GetStdHandle(STD_OUTPUT_HANDLE);
    startInfo.hStdError = GetStdHandle(STD_ERROR_HANDLE);

    DebugLog(L"Verb : %d", pCmdInfo->lpVerb);
    
    if ((((m_option & 1) == 1) && LOWORD(pCmdInfo->lpVerb) == 1) ||
        (((m_option & 1) == 0) && LOWORD(pCmdInfo->lpVerb) == 0))
        wsprintfW(wbuff, L"\"%s\" \"--pipe\" \"--notext\"", m_exePath);
    else
        wsprintfW(wbuff, L"\"%s\" \"--pipe\"", m_exePath);

    BOOL ret = CreateProcessW(m_exePath, wbuff, 0, 0, TRUE, CREATE_DEFAULT_ERROR_MODE, 0, 0, &startInfo, &procInfo);

    CloseHandle(hRead);

    if (ret)
    {
        WriteToHandle(wbuff, buff, hWrite);

        CloseHandle(procInfo.hThread);
        CloseHandle(procInfo.hProcess);
    }

    CloseHandle(hWrite);


    return S_OK;
}

void ContextMenu::WriteToHandle(LPWSTR buffUni, LPSTR buffUtf8, HANDLE hWnd)
{
    DWORD lenUni;
    DWORD len;

    for (auto iter = m_files.begin(); iter != m_files.end(); ++iter)
    {
        lenUni  = wsprintfW(buffUni, *iter);
        lenUni += wsprintfW(buffUni + lenUni, L"\n");
        
        len = WideCharToMultiByte(CP_UTF8, 0, buffUni, lenUni, NULL, 0, NULL, NULL);
        WideCharToMultiByte(CP_UTF8, 0, buffUni, lenUni, buffUtf8, len, NULL, NULL);

        WriteFile(hWnd, buffUtf8, len * sizeof(char), &len, 0);

        DebugLog(buffUni);
    }
}

#pragma endregion IContextMenu

void ContextMenu::ClearFiles()
{
    DebugLog(L"ClearFiles");

    for (auto iter = m_files.begin(); iter != m_files.end(); ++iter)
        delete *iter;
    m_files.clear();

    if (m_files.capacity() > VECTOR_RESERVE * 2)
        m_files.reserve(VECTOR_RESERVE);
}
