using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamCrawlerCore.Typings.Models
{
    public class PriceOverview : BaseModel
    {
        public int AppId { get; set; }
        public string Currency { get; set; }
        public int Initial { get; set; }
        public int Final { get; set; }
        public int DiscountPercent { get; set; }
        public string Country { get; set; }
    }
}
