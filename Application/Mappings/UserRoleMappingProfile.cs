using Application.Models.Dtos.Users;
using Application.Models.ViewModels.UserRoles;
using AutoMapper;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Mappings
{
    public class UserRoleMappingProfile : Profile
    {
        public UserRoleMappingProfile()
        {
            CreateMap<UserRole, UserRoleViewModel>().ReverseMap();
            CreateMap<CreateUserDto, UserRole>().ReverseMap();
            CreateMap<UpdateUserDto, UserRole>().ReverseMap();
        }
    }
}
