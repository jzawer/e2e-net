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
        const string domain = "https://pre3.stage.iberostar.com";
        const string view = "Reservations/availability";
        readonly string baseURL = $"{domain}/{view}?codiconc=24&conccodi=76&cp_tealium=&idiomercodi=es&numerohabitaciones=1&ok_promo=0&origen_soporte=IBE&search_origin=hotel&Cp=GOOGLEES&edP0_0=30&edadpersona0_0=30&edP0_1=30&edadpersona0_1=30&adultohab0=2&ninohab0=0&bebehab0=0&numeropersonas0=2&utm_source=pruebas_prm&utm_medium=pruebas_prm&utm_campaign=pruebas_prm";
        string currentUrl = "";

        public override BrowserNewContextOptions ContextOptions()
        {
            return new BrowserNewContextOptions()
            {
                RecordVideoDir = "videos/"
            };
        }

        [SetUp]
        public async Task Setup()
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

            await Context.Tracing.StartAsync(new()
            {
                Screenshots = true,
                Snapshots = true,
                Sources = true
            });
        }

        [TearDown]
        public async Task TearDown()
        {
            // Save video to report
            await Page.WaitForTimeoutAsync(1000);
            var video = Page.Video;
            var videoPath = await video.PathAsync();
            TestContext.AddTestAttachment(videoPath, "video del test");

            await Context.Tracing.StopAsync(new()
            {
                Path = "trace.zip"
            });
        }

        [Test]
        public async Task Emea_B2C_1_2_EUR_CheckCookies()
        {
            await Page.GotoAsync(currentUrl);
            await TestsHelpers.AcceptCookies(Page);

            var cookies = await Page.Context.CookiesAsync(new List<string> { domain });
            var cookiesConsent = cookies.FirstOrDefault(c => c.Name == "cookies_consent");
            Assert.IsTrue(cookiesConsent.Value == "true");
        }

        [Test]
        public async Task Emea_B2C_1_2_EUR_ChangeCurrency()
        {
            await Page.GotoAsync(currentUrl);
            await TestsHelpers.AcceptCookies(Page);

            var currencyListBtn = await TestsHelpers.LocatorAndClick(Page, "#currency-selector-btn");
            await Expect(currencyListBtn).ToHaveClassAsync(new Regex("active"));

            await Page.RunAndWaitForNavigationAsync(async () =>
            {
                await Page.Locator("#currencies-panel").GetByText("USD").ClickAsync();
            });

            var currencyRegex = new Regex("USD");
            await Expect(Page).ToHaveURLAsync(currencyRegex);

            var currencyTotalText = Page.Locator(".final-price");
            await Expect(currencyTotalText).ToHaveTextAsync(currencyRegex);
        }

        [Test]
        public async Task Emea_B2C_1_2_EUR_ChangeRegion()
        {
            await Page.GotoAsync(currentUrl);
            await TestsHelpers.AcceptCookies(Page);

            var langListBtn = await TestsHelpers.LocatorAndClick(Page, "#lang-selector-btn");
            await Expect(langListBtn).ToHaveClassAsync(new Regex("active"));

            await Page.RunAndWaitForNavigationAsync(async () =>
            {
                await Page.Locator("#panel-lang").GetByText("English").ClickAsync();
            });

            await Expect(Page).ToHaveURLAsync(new Regex("&idiocodi=2"));
            await Expect(Page.Locator("#lang-selector-btn")).ToHaveTextAsync(new Regex("English"));
        }

        [Test]
        public async Task Emea_B2C_1_2_EUR_LoadYoutubeVideoInGallery()
        {
            await Page.GotoAsync(currentUrl);
            await TestsHelpers.AcceptCookies(Page);

            await TestsHelpers.LocatorAndClick(Page, ".hotel-card .image img");

            var modalInfoBox = Page.Locator(".modal-wrapper .info-box .box-gallery.active");
            await modalInfoBox.WaitForAsync();
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
        }

        [Test]
        public async Task Emea_B2C_1_2_EUR_SelectRoomAndContinueToStep2()
        {
            await Page.GotoAsync(currentUrl);
            await TestsHelpers.AcceptCookies(Page);

            // Select room rate
            await Page.WaitForSelectorAsync(".room-list");
            var roomList = Page.Locator(".room-list .b-room-card");
            await Expect(roomList.First).Not.ToBeEmptyAsync();
            var roomRates = roomList.First.Locator(".b-rate-card");
            await Expect(roomRates.First).Not.ToBeEmptyAsync();
            await roomRates.First.Locator(".action").ClickAsync();
            await Expect(roomRates.First).ToHaveClassAsync(new Regex("selected"));

            // Go to Step 2 and check price
            var sideBar = Page.Locator("#divStickyBreakdown");
            var finalPriceText = await sideBar.Locator("span.final-price").TextContentAsync();
            double.TryParse(finalPriceText.Split(" ")[0], out double finalPrice);
            finalPrice = Math.Round(finalPrice);
            var nextStepBtn = sideBar.Locator("#btn-back");
            await nextStepBtn.ClickAsync();

            await Page.WaitForURLAsync(new Regex($"{domain}/Reservations/form"));

            var finalPriceSidePanel = Page.Locator(".b-side-final-price .content-final-price .final-price");
            await Expect(finalPriceSidePanel).ToHaveTextAsync(new Regex(finalPrice.ToString()));
        }
    }
}