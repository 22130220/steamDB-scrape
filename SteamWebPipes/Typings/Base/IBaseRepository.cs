using SteamCrawlerCore.Typings.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamCrawlerCore.Typings.Base
{
    public interface IBaseRepository
    {
        Task InsertAsync(IEnumerable<PriceOverview> prices);
        Task<IEnumerable<PriceOverview>> GetByAppIdAsync(int appId);
    }
}
