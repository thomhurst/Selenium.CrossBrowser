using System;
using System.Collections.Generic;
using OpenQA.Selenium;
using TomLonghurst.Selenium.CrossBrowser.Enums;

namespace TomLonghurst.Selenium.CrossBrowser
{
    internal class BrowserResult
    {
        public IWebDriver WebDriver { get; }
        public Exception Exception { get; }
        public Result Result { get; }
        public List<Screenshot> Screenshots { get; } = new List<Screenshot>();

        public BrowserResult(IWebDriver webDriver, Result result)
        {
            WebDriver = webDriver;
            Result = result;
        }
        
        public BrowserResult(IWebDriver webDriver, Exception exception)
        {
            WebDriver = webDriver;
            Exception = exception;
            Result = Result.Fail;
        }

        public void AddScreenshot(Screenshot screenshot)
        {
            Screenshots.Add(screenshot);
        }
    }
}