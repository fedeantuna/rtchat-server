using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using RTChat.Server.API.Cache;
using RTChat.Server.API.Constants;
using Xunit;

namespace RTChat.Server.API.Tests.Cache
{
    public class ApplicationCacheTests
    {
        private const String InMemoryCacheSizeLimit = "1024";
        
        private readonly ApplicationCache _sut;
        
        public ApplicationCacheTests()
        {
            var configuration = SetUpInMemoryConfiguration();
            
            this._sut = new ApplicationCache(configuration);
        }
        
        [Fact]
        public void ApplicationCache_SizeLimitIsSameAsInConfiguration()
        {
            // Arrange
            var expected = Int32.Parse(InMemoryCacheSizeLimit);

            // Act
            var optionsField = typeof(MemoryCache).GetField("_options", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(this._sut.MemoryCache) as MemoryCacheOptions;
            
            // Assert
            Assert.NotNull(optionsField);
            Assert.Equal(expected, optionsField.SizeLimit);
        }
        
        private static IConfiguration SetUpInMemoryConfiguration()
        {
            var inMemoryConfiguration = new Dictionary<String, String>
            {
                { ConfigurationKeys.InMemoryCacheSizeLimit, InMemoryCacheSizeLimit }
            };
            
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemoryConfiguration)
                .Build();

            return configuration;
        }
    }
}