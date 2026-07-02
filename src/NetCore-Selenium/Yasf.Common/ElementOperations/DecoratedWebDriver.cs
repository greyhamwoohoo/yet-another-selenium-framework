using Serilog;
using OpenQA.Selenium;
using OpenQA.Selenium.Internal;
using OpenQA.Selenium.Support.Extensions;
using System.Collections.ObjectModel;
using Yasf.Common.ElementOperations.Contracts;
using Yasf.Common.ExecutionContext.Runtime.ControlSettings;
using Yasf.Common.Reporting.Contracts;
using System.Threading.Tasks;
using System;

namespace Yasf.Common.ElementOperations
{
    /// <summary>
    /// Wraps the IWebDriver interface and enriches it. 
    /// </summary>
    public class DecoratedWebDriver : IDecoratedWebDriver, IWrapsDriver, ITakesScreenshot
    {
        private readonly IWebDriver _original;
        private readonly IControlSettings _controlSettings;
        private readonly ITestCaseReporter _testCaseReporter;
        private readonly ILogger _logger;

        public DecoratedWebDriver(IWebDriver original, IControlSettings controlSettings, ITestCaseReporter testCaseReporter, ILogger logger)
        {
            _original = original ?? throw new System.ArgumentNullException(nameof(original));
            _controlSettings = controlSettings ?? throw new System.ArgumentNullException(nameof(controlSettings));
            _testCaseReporter = testCaseReporter ?? throw new System.ArgumentNullException(nameof(testCaseReporter));
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));

            Assert = new WebDriverAssertions(this, _controlSettings, _testCaseReporter, _logger);
        }

        public IWebDriverAssertions Assert { get; }

        #region Wrap _original WebDriver properties and methods

        public string Url { get => _original.Url; set => _original.Url = value; }

        public string Title => _original.Title;

        public string PageSource => _original.PageSource;

        public string CurrentWindowHandle => _original.CurrentWindowHandle;

        public ReadOnlyCollection<string> WindowHandles => _original.WindowHandles;

        public IWebDriver WrappedDriver => _original;

        public void Close()
        {
            _original.Close();
        }

        public void Dispose()
        {
            _original.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            // TODO: Revisit this
            this.Dispose();

            return ValueTask.CompletedTask;
        }

        public IWebElement FindElement(By by)
        {
            return _original.FindElement(by);
        }

        public ReadOnlyCollection<IWebElement> FindElements(By by)
        {
            return _original.FindElements(by);
        }

        public Screenshot GetScreenshot()
        {
            return _original.TakeScreenshot();
        }

        public IOptions Manage()
        {
            return _original.Manage();
        }

        public INavigation Navigate()
        {
            return _original.Navigate();
        }

        public void Quit()
        {
            _original.Quit();
        }

        public ITargetLocator SwitchTo()
        {
            return _original.SwitchTo();
        }

        #endregion
    }
}
