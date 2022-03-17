﻿using Newtonsoft.Json;
using StockAnalyzer.Core.Domain;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;

namespace StockAnalyzer.Windows
{
    public partial class MainWindow : Window
    {
        private static string API_URL = "https://ps-async.fekberg.com/api/stocks";
        private Stopwatch stopwatch = new Stopwatch();

        public MainWindow()
        {
            InitializeComponent();
        }



        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            BeforeLoadingStockData();

            try
            {

                var loadLinesTask = Task.Run(async () =>
                  {
                      var lines = new List<string>();

                      using (var stream = new StreamReader(File.OpenRead("./StockPrices_Small.csv"))) {
                          
                          string line;
                          while ((line = await stream.ReadLineAsync()) != null) {
                              lines.Add(line);
                          }
                      }

                      return lines;});

                loadLinesTask.ContinueWith((task) => {
                    Dispatcher.Invoke(() =>
                    {
                        Notes.Text = task.Exception.InnerException.Message;
                    });
                }, TaskContinuationOptions.OnlyOnFaulted);

                var processStocksTask = loadLinesTask.ContinueWith((completedTask) => {
                    var lines = completedTask.Result;
                    var data = new List<StockPrice>();

                    foreach (var line in lines.Skip(1))
                    {
                        var price = StockPrice.FromCSV(line);
                        data.Add(price);
                    };

                    //Queues code execution on the UI Thread
                    Dispatcher.Invoke(() =>
                    {
                        Stocks.ItemsSource = data.Where(sp => sp.Identifier.ToLower() == StockIdentifier.Text.ToLower());
                    });
                }, 
                                        TaskContinuationOptions.OnlyOnRanToCompletion
                );

                processStocksTask.ContinueWith(_ => {
                    Dispatcher.Invoke(() => { AfterLoadingStockData(); });
                });

            }
            catch (System.Exception ex)
            {
                Notes.Text = ex.Message;
            }
            finally
            {
                AfterLoadingStockData();
            }
        }

        private async Task<IEnumerable<StockPrice>> GetStocks()
        {
            using (var client = new HttpClient())
            {

                var responseTask = client.GetAsync($"{API_URL}/{StockIdentifier.Text}");
                var response = await responseTask;
                var content = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<IEnumerable<StockPrice>>(content);
                return data;
            }
        }


        private void BeforeLoadingStockData()
        {
            stopwatch.Restart();
            StockProgress.Visibility = Visibility.Visible;
            StockProgress.IsIndeterminate = true;
        }

        private void AfterLoadingStockData()
        {
            StocksStatus.Text = $"Loaded stocks for {StockIdentifier.Text} in {stopwatch.ElapsedMilliseconds}ms";
            StockProgress.Visibility = Visibility.Hidden;
        }

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));

            e.Handled = true;
        }

        private void Close_OnClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void StockIdentifier_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }
    }
}
