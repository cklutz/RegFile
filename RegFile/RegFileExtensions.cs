using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32;
#pragma warning disable CA1416 // Validate platform compatibility

namespace RegFile
{
    public static class RegFileExtensions
    {
        public static (RegistryHive, string) SplitIntoHiveAndSubKey(this ReadOnlySpan<char> span, SourceLocation sourceLocation)
        {
            int pos = span.IndexOf('\\');
            if (pos == -1)
            {
                throw sourceLocation.ParseException("Invalid key specification");
            }

            var hive = StringToHive(span.Slice(0, pos).ToString());
            if (hive == null)
            {
                throw sourceLocation.ParseException("Invalid hive");
            }

            string subKey = span.Slice(pos + 1).ToString();
            if (string.IsNullOrWhiteSpace(subKey))
            {
                throw sourceLocation.ParseException("Invalid sub key");
            }

            return (hive.Value, subKey);
        }

        private static RegistryHive? StringToHive(string str)
        {
            switch (str)
            {
                case "HKEY_CLASSES_ROOT":
                    return RegistryHive.ClassesRoot;
                case "HKEY_CURRENT_USER":
                    return RegistryHive.CurrentUser;
                case "HKEY_LOCAL_MACHINE":
                    return RegistryHive.LocalMachine;
                case "HKEY_USERS":
                    return RegistryHive.Users;
                case "HKEY_PERFORMANCE_DATA":
                    return RegistryHive.PerformanceData;
                case "HKEY_CURRENT_CONFIG":
                    return RegistryHive.CurrentConfig;
                default:
                    return null;
            }
        }

        public static string HiveToString(this RegistryHive hive)
        {
            switch (hive)
            {
                case RegistryHive.ClassesRoot:
                    return "HKEY_CLASSES_ROOT";
                case RegistryHive.CurrentUser:
                    return "HKEY_CURRENT_USER";
                case RegistryHive.LocalMachine:
                    return "HKEY_LOCAL_MACHINE";
                case RegistryHive.Users:
                    return "HKEY_USERS";
                case RegistryHive.PerformanceData:
                    return "HKEY_PERFORMANCE_DATA";
                case RegistryHive.CurrentConfig:
                    return "HKEY_CURRENT_CONFIG";
                default:
                    throw new ArgumentOutOfRangeException(nameof(hive), hive, null);
            }
        }

        public static IOException ParseException(this SourceLocation location, string msg)
        {
            throw new IOException($"{msg} in {location.Source}, line {location.LineNumber}");
        }

