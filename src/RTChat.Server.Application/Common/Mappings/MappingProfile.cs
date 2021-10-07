using System;
using System.Linq;
using System.Reflection;
using AutoMapper;

namespace RTChat.Server.Application.Common.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            ApplyMappingsFromAssembly(Assembly.GetExecutingAssembly());
        }

        private void ApplyMappingsFromAssembly(Assembly assembly)
        {
            var types = assembly.GetExportedTypes()
                .Where(t => t.GetInterfaces().Any(i => 
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMapFrom<>)))
                .ToList();

            foreach (var type in types)
            {
                var instance = Activator.CreateInstance(type);

                const String mappingMethod = nameof(IMapFrom<Object>.Mapping);
                var mapFromInterfaceType = typeof(IMapFrom<>).Name;
                var methodInfo = type.GetMethod(mappingMethod) 
                                 ?? type.GetInterface(mapFromInterfaceType)?.GetMethod(nameof(mappingMethod));
                
                methodInfo?.Invoke(instance, new Object[] { this });

            }
        }
    }
}