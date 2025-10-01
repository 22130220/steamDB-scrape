using SteamKit2;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteamWebPipes
{
    public class SteamHelper(SteamApps steamApps)
    {
        private readonly SteamApps steamApps = steamApps;

        public async Task<List<SteamApps.PICSProductInfoCallback>> GetProductInfoAsync(
            List<uint> appIdList,
            List<uint> packageList)
        {
            var accessTokenJob = steamApps.PICSGetAccessTokens(appIdList, packageList);
            var accessTokenResult = await accessTokenJob;

            var appRequests = new List<SteamApps.PICSRequest>();
            var packageRequests = new List<SteamApps.PICSRequest>();

       
            foreach (var appId in appIdList)
            {
                accessTokenResult.AppTokens.TryGetValue(appId, out ulong token);
                appRequests.Add(new SteamApps.PICSRequest(appId, token));
            }

            foreach (var packageId in packageList)
            {
                accessTokenResult.PackageTokens.TryGetValue(packageId, out ulong token);
                packageRequests.Add(new SteamApps.PICSRequest(packageId, token));
            }

       
            var productJob = steamApps.PICSGetProductInfo(appRequests, packageRequests);
            var resultSet = await productJob;

            return [.. resultSet.Results];
        }
    }
}
