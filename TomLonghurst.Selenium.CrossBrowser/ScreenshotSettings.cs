using System.Collections.Generic;
using OpenQA.Selenium;

namespace TomLonghurst.Selenium.CrossBrowser
{
    public class ScreenshotSettings
    {
        public bool TakeScreenshots { get; set; } = true;
        public IEnumerable<Result> ResultsToTakeScreenshotsOf { get; set; } = new[] { Result.Fail };
        public ScreenshotImageFormat ScreenshotImageFormat { get; set; }
    }
}