using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace RegFile
{
    public class RegCommandExtractor
    {
        private readonly RegistryHive m_hive;
        private readonly string m_subKey;
        private readonly RegistryView m_view;

        public RegCommandExtractor(string key, RegistryView view)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            (m_hive, m_subKey) = RegFileExtensions.SplitIntoHiveAndSubKey(key.AsSpan(), default);
            m_view = view;
        }

        public RegCommandExtractor(RegistryHive hive, string subKey, RegistryView view)
        {
            m_hive = hive;
            m_subKey = subKey ?? throw new ArgumentNullException(nameof(subKey));
            m_view = view;
        }

        public IEnumerable<RegistrySubKeyCommand> Extract()
        {
            var result = new List<RegistrySubKeyCommand>();

            using (var baseKey = RegistryKey.OpenBaseKey(m_hive, m_view))
            {
                var sk = baseKey.OpenSubKey(m_subKey, writable: false);
                AddSubKey(sk, result);
            }

            return result;
        }

        private void AddSubKey(RegistryKey sk, List<RegistrySubKeyCommand> subKeyCommands)
        {
            if (sk != null)
            {
                var subKeyCommand = CreateCommand(sk);
                subKeyCommands.Add(subKeyCommand);

                foreach (var valueName in sk.GetValueNames())
                {
                    var value = sk.GetValue(valueName);
                    var kind = sk.GetValueKind(valueName);
                    var valueCommand = CreateCommand(valueName, value, kind);
                    subKeyCommand.Commands.Add(valueCommand);
                }

                foreach (var childSubKeyName in sk.GetSubKeyNames())
                {
                    using (var csk = sk.OpenSubKey(childSubKeyName, writable: false))
                    {
                        AddSubKey(csk, subKeyCommands);
                    }
                }
            }
        }

        private static RegistrySubKeyCommand CreateCommand(RegistryKey key)
        {
            var result = new RegistrySubKeyCommand();
            (result.Hive, result.SubKey) = key.Name.AsSpan().SplitIntoHiveAndSubKey(default);
            result.Commands = new List<RegistryValueCommand>();
            return result;
        }

        private static RegistryValueCommand CreateCommand(string name, object value, RegistryValueKind valueKind)
        {
            var result = new RegistryValueCommand();
            if (name == "")
            {
                result.SetDefault();
            }
            else
            {
                result.SetName(name);
            }

            var valueInfo = new RegValueInfo(valueKind, valueKind.DeduceValueFormat(value));
            result.SetValue(valueInfo, value);

            return result;
        }
    }


}
