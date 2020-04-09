using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Internal;
using OpenQA.Selenium.Opera;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Safari;
using OpenQA.Selenium.Support.Extensions;
using TomLonghurst.Selenium.CrossBrowser.Enums;
using TomLonghurst.Selenium.CrossBrowser.Helpers;
using TomLonghurst.Selenium.CrossBrowser.Models;

namespace TomLonghurst.Selenium.CrossBrowser
{
    public class CrossBrowserDriverManager
    {
        private readonly List<Func<IWebDriver>> _webDriverConstructors = new List<Func<IWebDriver>>();
        public TextWriter Logger { get; set; } = Console.Out;
        public ScreenshotSettings ScreenshotSettings { get; set; }
        public bool RunInParallel { get; set; }
        
        public CrossBrowserDriverManager(IEnumerable<Func<IWebDriver>> webdriverConstructors)
        {
            foreach (var webDriverConstructor in webdriverConstructors)
            {
                AddWebDriver(webDriverConstructor);
            }
        }

        public void AddWebDriver(Func<IWebDriver> webdriverConstructor)
        {
            _webDriverConstructors.Add(webdriverConstructor);
        }

        public void Execute(Action<IWebDriver> testJourney)
        {
            ExecuteAsync(driver =>
            {
                testJourney(driver);
                return Task.CompletedTask;
            }).GetAwaiter().GetResult();
        }

        public async Task ExecuteAsync(Func<IWebDriver, Task> testJourney)
        {
            var results = new List<BrowserResult>();
            var tasks = new List<Task>();

            foreach (var webDriverConstructor in _webDriverConstructors)
            {
                var task = RunJourney(testJourney, webDriverConstructor)
                    .ContinueWith(t => results.Add(t.Result));

                if (RunInParallel)
                {
                    tasks.Add(task);
                }
                else
                {
                    await task;
                }
            }

            await Task.WhenAll(tasks);

            await LogResults(results);
        }

        private Task<BrowserResult> RunJourney(Func<IWebDriver, Task> testJourney, Func<IWebDriver> webDriverConstructor)
        {
            return Task.Run(async () =>
            {
                BrowserResult result = null;

                IWebDriver webDriver = null;
                try
                {
                    webDriver = webDriverConstructor();
                    await testJourney(webDriver);
                    result = new BrowserResult(webDriver, Result.Pass);
                }
                catch (Exception exception)
                {
                    result = new BrowserResult(webDriver, exception);
                }
                finally
                {
                    CaptureScreenshots(webDriver, ref result);

                    TryCloseBrowser(webDriver);
                }

                return result;
            });
        }

        private void CaptureScreenshots(IWebDriver webDriver, ref BrowserResult result)
        {
            if (ScreenshotSettings?.TakeScreenshots != true || 
                !(ScreenshotSettings.ResultsToTakeScreenshotsOf ?? Enumerable.Empty<Result>()).Contains(result.Result))
            {
                return;
            }
            
            foreach (var windowHandle in webDriver.WindowHandles)
            {
                webDriver.SwitchTo().Window(windowHandle);
                result?.AddScreenshot(webDriver.TakeScreenshot());
            }
        }

        private static void TryCloseBrowser(IWebDriver webDriver)
        {
            try
            {
                webDriver.Quit();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async Task LogResults(IReadOnlyCollection<BrowserResult> results)
        {
            foreach (var result in results.OrderBy(r => r.Result))
            {
                var browserName = GetBrowserName(result.WebDriver);
                
                await Logger.WriteLineAsync($"\n{result.Result} on browser: {browserName}");
                
                if (result.Result == Result.Fail)
                {
                    await WriteException(browserName, result);
                }

                await LogScreenshots(browserName, result);
            }

            ThrowIfAnyExceptions(results);
        }

        private async Task LogScreenshots(string browserName, BrowserResult result)
        {
            var imageFormat = ScreenshotSettings?.ScreenshotImageFormat ?? ScreenshotImageFormat.Png;
            
            foreach (var screenshot in result.Screenshots)
            {
                Directory.CreateDirectory(FilePaths.Screenshots);
                
                var savePath = Path.Combine(FilePaths.Screenshots,
                    $"{browserName}-{Guid.NewGuid():N}.{imageFormat.ToString().ToLowerInvariant()}");

                screenshot.SaveAsFile(savePath, imageFormat);
                
                await Logger.WriteLineAsync($"\nScreenshot for browser {browserName} at path: {savePath}");
            }
        }

        private async Task WriteException(string browserName, BrowserResult result)
        {
            await Logger.WriteLineAsync($"Exception for browser {browserName}:");
            await Logger.WriteLineAsync($"[{result.Exception.GetType().Name}] - [{result.Exception.Message}]");
            await Logger.WriteLineAsync(result.Exception.StackTrace);
        }

        private static void ThrowIfAnyExceptions(IEnumerable<BrowserResult> results)
        {
            var exceptions = results.Select(result => result.Exception).Where(exception => exception != null).ToList();
            
            if (exceptions.Count == 1)
            {
                throw exceptions.First();
            }
            
            if (exceptions.Count > 1)
            {
                throw new AggregateException(exceptions);
            }
        }

        private static string GetBrowserName(IWebDriver webDriver)
        {
            if (webDriver is IWrapsDriver wrapsDriver)
            {
                return GetBrowserName(wrapsDriver.WrappedDriver);
            }
            
            if (webDriver is RemoteWebDriver remoteWebDriver)
            {
                var capabilities = remoteWebDriver.Capabilities;

                var version = "";
                if (capabilities.HasCapability("browserVersion"))
                {
                    version = capabilities.GetCapability("browserVersion").ToString();
                }

                if (capabilities.HasCapability("browserName"))
                {
                    return $"{capabilities.GetCapability("browserName")} {version}".Trim();
                }
            }

            switch (webDriver)
            {
                case ChromeDriver _:
                    return "Chrome";
                case FirefoxDriver _:
                    return "Firefox";
                case EdgeDriver _:
                    return "Edge";
                case InternetExplorerDriver _:
                    return "Internet Explorer";
                case SafariDriver _:
                    return "Safari";
                case OperaDriver _:
                    return "Opera";
                default:
                    return "Unknown Browser";
            }
        }
    }
}