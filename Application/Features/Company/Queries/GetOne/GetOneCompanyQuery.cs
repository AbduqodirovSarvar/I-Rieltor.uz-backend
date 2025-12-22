using Application.Features.Abstractions.Queries.Interfaces;
using Application.Models.Requests.Abstractions;
using Application.Models.Results;
using Application.Models.ViewModels.Companies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Company.Queries.GetOne
{
    public record GetOneCompanyQuery
        : BaseGetOneQuery,
        IQuery<ResponseModel<CompanyViewModel?>>
    {
    }
}
