using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Application.Options
{
    public class GivaudanApiOptions
    {
        public string BaseUrl { get; set; } = default!;
        public string AuthUrl { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
    }
}
