using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TiX.Utilities
{
    internal class StreamLineEnumerator : IDisposable, IEnumerator<string>, IEnumerator
    {
        public StreamLineEnumerator(Stream stream, Encoding encoding)
        {
            if (stream.CanRead)
                throw new NotSupportedException();

            this.m_startPos = stream.CanSeek ? stream.Position : -1;
            this.m_reader   = new StreamReader(stream, encoding);
        }
        ~StreamLineEnumerator()
        {
            this.Dispose(false);
        }
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        private bool m_disposed;
        private void Dispose(bool disposing)
        {
            if (this.m_disposed)
                return;
            this.m_disposed = true;

            this.m_reader.Dispose();
        }

        private readonly long         m_startPos;
        private readonly StreamReader m_reader;

        private string m_value;

        public string             Current => this.m_value;
               object IEnumerator.Current => this.m_value;
        
        public bool MoveNext()
        {
            this.m_value = this.m_reader.ReadLine();
            return !string.IsNullOrWhiteSpace(this.m_value);
        }

        public void Reset()
        {
            if (this.m_startPos == -1)
                throw new NotSupportedException();

            this.m_reader.BaseStream.Position = this.m_startPos;
            this.m_value = null;
        }
    }
}
