using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RegFile.Tests
{
    [TestClass]
    public class RegFileReaderTests
    {
        const string TestData1 = @"
Windows Registry Editor Version 5.00

[HKEY_LOCAL_MACHINE\SOFTWARE\KeyName]
@=""the default value""
""@""=""Not the default value""
""dword_value""=dword:00000001
""dword_value_max""=dword:7FFFFFFF
""qword_value""=hex(b):01,00,00,00,00,00,00,00
""qword_value_max""=hex(b):FF,FF,FF,FF,FF,FF,FF,7F
""string_value""=""Hello World""
""string_value_escape""=""Hello\\t\\n\\r\\""World\\""""
""string_value_empty""=""""
""binary_value""=hex:01,00,00
""expand_string_value""=hex(2):25,00,53,00,79,00,73,00,74,00,65,00,6D,00,52,00,6F,00,6F,00,74,00,25,00,5C,00,73,00,79,00,73,\
  00,74,00,65,00,6D,00,33,00,32,00,00,00
""multi_string_value""=hex(7):76,00,61,00,6C,00,75,00,65,00,31,00,00,00,76,00,61,00,6C,00,75,00,65,00,32,00,00,00,76,00,61,\
  00,6C,00,75,00,65,00,33,00,00,00,00,00
""multi_string_value_empty""=hex(7):00,00
""multi_string_value_empty_item""=hex(7):00,00,00,00
""multi_string_value_one_empty_two""=hex(7):76,00,61,00,6C,00,75,00,65,00,31,00,00,00,00,00,76,00,61,00,6C,00,75,00,65,00,31,00,00,00,00,\
  00
""multi_string_value_one""=hex(7):76,00,61,00,6C,00,75,00,65,00,31,00,00,00,00,00

";

        [TestMethod]
        public void RoundTrip()
        {
            string input = TestData1.TrimStart();

            IEnumerable<RegistrySubKeyCommand> commands;
            using (var reader = new RegFileReader(new StringReader(input), true))
            {
                commands = reader.Read();
            }

            using (var sw = new StringWriter())
            {
                using (var writer = new RegFileWriter(sw, false))
                {
                    writer.Write(commands);
                }

                Assert.AreEqual(input, sw.ToString());
            }
        }
    }
}