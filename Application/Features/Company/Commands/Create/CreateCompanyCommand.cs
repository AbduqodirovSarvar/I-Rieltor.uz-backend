using Application.Features.Abstractions.Commands.Interfaces;
using Application.Models.Dtos.Companies;
using Application.Models.Results;
using Application.Models.ViewModels.Companies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Company.Commands.Create
{
    public record CreateCompanyCommand
        : CreateCompanyDto,
        ICommand<ResponseModel<CompanyViewModel>>
    {
    }
}
