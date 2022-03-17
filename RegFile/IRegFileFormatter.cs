using System.IO;
#pragma warning disable CA1416 // Validate platform compatibility

namespace RegFile
{
    public interface IRegFileFormatter
    {
        void WriteTo(TextWriter writer);
    }
}