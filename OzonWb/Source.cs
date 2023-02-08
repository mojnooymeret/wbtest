namespace OzonWb
{
   public class Source
   {
      public string url { get; set; }
      public string price { get; set; }
      public string service { get; set; }
      public Source(string url, string price, string service)
      {
         this.url = url;
         this.price = price;
         this.service = service;
      }
   }
}
