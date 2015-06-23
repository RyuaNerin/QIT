using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QITxRegEditor
{
    class Program
    {
        static string[] FileType = { "jpegfile", "pngfile", "giffile", "bmpfile" };
        static string ProductName = "QITx";
        static void Main(string[] args)
        {
            if(args.Length > 0)
            {
                switch (args[0])
                {
                    case "add":
                        // full path to self, %L is placeholder for selected file
                        string menuCommand = string.Format(
                            "\"{0}\" \"%L\"", Environment.CurrentDirectory + "/QITx.exe");

                        // register the context menu
                        CygwinContextMenu.FileShellExtension.Register(
                            FileType,
                            ProductName, "QITx로 트윗하기",
                            menuCommand);

                        Console.WriteLine(menuCommand);
                        break;
                    case "remove":
                        CygwinContextMenu.FileShellExtension.Unregister(FileType, ProductName);
                        break;
                }
            }
            //Console.Read();
        }
    }
}
