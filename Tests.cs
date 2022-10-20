using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System.Linq;

namespace Poc2
{
    [Parallelizable(ParallelScope.Self)]
    [TestFixture]
    public class Tests : PageTest
    {
        const string domian = "https://presentaciones.stage.iberostar.com";
        const string view = "Reservations/availability";
        readonly string baseURL = $"{domian}/{view}?codiconc=24&conccodi=76&cp_tealium=&idiomercodi=es&numerohabitaciones=1&ok_promo=0&origen_soporte=IBE&search_origin=hotel&Cp=GOOGLEES&edP0_0=30&edadpersona0_0=30&edP0_1=30&edadpersona0_1=30&adultohab0=2&ninohab0=0&bebehab0=0&numeropersonas0=2&utm_source=pruebas_prm&utm_medium=pruebas_prm&utm_campaign=pruebas_prm";
        string currentUrl = "";

        public override BrowserNewContextOptions ContextOptions()
        {
            return new BrowserNewContextOptions()
            {
                RecordVideoDir = "videos/"
            };
        }

        [SetUp]
        public void Setup()
        {
            var today = DateTime.Now;
            var initDate = new DateTime(today.Ticks).AddDays(30);
            var endDate = new DateTime(today.Ticks).AddDays(34);
            var initDateQuery = $"&fechaini={initDate.ToString("dd/MM/yyyy")}";
            var endDateQuery = $"&fechafin={endDate.ToString("dd/MM/yyyy")}";
            var currencyCode = "EUR";
            var currencyCodeQuery = $"&monecodi={currencyCode}";
            var regionCode = 1; // espaniol
            var regionCodeQuery = $"&idiocodi={regionCode}";
            currentUrl = baseURL + initDateQuery + endDateQuery + currencyCodeQuery + regionCodeQuery;
        }

        [Test]
        public async Task Test1()
        {
            await Page.GotoAsync(currentUrl);

            await Page.WaitForSelectorAsync("#agree-cookies");
            var cookieButton = Page.Locator("#agree-cookies");
            await cookieButton.ClickAsync();

            await Expect(Page).ToHaveURLAsync(currentUrl);
            var cookies = await Page.Context.CookiesAsync(new List<string> { domian });
            var cookiesConsent = cookies.FirstOrDefault(c => c.Name == "cookies_consent");

            Assert.IsTrue(cookiesConsent.Value == "true");
            var video = Page.Video;
            var videoPath = await video.PathAsync();
            TestContext.AddTestAttachment(videoPath);
        }
    }
}