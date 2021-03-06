using System;
using System.Collections.Generic;
using System.Linq;
using Shouldly;
using Xunit;

namespace Fizz.API.Authentication.Tests
{
    public class AuthenticatorTests
    {
        [Fact]
        public void WillSetScheme()
        {
            var url = "http://www.fizzbenefits.com";
            var sud = GetAuthenticator();
            
            var actual = sud.GetAuthenticatedUrl(url);
            actual.ShouldStartWith("https://www.fizzbenefits.com/");
        }

        [Fact]
        public void WillFailOnInvalidUrl()
        {
            var url = "/userId=123";
            var sud = GetAuthenticator();

            var e = Should.Throw<ArgumentException>(() => sud.GetAuthenticatedUrl(url));
        }

        [Fact]
        public void ShouldAddKeyAndNonceAndTimestamp()
        {
            var url = "http://www.fizzbenefits.com/api/offers/cashback";
            var sud = GetAuthenticator();
            
            var actual = sud.GetAuthenticatedUrl(url);
            var uri = new Uri(actual);
            var queryParams = uri.GetQueryParams();
            queryParams.Keys.ShouldContain("key");
            queryParams.Keys.ShouldContain("nonce");
            queryParams.Keys.ShouldContain("timestamp");
        }

        [Fact]
        public void CanRetainParamsFromUrl()
        {
            var url = "http://www.fizzbenefits.com/foo/bar?userId=123";
            var sud = GetAuthenticator();
            
            var actual = sud.GetAuthenticatedUrl(url);
            var uri = new Uri(actual);
            var queryParams = uri.GetQueryParams();
            queryParams.Keys.ShouldContain("userId");
            queryParams["userId"].ShouldBe("123");
        }

        [Fact]
        public void CanAddParamsFromDictionary()
        {
            var url = "http://www.fizzbenefits.com/foo/bar";
            var sud = GetAuthenticator();
            
            var actual = sud.GetAuthenticatedUrl(url, new Dictionary<string, string> {{"userId", "123"}});
            var uri = new Uri(actual);
            var queryParams = uri.GetQueryParams();
            queryParams.Keys.ShouldContain("userId");
            queryParams["userId"].ShouldBe("123");
        }
    

        [Fact]
        public void CanVerify()
        {
            var url = "http://www.fizzbenefits.com/foo/bar?userId=123";
            var sud = GetAuthenticator();
            var authenticated = sud.GetAuthenticatedUrl(url);

            var actual = sud.VerifyAuthenticatedUrl(authenticated);

            actual.ShouldBeTrue();
        }

        [Fact]
        public void CanDetectTampering()
        {
            var url = "http://www.fizzbenefits.com/foo/bar?userId=123";
            var sud = GetAuthenticator();
            var authenticated = sud.GetAuthenticatedUrl(url);
            var tamperedWith = authenticated.Replace("userId=123", "userId=124");
            
            var actual = sud.VerifyAuthenticatedUrl(tamperedWith);

            actual.ShouldBeFalse();
        }

        [Fact]
        public void VerificationWillFailWhenTimestampExpired()
        {
            var url = "http://www.fizzbenefits.com/foo/bar";

            var encoder = GetAuthenticator(() => DateTime.SpecifyKind(new DateTime(2020,12,31,22,0,0), DateTimeKind.Utc));
            var authenticatedUrl = encoder.GetAuthenticatedUrl(url);

            var verifier = GetAuthenticator(() => DateTime.SpecifyKind(new DateTime(2020,12,31,23,1,0), DateTimeKind.Utc));
            var actual = verifier.VerifyAuthenticatedUrl(authenticatedUrl);

            actual.ShouldBeFalse();
        }
        
        private Authenticator GetAuthenticator(Func<DateTime> systemTimeProvider = null)
        {
            var key = "abc";
            var secret = "123";

            if (systemTimeProvider == null)
            {
                return new Authenticator(key, secret);
            }
            else
            {
                return new Authenticator(key, secret, systemTimeProvider);
            }
        }
    }
}
