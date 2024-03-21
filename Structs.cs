using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DisEmoteRipper
{
    public class Structs
    {
        public struct Img
        {
            public string GuildName { get; set; }
            public ulong GuildId { get; set; }
            public string Name { get; set; }
            public ulong Id { get; set; }
            public string Extension { get; set; }
            public bool IsSticker { get; set; }
            public MemoryStream stream { get; set; }
        }

        public struct Discord
        {
            public Discord()
            {
                Users = "/users/";
                Self = Users + "@me";
                Guilds = Self + "/guilds";
                Guild = "/guilds/";
            }

            public string Users { get; init; }
            public string Self { get; init; }
            public string Guilds { get; init; }
            public string Guild { get; init; }
        }

        public struct GuildInfo
        {

            public ulong ID { get; set; }
            public string Name { get; set; }
            public readonly string Get { get { return $"{ID}: {Name},"; } }

            public GuildInfo Add(ulong id, string name)
            {
                ID = id;
                Name = name;

                return this;
            }
        }
    }
}
