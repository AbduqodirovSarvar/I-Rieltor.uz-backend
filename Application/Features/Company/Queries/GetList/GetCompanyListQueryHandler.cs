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
            Expression<Func<Domain.Entities.Company, bool>>? predicate = null;
            if (request.Filters != null && request.Filters.Any())
            {
                predicate = request.Filters.BuildPredicate<Domain.Entities.Company>();
            }
            return await _companyService.GetAllAsync(predicate, request.PageIndex, request.PageSize, cancellationToken);
        }
    }
}
