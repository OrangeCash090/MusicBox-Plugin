using System.Runtime.InteropServices;

namespace MusicBox.Utils;

public static class NativeFileDialog {
	[ComImport]
	[Guid("42F85136-DB7E-439C-85F1-E4075D135FC8")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IFileOpenDialog {
		[PreserveSig]
		int Show([In] IntPtr hwndOwner);

		[PreserveSig]
		int SetFileTypes([In] uint cFileTypes, [In, MarshalAs(UnmanagedType.LPArray)] COMDLG_FILTERSPEC[] rgFilterSpec);

		[PreserveSig]
		int SetFileTypeIndex([In] uint iFileType);

		[PreserveSig]
		int GetFileTypeIndex(out uint piFileType);

		[PreserveSig]
		int Advise([In] IntPtr pfde, out uint pdwCookie);

		[PreserveSig]
		int Unadvise([In] uint dwCookie);

		[PreserveSig]
		int SetOptions([In] uint fos);

		[PreserveSig]
		int GetOptions(out uint pfos);

		[PreserveSig]
		int SetDefaultFolder([In] IShellItem psi);

		[PreserveSig]
		int SetFolder([In] IShellItem psi);

		[PreserveSig]
		int GetFolder(out IShellItem ppsi);

		[PreserveSig]
		int GetCurrentSelection(out IShellItem ppsi);

		[PreserveSig]
		int SetFileName([In, MarshalAs(UnmanagedType.LPWStr)] string pszName);

		[PreserveSig]
		int GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);

		[PreserveSig]
		int SetTitle([In, MarshalAs(UnmanagedType.LPWStr)] string pszTitle);

		[PreserveSig]
		int SetOkButtonLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszText);

		[PreserveSig]
		int SetFileNameLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

		[PreserveSig]
		int GetResult(out IShellItem ppsi);

		[PreserveSig]
		int AddPlace([In] IShellItem psi, int fdap);

		[PreserveSig]
		int SetDefaultExtension([In, MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);

		[PreserveSig]
		int Close(int hr);

		[PreserveSig]
		int SetClientGuid([In] ref Guid guid);

		[PreserveSig]
		int ClearClientData();

		[PreserveSig]
		int SetFilter([In] IntPtr pFilter);

		[PreserveSig]
		int GetResults(out IShellItemArray ppenum);

		[PreserveSig]
		int GetSelectedItems(out IShellItemArray ppenum);
	}

	[ComImport]
	[Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IShellItem {
		[PreserveSig]
		int BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);

		[PreserveSig]
		int GetParent(out IShellItem ppsi);

		[PreserveSig]
		int GetDisplayName([In] SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);

		[PreserveSig]
		int GetAttributes([In] uint sfgaoMask, out uint psfgaoAttribs);

		[PreserveSig]
		int Compare([In] IShellItem psi, [In] uint hint, out int piOrder);
	}

