using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextSearch.Helpers
{
    public static class Helper
    {
        public static bool ValidateUri(string uri)
        {
            try
            {
                return Uri.TryCreate(uri, UriKind.Absolute, out var uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            }
            catch
            {
                return false;
            }
        }
    }
}
