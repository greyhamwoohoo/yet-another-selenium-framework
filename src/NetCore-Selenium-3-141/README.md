# netcore-selenium-framework 
Bootstrapping of a bare minimum, opinionated .Net Core Selenium Framework using MsTest, the in-built .Net Core DI Container, Serilog, .runsettings and Visual Studio. 

My first goal is to be up and running with Selenium across several browsers within 15 minutes on either Linux or Windows in .Net Core using Visual Studio. By tweaking a few settings I want to optionally target Selenium Grid, different environments and change my control settings (such as timeouts and so forth). This repository lets me do that. 

My second goal is to have a personal reference showing patterns I can lift'n'shift into other frameworks (that I often encounter and have to maintain): browser selection, hot-reload, environment selection, timeouts/control management, superficial reporting, remote web driver configuration, environment variable overrides, multi-element eventual consistency, runsettings/IDE integration, simple logging, dependency injection/container initialization are included. 

If you are looking for a fully fledged Selenium / Automation framework implementation, that problem has already been solved by someone elsewhere: consider looking at [Atata Framework](https://github.com/atata-framework)

A few automated (raw, inline locator) tests are written against https://the-internet.herokuapp.com/ - that site contains all kinds of UI Automation Nastiness. 

NOTE: Appium is slowly being added, but is a work in progress. 

## Framework Parameters
The browser you use, where the test is executed (locally, Selenium Grid), how you control that execution (timeouts, logging) and the environment you target are runtime parameters and each one is a separate concern and should be configurable independently. Collectively these parameters are known as the "Test Execution Context"

By default, the tests will run using the Chrome browser against http://the-internet.herokuapp.com.

Environment variables can be set before running the tests to configure the Test Execution Context:

| Environment Variable | Default | Description |
| -------------------- | ------- | ----------- |
| YASF_BROWSERSETTINGS_FILES | common-chrome.json | Launches an incognito Chrome |
| YASF_REMOTEWEBDRIVERSETTINGS_FILES | common-localhost-selenium.json | Does not use a remote webdriver - launches locally (Selenium) |
| YASF_ENVIRONMENTSETTINGS_FILES | internet.json | The target environment for the tests. ie: where the application (baseUrl) will point to |
| YASF_CONTROLSETTINGS_FILES | common.json | Element timeouts, polling frequency etc. |
| YASF_DEVICESETTINGS_FILES | common-desktop-selenium.json | The 'Selenium Happy Path' - we want to launch browsers on the Desktop |

The following browsers (and configurations) are supported - choose the browser by setting YASF_BROWSERSETTINGS_FILES environment variable to one of these values:

| Browser | Value | Description |
| ------- | ----- | ----------- |
| Chrome | common-chrome.json | Launches Chrome with sensible default settings |
| Chrome | common-headless-chrome.json | Launches Chrome in headless mode (use this to run tests in Docker containers) |
| Chrome | common-performance-chrome.json | Launches Chrome and collects performance information |
| Edge | common-edge.json | Incognito / Private Browsing for Edge |
| FireFox | common-firefox.json | Launches FireFox with sensible default settings |

The following Remote Web Driver Settings files are provided:

| Setting | Description |
| ------- | ----------- |
| common-localhost-selenium.json | The default. Means: do not use a Remote Web Driver |

### Advanced Parameters
The optional xxx_FILES environment variables support either a fully qualified path; or the name of a file that is expected to exist under the Runtime folder when the tests are executed; or a combination of both. 

This implementation uses the .Net Core ConfigurationBuilder() - this means configuration files can be 'overlayed' and specialized like appsettings. For example, to specify a base file and then overload just one or two properties, specify the original file and then a Json file containing the variations:

```
SET YASF_BROWSERSETTINGS_FILES=common-chrome.json;chrome-captureLogFile.json;otherspecializations
```

The .Net Core conventions for environment variable overrides are also supported which means individual settings can be overridden by setting environment variables prior to execution. 

For example: to change the RemoteWebDriverSettings RemoteUri property in the JSON, do something like this:

```
SET YASF_REMOTEWEBDRIVERSETTINGS:REMOTEURI="https://localhost.com/overriddenUri"

REM Use the .Net Core __ notation for overriding nested values in the configuration files: see tec.attachable-chrome-localhost.json for an example
SET YASF_REMOTEWEBDRIVERSETTINGS__REMOTEURI="https://localhost.com/overriddenUri"
```

## Test Execution Contexts (.runsettings)
It is convenient within Visual Studio to quickly switch between - say - 'common-firefox' and 'common-chrome' and/or different environments depending on the work you are doing. This can be done using the .runsettings files in the TestExecutionContexts folder. 

To choose a .runsettings file:

1. Select Test / Configure Run Settings / Select Solution Wide runsettings file from the menu
2. Choose one of the .runsettings files
3. Run your tests

The 'real' settings are stored in the tec.*.json files - at the moment, these settings are the environment variables that need to be set for that test run. The 'TestRunInitialization.cs' file contains the logic to set this up. 

If executing the tests from the command line, you can specify the name of the test execution context by setting this variable:

```
YASF_TEST_EXECUTION_CONTEXT=common-chrome-localhost
```

| RunSettings | Test Execution Context | Full filename | Description | 
| ----------- | ---------------------- | ------------- | ----------- |
| Default-Chrome-Localhost.runsettings | default-chrome-localhost | tec.default-chrome-localhost.json | The default. Launches Chrome |
| Attachable-Chrome-Localhost.runsettings | attachable-chrome-localhost | tec.attachable-chrome-localhost.json | Will attach to an already-running Selenium Driver instance if it exists and was started when this .runsettings was active. |
| Attachable-IE11-Localhost.runsettings | attachable-ie11-localhost | tec.attachable-ie11-localhost.json | Will attach to an already-running Selenium Driver instance if it exists and was started when this .runsettings was active. 
| Attachable-Edge-Localhost.runsettings | attachable-edge-localhost | tec.attachable-edge-localhost.json | Will attach to an already-running Selenium Driver instance if it exists and was started when this .runsettings was active. |
| Default-Edge-Localhost.runsettings | default-edge-localhost | tec.default-edge-localhost.json | Runs the tests in Edge |
| Default-FireFox-Localhost.runsettings | default-firefox-localhost | tec.default-firefox-localhost.json | Runs the tests in Firerfox |
| Default-Android-Web-Chrome-Localhost.runsettings | default-android-chrome-web-localhost | tec.tec.default-android-chrome-web-localhost.json | Runs tests against Chrome on an Android Device using the chromedriver.exe that is part of the Solution |

## Hot Reload Functionality / Scratchpad
The problem I want to solve is this:

I want a 'scratchpad' where I can write a few lines of Selenium and have them executed against an existing broweser - on whichever page and state that browser might be. I want to do this within my testing framework and be able to leverage any functionality that my framework might provide. 

For simplicity in this case: the 'scratchpad' takes the form of a single test  - it's just a normmal test - called 'HotReloadScratchpad'. 

The first time the test is run with a test execution context of (say) 'attachable-chrome-localhost' (via Attachable-Chrome-Localhost.runsettings) it will start a browser instance BUT NOT CLOSE IT. Subsequent test runs using the same test execution context will reuse the same browser instance. This allows you experiment with one or two lines of Selenium at a time for faster feedback before moving that code into your main test workflow; you are of course able to manually interact and mutate that browser state as you wish. 

There are two viable workflows:

### Automatically
Open  up a command prompt in TheInternet.SystemTests.Raw folder and execute the following command:

Command Prompt:
```
SET YASF_TEST_EXECUTION_CONTEXT=attachable-chrome-localhost
dotnet watch test --filter "Name=HotReloadWorkflow"
```

PowerShell:
```
$env:YASF_TEST_EXECUTION_CONTEXT="attachable-chrome-localhost"
dotnet watch test --filter "Name=HotReloadWorkflow"
```

Whenever we save anything in the TheInternet.SystemTests.Raw project, that single test will be run. On my machine it takes around 6 seconds for the build and test exection to occur; perhaps 'warm-reload' is a better description :)

