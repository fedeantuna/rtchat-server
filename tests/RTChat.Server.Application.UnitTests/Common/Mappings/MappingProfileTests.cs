using System.Diagnostics.CodeAnalysis;
using AutoMapper;
using RTChat.Server.Application.Common.Mappings;
using Xunit;

namespace RTChat.Server.Application.UnitTests.Common.Mappings
{
    [ExcludeFromCodeCoverage]
    public class MappingProfileTests
    {
        private readonly IConfigurationProvider _configurationProvider;
        
        public MappingProfileTests()
        {
            this._configurationProvider = new MapperConfiguration(c => c.AddProfile<MappingProfile>());

            this._configurationProvider.CreateMapper();
        }

        [Fact]
        public void MappingConfigurationIsValid() => this._configurationProvider.AssertConfigurationIsValid();
    }
}