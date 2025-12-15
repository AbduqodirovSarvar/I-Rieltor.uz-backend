using Application.Models.Dtos.UserRolePermissions;
using Application.Models.ViewModels.UserRolePermisions;
using AutoMapper;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Mappings
{
    public class UserRolePermissionMappingProfile : Profile
    {
        public UserRolePermissionMappingProfile()
        {
            CreateMap<UserRolePermission, UserRolePermissionViewModel>().ReverseMap();
            CreateMap<CreateUserRolePermissionDto, UserRolePermission>().ReverseMap();
            CreateMap<UpdateUserRolePermissionDto, UserRolePermission>().ReverseMap();
        }
    }
}
