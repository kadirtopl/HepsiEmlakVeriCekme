using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.IO;
using System.Linq;
using System.Threading;
using Firebase.Database;
using Firebase.Database.Query;

namespace SahibindenScraper
{
    public partial class Form1 : Form
    {
        private Button openBrowserButton;
        private Button saveButton;
        private Button stopButton;
        private Button closeButton;
        private ProgressBar progressBar;
        private Label statusLabel;
        private Panel headerPanel;
        private Label titleLabel;
        private IWebDriver driver;
        private ChromeDriver chromeDriver;
        private bool isCollecting = false;
        private List<(string Title, string Price, string Location, string Details, string SellerInfo, string PhoneNumber)> listings = new List<(string Title, string Price, string Location, string Details, string SellerInfo, string PhoneNumber)>();
        private FirebaseService firebaseService;
        private System.Windows.Forms.Timer checkTimer;
        private bool isFirstUrl = true;
        private const string FIRST_URL = "https://www.hepsiemlak.com/kocaeli-satilik-sahibinden/arsa?counties=basiskele,golcuk,izmit,kandira,kartepe&sortField=UPDATED_DATE&sortDirection=DESC";
        private const string SECOND_URL = "https://www.hepsiemlak.com/kocaeli-satilik-sahibinden?counties=basiskele,golcuk,izmit,kandira,kartepe&sortField=UPDATED_DATE&sortDirection=DESC";
        private const string THIRD_URL = "https://www.hepsiemlak.com/kocaeli-kiralik-sahibinden?counties=basiskele,golcuk,izmit,kandira,kartepe&sortField=UPDATED_DATE&sortDirection=DESC";
        private const string WAIT_URL = "https://www.google.com";
        private const int CHECK_INTERVAL = 45 * 60 * 1000; // 45 dakika
        private const int TARGET_LISTINGS_PER_URL = 10;
        private int skippedListingsCount = 0;

        public Form1()
        {
            InitializeComponent();
            InitializeCustomComponents();
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Text = "HepsiEmlak İlan Bilgi Toplayıcı";
            firebaseService = new FirebaseService();
            InitializeTimer();
        }

        private void InitializeTimer()
        {
            checkTimer = new System.Windows.Forms.Timer();
            checkTimer.Interval = CHECK_INTERVAL;
            checkTimer.Tick += CheckTimer_Tick;
        }

        private async void CheckTimer_Tick(object sender, EventArgs e)
        {
            if (isCollecting)
            {
                checkTimer.Stop();
                isFirstUrl = true;
                skippedListingsCount = 0;
                await Task.Run(() => StartCollecting(FIRST_URL));
            }
        }

        private void InitializeCustomComponents()
        {
            // Header Panel
            headerPanel = new Panel
            {
                BackColor = Color.FromArgb(51, 51, 51),
                Dock = DockStyle.Top,
                Height = 60
            };

            titleLabel = new Label
            {
                Text = "HepsiEmlak İlan Bilgi Toplayıcı",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 15)
            };
            headerPanel.Controls.Add(titleLabel);

            // Button Panel
            var buttonPanel = new FlowLayoutPanel
            {
                Location = new Point(20, 80),
                Size = new Size(544, 90),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };

