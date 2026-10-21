using Application.Models.ViewModels.Public;
using Application.Features.Abstractions.Queries.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Results;
using Application.Models.ViewModels.Companies;

namespace Application.Features.Company.Queries.GetPublic
{
    public class GetPublicCompanyQueryHandler(ICompanyService companyService)
        : IQueryHandler<GetPublicCompanyQuery, ResponseModel<PublicCompanyViewModel?>>
    {
        private readonly ICompanyService _companyService = companyService;
        public async Task<ResponseModel<PublicCompanyViewModel?>> Handle(GetPublicCompanyQuery request, CancellationToken cancellationToken)
        {
            return await _companyService.GetPublicAsync(cancellationToken);
        }
    }
}