### Within Visual Studio
1. Select the Attachable-Chrome-Localhost .runsettings file *FIRST*
2. Run the 'HotReloadWorkflow' test (or: ANY test!)
3. The browser will NOT be terminated on shutdown (or test failure etc. )
4. Run a few lines of Selenium in the 'HotReloadWorkflow' test
5. The existing browser will be reused

### Implementation
The persisted session state is persisted in the test run folder as '.selenium.session'. If anything catastrophic should happen that it cannot recover from, manually delete that file. 

If you have closed the browser manually between test runs, the implementation will start a new browser session automatically. 

Implementation Notes:
1. The implementatation is in Drivers/ folder for each supported driver. 
2. The architecture and implementation of the WebDrivers makes it difficult / impossible to inject DI-resolved services.
   As a result, there's a lot of new-ing up going on and lots of cloned code. See Driver/*Driver.cs files for more information. 

## Dockerizing Chrome and Tests
To build a container with Chrome and the tests included:

```
docker build -t chrome-and-tests:local .
```

To run the tests, we always need to specify (at least) headless-chrome.json:

```
docker run -e YASF_TEST_EXECUTION_CONTEXT=empty -e YASF_BROWSERSETTINGS_FILES=common-headless-chrome.json chrome-and-tests:local
```

To add extra 'dotnet test' parameters:

```
docker run -e YASF_TEST_EXECUTION_CONTEXT=empty -e YASF_BROWSERSETTINGS_FILES=common-headless-chrome.json chrome-and-tests:local  --testcasefilter:"Name=DriverSessionExists"
```

To run the tests and capture the output / test results from the container (PowerShell)

```
New-Item -ItemType Directory -Name container-test-results -Force

docker run -v "$($pwd)/container-test-results:/app/TestResults" -e YASF_TEST_EXECUTION_CONTEXT=empty -e YASF_BROWSERSETTINGS_FILES=common-headless-chrome.json chrome-and-tests:local --logger:"trx;LogFileName=MyPsResults.trx"
```

To run the tests (CMD Prompt):

```
mkdir container-test-results

docker run -v %CD%/container-test-results:/app/TestResults -e YASF_TEST_EXECUTION_CONTEXT=empty -e YASF_BROWSERSETTINGS_FILES=common-headless-chrome.json chrome-and-tests:local --logger:"trx;LogFileName=MyCmdPromptResults.trx"
```

### Reference
| Reference | Link |
| --------- | ---- |
| '437' Encoding Error: Allowing FireFox WebDriver to run under .Net Core | https://github.com/SeleniumHQ/selenium/issues/4816 |
| ChromeDriver.exe not copied to output folder | https://stackoverflow.com/questions/55007311/selenium-webdriver-chromedriver-chromedriver-exe-is-not-being-publishing-for-n |
| Docker /dev/shm default is 64Meg - nowhere near enough for ChromeDriver Headless | https://developers.google.com/web/tools/puppeteer/troubleshooting |
