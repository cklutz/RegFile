
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using RegFile;

namespace RegImport
{
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    return Usage();
                }

                bool dump = false;
                var view = RegistryView.Registry64;
                string fileName = args[0];
                if (args.Length > 1)
                {
                    if ("/reg:32".Equals(args[1], StringComparison.OrdinalIgnoreCase))
                    {
                        view = RegistryView.Registry32;
                    }
                    else if ("/reg:64".Equals(args[1], StringComparison.OrdinalIgnoreCase))
                    {
                        view = RegistryView.Registry64;
                    }
                    else if ("/dump".Equals(args[1], StringComparison.OrdinalIgnoreCase))
                    {
                        dump = true;
                    }
                    else
                    {
                        return Usage();
                    }
                }

                IEnumerable<RegistrySubKeyCommand> commands;
                using (var reader = new RegFileReader(fileName))
                {
                    commands = reader.Read();
                }

                if (commands.Any())
                {
                    if (dump)
                    {
                        using (var writer = new RegFileWriter(Console.Out, false))
                        {
                            writer.Write(commands);
                        }
                    }
                    else
                    {
                        var processor = new RegCommandProcessor(view);
                        processor.Process(commands);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return ex.HResult;
            }

            return 0;
        }

        private static int Usage()
        {
            Console.Error.WriteLine($"Usage: {typeof(Program).Assembly.GetName().Name} FILENAME [/reg:32 | /reg:64 | /dump]");
            return 1;
        }
    }
}