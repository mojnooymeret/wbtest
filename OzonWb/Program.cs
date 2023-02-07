using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.ReplyMarkups;

namespace OzonWb
{
   internal class Program
   {
      private static string token { get; set; } = "6123367281:AAFrPTXtsRggDpUdP4j-7rzR40YL0FfUGEU";
      private static TelegramBotClient client;
      static void Main()
      {
         client = new TelegramBotClient(token);
         client.StartReceiving();
         test();
         client.OnMessage += ClientMessage;
         client.OnUpdate += UpdateData;
         client.OnCallbackQuery += (object sc, CallbackQueryEventArgs ev) => {
            InlineButtonOperation(sc, ev);
         };
         Console.ReadLine();
      }

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
               if (update.ChannelPost.Chat.Id == -1001753358180) {
                  if (update.ChannelPost.Text == "/addsource") {
                     try {
                        await client.DeleteMessageAsync(-1001753358180, update.ChannelPost.MessageId);
                     } catch { }
                     await client.SendTextMessageAsync(-1001753358180, "*Добавление ссылок*\n\nОтправьте следующим сообщением необходимые ссылки (по 1 ссылке на строку)", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancel);
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
                        if (url == null)
                           request += "insert into `Source` (url) values ('" + sources[j] + "');\n";
                     }
                     request = request.Trim('\n');
                     Connect.Query(request);
                     try {
                        await client.EditMessageReplyMarkupAsync(-1001753358180, update.ChannelPost.MessageId - 1, replyMarkup: null);
                     } catch { }
                     try {
                        await client.DeleteMessageAsync(-1001753358180, update.ChannelPost.MessageId);
                     } catch { }
                     await client.SendTextMessageAsync(-1001753358180, "*Добавление ссылок*\n\n✅ Ссылки успешно добавлены", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                  }
                  else {
                     try {
                        await client.DeleteMessageAsync(-1001753358180, update.ChannelPost.MessageId);
                     } catch { }
                  }
               }
            }
         } catch { }
      }

      public static async void test()
      {
         EdgeOptions options = new EdgeOptions();
         options.AddArguments(new List<string>() { "--headless", "--no-sandbox", "--disable-dev-shm-usage" });

         IWebDriver driver = new EdgeDriver(edgeDriverDirectory: "/usr/local/share/msedgedriver", options);
         driver.Url = "https://www.wildberries.ru/catalog/99544579/detail.aspx?targetUrl=BP";
         await Task.Delay(3000);
         string result = driver.FindElement(By.CssSelector(".product-page .price-block__final-price")).Text;
         await client.SendTextMessageAsync(885185553, result);
      }

      //private static string Client(string downloadString)
      //{
      //   //try {
      //   //   HtmlWeb web = new HtmlWeb();
      //   //   web.OverrideEncoding = Encoding.UTF8;
      //   //   web.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/53.0.2785.116 Safari/537.36";
      //   //   HtmlDocument html = new HtmlDocument();
      //   //   html = web.Load("https://www.wildberries.ru/catalog/99544579/detail.aspx?targetUrl=BP");
      //   //   return html.ParsedText;
      //   //} catch { return null; }
      //}
   }
}
