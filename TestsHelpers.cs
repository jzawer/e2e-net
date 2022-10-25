using Microsoft.Playwright;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;

namespace Poc2
{
    static class TestsHelpers
    {
        static public async Task AcceptCookies(IPage page)
        {
            await page.WaitForSelectorAsync("#agree-cookies", new PageWaitForSelectorOptions()
            {
                State = WaitForSelectorState.Visible,
                Strict = true
            });
            await page.ClickAsync("#agree-cookies");
        }

        static public async Task<ILocator> LocatorAndClick(IPage page, string selector, LocatorWaitForOptions? options = null)
        {
            var locator = page.Locator(selector);
            await locator.WaitForAsync(options);
            await locator.ClickAsync();

            return locator;
        }
    }
}
