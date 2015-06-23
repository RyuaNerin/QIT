// Sample application that demonstrates a simple shell context menu.
// Ralph Arvesen (www.vertigo.com / www.lostsprings.com)

using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace CygwinContextMenu
{
	/// <summary>
	/// Register and unregister simple shell context menus.
	/// </summary>
	static class FileShellExtension
	{
		/// <summary>
		/// Register a simple shell context menu.
		/// </summary>
		/// <param name="fileType">The file type to register.</param>
		/// <param name="shellKeyName">Name that appears in the registry.</param>
		/// <param name="menuText">Text that appears in the context menu.</param>
		/// <param name="menuCommand">Command line that is executed.</param>
		public static void Register(
			string[] fileType, string shellKeyName, 
			string menuText, string menuCommand)
		{

			
			// create full path to registry location
            foreach (var item in fileType)
            {			
                Debug.Assert(!string.IsNullOrEmpty(item) &&
				!string.IsNullOrEmpty(shellKeyName) &&
				!string.IsNullOrEmpty(menuText) && 
				!string.IsNullOrEmpty(menuCommand));
                string regPath = string.Format(@"{0}\shell\{1}", item, shellKeyName);

			    // add context menu to the registry
			    using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(regPath))
			    {
				    key.SetValue(null, menuText);
			    }
			
			    // add command that is invoked to the registry
			    using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(
				    string.Format(@"{0}\command", regPath)))
			    {				
				    key.SetValue(null, menuCommand);
			    }
            }

		}

		/// <summary>
		/// Unregister a simple shell context menu.
		/// </summary>
		/// <param name="fileType">The file type to unregister.</param>
		/// <param name="shellKeyName">Name that was registered in the registry.</param>
		public static void Unregister(string[] fileType, string shellKeyName)
		{
            
            foreach (var item in fileType)
            {
			    Debug.Assert(!string.IsNullOrEmpty(item) &&
				    !string.IsNullOrEmpty(shellKeyName));

			    // full path to the registry location			
                string regPath = string.Format(@"{0}\shell\{1}", item, shellKeyName);

			    // remove context menu from the registry
			    Registry.ClassesRoot.DeleteSubKeyTree(regPath);                
            }

		}
	}

}
