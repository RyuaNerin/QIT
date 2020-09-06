using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Serialization;

namespace TiX.Utilities
{
    internal class GifFrameReader
    {
        public const int DefaultDelay = 80;

        public struct Frame
        {
            public int Offset { get; set; }
            public int Delay  { get; set; }
        }

        private readonly Image m_image;

        public Size    Size   { get; }
        public Frame[] Frames { get; }
        public int     FrameCount => this.Frames.Length;

        private const int TimePropertyId = 0x5100;
        private const int PropertyTagTypeByte = 1;

        public static PropertyItem ToPropertyItem(Frame[] frames)
        {
            var item = (PropertyItem)FormatterServices.GetUninitializedObject(typeof(PropertyItem));
            item.Id    = TimePropertyId;
            item.Len   = frames.Length;
            item.Type  = PropertyTagTypeByte;
            item.Value = new byte[frames.Length * 4];

            for (var i = 0; i < frames.Length; i++)
            {
                var b = BitConverter.GetBytes(frames[i].Delay / 10);
                item.Value[i * 4 + 0] = b[0];
                item.Value[i * 4 + 1] = b[1];
                item.Value[i * 4 + 2] = b[2];
                item.Value[i * 4 + 3] = b[3];
            }

            return item;
        }

        public GifFrameReader(Image image)
        {
            this.Size = image.Size;
            this.m_image = image;

            var frames = image.GetFrameCount(FrameDimension.Time);
            if (frames < 1) throw new NotSupportedException();

            var times = image.GetPropertyItem(TimePropertyId).Value;
            this.Frames = new Frame[times.Length];

            for (int offset = 0; offset < frames; ++offset)
            {
                var delay = BitConverter.ToInt32(times, 4 * offset) * 10;
                if (delay == 0)
                    delay = DefaultDelay;

                this.Frames[offset] = new Frame
                {
                    Offset = offset,
                    Delay  = delay,
                };
            }
        }

        public Image GetImage(int offset)
        {
            this.m_image.SelectActiveFrame(FrameDimension.Time, offset);
            return new Bitmap(this.m_image);
        }
    }
}
