using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace RegFile
{
    public abstract class AbstractRegCommandProcessor
    {
        public virtual void Process(IEnumerable<RegistrySubKeyCommand> commands)
        {
            if (commands == null)
            {
                throw new ArgumentNullException(nameof(commands));
            }

            foreach (var command in commands)
            {
                Process(command);
            }
        }

        public virtual void Process(RegistrySubKeyCommand command)
        {
            if (command.Remove)
            {
                ProcessSubKeyRemove(command.Hive, command.SubKey);
            }
            else
            {
                foreach (var value in command.Commands)
                {
                    if (value.Remove)
                    {
                        ProcessValueRemove(command.Hive, command.SubKey, value.Name);
                    }
                    else
                    {
                        ProcessValueUpdate(command.Hive, command.SubKey, value.Name, value.Value, value.ValueKind);
                    }

                }
            }
        }

        public abstract void ProcessSubKeyRemove(RegistryHive hive, string subKey);
        public abstract void ProcessValueRemove(RegistryHive hive, string subKey, string valueName);
        public abstract void ProcessValueUpdate(RegistryHive hive, string subKey, string valueName, object value, RegistryValueKind valueKind);
    }
}