	[ComImport]
	[Guid("B63EA76D-1F85-456F-A19C-48159EFA858B")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IShellItemArray {
		[PreserveSig]
		int BindToHandler(IntPtr pbc, ref Guid rbhid, ref Guid riid, out IntPtr ppvOut);

		[PreserveSig]
		int GetPropertyStore(int flags, ref Guid riid, out IntPtr ppv);

		[PreserveSig]
		int GetPropertyDescriptionList(IntPtr keyType, ref Guid riid, out IntPtr ppv);

		[PreserveSig]
		int GetAttributes(int AttribFlags, uint sfgaoMask, out uint psfgaoAttribs);

		[PreserveSig]
		int GetCount(out uint pdwNumItems);

		[PreserveSig]
		int GetItemAt([In] uint dwIndex, out IShellItem ppsi);

		[PreserveSig]
		int EnumItems(out IntPtr ppenumShellItems);
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct COMDLG_FILTERSPEC {
		[MarshalAs(UnmanagedType.LPWStr)] public string pszName;
		[MarshalAs(UnmanagedType.LPWStr)] public string pszSpec;
	}

	private enum SIGDN : uint {
		FILESYSPATH = 0x80058000,
	}

	[ComImport]
	[Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
	private class FileOpenDialogCoClass { }

	private const int  HRESULT_CANCELLED  = unchecked((int)0x800704C7);
	private const int  S_OK               = 0;
	private const uint FOS_PICKFOLDERS    = 0x00000020;
	private const uint FOS_FORCEFILESYSTEM = 0x00000040;
	
	public static string? PickFolder(string? title = null, string? defaultPath = null) {
		IFileOpenDialog dialog = (IFileOpenDialog)new FileOpenDialogCoClass();

		try {
			dialog.GetOptions(out uint options);
			dialog.SetOptions(options | FOS_PICKFOLDERS | FOS_FORCEFILESYSTEM);

			if (title != null)
				dialog.SetTitle(title);

			if (defaultPath != null)
				SetInitialDirectory(dialog, defaultPath);

			int hr = dialog.Show(IntPtr.Zero);

			if (hr == HRESULT_CANCELLED) return null;
			if (hr != S_OK) Marshal.ThrowExceptionForHR(hr);

			dialog.GetResult(out IShellItem item);
			item.GetDisplayName(SIGDN.FILESYSPATH, out string path);
			return path;
		} finally {
			Marshal.ReleaseComObject(dialog);
		}
	}

	private static void SetInitialDirectory(IFileOpenDialog dialog, string path) {
		if (!Directory.Exists(path)) return;

		Guid clsidShellLink = new("9AC9FBE1-E0A2-4AD6-B4EE-E212013EA917");
		Guid iidShellItem   = new("43826D1E-E718-42EE-BC55-A1E261C37BFE");

		int hr = SHCreateItemFromParsingName(path, IntPtr.Zero, ref iidShellItem, out IShellItem item);
		if (hr != S_OK) return;

		try {
			dialog.SetFolder(item);
		} finally {
			Marshal.ReleaseComObject(item);
		}
	}

	[DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
	private static extern int SHCreateItemFromParsingName(
		[MarshalAs(UnmanagedType.LPWStr)] string pszPath,
		IntPtr pbc,
		ref Guid riid,
		out IShellItem ppv
	);

	private static void SetFileTypesManual(IFileOpenDialog dialog, COMDLG_FILTERSPEC[] filters) {
		int count = filters.Length;
		int structSize = Marshal.SizeOf<COMDLG_FILTERSPEC>();
		IntPtr pArray = Marshal.AllocCoTaskMem(structSize * count);

		IntPtr[] pinnedStrings = new IntPtr[count * 2];

		try {
			for (int i = 0; i < count; i++) {
				IntPtr pName = Marshal.StringToCoTaskMemUni(filters[i].pszName);
				IntPtr pSpec = Marshal.StringToCoTaskMemUni(filters[i].pszSpec);
				pinnedStrings[i * 2]     = pName;
				pinnedStrings[i * 2 + 1] = pSpec;

				IntPtr slot = pArray + i * structSize;
				Marshal.WriteIntPtr(slot, 0,            pName);
				Marshal.WriteIntPtr(slot, IntPtr.Size,  pSpec);
			}

			SetFileTypesViaPointer(dialog, (uint)count, pArray);
		} finally {
			foreach (IntPtr ptr in pinnedStrings)
				if (ptr != IntPtr.Zero)
					Marshal.FreeCoTaskMem(ptr);

			Marshal.FreeCoTaskMem(pArray);
		}
	}

	private static unsafe void SetFileTypesViaPointer(IFileOpenDialog dialog, uint count, IntPtr pFilterArray) {
		IntPtr pUnk = Marshal.GetIUnknownForObject(dialog);

		try {
			Guid iid = new("42F85136-DB7E-439C-85F1-E4075D135FC8");
			Marshal.QueryInterface(pUnk, ref iid, out IntPtr pDialog);

			try {
				IntPtr* vtable = *(IntPtr**)pDialog;
				delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr, int> setFileTypes =
					(delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr, int>)vtable[4];

				int hr = setFileTypes(pDialog, count, pFilterArray);
				if (hr != S_OK)
					Marshal.ThrowExceptionForHR(hr);
			} finally {
				Marshal.Release(pDialog);
			}
		} finally {
			Marshal.Release(pUnk);
		}
	}
}
