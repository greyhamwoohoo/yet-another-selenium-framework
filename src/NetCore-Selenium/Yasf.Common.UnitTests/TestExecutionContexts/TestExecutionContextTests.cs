﻿using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Xml;
using Yasf.Common.DataSource;

namespace Yasf.Common.UnitTests.TestExecutionContexts
{
    [TestClass]
    public class TestExecutionContextTests
    {
        [TestClass]
        public class WhenTecsAreDeployed : TestBase
        {
            [TestMethod]
            public void ThereAreManyFiles()
            {
                DiscoverFilesAtPath("TestExecutionContexts", "*.json").Count().Should().BeGreaterThan(5, because: "we will be parsing all Json files. ");
            }

            [TestMethod]
            [RuntimeFilePathDataSource(relativePath: "TestExecutionContexts", pattern: "*.json")]
            public void AllTecsCanBeParsed(string runtimeFilePath)
            {
                var content = File.ReadAllText(runtimeFilePath);

                _ = JsonConvert.DeserializeObject(content);
            }

            [TestMethod]
            [RuntimeFilePathDataSource(relativePath: "TestExecutionContexts", pattern: "*.runsettings")]
            public void AllRunSettingsCanBeParsed(string runtimeFilePath)
            {
                var content = File.ReadAllText(runtimeFilePath);

                XmlDocument doc = new XmlDocument();

                // This will throw an exception if it is invalid. 
                doc.Load(new StringReader(content));
            }
        }
    }
}
