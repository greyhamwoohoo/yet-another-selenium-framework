using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Yasf.Common.SessionManagement;

namespace Yasf.Common.Drivers
{
    /// <summary>
    /// Decorates the underlying driver with functionality to attach to an existing session
    /// </summary>
    public class DriverDecorator
    {
        public const string IMPLEMENTATION_NOTE = "This implementation is for Selenium 4.0.0.0 and is using protected / private methods to enable session reuse. The implementation might have changed for the Selenium you are using; we will need a new specific adapter for the (Driver,SeleniumVersion).";

        private readonly WebDriver _webDriver;
        private readonly string _browserName;
        private readonly string _targetFolder;

        public DriverDecorator(WebDriver webDriver, string browserName, string targetFolder)
        {
            if (null == webDriver) throw new ArgumentNullException(nameof(webDriver));
            if (null == browserName) throw new ArgumentNullException(nameof(browserName));
            if (null == targetFolder) throw new ArgumentNullException(nameof(targetFolder));

            _webDriver = webDriver;
            _browserName = browserName;
            _targetFolder = targetFolder;
        }

        public void AssertSeleniumVersionIsCompatible()
        {
            // From experience: Selenium preserves its major copmatibility very well. So I am only matching on the Major version here. 
            var seleniumVersion = typeof(RemoteWebDriver).Assembly.GetName().Version;
            var supportedVersion = Version.Parse("4.0.0.0");
            if (seleniumVersion.MajorRevision != supportedVersion.MajorRevision)
            {
                throw new NotSupportedException($"{IMPLEMENTATION_NOTE}");
            }
        }

