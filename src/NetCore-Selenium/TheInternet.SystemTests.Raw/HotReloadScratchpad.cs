using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Yasf.Common.ElementOperations.ElementState;
using static Yasf.Common.ElementOperations.ElementStateCondition;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Service;
using OpenQA.Selenium.Appium.Service.Options;
using OpenQA.Selenium.Appium.Enums;
using OpenQA.Selenium.Appium.Android;
using System;
using OpenQA.Selenium.Chrome;
using Yasf.Common.ExecutionContext.Runtime.DeviceSettings.Contracts;
using Yasf.Common;

namespace TheInternet.SystemTests.Raw
{
    /// <summary>
    /// Use this single test as a scratch pad for reusing a browser instance. 
    /// </summary>
    /// <remarks>
    /// If using Visual Studio, be sure to select Test / Configure Run Settings / Configure Solution Wide runsettings file and select 'TestExecutionContexts\Attachable-Chrome-Localhost.runsettings'
    /// Run the test once; it will open the browser
    /// Add a few lines of Selenium at a time; re-run the test and it will reuse the same browser instance
    /// ---
    /// To automatically run the test every time this file is saved, do this from a command line from this folder:
    /// 
    /// Cmd Prompt:
    /// SET YASF_TEST_EXECUTION_CONTEXT=attachable-chrome-localhost
    /// dotnet watch test --filter "Name=HotReloadWorkflow"
    ///
    /// PowerShell:
    /// $env:YASF_TEST_EXECUTION_CONTEXT="attachable-chrome-localhost"
    /// dotnet watch test --filter "Name=HotReloadWorkflow"
    /// </remarks>
    [TestClass]
    public class HotReloadScratchpad : SeleniumTestBase
    {
        [TestInitialize]
        public void BrowsersOnly()
        {
            var deviceProperties = Resolve<IDeviceProperties>();
            if(deviceProperties.Name != "DESKTOP")
            {
                // Assert.Fail($"Hot reload only works on Desktop Browser; not Appium. ");
            }
        }

        protected override void NavigateToBaseUrl()
        {
            // We do nothing here - we want to control everything 
        }

        [TestMethod]
        [TestCategory("HotReloadWorkflow")]
        public void HotReloadWorkflow()
        {
            // WebDriver.Navigate().GoToUrl("http://www.google.com");
            // WebDriver.FindElement(By.Name("q")).SendKeys("howdi!" + Keys.Enter);
        }
    }
}
