using Application.Models.Dtos.Companies;
using Application.Models.ViewModels.Companies;
using AutoMapper;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Mappings
{
    public class CompanyMappingProfile : Profile
    {
        public CompanyMappingProfile() 
        {
            CreateMap<Company, CompanyViewModel>();
            CreateMap<CreateCompanyDto, Company>();
            CreateMap<UpdateCompanyDto, Company>();
        }
    }
}
