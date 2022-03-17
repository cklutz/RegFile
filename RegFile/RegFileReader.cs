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
    public class RegFileReader : IDisposable
    {
        private TextReader m_reader;
        private readonly bool m_closeReader;
        private readonly string m_source;

        // Could also support "REGEDIT4" (the encoded as ANSI, not UTF-8), but whatever...
        internal const string Signature = "Windows Registry Editor Version 5.00";

        public RegFileReader(string fileName)
        {
            m_closeReader = true;
            m_reader = new StreamReader(fileName ?? throw new ArgumentNullException(nameof(fileName)));
            m_source = fileName;
        }

        public RegFileReader(TextReader reader, bool closeReader)
        {
            m_closeReader = closeReader;
            m_reader = reader ?? throw new ArgumentNullException(nameof(reader));
            m_source = "<stream>";
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (m_closeReader)
                {
                    m_reader?.Dispose();
                    m_reader = null;
                }
            }
        }

        public void Dispose() => Dispose(true);

        public IEnumerable<RegistrySubKeyCommand> Read()
        {
            var result = new List<RegistrySubKeyCommand>();
            string line;
            RegistrySubKeyCommand currentSubKey = null;
            RegistryValueCommand currentCommand = null;
            StringBuilder valueBuffer = null;
            string currentKindId = null;
            RegValueInfo currentValueInfo = default;
            var sourceLocation = new SourceLocation(m_source, 0);

            try
            {
                while ((line = m_reader.ReadLine()) != null)
                {
                    sourceLocation = sourceLocation.IncrementLineNumber();

                    if (sourceLocation.LineNumber == 1)
                    {
                        if (!line.Equals(Signature, StringComparison.Ordinal))
                        {
                            throw sourceLocation.ParseException($"Expected signature '{Signature}'");
                        }
                        continue;
                    }

                    if (sourceLocation.LineNumber == 2)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            throw sourceLocation.ParseException("Expected blank line after signature");
                        }
                        continue;
                    }

                    var data = line.AsSpan();

                    if (data.IsEmpty || data.IsWhiteSpace())
                    {
                        currentSubKey = null;
                        continue;
                    }

                    data = data.TrimStart();
                    if (data[0] == ';')
                    {
                        continue;
                    }

                    if (data[0] == '[')
                    {
                        data = data.TrimEnd();
                        if (data[data.Length - 1] == ']')
                        {
                            var subkey = data.Slice(1, data.Length - 2);
                            if (subkey.IsEmpty || subkey.IsWhiteSpace())
                            {
                                throw sourceLocation.ParseException("Missing subkey name");
                            }

                            currentSubKey = new RegistrySubKeyCommand();
                            currentSubKey.Commands = new List<RegistryValueCommand>();
                            if (subkey[0] == '-')
                            {
                                subkey = subkey.Slice(1);
                                currentSubKey.Remove = true;
                            }

                            (var hive, string subKeyName) = subkey.SplitIntoHiveAndSubKey(sourceLocation);

                            currentSubKey.SubKey = subKeyName;
                            currentSubKey.Hive = hive;

                            result.Add(currentSubKey);
                        }
                        else
                        {
                            throw sourceLocation.ParseException("Missing closing ']'");
                        }
                    }
                    else if (currentCommand != null)
                    {
                        data = data.Trim();
                        if (data[data.Length - 1] == '\\')
                        {
                            // Continuation continues
                            valueBuffer.Append(data.Slice(0, data.Length - 1));
                        }
                        else
                        {
                            // Continuation complete
                            valueBuffer.Append(data);
                            string raw = valueBuffer.ToString().Substring(currentKindId.Length + 1);
                            currentCommand.SetValue(currentValueInfo, currentValueInfo.GetNativeValue(raw, sourceLocation));
                            currentSubKey.Commands.Add(currentCommand);
                            valueBuffer.Clear();
                            currentCommand = null;
                            currentValueInfo = default;
                        }
                    }
                    else if (currentSubKey != null)
                    {
                        currentCommand = CreateCommand(sourceLocation, ref data);

                        int pos = data.IndexOf('=');
                        if (pos == -1)
                        {
                            throw sourceLocation.ParseException("Expected '='");
                        }
                        data = data.Slice(1).TrimStart();
                        if (data.IsEmpty || data.IsWhiteSpace())
                        {
                            throw sourceLocation.ParseException("Expected value");
                        }
                        if (data[0] == '-')
                        {
                            currentCommand.Remove = true;
                            currentSubKey.Commands.Add(currentCommand);
                            currentCommand = null;
                        }
                        else if (data[0] == '"')
                        {
                            pos = data.LastIndexOf('"');
                            if (pos == -1)
                            {
                                throw sourceLocation.ParseException("Missing closing '\"' in string value");
                            }
                            currentValueInfo = new RegValueInfo(RegistryValueKind.String, RegistryValueFormat.String);
                            currentCommand.SetValue(currentValueInfo, currentValueInfo.GetNativeValue(data.Slice(1, pos - 1), sourceLocation));
                            currentSubKey.Commands.Add(currentCommand);
                            currentCommand = null;
                            currentValueInfo = default;
                        }
                        else
                        {
                            pos = data.IndexOf(':');
                            if (pos == -1)
                            {
                                throw sourceLocation.ParseException("Missing ':' in value");
                            }

                            currentKindId = data.Slice(0, pos).ToString();
                            currentValueInfo = RegFileExtensions.GetValueInfo(currentKindId);
                            if (currentValueInfo.Kind == RegistryValueKind.Unknown)
                            {
                                throw sourceLocation.ParseException($"Cannot process value type {currentKindId}");
                            }

                            data = data.TrimEnd();
                            if (data[data.Length - 1] == '\\')
                            {
                                // Continuation
                                valueBuffer = new StringBuilder();
                                valueBuffer.Append(data.Slice(0, data.Length - 1));
                            }
                            else
                            {
                                currentCommand.SetValue(currentValueInfo, currentValueInfo.GetNativeValue(data.Slice(currentKindId.Length + 1), sourceLocation));
                                currentSubKey.Commands.Add(currentCommand);
                                currentCommand = null;
                                currentValueInfo = default;
                            }
                        }
                    }
                    else
                    {
                        throw sourceLocation.ParseException("Expected '\"'");
                    }
                }
            }
            catch (IOException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new IOException($"Unexpected exception parsing '{sourceLocation.Source}', line {sourceLocation.LineNumber}", ex);
            }

            return result;
        }

        private static RegistryValueCommand CreateCommand(SourceLocation sourceLocation, ref ReadOnlySpan<char> data)
        {
            RegistryValueCommand currentCommand;

            if (data[0] == '@')
            {
                currentCommand = new RegistryValueCommand();
                currentCommand.SetDefault();
                data = data.Slice(1);
            }
            else if (data[0] == '"')
            {
                currentCommand = new RegistryValueCommand();
                data = data.Slice(1);
                int endKey = data.IndexOf('"');
                if (endKey == -1)
                {
                    throw sourceLocation.ParseException("Missing closing '\"' in key");
                }
                currentCommand.SetName(data.Slice(0, endKey).ToString());
                data = data.Slice(endKey + 1);
            }
            else
            {
                throw sourceLocation.ParseException("Invalid key specification");
            }

            return currentCommand;
        }
    }
}