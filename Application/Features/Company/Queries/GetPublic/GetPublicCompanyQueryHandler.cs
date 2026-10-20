using Application.Features.Abstractions.Queries.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Results;
using Application.Models.ViewModels.Companies;

namespace Application.Features.Company.Queries.GetPublic
{
    public class GetPublicCompanyQueryHandler(ICompanyService companyService)
        : IQueryHandler<GetPublicCompanyQuery, ResponseModel<CompanyViewModel?>>
    {
        private readonly ICompanyService _companyService = companyService;
        public async Task<ResponseModel<CompanyViewModel?>> Handle(GetPublicCompanyQuery request, CancellationToken cancellationToken)
        {
            return await _companyService.GetPublicAsync(cancellationToken);
        }
    }
}
