using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
#pragma warning disable CA1416 // Validate platform compatibility

namespace RegFile
{
    public class RegistrySubKeyCommand : IRegFileFormatter
    {
        public RegistryHive Hive { get; set;}
        public string SubKey { get; set; }
        public bool Remove { get; set; }
        public List<RegistryValueCommand> Commands { get; set; }

        public void WriteTo(TextWriter writer)
        {
            writer.Write("[");
            if (Remove)
            {
                writer.Write("-");
            }
            writer.Write(RegFileExtensions.HiveToString(Hive));
            writer.Write("\\");
            writer.Write(SubKey);
            writer.Write("]");
            writer.WriteLine();
            foreach (var command in Commands)
            {
                command.WriteTo(writer);
            }
        }
    }
}