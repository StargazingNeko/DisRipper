using System.Threading.Tasks;

namespace DisRipper
{
    public static class Config
    {
        public static bool bIsTokenRetrievalEnabled { get; private set; } = false;

        static Config()
        {
            if(Utility.db.GetConfigValue("bAutomaticallyRetrieveToken").Result != null)
                bIsTokenRetrievalEnabled = Utility.db.GetConfigValue("bAutomaticallyRetrieveToken").Result;
        }

        public static async Task SetAutomaticTokenRetrieval(bool val)
        {
            bIsTokenRetrievalEnabled = val;
            await Utility.db.UpdateConfigTable("bAutomaticallyRetrieveToken", bIsTokenRetrievalEnabled);
        }
    }
}