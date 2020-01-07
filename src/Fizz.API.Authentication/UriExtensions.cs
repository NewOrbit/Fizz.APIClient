using System;
using System.Collections.Generic;
using System.Linq;

namespace Fizz.API.Authentication
{
    public static class UriExtensions
    {
        public static Dictionary<string, string> GetQueryParams(this Uri uri)
        {
            if (String.IsNullOrWhiteSpace(uri.Query))
            {
                return new Dictionary<string, string>();
            }

            var queryPartsOne = uri.Query.TrimStart('?').Split('&');
            var queryPartsTwo = queryPartsOne.Select(qp => {
                var parts = qp.Split('=');
                return new KeyValuePair<string, string>(parts[0], parts[1]);
            });
            var result = new Dictionary<string, string>(queryPartsTwo.Count());
            foreach (var qp in queryPartsTwo)
            {
              result.Add(qp.Key, qp.Value);
            }

            return result;
        }
    }
}