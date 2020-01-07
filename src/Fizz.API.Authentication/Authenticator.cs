using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Fizz.API.Authentication
{
    public class Authenticator
    {
        private string key;
        private string secret;

        private const string signatureParamName = "&signature=";

        public Authenticator(string key, string secret)
        {
            this.key = key;
            this.secret = secret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url">Only the "path" and "query" parts are used. You can pass in a full URL or just the "path" bit as you like.</param>
        /// <returns></returns>
        public string GetAuthenticatedUrl(string url, Dictionary<string, string> parameters= null)
        {
            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                throw new ArgumentException("The url must be an absolute URL in the format https://www.fizzbenefits.com....", nameof(url));
            }

            var builder = new UriBuilder(url);
            builder.Scheme = "https";

            var queryParams = builder.Uri.GetQueryParams();

            if (!(parameters == null))
            {
                foreach (var item in parameters)
                {
                    queryParams.Add(item.Key, Uri.EscapeDataString(item.Value));
                }
            }

            queryParams.Add("key", Uri.EscapeDataString(this.key));
            queryParams.Add("nonce", Guid.NewGuid().ToString("N"));
            queryParams.Add("timestamp", ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));

            
            builder.Query = String.Join("&", queryParams.Select(qp => $"{qp.Key}={qp.Value}"));

            builder.Port = -1;
            var resultUrl = builder.ToString();

            var signature = GetSignature(resultUrl);
            return $"{resultUrl}{signatureParamName}{signature}";
        }

        public bool VerifyAuthenticatedUrl(string url)
        {
            var index = url.IndexOf(signatureParamName);
            var unsignedUrl = url.Substring(0, index);
            var sig = url.Substring(index + signatureParamName.Length);
            var expectedSig = GetSignature(unsignedUrl);
            return sig == expectedSig;
        }

        private string GetSignature(string url)
        {
            var signatureBase = url + this.secret; // Append the secret so it becomes part of the hash. This is essentially the authentication step.

             using (var sha = SHA256.Create()) 
            {
                var mac = sha.ComputeHash(Encoding.ASCII.GetBytes(signatureBase));
                return Base64UrlEncoder.Encode(mac);
                
            }
        }
    }
}
