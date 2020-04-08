using System;
using System.Collections.Generic;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;

namespace TomLonghurst.Selenium.CrossBrowser.Tests
{
    [Parallelizable(ParallelScope.None)]
    [TestFixture]
    public class Tests
    {
        private CrossBrowserDriverManager _crossBrowserDriverManager;
        
        [SetUp]
        public void Setup()
        {
            _crossBrowserDriverManager = new CrossBrowserDriverManager(new List<Func<IWebDriver>>()
            {
                () => new FirefoxDriver(),
                () => new ChromeDriver()
            });
        }

        private static void TestBodySuccessful(IWebDriver webdriver)
        {
            webdriver.Navigate().GoToUrl("https://www.google.com");
            var searchBar = webdriver.FindElement(By.XPath("//input[@title='Search']"));
            searchBar.SendKeys("Hey Google!");
            searchBar.SendKeys(Keys.Enter);
        }
        
        private static void TestBodyFailChrome(IWebDriver webdriver)
        {
            webdriver.Navigate().GoToUrl("https://www.google.com");
            var searchBar = webdriver.FindElement(By.XPath("//input[@title='Search']"));
            searchBar.SendKeys("Hey Google!");

            if (webdriver is ChromeDriver)
            {
                throw new Exception("My test has failed!");
            }
            
            searchBar.SendKeys(Keys.Enter);
        }
        
        private static void TestBodyFail(IWebDriver webdriver)
        {
            throw new Exception("My test has failed!");
        }

        [Test, Combinatorial]
        public void SuccessfullyRun(
            [Values(true, false)] bool runInParallel,
            [Values(true, false)] bool takeScreenshots
        )
        {
            _crossBrowserDriverManager.RunInParallel = runInParallel;
            _crossBrowserDriverManager.ScreenshotSettings = new ScreenshotSettings {TakeScreenshots = takeScreenshots};
            
            _crossBrowserDriverManager.Execute(TestBodySuccessful);
        }

        [Test]
        public void FailChrome(
            [Values(true, false)] bool runInParallel,
            [Values(true, false)] bool takeScreenshots
            )
        {
            _crossBrowserDriverManager.RunInParallel = runInParallel;
            _crossBrowserDriverManager.ScreenshotSettings = new ScreenshotSettings {TakeScreenshots = takeScreenshots};

            Assert.Throws<Exception>(() => _crossBrowserDriverManager.Execute(TestBodyFailChrome));
        }

        [Test]
        public void FailAll(
            [Values(true, false)] bool runInParallel,
            [Values(true, false)] bool takeScreenshots
            )
        {
            _crossBrowserDriverManager.RunInParallel = runInParallel;
            _crossBrowserDriverManager.ScreenshotSettings = new ScreenshotSettings {TakeScreenshots = takeScreenshots};

            Assert.Throws<AggregateException>(() => _crossBrowserDriverManager.Execute(TestBodyFail));
        }
    }
}