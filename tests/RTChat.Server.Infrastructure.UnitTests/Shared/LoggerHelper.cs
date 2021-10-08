using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace RTChat.Server.Infrastructure.UnitTests.Shared
{
    [ExcludeFromCodeCoverage]
    public static class LoggerHelper
    {
        public static Boolean CheckValue(Object state, Object expectedValue, String key)
        {
            var keyValuePairList = (IReadOnlyList<KeyValuePair<String, Object>>)state;

            var actualValue = keyValuePairList.First(kvp => String.Compare(kvp.Key, key, StringComparison.Ordinal) == 0).Value;

            return expectedValue.Equals(actualValue);
        }
    }
}