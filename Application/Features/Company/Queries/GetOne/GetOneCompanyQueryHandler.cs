using Application.Features.Abstractions.Queries.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Results;
using Application.Models.ViewModels.Companies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Company.Queries.GetOne
{
    public class GetOneCompanyQueryHandler(
        ICompanyService companyService)
        : IQueryHandler<GetOneCompanyQuery, ResponseModel<CompanyViewModel?>>
    {
        private readonly ICompanyService _companyService = companyService;
        public async Task<ResponseModel<CompanyViewModel?>> Handle(GetOneCompanyQuery request, CancellationToken cancellationToken)
        {
            return await _companyService.GetByIdAsync(request.Id, cancellationToken);
        }
    }
}
