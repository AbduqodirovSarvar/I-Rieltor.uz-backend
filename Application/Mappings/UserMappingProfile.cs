using Application.Models.Dtos.Auths;
using Application.Models.Dtos.Users;
using Application.Models.ViewModels.Users;
using AutoMapper;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Mappings
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<User, UserViewModel>().ReverseMap();
            CreateMap<CreateUserDto, User>()
                .ForMember(x => x.PasswordHash, opt => opt.Ignore())
                .ForMember(x => x.FullName, opt => opt.MapFrom(src => BuildFullName(src.LastName, src.FirstName, src.MiddleName)));
            CreateMap<UpdateUserDto, User>()
                .ForMember(x => x.PasswordHash, opt => opt.Ignore())
                .ForMember(x => x.FullName, opt => opt.MapFrom(src => BuildFullName(src.LastName, src.FirstName, src.MiddleName)));
            CreateMap<RegisterDto, User>()
                .ForMember(x => x.PasswordHash, opt => opt.Ignore())
                .ForMember(x => x.FullName, opt => opt.MapFrom(src => BuildFullName(src.LastName, src.FirstName, src.MiddleName)));
        }

        /// <summary>
        /// "Familiya Ism Otasining ismi", skipping empty parts.
        /// </summary>
        private static string BuildFullName(string? lastName, string? firstName, string? middleName)
            => string.Join(' ', new[] { lastName, firstName, middleName }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));
    }
}