        public static string Unescape(string str)
        {
            return str.Replace(@"\\", @"\");
        }

        public static string Escape(string str)
        {
            return str.Replace(@"\", @"\\");
        }

        public static RegValueInfo GetValueInfo(string buf/*ReadOnlySpan<char> buf*/)
        {
#if false
            "Value A"="<String value data with escape characters>"
"Value B"=hex:<Binary data (as comma-delimited list of hexadecimal values)>
"Value C"=dword:<DWORD value integer>
"Value D"=hex(0):<REG_NONE (as comma-delimited list of hexadecimal values)>
"Value E"=hex(1):<REG_SZ (as comma-delimited list of hexadecimal values representing a UTF-16LE NUL-terminated string)>
"Value F"=hex(2):<Expandable string value data (as comma-delimited list of hexadecimal values representing a UTF-16LE NUL-terminated string)>
"Value G"=hex(3):<Binary data (as comma-delimited list of hexadecimal values)> ; equal to "Value B"
"Value H"=hex(4):<DWORD value (as comma-delimited list of 4 hexadecimal values, in little endian byte order)>
"Value I"=hex(5):<DWORD value (as comma-delimited list of 4 hexadecimal values, in big endian byte order)>
"Value J"=hex(7):<Multi-string value data (as comma-delimited list of hexadecimal values representing UTF-16LE NUL-terminated strings)>
"Value K"=hex(8):<REG_RESOURCE_LIST (as comma-delimited list of hexadecimal values)>
"Value L"=hex(a):<REG_RESOURCE_REQUIREMENTS_LIST (as comma-delimited list of hexadecimal values)>
"Value M"=hex(b):<QWORD value (as comma-delimited list of 8 hexadecimal values, in little endian byte order)>
#endif
            switch (buf)
            {
                case "dword":
                    return new RegValueInfo(RegistryValueKind.DWord, RegistryValueFormat.Integer);
                case "hex":
                case "hex(3)":
                    return new RegValueInfo(RegistryValueKind.Binary, RegistryValueFormat.HexValues);
                case "hex(0)":
                    return new RegValueInfo(RegistryValueKind.None, RegistryValueFormat.HexValues);
                case "hex(1)":
                    return new RegValueInfo(RegistryValueKind.String, RegistryValueFormat.Utf16LEHexValues);
                case "hex(2)":
                    return new RegValueInfo(RegistryValueKind.ExpandString, RegistryValueFormat.Utf16LEHexValues);
                case "hex(4)":
                    return new RegValueInfo(RegistryValueKind.DWord, RegistryValueFormat.HexValuesLE);
                case "hex(5)":
                    return new RegValueInfo(RegistryValueKind.DWord, RegistryValueFormat.HexValuesBE);
                case "hex(7)":
                    return new RegValueInfo(RegistryValueKind.MultiString, RegistryValueFormat.Utf16LEHexValues);
                case "hex(b)":
                    return new RegValueInfo(RegistryValueKind.QWord, RegistryValueFormat.HexValuesLE);
                case "hex(8)":
                case "hex(a)":
                default:
                    return new RegValueInfo(RegistryValueKind.Unknown, RegistryValueFormat.Unknown);
            }
        }

        private static string GetHexMultiString(this RegValueInfo valueInfo, string[] strings)
        {
            var sb = new StringBuilder();

            int total = 0;
            int i;
            for (i = 0; i < strings.Length; i++)
            {
                AddHexString(ref total, valueInfo, sb, Encoding.Unicode.GetBytes(strings[i]));
            }

            AddNullTermination(ref total, sb);

            return sb.ToString();
        }

        private static string GetHexString(this RegValueInfo valueInfo, byte[] bytes)
        {
            var sb = new StringBuilder();
            if (bytes != null)
            {
                int total = 0;
                AddHexString(ref total, valueInfo, sb, bytes);
            }
            return sb.ToString();
        }

        private static void AddHexString(ref int total, RegValueInfo valueInfo, StringBuilder sb, byte[] bytes)
        {
            // TODO: Line continuation is different than with reg.exe (according to ReactOS sources).
            // Here we just consider the number of hex pairs written - in reg.exe it also considers
            // the length of the complete line, not to be more than 77 characters.

            int i;
            for (i = 0; i < bytes.Length; i++, total++)
            {
                if (total > 0)
                {
                    sb.Append(',');
                }
                if (total % 32 == 31)
                {
                    sb.AppendLine("\\");
                    sb.Append("  ");
                }
                sb.Append(bytes[i].ToString("X2"));
            }


            if (valueInfo.Format == RegistryValueFormat.Utf16LEHexValues)
            {
                // UTF 16 need two (encoded) bytes to represent NUL
                AddNullTermination(ref total, sb);
            }
        }

        private static void AddNullTermination(ref int total, StringBuilder sb)
        {
            int i = 0;
            for (; i < 2; i++, total++)
            {
                if (total > 0 || i > 0)
                {
                    sb.Append(',');
                }
                if (total % 32 == 31)
                {
                    sb.AppendLine("\\");
                    sb.Append("  ");
                }
                sb.Append("00");
            }

        }

        public static string GetRegFileValue(this RegValueInfo valueInfo, object value)
        {
            switch (valueInfo.Format)
            {
                case RegistryValueFormat.Integer:
                    return "dword:" + ((int)value).ToString("X8");
                case RegistryValueFormat.String:
                    return "\"" + Escape(value.ToString()) + "\"";
                case RegistryValueFormat.HexValues:
                    switch (valueInfo.Kind)
                    {
                        case RegistryValueKind.Binary:
                            return "hex:" + valueInfo.GetHexString((byte[])value);
                        case RegistryValueKind.None:
                            return "hex(0):" + valueInfo.GetHexString((byte[])value);
                        default:
                            throw new ArgumentOutOfRangeException(nameof(valueInfo), valueInfo.Kind, null);
                    }
                case RegistryValueFormat.HexValuesLE:
                    switch (valueInfo.Kind)
                    {
                        case RegistryValueKind.QWord:
                            var bytes = BitConverter.GetBytes((long)value);
                            Array.Reverse(bytes);
                            return "hex(b):" + valueInfo.GetHexString(bytes);
                        case RegistryValueKind.DWord:
                            return "hex(4):" + valueInfo.GetHexString(BitConverter.GetBytes((int)value));
                        default:
                            throw new ArgumentOutOfRangeException(nameof(valueInfo), valueInfo.Kind, null);
                    }
                case RegistryValueFormat.HexValuesBE:
                    // TODO: Endianess?
                    return "hex(5):" + valueInfo.GetHexString(BitConverter.GetBytes((int)value));
                case RegistryValueFormat.Utf16LEHexValues:
                    switch (valueInfo.Kind)
                    {
                        // TODO: Add trailing NUL
                        case RegistryValueKind.String:
                            return "hex(1):" + valueInfo.GetHexString(Encoding.Unicode.GetBytes((string)value));
                        case RegistryValueKind.ExpandString:
                            return "hex(2):" + valueInfo.GetHexString(Encoding.Unicode.GetBytes((string)value));
                        case RegistryValueKind.MultiString:
                            return "hex(7):" + valueInfo.GetHexMultiString((string[])value);
                        // TODO: value is string[]
                        //return "hex(7):" + GetHexString(Encoding.Unicode.GetBytes((string)value));
                        default:
                            throw new ArgumentOutOfRangeException(nameof(valueInfo), valueInfo.Kind, null);
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(valueInfo), valueInfo.Format, null);
            }
        }

        public static string GetRegFileToken(this RegValueInfo valueInfo)
        {
            switch (valueInfo.Format)
            {
                case RegistryValueFormat.Integer:
                    return "dword";
                case RegistryValueFormat.String:
                    return null;
                case RegistryValueFormat.HexValues:
                    switch (valueInfo.Kind)
                    {
                        case RegistryValueKind.Binary:
                            return "hex";
                        case RegistryValueKind.None:
                            return "hex(0)";
                        default:
                            throw new ArgumentOutOfRangeException(nameof(valueInfo), valueInfo.Kind, null);
                    }
                case RegistryValueFormat.HexValuesLE:
                    switch (valueInfo.Kind)
                    {
                        case RegistryValueKind.QWord:
                            return "hex(b)";
                        case RegistryValueKind.DWord:
                            return "hex(4)";
                        default:
                            throw new ArgumentOutOfRangeException(nameof(valueInfo), valueInfo.Kind, null);
                    }
                case RegistryValueFormat.HexValuesBE:
                    return "hex(5)";
                case RegistryValueFormat.Utf16LEHexValues:
                    switch (valueInfo.Kind)
                    {
                        case RegistryValueKind.String:
                            return "hex(1)";
                        case RegistryValueKind.ExpandString:
                            return "hex(2)";
                        case RegistryValueKind.MultiString:
                            return "hex(7)";
                        default:
                            throw new ArgumentOutOfRangeException(nameof(valueInfo), valueInfo.Kind, null);
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(valueInfo), valueInfo.Format, null);
            }
        }

        public static object GetNativeValue(this RegValueInfo valueInfo, ReadOnlySpan<char> span, SourceLocation sourceLocation)
        {
            return GetNativeValue(valueInfo, span.ToString(), sourceLocation);
        }

        public static object GetNativeValue(this RegValueInfo valueInfo, string str, SourceLocation sourceLocation)
        {
            if (valueInfo.Format == RegistryValueFormat.String)
            {
                return Unescape(str);
            }
            else if (valueInfo.Format == RegistryValueFormat.Integer)
            {
                if (!int.TryParse(str, System.Globalization.NumberStyles.HexNumber, null, out int value))
                {
                    throw sourceLocation.ParseException($"Expected integer value '{str}'");
                }
                return value;
            }
            else if (valueInfo.Format == RegistryValueFormat.Utf16LEHexValues)
            {
                byte[] bytes = str.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(h => ToByte(h)).ToArray();
                if (valueInfo.Kind == RegistryValueKind.MultiString)
                {
                    if (bytes.Length < 2 || bytes[bytes.Length - 1] != 0x00 || bytes[bytes.Length - 2] != 0x00)
                    {
                        throw sourceLocation.ParseException("Expected two NUL bytes at end of value");
                    }

                    if (bytes.Length % 2 != 0)
                    {
                        throw sourceLocation.ParseException("Expected even number of hex pairs");
                    }

                    var segment = bytes.AsSpan().Slice(0, bytes.Length - 2);

                    var result = new List<string>();
                    foreach (var part in Split(segment.ToArray()))
                    {
                        string s = Encoding.Unicode.GetString(part);
                        result.Add(s);
                    }

                    return result.ToArray();
                }

                if (bytes.Length < 1 || bytes[bytes.Length - 1] != 0x00)
                {
                    throw sourceLocation.ParseException("Expected NUL byte at end of value");
                }

                string str2 = Encoding.Unicode.GetString(bytes, 0, bytes.Length).TrimEnd('\0'); // remove trailing NUL
                return str2;
            }
            else if (valueInfo.Format == RegistryValueFormat.HexValues || valueInfo.Format == RegistryValueFormat.HexValuesLE || valueInfo.Format == RegistryValueFormat.HexValuesBE)
            {
                byte[] bytes = str.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(h => ToByte(h)).ToArray();

                if (valueInfo.Kind == RegistryValueKind.None || valueInfo.Kind == RegistryValueKind.Binary)
                {
                    return bytes;
                }
                else if (valueInfo.Kind == RegistryValueKind.DWord)
                {
                    if (bytes.Length < sizeof(int))
                    {
                        throw sourceLocation.ParseException($"Expected {sizeof(int)} hex pairs");
                    }

                    return BitConverter.ToInt32(bytes);
                }
                else if (valueInfo.Kind == RegistryValueKind.QWord)
                {
                    if (bytes.Length < sizeof(long))
                    {
                        throw sourceLocation.ParseException($"Expected {sizeof(long)} hex pairs");
                    }

                    Array.Reverse(bytes);
                    return BitConverter.ToInt64(bytes);
                }
            }

            throw sourceLocation.ParseException($"Cannot process value type {valueInfo}");
        }

        private static byte ToByte(string s)
        {
            Debug.Assert(s.Length == 2, "s.Length == 2", $">{s}<");
            int[] HexValue = new int[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F };
            return (byte)(HexValue[char.ToUpper(s[0]) - '0'] << 4 | HexValue[char.ToUpper(s[1]) - '0']);
        }

        private static List<ArraySegment<byte>> Split(byte[] arr)
        {
            var result = new List<ArraySegment<byte>>();
            int segStart = 0;

            for (int i = 0; i < arr.Length; i += 2)
            {
                byte b0 = arr[i];
                byte b1 = arr[i + 1];

                if (b0 == '\0' && b1 == '\0')
                {
                    int segLen = (i + 2) - segStart - 2;
                    // Return empty segements as well. In the context of "MultiString", this is
                    // required to support "empty elements" in an array of strings.
                    result.Add(new ArraySegment<byte>(arr, segStart, segLen));
                    segStart = i + 2;
                }
            }

            if (segStart < arr.Length)
            {
                result.Add(new ArraySegment<byte>(arr, segStart, arr.Length - segStart));
            }

            return result;
        }
    }
}