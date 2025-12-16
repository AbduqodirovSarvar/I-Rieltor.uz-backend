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
                .ReverseMap();
            CreateMap<UpdateUserDto, User>()
                .ForMember(x => x.PasswordHash, opt => opt.Ignore())
                .ReverseMap();
        }
    }
}