        public string GetRemoteServerUri()
        {
            // return this.CommandExecutor.HttpExecutor.remoteServerUri.ToString();
            var commandExecutor = GetCommandExecutor(_webDriver);

            var httpExecutorProperty = commandExecutor.GetType().GetProperty("HttpExecutor");
            if (null == httpExecutorProperty) throw new InvalidOperationException($"remoteWebDriver.CommandExecutor.HttpExecutor property should exist. {IMPLEMENTATION_NOTE}");

            var executor = httpExecutorProperty.GetValue(commandExecutor);
            if (null == executor) throw new InvalidOperationException($"remoteWebDriver.CommandExecutor.HttpExecutor property should not be null. {IMPLEMENTATION_NOTE}");

            var remoteServerUriField = executor.GetType().GetField("remoteServerUri", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (null == remoteServerUriField) throw new InvalidOperationException($"remoteWebDriver.CommandExecutor.HttpExecutor.remoteServerUriField property should not be null. {IMPLEMENTATION_NOTE}");

            var remoteServerUri = remoteServerUriField.GetValue(executor);
            if (null == remoteServerUri) throw new InvalidOperationException($"remoteWebDriver.CommandExecutor.HttpExecutor.remoteServerUri should not be null. {IMPLEMENTATION_NOTE}");

            return remoteServerUri.ToString();
        }

        public void SetRemoteServerUri(string value)
        {
            // this.CommandExecutor.HttpExecutor.remoteServerUri = new Uri(value)
            var commandExecutor = GetCommandExecutor(_webDriver);

            var httpExecutorProperty = commandExecutor.GetType().GetProperty("HttpExecutor");
            if (null == httpExecutorProperty) throw new InvalidOperationException($"remoteWebDriver.CommandExecutor.HttpExecutor property should exist. {IMPLEMENTATION_NOTE}");

            var executor = httpExecutorProperty.GetValue(commandExecutor);
            if (null == executor) throw new InvalidOperationException($"remoteWebDriver.CommandExecutor.HttpExecutor property should not be null. {IMPLEMENTATION_NOTE}");

            var remoteServerUriField = executor.GetType().GetField("remoteServerUri", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (null == remoteServerUriField) throw new InvalidOperationException($"remoteWebDriver.CommandExecutor.HttpExecutor.remoteServerUriField property should not be null. {IMPLEMENTATION_NOTE}");

            remoteServerUriField.SetValue(executor, new Uri(value));
        }

        public CommandInfoRepository GetCommandInfoRepository()
        {
            // return this.CommandExecutor.HttpExecutor.commandInfoRepository
            var commandExecutor = GetCommandExecutor(_webDriver);

            var httpExecutorProperty = commandExecutor.GetType().GetProperty("HttpExecutor");
            if (null == httpExecutorProperty) throw new InvalidOperationException($"remoteWebDriver.CommandExecutor.HttpExecutor property should exist. {IMPLEMENTATION_NOTE}");

            var executor = httpExecutorProperty.GetValue(commandExecutor);
            if (null == executor) throw new InvalidOperationException($"remoteWebDriver.CommandExecutor.HttpExecutor property should not be null. {IMPLEMENTATION_NOTE}");

            var commandInfoRepositoryField = executor.GetType().GetField("commandInfoRepository", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (null == commandInfoRepositoryField) throw new InvalidOperationException($"remoteWebDriver.CommandExecutor.HttpExecutor.commandInfoRepository property should exist. {IMPLEMENTATION_NOTE}");

            var commandInfoRepository = commandInfoRepositoryField.GetValue(executor);
            if (null == commandInfoRepository) throw new InvalidOperationException($"remoteWebDriver.CommandExecutor.HttpExecutor.commandInfoRepository property should not be null. {IMPLEMENTATION_NOTE}");

            var result = commandInfoRepository as CommandInfoRepository;
            if (null == result) throw new InvalidOperationException($"remoteWebDriver.CommandExecutor.HttpExecutor.commandInfoRepository property should be of type CommandInfoRepository. {IMPLEMENTATION_NOTE}");

            return result;
        }

        public void SetCommandInfoRepository(CommandInfoRepository repository)
        {
            // this.CommandExecutor.HttpExecutor.commandInfoRepository = repository
            var commandExecutor = GetCommandExecutor(_webDriver);

            var httpExecutorProperty = commandExecutor.GetType().GetProperty("HttpExecutor");
            if (null == httpExecutorProperty) throw new InvalidOperationException($"remoteWebDriver.CommandExecutor.HttpExecutor property should exist. {IMPLEMENTATION_NOTE}");

            var executor = httpExecutorProperty.GetValue(commandExecutor);
            if (null == executor) throw new InvalidOperationException($"remoteWebDriver.CommandExecutor.HttpExecutor property should not be null. {IMPLEMENTATION_NOTE}");

            var commandInfoRepositoryField = executor.GetType().GetField("commandInfoRepository", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (null == commandInfoRepositoryField) throw new InvalidOperationException($"remoteWebDriver.CommandExecutor.HttpExecutor.commandInfoRepository property should exist. {IMPLEMENTATION_NOTE}");

            commandInfoRepositoryField.SetValue(executor, repository);
        }

        public Response Execute(string driverCommandToExecute, Dictionary<string, object> parameters, Func<string, Dictionary<string, object>, Response> executor)
        {
            if (null == executor) throw new ArgumentNullException(nameof(executor));

            if (driverCommandToExecute == "newSession")
            {
                var attachableSeleniumSessionStorage = new AttachableSeleniumSessionStorage(_targetFolder);
                var existingSession = attachableSeleniumSessionStorage.ReadSessionState(_browserName);
                var sessionProbe = new AttachableSeleniumSessionProbe();

                if (!existingSession.IsValid || existingSession.BrowserName != _browserName || !sessionProbe.IsRunning(existingSession))
                {
                    attachableSeleniumSessionStorage.RemoveSessionState();
                    existingSession = attachableSeleniumSessionStorage.ReadSessionState(_browserName);
                }

                if (!existingSession.IsValid)
                {
                    // There is currently no persisted session we can use. 
                    var newSession = executor(driverCommandToExecute, parameters);
                    if (newSession.Status != WebDriverResult.Success) return newSession;

                    // NOTE: The explicit serialization call here is to coerce Value, SessionId and Status to value, sessionId and status so that AttachableSeleniumSessionStorage
                    //       can still blindly call Response.FromJson to rebuild a valid attachable browser context. 
                    var attachableSeleniumSession = new AttachableSeleniumSession()
                    {
                        BrowserName = _browserName,
                        Response = newSession,
                        OfficialResponse = JsonSerializer.Serialize(newSession, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                        RemoteServerUri = GetRemoteServerUri(),
                        CommandRepositoryTypeName = GetCommandInfoRepository().GetType().FullName
                    };

                    attachableSeleniumSessionStorage.WriteSessionState(attachableSeleniumSession);

                    return newSession;
                }

                // The HttpCommandExecutor has some specific logic if handling the NewSession command - it will determine
                // the specification and update the internal CommandInfoRepository. This is what gives the 'specification level'
                // of the connection. Therefore, as we skipped that call and constructed the session ourselves, we need to inject
                // the CommandInfoRepository here. 
                if (existingSession.CommandRepositoryTypeName == typeof(W3CWireProtocolCommandInfoRepository).FullName)
                {
                    SetCommandInfoRepository(new W3CWireProtocolCommandInfoRepository());
                }
                else
                {
                    throw new InvalidOperationException($"At the time of writing this there were two implementations of CommandInfoRepository. Add a switch statement and new up a type of {existingSession.CommandRepositoryTypeName}");
                }

                SetRemoteServerUri(existingSession.RemoteServerUri);
                return existingSession.Response;
            }

            var result = executor(driverCommandToExecute, parameters);
            return result;
        }

        private ICommandExecutor GetCommandExecutor(WebDriver remoteWebDriver)
        {
            var commandExecutorProperty = remoteWebDriver.GetType().GetProperty("CommandExecutor");
            if (null == commandExecutorProperty) throw new InvalidOperationException($"remoteWebDriver.CommandExecutor property should exist. {IMPLEMENTATION_NOTE}");

            var commandExecutor = commandExecutorProperty.GetValue(remoteWebDriver) as ICommandExecutor;
            if (null == commandExecutor) throw new InvalidOperationException($"remoteWebDriver.CommandExecutor should be of type ICommandExecutor. {IMPLEMENTATION_NOTE}");

            return commandExecutor;
        }
    }
}
