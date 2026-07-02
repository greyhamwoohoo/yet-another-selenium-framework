using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using System.Linq;
using static Yasf.Common.ElementOperations.ElementState;

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
            var path = System.IO.Path.Combine(base.TestContext.DeploymentDirectory, "Content", "SampleFileToUpload.txt");
            var fileuploadElement = WebDriver.FindElements(By.XPath("//input[@id='file-upload']")).Single();
            fileuploadElement.SendKeys(path);

            var uploadButton = WebDriver.FindElements(By.XPath("//input[@id='file-submit']")).Single();
            uploadButton.Click();

            WebDriver.Assert.State(By.XPath("//div[@id='uploaded-files']"), Exists);

            WebDriver.Assert.Eventually(browser =>
            {
                browser.PageSource.Should().Contain("SampleFileToUpload.txt", because: "that was the filename uploaded");
            });
        }
    }
}
