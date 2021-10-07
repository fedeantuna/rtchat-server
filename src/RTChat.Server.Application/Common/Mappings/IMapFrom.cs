using AutoMapper;

namespace RTChat.Server.Application.Common.Mappings
{
    public interface IMapFrom<T>
        where T : class
    {
        void Mapping(Profile profile) => profile.CreateMap(typeof(T), this.GetType());
    }
}