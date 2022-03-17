//#define RUN_LOCAL_STATE_TESTS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;

namespace RegFile.Tests
{
    [TestClass]
    public class RegCommandExtractorTests
    {
        [TestMethod]
        public void Test()
        {
#if RUN_LOCAL_STATE_TESTS
            var extractor = new RegCommandExtractor(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\.NETFramework", RegistryView.Registry64);
            var commands = extractor.Extract();

            using (var writer = new RegFileWriter(Console.Out, false))
            {
                writer.Write(commands);
            }
#endif
        }
    }
}
