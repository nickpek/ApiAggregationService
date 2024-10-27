using ApiAggregation.Services;

namespace ApiAggregation.Tests
{
    public class ParseCountryInput
    {
        // returns both country code and name when a valid country code is provided.
        [Fact]
        public void ParseCountryInput_ShouldReturnCodeAndName_ForValidCountryCode()
        {
            var country = "US";
            var (countryCode, countryName) = AggregationService.ParseCountryInput(country);

            Assert.Equal("US", countryCode);
            Assert.Equal("United States", countryName);

        }
        // returns the correct code and name when a valid country name is given.
        [Fact]
        public void ParseCountryInput_ShouldReturnCodeAndName_ForValidCountryName()
        {
            var country = "Greece";
            var (countryCode, countryName) = AggregationService.ParseCountryInput(country);

            Assert.Equal("GR", countryCode);
            Assert.Equal("Greece", countryName);
        }
        //returns null for the code and provides the input string as the name for an unrecognized country code.
        [Fact]
        public void ParseCountryInput_ShouldReturnNullCodeAndName_ForInvalidCountryCode()
        {
            var country = "ZZ";
            var (countryCode, countryName) = AggregationService.ParseCountryInput(country);

            Assert.Null(countryCode);
            Assert.Equal("ZZ", countryName);
        }

        //handles an invalid country name by returning null for the code and the input string as the name
        [Fact]
        public void ParseCountryInput_ShouldReturnNullCodeAndName_ForInvalidCountryName()
        {
            var country = "InvalidCountry";
            var (countryCode, countryName) = AggregationService.ParseCountryInput(country);

            Assert.Null(countryCode);
            Assert.Equal("InvalidCountry", countryName);
        }
        //eturns null for both code and name when the input is null or empty, confirming appropriate handling of blank input.
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void ParseCountryInput_ShouldReturnNull_ForNullOrEmptyInput(string? country)
        {
            var (countryCode, countryName) = AggregationService.ParseCountryInput(country);

            Assert.Null(countryCode);
            Assert.Null(countryName);
        }
    }
}