            // Open Browser Button
            openBrowserButton = new Button
            {
                Text = "İlanları Toplamaya Başla",
                Size = new Size(260, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            openBrowserButton.Click += OpenBrowserButton_Click;

            // Stop Button
            stopButton = new Button
            {
                Text = "Durdur",
                Size = new Size(260, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(255, 69, 0),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            stopButton.Click += StopButton_Click;

            // Save Button
            saveButton = new Button
            {
                Text = "Kaydet",
                Size = new Size(260, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 191, 255),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            saveButton.Click += SaveButton_Click;

            // Close Button
            closeButton = new Button
            {
                Text = "Kapat",
                Size = new Size(260, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(204, 0, 0),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            closeButton.Click += CloseButton_Click;

            buttonPanel.Controls.AddRange(new Control[] {
                openBrowserButton,
                stopButton,
                saveButton,
                closeButton
            });

            // Progress Bar
            progressBar = new ProgressBar
            {
                Location = new Point(20, 190),
                Size = new Size(544, 25),
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Visible = false
            };

            // Status Label
            statusLabel = new Label
            {
                Location = new Point(20, 225),
                Size = new Size(544, 20),
                Font = new Font("Segoe UI", 10),
                TextAlign = ContentAlignment.MiddleLeft,
                Visible = false
            };

            // Form Properties
            this.Size = new Size(600, 300);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Add Controls
            this.Controls.AddRange(new Control[] {
                headerPanel,
                buttonPanel,
                progressBar,
                statusLabel
            });
        }

        private async void OpenBrowserButton_Click(object sender, EventArgs e)
        {
            try
            {
                openBrowserButton.Enabled = false;
                stopButton.Enabled = true;
                closeButton.Enabled = true;
                progressBar.Visible = true;
                statusLabel.Visible = true;
                statusLabel.Text = "İlanlar toplanıyor...";
                isCollecting = true;
                isFirstUrl = true;
                skippedListingsCount = 0;

                await Task.Run(() => StartCollecting(FIRST_URL));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Bir hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetControls();
            }
        }

        private async void StartCollecting(string url)
        {
            try
            {
                if (driver == null)
                {
                    var chromeOptions = new ChromeOptions();
                    chromeOptions.AddArgument("--disable-gpu");
                    chromeOptions.AddArgument("--no-sandbox");
                    chromeOptions.AddArgument("--disable-dev-shm-usage");
                    chromeOptions.AddArgument("--disable-extensions");
                    chromeOptions.AddArgument("--disable-popup-blocking");
                    chromeOptions.AddArgument("--disable-javascript-harmony-shipping");
                    chromeOptions.AddArgument("--dns-prefetch-disable");
                    chromeOptions.AddArgument("--disable-background-networking");
                    chromeOptions.AddArgument("--disk-cache-size=1");
                    chromeOptions.AddArgument("--media-cache-size=1");
                    chromeOptions.AddArgument("--disable-blink-features=AutomationControlled");
                    chromeOptions.AddArgument("--disable-infobars");
                    chromeOptions.AddArgument("--disable-web-security");
                    chromeOptions.AddArgument("--allow-running-insecure-content");
                    chromeOptions.AddArgument("--ignore-certificate-errors");
                    chromeOptions.AddArgument("--window-size=1920,1080");
                    chromeOptions.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/135.0.0.0 Safari/537.36");
                    chromeOptions.AddArgument("--disable-notifications");
                    chromeOptions.AddArgument("--disable-logging");
                    chromeOptions.AddArgument("--log-level=3");
                    chromeOptions.AddArgument("--silent");
                    chromeOptions.AddArgument("--disable-logging");
                    chromeOptions.AddArgument("--disable-dev-shm-usage");
                    chromeOptions.AddArgument("--disable-software-rasterizer");
                    chromeOptions.AddArgument("--disable-features=TranslateUI");
                    chromeOptions.AddArgument("--disable-features=IsolateOrigins,site-per-process");
                    chromeOptions.AddArgument("--disable-site-isolation-trials");
                    chromeOptions.AddArgument("--disable-features=NetworkService");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess2");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess3");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess4");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess5");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess6");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess7");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess8");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess9");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess10");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess11");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess12");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess13");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess14");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess15");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess16");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess17");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess18");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess19");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess20");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess21");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess22");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess23");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess24");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess25");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess26");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess27");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess28");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess29");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess30");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess31");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess32");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess33");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess34");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess35");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess36");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess37");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess38");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess39");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess40");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess41");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess42");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess43");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess44");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess45");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess46");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess47");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess48");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess49");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess50");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess51");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess52");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess53");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess54");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess55");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess56");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess57");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess58");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess59");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess60");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess61");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess62");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess63");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess64");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess65");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess66");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess67");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess68");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess69");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess70");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess71");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess72");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess73");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess74");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess75");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess76");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess77");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess78");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess79");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess80");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess81");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess82");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess83");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess84");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess85");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess86");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess87");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess88");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess89");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess90");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess91");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess92");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess93");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess94");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess95");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess96");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess97");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess98");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess99");
                    chromeOptions.AddArgument("--disable-features=NetworkServiceInProcess100");

                    var chromeDriverService = ChromeDriverService.CreateDefaultService();
                    chromeDriverService.HideCommandPromptWindow = true;

                    chromeDriver = new ChromeDriver(chromeDriverService, chromeOptions);
                    driver = chromeDriver;

                    driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(60);
                    driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(20);
                    driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromSeconds(60);
                }

                UpdateStatus($"Sayfa yükleniyor: {url}");
                driver.Navigate().GoToUrl(url);
                
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
                wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState") as string == "complete");
                Thread.Sleep(5000);
                
                try
                {
                    wait.Until(d => d.FindElements(By.CssSelector(".listing-item")).Count > 0);
                    UpdateStatus("Sayfa başarıyla yüklendi, ilanlar toplanmaya başlanıyor...");
                }
                catch (Exception ex)
                {
                    UpdateStatus("Sayfa yüklenirken sorun oluştu, yeniden deneniyor...");
                    driver.Navigate().Refresh();
                    Thread.Sleep(5000);
                    wait.Until(d => d.FindElements(By.CssSelector(".listing-item")).Count > 0);
                }

                var totalPages = 1;
                try
                {
                    var pageLinks = driver.FindElements(By.CssSelector(".he-pagination__link"));
                    if (pageLinks.Count > 0)
                    {
                        var lastPageText = pageLinks[pageLinks.Count - 1].Text;
                        if (int.TryParse(lastPageText, out int lastPage))
                        {
                            totalPages = lastPage;
                        }
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Sayfa sayısı alınırken hata oluştu: {ex.Message}");
                }

                int currentPage = 1;
                int retryCount = 0;
                const int maxRetries = 3;

                UpdateStatus($"Veri toplama başlıyor... (Sayfa {currentPage}/{totalPages})");

                while (isCollecting && skippedListingsCount < TARGET_LISTINGS_PER_URL)
                {
                    try
                    {
                        wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState") as string == "complete");
                        var listingElements = wait.Until(d => d.FindElements(By.XPath("//li[contains(@class, 'listing-item')]")));

                        if (listingElements.Count == 0)
                        {
                            UpdateStatus("İlanlar bulunamadı, sayfa yenileniyor...");
                            driver.Navigate().Refresh();
                            Thread.Sleep(100);
                            continue;
                        }

                        UpdateStatus($"İlanlar toplanıyor... (Sayfa {currentPage}/{totalPages}, Atlanan: {skippedListingsCount}/{TARGET_LISTINGS_PER_URL})");

                        foreach (var element in listingElements)
                        {
                            if (!isCollecting || skippedListingsCount >= TARGET_LISTINGS_PER_URL) break;

                            try
                            {
                                wait.Until(d => element.Displayed);
                                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center', behavior: 'smooth'});", element);
                                Thread.Sleep(200);

                                var title = element.FindElement(By.XPath(".//section[contains(@class, 'bottom')]//h3")).Text;
                                var price = element.FindElement(By.XPath(".//span[contains(@class, 'list-view-price')]")).Text;
                                var location = element.FindElement(By.XPath(".//span[contains(@class, 'list-view-location')]")).Text.Replace("\r\n", "").Trim();
                                var details = element.FindElement(By.XPath(".//span[contains(@class, 'short-property')]")).Text;

                                var phoneButton = element.FindElement(By.XPath(".//button[contains(@class, 'listing-card--action__button') and contains(@class, 'action-telephone')]"));
                                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center', behavior: 'smooth'}); arguments[0].click();", phoneButton);
                                Thread.Sleep(500);

                                var phoneNumber = "";
                                var sellerInfo = "";
                                string currentListingId = "";

                                try
                                {
                                    var modalWait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));
                                    
                                    var listingIdElement = modalWait.Until(d => d.FindElement(By.XPath("//span[contains(@class, 'phone-listing-id')]")));
                                    currentListingId = listingIdElement.Text.Replace("İlan No:", "").Trim();
                                    
                                    // Firebase'den kontrol et
                                    bool exists = await firebaseService.CheckIfListingExists(currentListingId);
                                    if (exists)
                                    {
                                        skippedListingsCount++;
                                        UpdateStatus($"Kayıtlı ilan atlandı: {currentListingId} ({skippedListingsCount}/{TARGET_LISTINGS_PER_URL})");
                                        driver.FindElement(By.XPath("//a[contains(@class, 'close-list-phone-wrapper')]")).Click();
                                        Thread.Sleep(200);

                                        if (skippedListingsCount >= TARGET_LISTINGS_PER_URL)
                                        {
                                            if (!isFirstUrl)
                                            {
                                                if (url == SECOND_URL)
                                                {
                                                    UpdateStatus($"İkinci linkte hedef sayıya ulaşıldı. Üçüncü linke geçiliyor...");
                                                    await Task.Run(() => StartCollecting(THIRD_URL));
                                                }
                                                else
                                                {
                                                    UpdateStatus($"Üçüncü linkte hedef sayıya ulaşıldı. Google'a gidiliyor ve 45 dakika bekleniyor...");
                                                    driver.Navigate().GoToUrl(WAIT_URL);
                                                    checkTimer.Interval = CHECK_INTERVAL;
                                                    checkTimer.Start();
                                                }
                                            }
                                            else
                                            {
                                                UpdateStatus($"İlk linkte hedef sayıya ulaşıldı. İkinci linke geçiliyor...");
                                                isFirstUrl = false;
                                                skippedListingsCount = 0;
                                                await Task.Run(() => StartCollecting(SECOND_URL));
                                            }
                                            return;
                                        }
                                        continue;
                                    }

                                    var sellerElement = modalWait.Until(d => d.FindElement(By.XPath("//div[contains(@class, 'list-phone-info')]//span[contains(@class, 'phone-consultant-name')]")));
                                    sellerInfo = sellerElement.Text.Trim();

                                    var phoneElement = modalWait.Until(d => d.FindElement(By.XPath("//ul[contains(@class, 'list-phone-numbers')]//a")));
                                    phoneNumber = phoneElement.Text.Replace("\n", " ").Trim();

                                    driver.FindElement(By.XPath("//a[contains(@class, 'close-list-phone-wrapper')]")).Click();
                                    Thread.Sleep(200);

                                    var listingData = new Dictionary<string, string>
                                    {
                                        { "Başlık", title },
                                        { "Fiyat", price },
                                        { "Konum", location },
                                        { "Detaylar", details },
                                        { "İlan Sahibi", sellerInfo },
                                        { "Telefon", phoneNumber },
                                        { "Şehir", "Kocaeli" },
                                        { "İlçe", "İzmit" },
                                        { "İlan Tarihi", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
                                        { "Yeni İlan", "Evet" },
                                        { "İlan ID", currentListingId }
                                    };

                                    var detailItems = details.Split(',').Select(d => d.Trim()).ToList();
                                    foreach (var detail in detailItems)
                                    {
                                        if (detail.Contains("m²"))
                                        {
                                            listingData["m² (Brüt)"] = detail;
                                        }
                                        else if (detail.Contains("Oda"))
                                        {
                                            listingData["Oda Sayısı"] = detail;
                                        }
                                        else if (detail.Contains("Kat"))
                                        {
                                            listingData["Bulunduğu Kat"] = detail;
                                        }
                                        else if (detail.Contains("Bina Yaşı"))
                                        {
                                            listingData["Bina Yaşı"] = detail;
                                        }
                                    }

                                    bool success = await firebaseService.SaveListingData(listingData);
                                    if (success)
                                    {
                                        listings.Add((title, price, location, details, sellerInfo, phoneNumber));
                                        UpdateStatus($"İlan kaydedildi: {currentListingId} - {title}");
                                    }
                                    else
                                    {
                                        UpdateStatus($"İlan kaydedilemedi: {currentListingId} - {title}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    UpdateStatus($"Telefon/satıcı bilgisi alınamadı: {ex.Message}");
                                }
                            }
                            catch (Exception ex)
                            {
                                UpdateStatus($"Bir ilan atlandı: {ex.Message}");
                                continue;
                            }
                        }

                        if (skippedListingsCount >= TARGET_LISTINGS_PER_URL)
                        {
                            if (!isFirstUrl)
                            {
                                if (url == SECOND_URL)
                                {
                                    UpdateStatus($"İkinci linkte hedef sayıya ulaşıldı. Üçüncü linke geçiliyor...");
                                    await Task.Run(() => StartCollecting(THIRD_URL));
                                }
                                else
                                {
                                    UpdateStatus($"Üçüncü linkte hedef sayıya ulaşıldı. Google'a gidiliyor ve 45 dakika bekleniyor...");
                                    driver.Navigate().GoToUrl(WAIT_URL);
                                    checkTimer.Interval = CHECK_INTERVAL;
                                    checkTimer.Start();
                                }
                            }
                            else
                            {
                                UpdateStatus($"İlk linkte hedef sayıya ulaşıldı. İkinci linke geçiliyor...");
                                isFirstUrl = false;
                                skippedListingsCount = 0;
                                await Task.Run(() => StartCollecting(SECOND_URL));
                            }
                            return;
                        }

                        try
                        {
                            var nextButton = driver.FindElement(By.CssSelector(".he-pagination__navigate-text--next:not(.he-pagination__navigate-text--disabled)"));
                            if (nextButton != null && nextButton.Displayed)
                            {
                                currentPage++;
                                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center', behavior: 'smooth'}); arguments[0].click();", nextButton);
                                
                                wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState") as string == "complete");
                                wait.Until(d => d.FindElements(By.CssSelector(".listing-item")).Count > 0);
                                
                                UpdateStatus($"Sonraki sayfaya geçiliyor... (Sayfa {currentPage}/{totalPages})");
                                retryCount = 0;
                            }
                            else
                            {
                                UpdateStatus("Son sayfaya ulaşıldı. Diğer linke geçiliyor...");
                                if (!isFirstUrl)
                                {
                                    if (url == SECOND_URL)
                                    {
                                        UpdateStatus($"İkinci linkte hedef sayıya ulaşıldı. Üçüncü linke geçiliyor...");
                                        await Task.Run(() => StartCollecting(THIRD_URL));
                                    }
                                    else
                                    {
                                        UpdateStatus($"Üçüncü linkte hedef sayıya ulaşıldı. Google'a gidiliyor ve 45 dakika bekleniyor...");
                                        driver.Navigate().GoToUrl(WAIT_URL);
                                        checkTimer.Interval = CHECK_INTERVAL;
                                        checkTimer.Start();
                                    }
                                }
                                else
                                {
                                    isFirstUrl = false;
                                    skippedListingsCount = 0;
                                    await Task.Run(() => StartCollecting(SECOND_URL));
                                }
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            UpdateStatus($"Sayfa geçişinde hata: {ex.Message}");
                            retryCount++;
                            
                            if (retryCount >= maxRetries)
                            {
                                UpdateStatus("Maksimum deneme sayısına ulaşıldı. Diğer linke geçiliyor...");
                                if (!isFirstUrl)
                                {
                                    if (url == SECOND_URL)
                                    {
                                        UpdateStatus($"İkinci linkte hedef sayıya ulaşıldı. Üçüncü linke geçiliyor...");
                                        await Task.Run(() => StartCollecting(THIRD_URL));
                                    }
                                    else
                                    {
                                        UpdateStatus($"Üçüncü linkte hedef sayıya ulaşıldı. Google'a gidiliyor ve 45 dakika bekleniyor...");
                                        driver.Navigate().GoToUrl(WAIT_URL);
                                        checkTimer.Interval = CHECK_INTERVAL;
                                        checkTimer.Start();
                                    }
                                }
                                else
                                {
                                    isFirstUrl = false;
                                    skippedListingsCount = 0;
                                    await Task.Run(() => StartCollecting(SECOND_URL));
                                }
                                return;
                            }
                            
                            Thread.Sleep(100);
                            driver.Navigate().Refresh();
                            Thread.Sleep(100);
                        }
                    }
                    catch (Exception ex)
                    {
                        UpdateStatus($"Sayfa yüklenirken hata oluştu: {ex.Message}");
                        retryCount++;
                        
                        if (retryCount >= maxRetries)
                        {
                            UpdateStatus("Maksimum deneme sayısına ulaşıldı. Diğer linke geçiliyor...");
                            if (!isFirstUrl)
                            {
                                if (url == SECOND_URL)
                                {
                                    UpdateStatus($"İkinci linkte hedef sayıya ulaşıldı. Üçüncü linke geçiliyor...");
                                    await Task.Run(() => StartCollecting(THIRD_URL));
                                }
                                else
                                {
                                    UpdateStatus($"Üçüncü linkte hedef sayıya ulaşıldı. Google'a gidiliyor ve 45 dakika bekleniyor...");
                                    driver.Navigate().GoToUrl(WAIT_URL);
                                    checkTimer.Interval = CHECK_INTERVAL;
                                    checkTimer.Start();
                                }
                            }
                            else
                            {
                                isFirstUrl = false;
                                skippedListingsCount = 0;
                                await Task.Run(() => StartCollecting(SECOND_URL));
                            }
                            return;
                        }
                        
                        Thread.Sleep(100);
                        driver.Navigate().Refresh();
                        Thread.Sleep(100);
                    }
                }

                if (!isCollecting)
                {
                    UpdateStatus("Veri toplama durduruldu.");
                }
                else if (skippedListingsCount >= TARGET_LISTINGS_PER_URL)
                {
                    if (!isFirstUrl)
                    {
                        if (url == SECOND_URL)
                        {
                            UpdateStatus($"İkinci linkte hedef sayıya ulaşıldı. Üçüncü linke geçiliyor...");
                            await Task.Run(() => StartCollecting(THIRD_URL));
                        }
                        else
                        {
                            UpdateStatus($"Üçüncü linkte hedef sayıya ulaşıldı. Google'a gidiliyor ve 45 dakika bekleniyor...");
                            driver.Navigate().GoToUrl(WAIT_URL);
                            checkTimer.Interval = CHECK_INTERVAL;
                            checkTimer.Start();
                        }
                    }
                    else
                    {
                        UpdateStatus($"İlk linkte hedef sayıya ulaşıldı. İkinci linke geçiliyor...");
                        isFirstUrl = false;
                        skippedListingsCount = 0;
                        await Task.Run(() => StartCollecting(SECOND_URL));
                    }
                }
                else
                {
                    UpdateStatus("Tüm veriler toplandı.");
                }

                this.Invoke((MethodInvoker)delegate
                {
                    saveButton.Enabled = true;
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veri toplama sırasında hata oluştu: {ex.Message}");
            }
        }

        private async void SaveButton_Click(object sender, EventArgs e)
        {
            try
            {
                int successCount = 0;
                int failCount = 0;

                foreach (var listing in listings)
                {
                    try
                    {
                        var listingData = new Dictionary<string, string>
                        {
                            { "Başlık", listing.Title },
                            { "Fiyat", listing.Price },
                            { "Konum", listing.Location },
                            { "Detaylar", listing.Details },
                            { "İlan Sahibi", listing.SellerInfo },
                            { "Telefon", listing.PhoneNumber },
                            { "Şehir", "Kocaeli" },
                            { "İlçe", "İzmit" },
                            { "İlan Tarihi", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
                            { "Yeni İlan", "Evet" }
                        };

                        var details = listing.Details.Split(',').Select(d => d.Trim()).ToList();
                        foreach (var detail in details)
                        {
                            if (detail.Contains("m²"))
                            {
                                listingData["m² (Brüt)"] = detail;
                            }
                            else if (detail.Contains("Oda"))
                            {
                                listingData["Oda Sayısı"] = detail;
                            }
                            else if (detail.Contains("Kat"))
                            {
                                listingData["Bulunduğu Kat"] = detail;
                            }
                            else if (detail.Contains("Bina Yaşı"))
                            {
                                listingData["Bina Yaşı"] = detail;
                            }
                        }

                        bool success = await firebaseService.SaveListingData(listingData);
                        if (success)
                        {
                            successCount++;
                            UpdateStatus($"İlan kaydedildi: {listing.Title}");
                        }
                        else
                        {
                            failCount++;
                            UpdateStatus($"İlan kaydedilemedi: {listing.Title}");
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        UpdateStatus($"İlan kaydedilirken hata oluştu: {ex.Message}");
                    }
                }

                MessageBox.Show($"Toplam {listings.Count} ilan işlendi:\nBaşarılı: {successCount}\nBaşarısız: {failCount}", "İşlem Tamamlandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                listings.Clear();
                saveButton.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Veriler kaydedilirken hata oluştu: " + ex.Message);
            }
        }

        private void StopButton_Click(object sender, EventArgs e)
        {
            isCollecting = false;
            checkTimer.Stop();
            if (driver != null)
            {
                try
                {
                    driver.Quit();
                    driver = null;
                }
                catch { }
            }
            ResetControls();
            UpdateStatus("İşlem durduruldu.");
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            try
            {
                driver?.Quit();
                chromeDriver = null;

                foreach (var process in System.Diagnostics.Process.GetProcessesByName("chromedriver"))
                {
                    try { process.Kill(); } catch { }
                }
            }
            catch { }
            finally
            {
                ResetControls();
            }
        }

        private void ResetControls()
        {
            openBrowserButton.Enabled = true;
            stopButton.Enabled = false;
            saveButton.Enabled = false;
            closeButton.Enabled = false;
            progressBar.Visible = false;
            statusLabel.Visible = false;
            isCollecting = false;
            listings.Clear();
        }

        private void UpdateStatus(string message)
        {
            if (statusLabel.InvokeRequired)
            {
                statusLabel.Invoke(new Action(() => statusLabel.Text = message));
            }
            else
            {
                statusLabel.Text = message;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            try
            {
                driver?.Quit();
                chromeDriver?.Quit();

                foreach (var process in System.Diagnostics.Process.GetProcessesByName("chromedriver"))
                {
                    try { process.Kill(); } catch { }
                }
            }
            catch { }
        }
    }
}
