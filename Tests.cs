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
        public async Task Emea_B2C_1_2_EUR()
        {
            await Page.GotoAsync(currentUrl);

            // Accept Cookies
            await Page.WaitForSelectorAsync("#agree-cookies");
            var cookieButton = Page.Locator("#agree-cookies");
            await cookieButton.ClickAsync();

            await Expect(Page).ToHaveURLAsync(currentUrl);
            
            var cookies = await Page.Context.CookiesAsync(new List<string> { domian });
            var cookiesConsent = cookies.FirstOrDefault(c => c.Name == "cookies_consent");
            Assert.IsTrue(cookiesConsent.Value == "true");

            // Currency change
            await Page.WaitForSelectorAsync("#currency-selector-btn");

            var currencyListBtn = Page.Locator("#currency-selector-btn");
            await currencyListBtn.WaitForAsync(new (){ State = WaitForSelectorState.Visible });
            await currencyListBtn.ClickAsync();
            await Expect(currencyListBtn).ToHaveClassAsync(new Regex("active"));

            var currencyPanel = Page.Locator("#currencies-panel");
            await currencyPanel.GetByText("USD").ClickAsync();

            var currencyRegex = new Regex("USD");
            await Expect(Page).ToHaveURLAsync(currencyRegex);

            var currencyTotalText = Page.Locator(".final-price");
            await Expect(currencyTotalText).ToHaveTextAsync(currencyRegex);

            // Region Change
            await Page.WaitForSelectorAsync("#lang-selector-btn");

            var langListBtn = Page.Locator("#lang-selector-btn");
            await langListBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await langListBtn.ClickAsync();

            await Expect(langListBtn).ToHaveClassAsync(new Regex("active"));

            var langPanel = Page.Locator("#panel-lang");
            await langPanel.GetByText("English").ClickAsync();

            await Expect(Page).ToHaveURLAsync(new Regex("&idiocodi=2"));
            await Expect(Page.Locator("#lang-selector-btn")).ToHaveTextAsync(new Regex("English"));

            // Youtube video loading
            var photosButton = Page.Locator(".hotel-card .image img");
            await photosButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await photosButton.ClickAsync();

            var modalInfoBox = Page.Locator(".modal-wrapper .info-box .box-gallery.active");
            await modalInfoBox.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await Expect(modalInfoBox).Not.ToBeEmptyAsync();

            var videosFilter = modalInfoBox.Locator(".gallery-content .gallery-filters ul li [data-tag-id=\"videos\"]");
            await Expect(videosFilter).Not.ToBeEmptyAsync();
            await Expect(videosFilter).Not.ToHaveClassAsync(new Regex("active"));
            await videosFilter.ClickAsync();
            await Expect(videosFilter).ToHaveClassAsync(new Regex("active"));

            var galleryItems = modalInfoBox.Locator(".gallery-items .gallery-item");
            var galleryItemsCount = await galleryItems.CountAsync();
            Assert.IsTrue(galleryItemsCount > 0);
            var currentVideo = galleryItems.First.FrameLocator(".ytmedia").Locator("#player");
            await Expect(currentVideo).Not.ToBeEmptyAsync();

            // Save video to report
            await Page.WaitForTimeoutAsync(1000);
            var video = Page.Video;
            var videoPath = await video.PathAsync();
            TestContext.AddTestAttachment(videoPath, "video del test");
        }
    }
}