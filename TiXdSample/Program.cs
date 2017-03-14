using System;
using System.Drawing;
using System.IO;
using TiX;

namespace TiXdSample
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            using (var bitmap = Bitmap.FromFile(@"..\..\..\sample_image.png"))
            {
                Console.WriteLine("ResizeImage : ");
                using (var r = TiX.LibTiX.ResizeImage(bitmap))
                {
                    r.Task.Wait();

                    Console.WriteLine("result : {0}", r.Task.Result != null);

                    Console.WriteLine("ori width : {0}", bitmap.Width);
                    Console.WriteLine("new width : {0}", r.ResizedImage.Width);

                    Console.WriteLine("ori height : {0}", bitmap.Width);
                    Console.WriteLine("new height : {0}", r.ResizedImage.Width);

                    Console.WriteLine("ori file size : {0}", new FileInfo(@"..\..\..\sample_image.png").Length);
                    Console.WriteLine("new file size : {0}", r.ResizeRawImage.Length);

                    Console.ReadKey();
                }

                Console.Write("TweetByTiX from image : ");
                Console.WriteLine(LibTiX.TweetByTiX(null, new Image[] { bitmap }, null, null, false));
                Console.ReadKey();
            }

            Console.Write("TweetByTiX from path : ");
            Console.WriteLine(LibTiX.TweetByTiX(null, new string[] { @"..\..\..\sample_image.png" }, false, null, null, false));
            Console.ReadKey();

            Console.Write("ShowSettingDialog : ");
            LibTiX.ShowSettingDialog(null);
            Console.ReadKey();
        }
    }
}
