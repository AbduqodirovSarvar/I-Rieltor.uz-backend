using Application.Extentions;
using Application.Features.Abstractions.Queries.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Results;
using Application.Models.ViewModels.Companies;
using System.Linq.Expressions;

namespace Application.Features.Company.Queries.GetList
{
    public class GetCompanyListQueryHandler(ICompanyService companyService)
        : IQueryHandler<GetCompanyListQuery, ResponseModel<List<CompanyViewModel>>>
    {
        private readonly ICompanyService _companyService = companyService;
        public async Task<ResponseModel<List<CompanyViewModel>>> Handle(GetCompanyListQuery request, CancellationToken cancellationToken)
        {
            var predicate = request.BuildPredicate<Domain.Entities.Company>();
            var orderBy = request.BuildOrderBy<Domain.Entities.Company>();
            return await _companyService.GetAllAsync(predicate, request.PageIndex, request.PageSize, orderBy, cancellationToken);
        }
    }
}
