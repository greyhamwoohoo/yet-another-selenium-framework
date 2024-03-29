﻿using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using System.Collections.Generic;
using System.IO;

namespace Yasf.Common.Drivers
{
    /// <summary>
    /// Allows us to attach to a running Chrome Driver instance / Selenium Session
    /// </summary>
    public class AttachableChromeDriver : ChromeDriver
    {
        public AttachableChromeDriver(ChromeOptions options) : base(options)
        {
        }

        protected override Response Execute(string driverCommandToExecute, Dictionary<string, object> parameters)
        {
           var decoratedRemoteWebDriver = new DriverDecorator(this, "CHROME", Directory.GetCurrentDirectory());
           decoratedRemoteWebDriver.AssertSeleniumVersionIsCompatible();

           return decoratedRemoteWebDriver.Execute(driverCommandToExecute, parameters, Executor);
        }

        private Response Executor(string driverCommandToExecute, Dictionary<string, object> parameters)
        {
            // We need access to base.Execute as there is no clear way to 'hook in' to the web driver architecture the way I need to
            return base.Execute(driverCommandToExecute, parameters);
        }
    }
}
