
using SteamCrawlerCore.Typings.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamCrawlerCore.Typings.Models
{
    public class BaseModel : IBaseModel
    {
        public Guid Id { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
