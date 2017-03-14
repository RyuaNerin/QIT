using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace TiXdTest
{
    [TestClass]
    public class TiXdTest
    {
        [TestMethod]
        public void ResizeImage()
        {
            using (var bitmap = Bitmap.FromFile(@"..\..\..\sample_image.png"))
            {
                using (var r = TiX.LibTiX.ResizeImage(bitmap))
                {
                    r.Task.Wait();

                    Assert.IsNotNull(r.ResizedImage);
                    Assert.IsNotNull(r.ResizeRawImage);
                    Assert.IsNotNull(r.Thumbnail);
                    Assert.IsFalse(r.ResizeRawImage.Length > 3 * 1024 * 1024); // 3 MB
                    Assert.IsNull(r.GifFrames);
                }
            }
        }
    }
}
