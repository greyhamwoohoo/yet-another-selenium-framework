using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Yasf.Common.ElementOperations.Contracts;
using Yasf.Common.ExecutionContext.Runtime.ControlSettings;
using Yasf.Common.Reporting.Contracts;

namespace Yasf.Common.ElementOperations
{
    public class WebDriverAssertions : IWebDriverAssertions
    {
        private readonly IDecoratedWebDriver _webDriver;
        private readonly IControlSettings _controlSettings;
        private readonly ITestCaseReporter _testCaseReporter;
        private readonly ILogger _logger;

        public WebDriverAssertions(IDecoratedWebDriver webDriver, IControlSettings controlSettings, ITestCaseReporter testCaseReporter, ILogger logger)
        {
            _testCaseReporter = testCaseReporter ?? throw new ArgumentNullException(nameof(testCaseReporter));
            _webDriver = webDriver ?? throw new ArgumentNullException(nameof(webDriver)); ;
            _controlSettings = controlSettings ?? throw new ArgumentNullException(nameof(controlSettings)); ;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IWebElement Click(By locator)
        {
            return Click(locator, because: $"{typeof(IWebDriverAssertions).Name}.Click({locator})");
        }

        public IWebElement Click(By locator, string because)
        {
            IWebElement element = default;

            AssertThatEventually(driver =>
            {
                element = AssertExactlyOneElementExists(_webDriver, locator);

                element.Click();
            }, because);

            return element;
        }

        public IWebElement Type(By locator, string keys, bool andPressEnter)
        {
            return Type(locator, keys, andPressEnter, because: $"{typeof(IWebDriverAssertions).Name}.Type({locator}, keys: '{keys}', andPressEnter: {andPressEnter})");
        }

        public IWebElement Type(By locator, string keys, bool andPressEnter, string because)
        {
            IWebElement element = default;

            AssertThatEventually(driver =>
            {
                element = AssertExactlyOneElementExists(_webDriver, locator);

                element.SendKeys(keys);
                if (andPressEnter)
                {
                    element.SendKeys(Keys.Enter);
                }
            }, because);

            return element;
        }

        /// <summary>
        /// Wait until the element matches the given state. 
        /// </summary>
        /// <param name="element">Element to match</param>
        /// <param name="state">The state to match</param>
        /// <returns></returns>
        public IWebElement State(By element, ElementState state)
        {
            return State(new ElementStateCondition() { Locator = element, State = state });
        }

        /// <summary>
        /// Wait until all of the conditions match; or throw an exception if one or more of the conditions cannot be matched. 
        /// </summary>
        /// <param name="element">Search criteria</param>
        /// <param name="state">The state we are searching for</param>
        /// <param name="conditions">Conditions to match</param>
        public void State(By element, ElementState state, params ElementStateCondition[] conditions)
        {
            var temp = new List<ElementStateCondition>();
            temp.Add(new ElementStateCondition() { Locator = element, State = state });
            temp.AddRange(conditions);

            State(temp);
        }

        /// <summary>
        /// Handler for the specific and most common use case - one match. 
        /// </summary>
        /// <param name="condition">The condition to match</param>
        /// <returns>The WebElement matching the condition; or any number of Selenium and generic exceptions. </returns>
        public IWebElement State(ElementStateCondition condition)
        {
            State(new ElementStateCondition[] { condition });
            return Match(condition);
        }

        /// <summary>
        /// Wait until all of the elements match or the timeout is exceeded. 
        /// </summary>
        /// <param name="conditions">The conditions to match. </param>
        public void State(IEnumerable<ElementStateCondition> conditions)
        {
            var foundElements = new List<ElementStateCondition>();
            var exceptions = new List<Exception>();

            DateTime endMatch = DateTime.Now.AddSeconds(_controlSettings.WaitUntilTimeoutInSeconds);

            _logger.Information($"WaitUntil:Conditions:Dump");
            conditions.ToList().ForEach(condition => _logger.Information($"{condition}"));

            do
            {
                foundElements.Clear();
                exceptions.Clear();
                foreach (var condition in conditions)
                {
                    try
                    {
                        _logger.Information($"Match:Condition:Begin:{condition}");
                        var match = Match(condition);
                        _logger.Information($"Match:Condition:Success:{condition}");
                        foundElements.Add(condition);
                    }
                    catch (Exception ex)
                    {
                        _logger.Information($"Match:Condition:Fail:{condition}:{ex.GetType().FullName}");
                        exceptions.Add(ex);
                    }
                }

                if (foundElements.Count == conditions.Count()) return;

                // NOTE: We have AT LEAST one exception. 
                System.Threading.Thread.Sleep(_controlSettings.PollingTimeInMilliseconds);

            } while (DateTime.Now < endMatch);

            // ASSERTION: At least one of the elements did not match. Dump out all the information we have about the matching. 
            var builder = new StringBuilder();

            var notFoundElements = conditions.Except(foundElements);
            if (notFoundElements.Count() > 0)
            {
                builder.AppendFormat("The following elements were not in the expected state: \r\n");
                notFoundElements.ToList().ForEach(notFoundElement =>
                {
                    builder.AppendFormat($"{notFoundElement.Locator.ToString()}\r\n");
                });
                builder.AppendFormat("\r\n");
            }

            if (foundElements.Count > 0)
            {
                builder.AppendFormat("The following elements were in the expected state: \r\n");
                foundElements.ForEach(foundElement =>
                {
                    builder.AppendFormat($"{foundElement.Locator.ToString()}\r\n");
                });
                builder.AppendFormat("\r\n");
            }

            if (exceptions.Count > 0)
            {
                builder.AppendFormat("Exception Information for elements not in the expected state: \r\n");
                exceptions.ForEach(exception =>
                {
                    builder.AppendFormat($"{exception.ToString()}\r\n");
                });
            }

            _logger.Error(builder.ToString());
            throw new NoSuchElementException(builder.ToString());
        }

        /// <summary>
        /// Wait until the expected callback DOES NOT throw an exception. 
        /// The callback usually contains one or more assertions. 
        /// </summary>
        /// <param name="callback"></param>
        public void Eventually(Action<IWebDriver> callback)
        {
            Eventually(callback, because: "(no reason was provided)");
        }

        /// <summary>
        /// Wait until the expected callback DOES NOT throw an exception. 
        /// The callback usually contains one or more assertions. 
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="because">Reason for waiting. </param>
        public void Eventually(Action<IWebDriver> callback, string because)
        {
            AssertThatEventually(callback, because);
        }

        /// <summary>
        /// Wait until the expected condition is true. 
        /// </summary>
        /// <param name="callback">Callback. </param>
        public void Eventually(Func<IWebDriver, bool> callback)
        {
            WebDriverWait wait = new WebDriverWait(_webDriver, TimeSpan.FromSeconds(_controlSettings.WaitUntilTimeoutInSeconds));
            wait.Until(browser => callback(browser));
        }

        /// <summary>
        /// Immediately matches the current condition by trying to locate the element. 
        /// </summary>
        /// <param name="condition">Condition to match</param>
        /// <returns>The element if it exists; will throw an exception if the element cannot be matched. </returns>
        protected IWebElement Match(ElementStateCondition condition)
        {
            // TODO: Use custom exceptions here
            var webElement = default(IWebElement);

            if (condition.State.HasFlag(ElementState.Exists))
            {
                var webElements = _webDriver.FindElements(condition.Locator);
                if (webElements.Count() == 0) throw new NoSuchElementException($"{condition.Locator}");
                if (webElements.Count() != 1) throw new InvalidOperationException($"ERROR: There is more than one instance of {condition.State.ToString()}. There must be exactly one instance on the page. ");

                webElement = webElements[0];
            }

            if (condition.State.HasFlag(ElementState.IsDisplayed))
            {
                // NOTE: ElementNotVisibleException is not defined in Selenium 4+. ElementNotInteractableException is now the supported method: https://github.com/SeleniumHQ/selenium/issues/10538
                if (!webElement.Displayed) throw new NoSuchElementException($"The Element is not visible", new ElementNotInteractableException($"The Element is not visible: {webElement}"));
            }

            if (condition.State.HasFlag(ElementState.IsEnabled))
            {
                if (!webElement.Enabled) throw new NoSuchElementException($"The Element is not enabled", new ElementNotInteractableException($"The Element is not enabled: {webElement}"));
            }

            return webElement;
        }

        private IWebElement AssertExactlyOneElementExists(IDecoratedWebDriver webDriver, By locator)
        {
            var elements = webDriver.FindElements(locator);
            if (elements.Count() == 0) throw new NotFoundException($"The element {locator} could not be found. ");
            if (elements.Count() > 1) throw new NotFoundException($"The element {locator} was found {elements.Count()} times instead of exactly once. ");

            return elements.Single();
        }

        /// <summary>
        /// Wait until the expected callback DOES NOT throw an exception. 
        /// The callback usually contains one or more assertions. 
        /// </summary>
        /// <param name="callback"></param>
        private void AssertThatEventually(Action<IDecoratedWebDriver> callback, string because)
        {
            if (null == callback) throw new ArgumentNullException(nameof(callback));

            DateTime endMatch = DateTime.Now.AddSeconds(_controlSettings.WaitUntilTimeoutInSeconds);
            Exception lastException = default;

            do
            {
                try
                {
                    callback(_webDriver);
                    _testCaseReporter.Pass(because);
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }

                System.Threading.Thread.Sleep(_controlSettings.PollingTimeInMilliseconds);

            } while (DateTime.Now < endMatch);

            _testCaseReporter.Fail(because, lastException);

            throw lastException;
        }
    }
}
