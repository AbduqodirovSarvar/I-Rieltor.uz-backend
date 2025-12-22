using Application.Interfaces.EntityServices.Abstractions;
using Application.Models.Dtos.Companies;
using Application.Models.ViewModels.Companies;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.EntityServices
{
    public interface ICompanyService
        : IGenericEntityService<Company,
                                CreateCompanyDto,
                                UpdateCompanyDto,
                                CompanyViewModel>
    {
    }
}
