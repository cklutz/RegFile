#pragma warning disable CA1416 // Validate platform compatibility

using System;
using System.Collections.Generic;
using System.IO;

namespace RegFile
{
    public class RegFileWriter : IDisposable
    {
        private TextWriter m_writer;
        private readonly bool m_closeWriter;

        public RegFileWriter(string fileName)
        {
            m_closeWriter = true;
            m_writer = new StreamWriter(fileName ?? throw new ArgumentNullException(nameof(fileName)));
        }

        public RegFileWriter(TextWriter writer, bool closeWriter)
        {
            m_closeWriter = closeWriter;
            m_writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        public void Write(IEnumerable<RegistrySubKeyCommand> commands)
        {
            if (commands == null)
            {
                throw new ArgumentNullException(nameof(commands));
            }

            m_writer.WriteLine(RegFileReader.Signature);
            m_writer.WriteLine();

            foreach (var command in commands)
            {
                command.WriteTo(m_writer);
                m_writer.WriteLine();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (m_closeWriter)
                {
                    m_writer?.Dispose();
                    m_writer = null;
                }
            }
        }

        public void Dispose() => Dispose(true);
    }
}