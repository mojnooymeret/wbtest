using System.Collections.Generic;
using System.Data.SQLite;

namespace OzonWb
{
   public class Connect
   {
      public static SQLiteDataReader Query(string str)
      {
         SQLiteConnection SQLiteConnection = new SQLiteConnection("Data Source=|DataDirectory|marketcheck.db");
         SQLiteCommand SQLiteCommand = new SQLiteCommand(str, SQLiteConnection);
         try {
            SQLiteConnection.Open();
            SQLiteDataReader reader = SQLiteCommand.ExecuteReader();
            return reader;
         } catch { return null; }
      }

      public static void LoadUrl(List<Source> data)
      {
         try {
            data.Clear();
            SQLiteDataReader query = Query("select * from `Source`;");
            if (query != null) {
               while (query.Read()) {
                  data.Add(new Source(
                     query.GetValue(0).ToString(),
                     query.GetValue(1).ToString(),
                     query.GetValue(2).ToString()
                  ));
               }
            }
         } catch { }
      }
   }
}
