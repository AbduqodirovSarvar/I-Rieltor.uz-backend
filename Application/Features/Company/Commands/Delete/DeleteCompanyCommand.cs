using Application.Features.Abstractions.Commands.Interfaces;
using Application.Models.Dtos.Companies;
using Application.Models.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Company.Commands.Delete
{
    public record DeleteCompanyCommand
        : DeleteCompanyDto,
        ICommand<ResponseModel<bool>>
    {
    }
}
