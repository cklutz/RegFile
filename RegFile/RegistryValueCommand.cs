using System;
using System.IO;
using Microsoft.Win32;

namespace RegFile
{
    public class RegistryValueCommand : IRegFileFormatter
    {
        public string Name { get; private set; }
        public object Value { get; private set; }
        public RegistryValueKind ValueKind => m_valueInfo.Kind;
        public RegistryValueFormat ValueFormat => m_valueInfo.Format;
        public bool Remove { get; set; }
        public bool IsDefault { get; private set; }

        private RegValueInfo m_valueInfo;

        internal void SetDefault()
        {
            IsDefault = true;
            // Setting name to empty string helps with Registry-API, where an empty name
            // is used to address the default value.
            Name = "";
        }

        internal void SetName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (name.Length == 0)
            {
                throw new ArgumentException("Name cannot be empty", nameof(name));
            }

            IsDefault = false;
            Name = name;
        }

        internal void SetValue(RegValueInfo valueInfo, object value)
        {
            Value = value;
            m_valueInfo = valueInfo;
        }

        public void WriteTo(TextWriter writer)
        {
            if (IsDefault)
            {
                writer.Write("@=");
            }
            else
            {
                writer.Write("\"");
                writer.Write(Name);
                writer.Write("\"=");
            }
            if (Remove)
            {
                writer.Write("-");
            }
            else
            {
                writer.Write(m_valueInfo.GetRegFileValue(Value));
            }
            writer.WriteLine();
        }
    }
}