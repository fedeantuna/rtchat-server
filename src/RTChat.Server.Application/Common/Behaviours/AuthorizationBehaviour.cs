using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using RTChat.Server.Application.Common.Exceptions;
using RTChat.Server.Application.Common.Security;
using RTChat.Server.Application.Common.Services;

namespace RTChat.Server.Application.Common.Behaviours
{
    public class AuthorizationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IIdentityService _identityService;

        public AuthorizationBehaviour(ICurrentUserService currentUserService, IIdentityService identityService)
        {
            this._currentUserService = currentUserService;
            this._identityService = identityService;
        }
        
        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            var authorizeAttributes = GetAuthorizeAttributes(request).ToArray();
            
            if (authorizeAttributes.Any())
            {
                if (String.IsNullOrEmpty(_currentUserService.GetUserId()))
                {
                    throw new UnauthorizedAccessException();
                }

                await ExecuteRoleBasedAuthorization(authorizeAttributes);

                await ExecutePolicyBasedAuthorization(authorizeAttributes);
            }
            
            return await next();
        }

        private async Task ExecuteRoleBasedAuthorization(IEnumerable<AuthorizeAttribute> authorizeAttributes)
        {
            var authorizeAttributesWithRoles =
                authorizeAttributes.Where(aa => !String.IsNullOrWhiteSpace(aa.Roles))
                    .ToArray();
            if (authorizeAttributesWithRoles.Any())
            {
                var authorized = false;
                    
                var roles = authorizeAttributesWithRoles.SelectMany(aa => aa.Roles.Split(','));
                foreach (var role in roles)
                {
                    var isInRole = await _identityService.IsInRole(_currentUserService.GetUserId(), role.Trim());

                    if (isInRole)
                    {
                        authorized = true;
                        break;
                    }
                }

                if (!authorized)
                {
                    throw new ForbiddenAccessException();
                }
            }
        }

        private async Task ExecutePolicyBasedAuthorization(IEnumerable<AuthorizeAttribute> authorizeAttributes)
        {
            var authorizeAttributesWithPolicies =
                authorizeAttributes.Where(aa => !String.IsNullOrWhiteSpace(aa.Policy))
                    .ToArray();
            if (authorizeAttributesWithPolicies.Any())
            {
                var policies = authorizeAttributesWithPolicies.Select(a => a.Policy);
                foreach (var policy in policies)
                {
                    var authorized = await _identityService.Authorize(_currentUserService.GetUserId(), policy);

                    if (!authorized)
                    {
                        throw new ForbiddenAccessException();
                    }
                }
            }
        }

        private static IEnumerable<AuthorizeAttribute> GetAuthorizeAttributes(TRequest request)
        {
            var attributeCollectionEnumerator = TypeDescriptor.GetAttributes(request).GetEnumerator();

            while (attributeCollectionEnumerator.MoveNext())
            {
                if (attributeCollectionEnumerator.Current?.GetType() == typeof(AuthorizeAttribute))
                {
                    yield return (AuthorizeAttribute)attributeCollectionEnumerator.Current;
                }
            }
        }
    }
}