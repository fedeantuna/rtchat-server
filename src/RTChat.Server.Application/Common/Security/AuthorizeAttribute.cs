using System;

namespace RTChat.Server.Application.Common.Security
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class AuthorizeAttribute : Attribute
    {
        public AuthorizeAttribute()
        {
        }

        public AuthorizeAttribute(String policy) => this.Policy = policy;

        public String Policy { get; set; }

        public String Roles { get; set; }

        public String AuthenticationSchemes { get; set; }
    }
}