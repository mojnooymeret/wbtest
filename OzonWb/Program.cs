using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.ReplyMarkups;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium;
using System.Linq;
using System.Threading;

namespace OzonWb
{
   internal class Program
   {
      private static string token { get; set; } = "6123367281:AAFrPTXtsRggDpUdP4j-7rzR40YL0FfUGEU";
      private static TelegramBotClient client;
      private static string pathBrowser = string.Empty;
      static void Main()
      {
         var location = System.Reflection.Assembly.GetExecutingAssembly().Location;
         pathBrowser = Path.GetDirectoryName(location);
         Connect.LoadUrl(urls);
         test();
         client = new TelegramBotClient(token);
         client.StartReceiving();
         client.OnMessage += ClientMessage;
         client.OnUpdate += UpdateData;
         client.OnCallbackQuery += (object sc, CallbackQueryEventArgs ev) => {
            InlineButtonOperation(sc, ev);
         };
         Console.ReadLine();
      }

      private static long chanelId = -1001753358180;
      private static string status = "none";
      public static List<Source> urls = new List<Source>();
      readonly static InlineKeyboardMarkup cancel = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("⛔️ Отменить", "Cancel") } });

      private static async void ClientMessage(object sender, MessageEventArgs e)
      {
         try {
            var message = e.Message;
            try {
               await client.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId - 1);
            } catch { }
         } catch { }
      }

      private static async void InlineButtonOperation(object sc, CallbackQueryEventArgs ev)
      {
         try {
            var message = ev.CallbackQuery.Message;
            var data = ev.CallbackQuery.Data;
            if (data == "Cancel") {
               status = "none";
               await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
            }
         } catch { }
      }

      private static async void UpdateData(object sender, UpdateEventArgs e)
      {
         try {
            var update = e.Update;
            if (update.ChannelPost != null) {
               if (update.ChannelPost.Chat.Id == chanelId) {
                  if (update.ChannelPost.Text == "/addsource") {
                     try {
                        await client.DeleteMessageAsync(chanelId, update.ChannelPost.MessageId);
                     } catch { }
                     await client.SendTextMessageAsync(chanelId, "*Добавление ссылок*\n\nОтправьте следующим сообщением необходимые ссылки (по 1 ссылке на строку)", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancel);
                     status = "waitSource";
                  }
                  else if (update.ChannelPost.Text != null && status == "waitSource") {
                     status = "none";
                     string[] sources = new string[1];
                     if (update.ChannelPost.Text.Contains('\n')) {
                        Array.Resize(ref sources, update.ChannelPost.Text.Split('\n').Length);
                        sources = update.ChannelPost.Text.Split('\n');
                     }
                     else sources[0] = update.ChannelPost.Text;
                     string request = string.Empty;
                     Connect.LoadUrl(urls);
                     for (int j = 0; j < sources.Length; j++) {
                        var url = urls.Find(x => x.url == sources[j]);
                        if (url == null) {
                           if (sources[j].Contains("wildberries"))
                              request += "insert into `Source` (url, price, service) values ('" + sources[j] + "', '0', 'wildberries');\n";
                           else if (sources[j].Contains("ozon"))
                              request += "insert into `Source` (url, price, service) values ('" + sources[j] + "', '0', 'ozon');\n";
                        }
                     }
                     request = request.Trim('\n');
                     Connect.Query(request);
                     try {
                        await client.EditMessageReplyMarkupAsync(chanelId, update.ChannelPost.MessageId - 1, replyMarkup: null);
                     } catch { }
                     try {
                        await client.DeleteMessageAsync(chanelId, update.ChannelPost.MessageId);
                     } catch { }
                     await client.SendTextMessageAsync(chanelId, "*Добавление ссылок*\n\n✅ Ссылки успешно добавлены", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                  }
                  else {
                     try {
                        await client.DeleteMessageAsync(chanelId, update.ChannelPost.MessageId);
                     } catch { }
                  }
               }
            }
         } catch { }
      }


      public static async void test()
      {
         Random random = new Random();
         Connect.LoadUrl(urls);
         var wild = urls.FindAll(x => x.service == "wildberries");
         var ozon = urls.FindAll(x => x.service == "ozon");
         new Thread(() => {
            Wilderries(wild);
         }).Start();
         new Thread(() => {
            Ozon(ozon);
         }).Start();
      }

      static string[] ua = {"Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.99 Safari/537.36",
                 "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.99 Safari/537.36",
                 "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.99 Safari/537.36",
                 "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_12_1) AppleWebKit/602.2.14 (KHTML, like Gecko) Version/10.0.1 Safari/602.2.14",
                 "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.71 Safari/537.36",
                 "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_12_1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.98 Safari/537.36",
                 "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_11_6) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.98 Safari/537.36",
                 "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.71 Safari/537.36",
                 "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.99 Safari/537.36",
                 "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:50.0) Gecko/20100101 Firefox/50.0" };

      public static async void Wilderries(List<Source> data)
      {
         try {
            string result = string.Empty;
            for (int i = 0; i < data.Count; i++) {
               IWebDriver driver = new EdgeDriver(edgeDriverDirectory: pathBrowser, GetOptions());
               try {
                  result = string.Empty;
                  driver.Url = data[i].url;
                  await Task.Delay(8000);
                  try {
                     result = driver.FindElement(By.CssSelector(".product-page .price-block__final-price")).Text;
                  } catch { }
                  if (result == string.Empty) {
                     try {
                        result = driver.FindElement(By.ClassName("sold-out-product__text")).Text;
                     } catch { }
                  }
                  if (result != string.Empty) {
                     if (data[i].price != result) {
                        Connect.Query("update `Source` set price = '" + result + "' where url = '" + data[i].url + "';");
                        //if (data[i].price != "0")
                        try {
                           await client.SendTextMessageAsync(chanelId, "Цена изменилась\n\nСтарая цена: " + data[i].price + " \nНовая цена: " + result + "\n\nКарточка товаров:\n" + data[i].url, disableWebPagePreview: true);
                        } catch { }
                     }
                  }
                  await Task.Delay(1000);
                  driver.Quit();
               } catch { driver.Quit(); }
            }
         } catch { }
      }

      public static async void Ozon(List<Source> data)
      {
         try {
            string result = string.Empty;
            for (int i = 0; i < data.Count; i++) {
               IWebDriver driver = new EdgeDriver(edgeDriverDirectory: pathBrowser, GetOptions());
               try {
                  result = string.Empty;
                  driver.Url = data[i].url;
                  await Task.Delay(8000);
                  try {
                     result = driver.FindElement(By.CssSelector("#layoutPage > div.b0 > div.container.b4 > div.m9o.mp4 > div.m9o.mp5.mp2.m2p > div.m9o.mp5.mp2.pm2 > div > div > div > div.p1n > div > div > div.mw6.w7m.m9w > div > span.w6m.mw7 > span")).Text.Replace(" ", " ");
                  } catch { }
                  if (result == string.Empty) {
                     try {
                        result = driver.FindElement(By.CssSelector("#layoutPage > div.b0 > div.container.b4 > div:nth-child(3) > div:nth-child(3) > div > div.pn1.k5y > div > div > div.p1n > div > div > div > div > span.w6m.mw7 > span")).Text;
                     } catch { }
                  }
                  if (result == string.Empty) {
                     try {
                        result = driver.FindElement(By.CssSelector("#layoutPage > div.b0 > div.container.b4 > div:nth-child(1) > div > div.e0 > div.d0.c7 > div.um5 > h2")).Text;
                        if (result != string.Empty) result = "Нет в наличии";
                        else {
                           result = driver.FindElement(By.CssSelector("#layoutPage > div.b0 > div.container.b4 > div:nth-child(1) > div > div.um5 > h2")).Text;
                           if (result != string.Empty) result = "Нет в наличии";
                        }
                     } catch { }
                  }
                  if (result == string.Empty) {
                     try {
                        result = driver.FindElement(By.CssSelector("#layoutPage > div.b0 > div.container.b4 > div.c2 > h2")).Text;
                        if (result != string.Empty) {
                           Connect.Query("delete from `Source` where url = '" + data[i].url + "';");
                        }
                     } catch { }
                  }
                  if (result != string.Empty) {
                     if (data[i].price != result) {
                        Connect.Query("update `Source` set price = '" + result + "' where url = '" + data[i].url + "';");
                        //if (data[i].price != "0")
                        try {
                           await client.SendTextMessageAsync(chanelId, "Цена изменилась\n\nСтарая цена: " + data[i].price + "\nНовая цена: " + result + "\n\nКарточка товаров:\n" + data[i].url, disableWebPagePreview: true);
                        } catch (Exception ex) { }
                     }
                  }
                  await Task.Delay(1000);
                  driver.Quit();
               } catch { driver.Quit(); }
            }
         } catch { }
      }

      public static void Log(string error)
      {
         try {
            string text = File.ReadAllText(Path.GetFullPath("error.log"));
            text += "\n\n" + error;
            File.WriteAllText(Path.GetFullPath("error.log"), text);
         } catch { }
      }

      public static EdgeOptions GetOptions()
      {
         try {
            Random rnd = new Random();
            EdgeOptions options = new EdgeOptions();
            options.AddArgument("--user-agent=" + ua[rnd.Next(0, ua.Length)]);
            options.AddArgument("--ignore-certificate-errors-spki-list");
            options.AddArguments(new List<string>() { "--headless", "--no-sandbox", "--disable-dev-shm-usage" });
            return options;
         } catch { return null; }
      }
   }
}
