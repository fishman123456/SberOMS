using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace SberOmsRates
{
    public partial class MainWindow : Window
    {
        private static readonly HttpClient httpClient = new HttpClient();

        // JSON, которые использует rub24
        private const string SilverApiUrl = "https://rub24.com/an/kot-arg-sber.php";   // серебро
        private const string GoldApiUrl = "https://rub24.com/an/kot-gold-sber.php";  // золото

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateButton.IsEnabled = false;
            ProgressBar.Visibility = Visibility.Visible;

            try
            {
                // параллельно тянем золото и серебро
                var goldTask = GetQuotesAsync(GoldApiUrl);
                var silverTask = GetQuotesAsync(SilverApiUrl);

                var goldQuotes = await goldTask;
                var silverQuotes = await silverTask;

                var goldBuy = FindBySymbol(goldQuotes, "POKUP");
                var goldSell = FindBySymbol(goldQuotes, "PROD");
                var silvBuy = FindBySymbol(silverQuotes, "POKUP");
                var silvSell = FindBySymbol(silverQuotes, "PROD");

                GoldBuyLabel.Text = $"Покупка: {FormatPrice(goldBuy?.VAL1)} ₽/г";
                GoldSellLabel.Text = $"Продажа: {FormatPrice(goldSell?.VAL1)} ₽/г";
                SilverBuyLabel.Text = $"Покупка: {FormatPrice(silvBuy?.VAL1)} ₽/г";
                SilverSellLabel.Text = $"Продажа: {FormatPrice(silvSell?.VAL1)} ₽/г";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ProgressBar.Visibility = Visibility.Collapsed;
                UpdateButton.IsEnabled = true;
            }
        }

        // ---------- работа с JSON ----------

        private async Task<List<QuoteRow>> GetQuotesAsync(string url)
        {
            string json = await httpClient.GetStringAsync(url);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var rows = JsonSerializer.Deserialize<List<QuoteRow>>(json, options);
            return rows ?? new List<QuoteRow>();
        }

        private QuoteRow FindBySymbol(IEnumerable<QuoteRow> rows, string symbol)
        {
            foreach (var r in rows)
            {
                if (r == null) continue;

                if (string.Equals(r.SYMBOL, symbol, StringComparison.OrdinalIgnoreCase))
                    return r;
            }

            return null;
        }

        private string FormatPrice(string val)
        {
            if (string.IsNullOrWhiteSpace(val))
                return "--";

            val = val.Trim()
                     .Replace(" ", "")
                     .Replace(",", ".");

            return val;
        }
    }

    // структура JSON: [{"SYMBOL":"POKUP","DATE":"02-03-2026","VAL1":"213.77","VAL2":"211.34"}, ...]
    public class QuoteRow
    {
        public string SYMBOL { get; set; }  // POKUP / PROD
        public string DATE { get; set; }
        public string VAL1 { get; set; }  // текущий курс
        public string VAL2 { get; set; }  // предыдущий курс
    }
}
