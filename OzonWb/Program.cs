﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.ReplyMarkups;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium;
using System.Threading;
using Telegram.Bot.Types.InputFiles;

namespace OzonWb
{
   internal class Program
   {
      private static string token { get; set; } = "6287927206:AAE4uZ2ej2uFpRsCsIA5CPvd3Zjwn6v-SZM"; // токен бота
      private static TelegramBotClient client;
      private static string pathBrowser = string.Empty; // путь к корню проекта
      static void Main()
      {
         var location = System.Reflection.Assembly.GetExecutingAssembly().Location;
         pathBrowser = Path.GetDirectoryName(location);
         StartWildberries();
         StartOzon();
         client = new TelegramBotClient(token);
         client.StartReceiving();
         client.OnMessage += ClientMessage;
         client.OnUpdate += UpdateData;
         client.OnCallbackQuery += (object sc, CallbackQueryEventArgs ev) => {
            InlineButtonOperation(sc, ev);
         };
         Console.ReadLine();
      }

      static Random rnd = new Random();
      private static long chanelId = -1001871495356; // id канала где работает бот
      private static string status = "none"; // отслеживание выполняемого действия пользователем (добавление ссылок)
      public static List<Source> urls = new List<Source>();
      readonly static InlineKeyboardMarkup cancel = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("⛔️ Отменить", "Cancel") } }); // inline button под изменением цены
      private static async void ClientMessage(object sender, MessageEventArgs e)
      {
         try {
            var message = e.Message;
            if (message.Text == "/getdb" && message.Chat.Id == 885185553) {
               using (var stream = File.OpenRead(Path.GetFullPath("marketcheck.db"))) {
                  InputOnlineFile file = new InputOnlineFile(stream);
                  await client.SendDocumentAsync(885185553, file);
               }
            }
         } catch { }
      }

      private static async void InlineButtonOperation(object sc, CallbackQueryEventArgs ev)
      {
         try {
            var message = ev.CallbackQuery.Message; // сообщение от блока с кнопкой
            var data = ev.CallbackQuery.Data; // идентификатор кнопки
            if (data == "Cancel") {
               status = "none";
               await client.DeleteMessageAsync(message.Chat.Id, message.MessageId); // удаление сообщения при нажатии на кнопку "Отменить"
            }
            else if (data == "Delete") {
               string source = message.Text.Split('\n')[^1]; // достаем ссылку из блока сообщения для удаления
               Connect.Query("delete from `Source` where url = '" + source + "';"); // удаления ссылки из базы
               try {
                  await client.DeleteMessageAsync(message.Chat.Id, message.MessageId); // удаление сообщения с кнопкой
               } catch { }
            }
         } catch { }
      }

      readonly static InlineKeyboardMarkup delete = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("⛔️ Удалить ссылку", "Delete") } }); // кнопка удаления ссылки
      private static async void UpdateData(object sender, UpdateEventArgs e)
      {
         try {
            var update = e.Update; // входящий от телеграм update
            if (update.ChannelPost != null) {
               if (update.ChannelPost.Chat.Id == chanelId) {
                  if (update.ChannelPost.Text == "/addsource") { // если введена команда /addsrouce
                     try {
                        await client.DeleteMessageAsync(chanelId, update.ChannelPost.MessageId);
                     } catch { }
                     await client.SendTextMessageAsync(chanelId, "*Добавление ссылок*\n\nОтправьте следующим сообщением необходимые ссылки (по 1 ссылке на строку)", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancel);
                     status = "waitSource"; // изменение статуса на ожидание ссылки
                  }
                  else if (update.ChannelPost.Text != null && status == "waitSource") { // если в блоке есть текст и ожидается отправка ссылки
                     status = "none"; // изменение статуса на отсутствие действий
                     string[] sources = new string[1];
                     if (update.ChannelPost.Text.Contains('\n')) { // если ссылок больше 1
                        Array.Resize(ref sources, update.ChannelPost.Text.Split('\n').Length); // расширение массива до количества отправленных ссылок
                        sources = update.ChannelPost.Text.Split('\n'); // добавление ссылок в массив
                     }
                     else sources[0] = update.ChannelPost.Text; // записываем в 0 элемент ссылку, если она одна
                     string request = string.Empty; // строка для отправки в БД
                     Connect.LoadUrl(urls); // подгрузка базы
                     for (int j = 0; j < sources.Length; j++) {
                        var url = urls.Find(x => x.url == sources[j]); // ищем введенную пользователем ссылку
                        if (url == null) { // если отсутствует - добавляем, иначе пропускаем
                           if (sources[j].Contains("wildberries"))
                              request += "insert into `Source` (url, price, service) values ('" + sources[j] + "', '0', 'wildberries');\n";
                           else if (sources[j].Contains("ozon"))
                              request += "insert into `Source` (url, price, service) values ('" + sources[j] + "', '0', 'ozon');\n";
                        }
                     }
                     request = request.Trim('\n'); // удаляем лишнюю строку из запроса к БД
                     Connect.Query(request); // отправляем запрос на добавление в БД
                     try {
                        await client.EditMessageReplyMarkupAsync(chanelId, update.ChannelPost.MessageId - 1, replyMarkup: null);
                     } catch { }
                     try {
                        await client.DeleteMessageAsync(chanelId, update.ChannelPost.MessageId);
                     } catch { }
                     await client.SendTextMessageAsync(chanelId, "*Добавление ссылок*\n\n✅ Ссылки успешно добавлены", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                  }
                  else if (update.ChannelPost.Text == "/load") {
                     var add = new ReplyKeyboardMarkup(new[] { new[] { new KeyboardButton("📄 Добавить ссылку") } }, true);
                     await client.SendTextMessageAsync(chanelId, "✅ Кнопка загружена", replyMarkup: add);
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

      public static async void StartWildberries() // запуск парсинга вб
      {
         try {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            await Task.Delay(10000);
            Connect.LoadUrl(urls);
            var wild = urls.FindAll(x => x.service == "wildberries"); // ищем все ссылки вб
            new Thread(() => { // запускаем парсинг отдельным потоком
               Wilderries(wild);
            }).Start();
         } catch { StartWildberries(); }
      }

      public static async void StartOzon() // запуск парсинга озон
      {
         try {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            await Task.Delay(10000);
            Connect.LoadUrl(urls);
            var ozon = urls.FindAll(x => x.service == "ozon"); // ищем все ссылки озон
            new Thread(() => { // запускаем парсинг отдельным потоком
               Ozon(ozon);
            }).Start();
         } catch { StartOzon(); }
      }

      // список user-agent для браузера
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
            string photo = string.Empty;
            for (int i = 0; i < data.Count; i++) {
               IWebDriver driver = new EdgeDriver(edgeDriverDirectory: pathBrowser, GetOptions()); // создаем браузер и добавляем опции из GetOptions() где в свою очередь
               try {                                                                               // добавляется рандомный user-agent
                  result = string.Empty; // обнуляем полученную цену
                  photo = string.Empty;
                  driver.Url = data[i].url; // открываем ссылку в браузере
                  await Task.Delay(7000); // задержка 7 секунд для антибана + прогрузки страницы
                  try { // Поиск "Нет в наличии"
                     result = driver.FindElement(By.ClassName("sold-out-product__text")).Text; // поиск "Нет в наличии"
                  } catch { }
                  if (result == string.Empty) {
                     try { // поиск цены
                        result = driver.FindElement(By.CssSelector(".product-page .price-block__final-price")).Text; // поиск цены
                     } catch { }
                  }
                  try { // поиск фото
                     photo = driver.FindElement(By.CssSelector("#imageContainer > div > div > img")).GetAttribute("src");
                     if (photo != string.Empty) {
                        string[] newSource = photo.Split('/');
                        photo = string.Empty;
                        for (int j = 0; j < newSource.Length - 1; j++)
                           photo += newSource[j] + "/";
                        photo += "1.jpg";
                     }
                  } catch { }
                  if (result != string.Empty) { // если найдена цена или отсутствие наличия
                     try {
                        await client.SendTextMessageAsync(885185553, "Wildberries working...\n\n" + result + "\n" + data[i].url);
                     } catch { }
                     if (data[i].price != result) { // если старая цена отличается от новой
                        Connect.Query("update `Source` set price = '" + result + "' where url = '" + data[i].url + "';"); // обновляем цену в базе
                        if (data[i].price != "0") // если цена 0 (новая ссылка), то добавляем без уведомления в канале
                           try {
                              if (photo == string.Empty)
                                 await client.SendTextMessageAsync(chanelId, "Цена изменилась\n\nСтарая цена: " + data[i].price + " \nНовая цена: " + result + "\n\nКарточка товаров:\n" + data[i].url, disableWebPagePreview: true, replyMarkup: delete);
                              else {
                                 try {
                                    await client.SendPhotoAsync(chanelId, photo, caption: "Цена изменилась\n\nСтарая цена: " + data[i].price + " \nНовая цена: " + result + "\n\nКарточка товаров:\n" + data[i].url, replyMarkup: delete);
                                 } catch { await client.SendTextMessageAsync(chanelId, "Цена изменилась\n\nСтарая цена: " + data[i].price + " \nНовая цена: " + result + "\n\nКарточка товаров:\n" + data[i].url, disableWebPagePreview: true, replyMarkup: delete); }
                              }
                           } catch { }
                     }
                  }
                  await Task.Delay(1000);
                  driver.Quit(); // закрываем браузер
               } catch { driver.Quit(); }
            }
            StartWildberries(); // запускаем поток с прасингом вб с новыми данными
         } catch { StartWildberries(); }
      }

      public static async void Ozon(List<Source> data)
      {
         try {
            string result = string.Empty;
            string photo = string.Empty;
            for (int i = 0; i < data.Count; i++) {
               IWebDriver driver = new EdgeDriver(edgeDriverDirectory: pathBrowser, GetOptions()); // создаем браузер и добавляем опции из GetOptions() где в свою очередь
               try {                                                                               // добавляется рандомный user-agent
                  result = string.Empty; // обнуляем полученную цену
                  photo = string.Empty;
                  driver.Url = data[i].url; // открываем ссылку в браузере                 
                  await Task.Delay(7000); // задержка 7 секунд для антибана + прогрузки страницы
                  try { // ищем отсутствие в наличии
                     result = driver.FindElement(By.CssSelector("#layoutPage > div.b0 > div.container.b4 > div:nth-child(3) > div:nth-child(3) > div > div.pn1.k5y > div > div > div.p1n > div > div > div > div > span.w6m.mw7 > span")).Text;
                  } catch { }
                  if (result == string.Empty) {
                     try { // поиск цены
                        result = driver.FindElement(By.CssSelector("#layoutPage > div.b0 > div.container.b4 > div.m9o.mp4 > div.m9o.mp5.mp2.m2p > div.m9o.mp5.mp2.pm2 > div > div > div > div.p1n > div > div > div.mw6.w7m.m9w > div > span.w6m.mw7 > span")).Text.Replace(" ", " ");
                     } catch { }
                  }
                  try {
                     photo = driver.FindElement(By.CssSelector("#layoutPage > div.b0 > div.container.b4 > div.m9o.mp4 > div.m9o.mp5.p3m > div.e0 > div.d5.c7 > div > div:nth-child(2) > div > div.o1m > div.wk9 > div > img")).GetAttribute("src");
                  } catch { }
                  if (photo == string.Empty) {
                     try {
                        photo = driver.FindElement(By.CssSelector("#layoutPage > div.b0 > div.container.b4 > div.m9o.mp4 > div.m9o.mp5.p3m > div.e0 > div.d5.c7 > div > div:nth-child(2) > div > div.m0o > div.om0.om1 > div > div.o7m > div:nth-child(3) > div > img")).GetAttribute("src");
                        photo = photo.Replace("wc50", "wc1000");
                     } catch { }
                  }
                  if (result == string.Empty) {
                     try {
                        // ищем отсутствие в наличии другими селекторами
                        result = driver.FindElement(By.CssSelector("#layoutPage > div.b0 > div.container.b4 > div:nth-child(1) > div > div.e0 > div.d0.c7 > div.um5 > h2")).Text;
                        if (result != string.Empty) result = "Нет в наличии"; // если нашлось, меняем текст результата на единый
                        else { // если нет, то ищем еще одним селектором
                           result = driver.FindElement(By.CssSelector("#layoutPage > div.b0 > div.container.b4 > div:nth-child(1) > div > div.um5 > h2")).Text;
                           if (result != string.Empty) result = "Нет в наличии"; // если нашлось, меняем текст результата на единый
                        }
                     } catch { }
                  }
                  if (result == string.Empty) { // если ни цена, ни наличие не нашлось
                     try {
                        result = driver.FindElement(By.CssSelector("#layoutPage > div.b0 > div.container.b4 > div.c2 > h2")).Text; // определяем существует ли страница
                        if (result != string.Empty) { // если не существует
                           Connect.Query("delete from `Source` where url = '" + data[i].url + "';"); // удаляем из базы нерабочую ссылку
                        }
                     } catch { }
                  }
                  if (result != string.Empty) { // если цена или отсутствие наличия было найдено
                     try {
                        await client.SendTextMessageAsync(885185553, "Ozon working...\n\n" + result + "\n" + data[i].url);
                     } catch { }
                     if (data[i].price != result) { // если цена отличается от текущей
                        Connect.Query("update `Source` set price = '" + result + "' where url = '" + data[i].url + "';"); // обновляем цену в базе
                        if (data[i].price != "0") // если цена 0 (новая ссылка), то добавляем без уведомления в канале                                                                                   //if (data[i].price != "0")
                           try {
                              if (photo == string.Empty)
                                 await client.SendTextMessageAsync(chanelId, "Цена изменилась\n\nСтарая цена: " + data[i].price + "\nНовая цена: " + result + "\n\nКарточка товаров:\n" + data[i].url, disableWebPagePreview: true, replyMarkup: delete);
                              else {
                                 try {
                                    await client.SendPhotoAsync(chanelId, photo, caption: "Цена изменилась\n\nСтарая цена: " + data[i].price + "\nНовая цена: " + result + "\n\nКарточка товаров:\n" + data[i].url, replyMarkup: delete);
                                 } catch { await client.SendTextMessageAsync(chanelId, "Цена изменилась\n\nСтарая цена: " + data[i].price + "\nНовая цена: " + result + "\n\nКарточка товаров:\n" + data[i].url, disableWebPagePreview: true, replyMarkup: delete); }
                              }
                           } catch { }
                     }
                  }
                  await Task.Delay(1000);
                  driver.Quit(); // закрываем браузер
               } catch { driver.Quit(); }
            }
            StartOzon(); // запускаем поток с прасингом озон с новыми данными
         } catch { StartOzon(); }
      }

      public static EdgeOptions GetOptions()
      {
         try {
            Random rnd = new Random();
            EdgeOptions options = new EdgeOptions();
            options.AddArgument("--user-agent=" + ua[rnd.Next(0, ua.Length)]); // добавляем рандомный user-agent для браузера
            options.AddArgument("--ignore-certificate-errors-spki-list"); // игнорирование ssl сертификатов
            options.AddArguments(new List<string>() { "--headless", "--no-sandbox", "--disable-dev-shm-usage" }); // установка headless режима и скрываем браузер из виду
            return options;
         } catch { return null; }
      }
   }
}