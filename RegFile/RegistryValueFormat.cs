#pragma warning disable CA1416 // Validate platform compatibility

namespace RegFile
{
    public enum RegistryValueFormat
    {
        Unknown,
        Integer,
        String,
        HexValues,
        HexValuesLE,
        HexValuesBE,
        Utf16LEHexValues
    }
}