using System;
using Xunit;
using Fizz.API.Authentication;
using Shouldly;

namespace Fizz.API.Authentication.Tests
{
    public class QueryParamsTests
    {
        [Fact]
        public void CanParseString()
        {
            var uri = new Uri("http://foo.bar?a=b&b=c");
            var queryParams = uri.GetQueryParams();
            queryParams.Count.ShouldBe(2);
            queryParams["a"].ShouldBe("b");
            queryParams["b"].ShouldBe("c");
        }

        [Fact]
        public void CanHandleNoQueryParams()
        {
            var uri = new Uri("https://foo.bar");
            var queryParams = uri.GetQueryParams();
            queryParams.Count.ShouldBe(0);
        }
    }
}