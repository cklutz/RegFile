using System;
using Microsoft.Win32;

namespace RegFile
{
    public class RegCommandProcessor : AbstractRegCommandProcessor
    {
        private readonly RegistryView m_view;

        public RegCommandProcessor(RegistryView view)
        {
            m_view = view;
        }

        public override void ProcessSubKeyRemove(RegistryHive hive, string subKey)
        {
            try
            {
                using (var baseKey = RegistryKey.OpenBaseKey(hive, m_view))
                {
                    baseKey.DeleteSubKeyTree(subKey, throwOnMissingSubKey: false);
                }
            }
            catch (Exception ex) when (ex is not ArgumentNullException)
            {
                throw new RegistryProcessingException($"Failed to remove subkey '{hive.HiveToString()}\\{subKey}'", ex);
            }
        }

        public override void ProcessValueRemove(RegistryHive hive, string subKey, string valueName)
        {
            try
            {
                using (var baseKey = RegistryKey.OpenBaseKey(hive, m_view))
                {
                    var key = baseKey.OpenSubKey(subKey);
                    if (key != null)
                    {
                        key.DeleteValue(valueName, throwOnMissingValue: false);
                    }
                }
            }
            catch (Exception ex) when (ex is not ArgumentNullException)
            {
                throw new RegistryProcessingException($"Failed to remove value '{valueName}' from '{hive.HiveToString()}\\{subKey}'", ex);
            }
        }

        public override void ProcessValueUpdate(RegistryHive hive, string subKey, string valueName, object value, RegistryValueKind valueKind)
        {
            try
            {
                using (var baseKey = RegistryKey.OpenBaseKey(hive, m_view))
                {
                    var key = baseKey.CreateSubKey(subKey, writable: true);
                    key.SetValue(valueName, value, valueKind);
                }
            }
            catch (Exception ex) when (ex is not ArgumentNullException)
            {
                throw new RegistryProcessingException($"Failed to update the value '{valueName}' of kind {valueKind} in '{hive.HiveToString()}\\{subKey}'", ex);
            }
        }
    }
}
