using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace DisRipper
{
    public class DatabaseHandler
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

            if (_disposed)
                return;

            await _connection.OpenAsync();

            using (SQLiteCommand command = _connection.CreateCommand())
            {
                switch (Values.Count())
                {
                    case 3:
                        command.CommandText = $"INSERT INTO Guilds VALUES ({Values[0]},{Values[1]},{Values[2]});";
                        break;
                    case 8:
                        command.CommandText = $"INSERT INTO Emotes VALUES ({Values[0]},\"{Values[1]}\",{Values[2]},\"{Values[3]}\",\"{Values[4]}\",{Values[5]},@0,{Values[7]});";
                        SQLiteParameter parameter = new SQLiteParameter("@0", DbType.Binary);
                        parameter.Value = Values[6];
                        command.Parameters.Add(parameter);
                        break;
                }

                await command.ExecuteNonQueryAsync();
            }

            await _connection.CloseAsync();
        }

        public async Task CreateConfigTable()
        {
            await _connection.OpenAsync();
            using (SQLiteCommand command = _connection.CreateCommand())
            {
                command.CommandText = $"CREATE TABLE IF NOT EXISTS \"Config\" (\"Option\" TEXT NOT NULL UNIQUE, \"Value\" DYNAMIC NOT NULL UNIQUE, PRIMARY KEY (\"Option\")); INSERT OR IGNORE INTO \"Config\" VALUES (\"bAutomaticallyRetrieveToken\", {false});";
                await command.ExecuteNonQueryAsync();
            }

            await _connection.CloseAsync();
        }

        public async Task UpdateConfigTable(string ConfigOption, dynamic Value)
        {
            await _connection.OpenAsync();
            using (SQLiteCommand command = _connection.CreateCommand())
            {
                int RowID = 0;
                switch (ConfigOption)
                {
                    case "bAutomaticallyRetrieveToken":
                        RowID = 1;
                        break;
                }

                if (RowID == 0)
                {
                    MessageBox.Show("Error updating config.");
                    return;
                }

                command.CommandText = $"UPDATE Config SET Value = {Value} WHERE ROWID = {RowID}";
                await command.ExecuteNonQueryAsync();
            }

            await _connection.CloseAsync();
        }

        public async Task<dynamic> GetConfigValue(string ConfigOption)
        {
            dynamic value = false;
            await _connection.OpenAsync();
            using (SQLiteCommand command = _connection.CreateCommand())
            {
                command.CommandText = $"SELECT Value FROM Config WHERE Option = '{ConfigOption}';";
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        value = reader.GetBoolean(0);
                    }
                }
            }

            await _connection.CloseAsync();
            return value;
        }

        public async Task CreateTable(string GuildName)
        {
            await _connection.OpenAsync();

            using(SQLiteCommand command = _connection.CreateCommand())
            {
                command.CommandText = $"CREATE TABLE IF NOT EXISTS \"{GuildName}\" (GuildId ULONG NOT NULL, GuildName TEXT NOT NULL, EmoteId ULONG NOT NULL UNIQUE, EmoteName TEXT NOT NULL, Extension TEXT NOT NULL, IsSticker BOOLEAN NOT NULL, MemStream BLOB NOT NULL, Exported BOOLEAN NOT NULL)";
                await command.ExecuteNonQueryAsync();
            }

            await _connection.CloseAsync();
        }

        public async Task<List<string>?> GetTables()
        {
            await _connection.OpenAsync();
            List<string> Tables = new List<string>();
            using (SQLiteCommand command = _connection.CreateCommand())
            {
                command.CommandText = "SELECT name FROM sqlite_schema WHERE type='table' AND name NOT LIKE 'sqlite_%'";
                DbDataReader reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        if (Utility.IsTokenCanceled())
                            return null;

                        Tables.Add(reader.GetString(i));
                    }
                }
            }
            await _connection.CloseAsync();
            return Tables;
        }

        public async Task WriteEmotes(ulong GuildId, string GuildName, ulong EmoteId, string EmoteName, string Extension, bool IsSticker, MemoryStream Stream)
        {
            await _connection.OpenAsync();

            using (SQLiteCommand command = _connection.CreateCommand())
            {
                command.CommandText = $"INSERT OR IGNORE INTO \"{GuildName}\" VALUES ({GuildId}, \"{GuildName}\", {EmoteId}, \"{EmoteName}\", \"{Extension}\", {IsSticker}, @0, {false})";
                SQLiteParameter parameter = new SQLiteParameter("@0", DbType.Binary);
                parameter.Value = Stream.ToArray();
                command.Parameters.Add(parameter);
                await command.ExecuteNonQueryAsync();
            }
            await _connection.CloseAsync();
        }

        public async Task<List<Structs.Img>?> ReadEmotes(string TABLE)
        {
            if (_disposed) return null;

            using (SQLiteCommand command = _connection.CreateCommand())
            {
                await _connection.OpenAsync();
                command.CommandText = $"SELECT * FROM '{TABLE}' WHERE NOT 'Config'";

                using (var reader = command.ExecuteReader())
                {
                    List<Structs.Img> list = new();
                    while (reader.Read())
                    {
                        Stream readStream = reader.GetStream(6);
                        MemoryStream ms = new MemoryStream(new byte[readStream.Length], true);
                        await readStream.CopyToAsync(ms);
                        list.Add(new Structs.Img().Create(Convert.ToUInt64(reader.GetValue(0)),
                            reader.GetValue(1).ToString(),
                            Convert.ToUInt64(reader.GetValue(2)),
                            reader.GetValue(3).ToString(),
                            reader.GetValue(4).ToString(),
                            bool.Parse(reader.GetValue(5).ToString()),
                            ms));
                    }

                    await _connection.CloseAsync();
                    return list;
                };
            }
        }

        public async Task<Structs.Img?> GetSingularEmote(string GuildName, ulong EmoteID)
        {
            if (_disposed) return null;

            Structs.Img? Emote = null;
            await using (SQLiteCommand command = _connection.CreateCommand())
            {
                await _connection.OpenAsync();
                command.CommandText = $"SELECT * FROM '{GuildName}' WHERE EmoteId = '{EmoteID}'";

                await using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        Stream readStream = reader.GetStream(6);
                        MemoryStream ms = new MemoryStream(new byte[readStream.Length], true);
                        await readStream.CopyToAsync(ms);
                        ms.Position = 0;
                        Emote = new Structs.Img().Create(Convert.ToUInt64(reader.GetValue(0)),
                            reader.GetValue(1).ToString(),
                            Convert.ToUInt64(reader.GetValue(2)),
                            reader.GetValue(3).ToString(),
                            reader.GetValue(4).ToString(),
                            bool.Parse(reader.GetValue(5).ToString()),
                            ms);
                    }
                }
            }

            await _connection.CloseAsync();
            return Emote;
        }

        public void Dispose()
        {
            try
            {
                _connection.Close();
                _connection.Dispose();
            }
            catch(Exception ex)
            {

            }
            _disposed = true;
        }
    }
}