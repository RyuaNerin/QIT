#pragma once

#include <shlobj.h> // IShellExtInit IContextMenu
#include <vector>

class ContextMenu : public IShellExtInit, IContextMenu
{
public:
    ContextMenu(void);
    virtual ~ContextMenu(void);

    // IUnknown
    IFACEMETHODIMP QueryInterface(REFIID, void**);
    IFACEMETHODIMP_(ULONG) AddRef();
    IFACEMETHODIMP_(ULONG) Release();

    // IShellExtInit
    IFACEMETHODIMP Initialize(LPCITEMIDLIST, LPDATAOBJECT, HKEY);

    // IContextMenu
    IFACEMETHODIMP QueryContextMenu(HMENU, UINT, UINT, UINT, UINT);
    IFACEMETHODIMP InvokeCommand(LPCMINVOKECOMMANDINFO);
    IFACEMETHODIMP GetCommandString(UINT_PTR, UINT, UINT*, LPSTR, UINT);

private:
    void ClearFiles();
    void WriteToHandle(LPWSTR, LPSTR, HANDLE);

    ULONG m_refCnt;
    std::vector<LPCWSTR> m_files;

    DWORD m_id_withText    = -1;
    DWORD m_id_withoutText = -1;
};
