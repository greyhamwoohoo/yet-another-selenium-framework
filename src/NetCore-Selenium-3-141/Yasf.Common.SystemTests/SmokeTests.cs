﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using Yasf.Common;

namespace Yasf.Common.SystemTests
{
    [TestClass]
    public class SmokeTests : SeleniumTestBase
    {
        protected override string BaseUrl => "https://www.google.com";

        [TestMethod]
        public void Google_Can_Be_Searched()
        {
            WebDriver.Assert.Type(By.Name("q"), "greyhamwoohoo", andPressEnter: true);
        }
    }
}
