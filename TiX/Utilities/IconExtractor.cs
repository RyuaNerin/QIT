using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace TiX.Utilities
{
    internal struct IconExtractorEntry
    {
        public int    Width;
        public int    Height;
        public int    ColorCount;
        public int    Reserved;
        public int    ColorPlanes;
        public int    BitsPerPixel;
        public int    DataLength;
        public int    DataOffset;
        public Bitmap Image;
    }

    internal sealed class IconExtractor : IDisposable, IEnumerable, IEnumerable<IconExtractorEntry>
    {

        private readonly bool         m_leaveOpen;
        private readonly Stream       m_stream;
        private readonly BinaryReader m_reader;
        private readonly short        m_reserved;
        private readonly short        m_type;
        private readonly short        m_count;

        private readonly IconExtractorEntry[] m_entry;

        public IconExtractor(Stream stream, bool leaveOpen)
        {
            if (this.m_stream.CanSeek || this.m_stream.CanRead) throw new NotSupportedException();

            this.m_leaveOpen = leaveOpen;

            this.m_stream = stream;
            this.m_reader = new BinaryReader(stream, Encoding.Default, leaveOpen);

            this.m_reserved = this.m_reader.ReadInt16();
            if (this.m_reserved != 0) throw new NotSupportedException();

            this.m_type = this.m_reader.ReadInt16();
            if (this.m_type != 1 && this.m_type != 2) throw new NotSupportedException();

            this.m_count = this.m_reader.ReadInt16();

            int i;
            this.m_entry = new IconExtractorEntry[this.m_count];

            for (i = 0; i < this.m_count; i++)
                this.m_entry[i] = this.ReadEntry();

            for (i = 0; i < this.m_count; ++i)
                this.m_entry[i] = this.GetImage(this.m_entry[i]);
        }
        public IconExtractor(string filename)
            : this(File.OpenRead(filename), false)
        {
        }

        ~IconExtractor()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool m_disposed = false;
        private void Dispose(bool disposing)
        {
            if (this.m_disposed)
                return;
            this.m_disposed = true;

            if (disposing)
            {
                this.m_reader.Dispose();

                if (!this.m_leaveOpen)
                    this.m_stream.Dispose();
            }
        }
         
        private IconExtractorEntry ReadEntry()
        {
            var entry = new IconExtractorEntry()
            {
                Width        = this.m_reader.ReadByte(),
                Height       = this.m_reader.ReadByte(),
                ColorCount   = this.m_reader.ReadByte(),
                Reserved     = this.m_reader.ReadByte(),
                ColorPlanes  = this.m_reader.ReadInt16(),
                BitsPerPixel = this.m_reader.ReadInt16(),
                DataLength   = this.m_reader.ReadInt32(),
                DataOffset   = this.m_reader.ReadInt32()
            };
            if (entry.Width  == 0) entry.Width = 256;
            if (entry.Height == 0) entry.Height = 256;

            return entry;
        }

        private IconExtractorEntry GetImage(IconExtractorEntry entry)
        {
            using (var mem = new MemoryStream())
            using (var writer = new BinaryWriter(mem))
            {
                writer.Write((short)this.m_reserved); // Reserved
                writer.Write((short)this.m_type); // Type
                writer.Write((short)1); // Count

                /////

                writer.Write((byte )(entry.Width  == 256 ? 0 : entry.Width ));
                writer.Write((byte )(entry.Height == 256 ? 0 : entry.Height));;
                writer.Write((byte ) entry.ColorCount);
                writer.Write((byte ) entry.Reserved);
                writer.Write((short) entry.ColorPlanes);
                writer.Write((short) entry.BitsPerPixel);
                writer.Write((int  ) entry.DataLength);
                writer.Write((int  ) 22);

                /////

                this.m_stream.Position = entry.DataOffset;

                var buff = new byte[4096];
                int read;
                int remain = entry.DataLength;
                do
                {
                    read = this.m_stream.Read(buff, 0, Math.Min(remain, 4096));

                    mem.Write(buff, 0, read);

                    remain -= read;
                } while (remain > 0);

                writer.Flush();

                mem.Position = 0;

                entry.Image = (Bitmap)Image.FromStream(mem);
                entry.Width = entry.Image.Width;
                entry.Height = entry.Image.Height;
            }

            return entry;
        }

        public int Count => this.m_count;
        public IconExtractorEntry this[int index] => this.m_entry[index];

        public IEnumerator<IconExtractorEntry> GetEnumerator()
            => ((IEnumerable<IconExtractorEntry>)this.m_entry).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.m_entry.GetEnumerator();
    }
}
