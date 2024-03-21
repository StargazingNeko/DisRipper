using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace DisEmoteRipper
{
    internal class DatabaseHandler
    {
        private SQLiteConnection _connection;
        private bool _disposed;

        public DatabaseHandler()
        {
            _disposed = false;
            _connection = new SQLiteConnection("Data source=data.db");
        }

        public async Task Write(params dynamic[] Values)
        {
            if (Values is null)
            {
                MessageBox.Show($"{this.GetType().Name}\nTable values were null, this is a bug please submit an issue on the github.");
                return;
            }

            if (_disposed) return;

            await _connection.OpenAsync();

            using (SQLiteCommand command = _connection.CreateCommand())
            {
                switch (Values.Count())
                {
                    case 3:
                        command.CommandText = $"INSERT INTO Guilds VALUES ({Values[0]},{Values[1]},{Values[2]})";
                        break;
                    case 8:
                        command.CommandText = $"INSERT INTO Emotes VALUES ({Values[0]},\"{Values[1]}\",{Values[2]},\"{Values[3]}\",\"{Values[4]}\",{Values[5]},@0,{Values[7]})";
                        SQLiteParameter parameter = new SQLiteParameter("@0", DbType.Binary);
                        parameter.Value = Values[6];
                        command.Parameters.Add(parameter);
                        break;
                }

                command.ExecuteNonQuery();
            }

            await _connection.CloseAsync();
        }

        public async Task<List<Structs.Img>> ReadEmotes(string TABLE)
        {
            if (_disposed) return null;

            using (SQLiteCommand command = _connection.CreateCommand())
            {
                await _connection.OpenAsync();
                command.CommandText = $"SELECT * FROM {TABLE}";

                using (var reader = command.ExecuteReader())
                {
                    List<Structs.Img> list = new();
                    Structs.Img img = new();
                    while (reader.Read())
                    {
                        img.GuildId = Convert.ToUInt64(reader.GetValue(0));
                        img.GuildName = reader.GetValue(1).ToString();
                        img.Id = Convert.ToUInt64(reader.GetValue(2));
                        img.Name = reader.GetValue(3).ToString();
                        img.Extension = reader.GetValue(4).ToString();
                        img.IsSticker = bool.Parse(reader.GetValue(5).ToString());
                        img.stream = reader.GetValue(6) as MemoryStream;
                        list.Add(img);
                    }
                    await _connection.CloseAsync();
                    return list;
                };
            }
        }

        public void Dispose()
        {
            _connection.Close();
            _connection.Dispose();
            _disposed = true;
        }
    }
}
