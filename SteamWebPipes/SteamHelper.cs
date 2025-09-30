using SteamKit2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamWebPipes
{
    public class SteamHelper
    {
        private SteamApps steamApps;

        public SteamHelper(SteamApps steamApps)
        {
            this.steamApps = steamApps;
        }
        public async Task<List<SteamApps.PICSProductInfoCallback>> GetProductInfoAsync(
            List<uint> appIdList,
            List<uint> packageList)
        {
            // 1. Xin access token cho tất cả AppID
            var accessTokenJob = steamApps.PICSGetAccessTokens(appIdList, packageList);
            var accessTokenResult = await accessTokenJob;

            var appRequests = new List<SteamApps.PICSRequest>();
            var packageRequests = new List<SteamApps.PICSRequest>();

            // Tạo danh sách request cho Apps
            foreach (var appId in appIdList)
            {
                ulong token = 0;
                accessTokenResult.AppTokens.TryGetValue(appId, out token);
                appRequests.Add(new SteamApps.PICSRequest(appId, token));
            }

            // Tạo danh sách request cho Packages
            foreach (var packageId in packageList)
            {
                ulong token = 0;
                accessTokenResult.PackageTokens.TryGetValue(packageId, out token);
                packageRequests.Add(new SteamApps.PICSRequest(packageId, token));
            }

            // 2. Gửi request product info
            var productJob = steamApps.PICSGetProductInfo(appRequests, packageRequests);
            var resultSet = await productJob;

            // 3. Trả về tất cả kết quả (có thể có nhiều callback)
            return resultSet.Results.ToList();
        }
    }
}
