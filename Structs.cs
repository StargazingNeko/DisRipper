using System.DirectoryServices;
using System.Drawing.Printing;
using System.IO;

namespace DisRipper
{
    public class Structs
    {
        public struct Img
        {
            public ulong GuildId { readonly get; private set; }
            public string GuildName { readonly get; private set; }
            public ulong EmoteId { readonly get; private set; }
            public string EmoteName { readonly get; private set; }
            public string Extension { readonly get; private set; }
            public bool IsSticker { readonly get; private set; }
            public MemoryStream Stream { readonly get; private set; }

            public Img Create(ulong GuildId, string GuildName, ulong EmoteId, string EmoteName, string Extension, bool IsSticker, MemoryStream Stream) =>
                new() { GuildId = GuildId, GuildName = GuildName, EmoteId = EmoteId, EmoteName = EmoteName, Extension = Extension, IsSticker = IsSticker, Stream = Stream };
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

            public readonly string Users { get; init; }
            public readonly string Self { get; init; }
            public readonly string Guilds { get; init; }
            public readonly string Guild { get; init; }
        }

        public struct GuildInfo
        {

            public ulong Id { readonly get; private set; }
            public string Name { readonly get; private set; }
            public readonly string Get { get { return $"{Id}: {Name},"; } }

            public GuildInfo Create(ulong Id, string Name) => new GuildInfo { Id = Id, Name = Name };
        }
    }
}
