#pragma warning disable CA1416 // Validate platform compatibility

namespace RegFile
{
    public readonly struct SourceLocation
    {
        public SourceLocation(string source, int lineNumber)
        {
            Source = source;
            LineNumber = lineNumber;
        }

        public SourceLocation IncrementLineNumber()
        {
            return new SourceLocation(Source, LineNumber + 1);
        }

        public readonly int LineNumber { get; }
        public readonly string Source { get; }
    }
}