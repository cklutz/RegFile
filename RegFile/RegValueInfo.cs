using Microsoft.Win32;

namespace RegFile
{
    public readonly struct RegValueInfo
    {
        public RegValueInfo(RegistryValueKind kind, RegistryValueFormat format)
        {
            Kind = kind;
            Format = format;
        }

        public readonly RegistryValueKind Kind { get; }
        public readonly RegistryValueFormat Format { get; }

        public override string ToString() => Kind + ", " + Format;
    }
}