﻿using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using System.Linq;
using static TheInternet.Common.ElementOperations.ElementState;

namespace TheInternet.SystemTests.Raw.Tests
{
    [TestClass]
    public class FileUploadTests : TheInternetTestBase
    {
        protected override string BaseUrl => base.BaseUrl + "/upload";

        [TestMethod]
        [Ignore("because this has issues in some execution contexts")]
        public void UploadFile()
        {
            var path = System.IO.Path.Combine(TestContext.TestDeploymentDir, "Content", "SampleFileToUpload.txt");
            var fileuploadElement = WebDriver.FindElements(By.XPath("//input[@id='file-upload']")).Single();
            fileuploadElement.SendKeys(path);

            var uploadButton = WebDriver.FindElements(By.XPath("//input[@id='file-submit']")).Single();
            uploadButton.Click();

            DriverSession.Waiter.AssertThatEventually(By.XPath("//div[@id='uploaded-files']"), Exists);

            DriverSession.Waiter.AssertThatEventually(browser =>
            {
                browser.PageSource.Should().Contain("SampleFileToUpload.txt", because: "that was the filename uploaded");
            });
        }
    }
}