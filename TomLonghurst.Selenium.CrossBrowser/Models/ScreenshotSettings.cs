using System.Collections.Generic;
using OpenQA.Selenium;
using TomLonghurst.Selenium.CrossBrowser.Enums;

namespace TomLonghurst.Selenium.CrossBrowser.Models
{
    public class ScreenshotSettings
    {
        public bool TakeScreenshots { get; set; } = true;
        public IEnumerable<Result> ResultsToTakeScreenshotsOf { get; set; } = new[] { Result.Fail };
        public ScreenshotImageFormat ScreenshotImageFormat { get; set; }
    }
}