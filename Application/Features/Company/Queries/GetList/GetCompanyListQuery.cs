using Application.Features.Abstractions.Queries.Interfaces;
using Application.Models.Requests.Abstractions;
using Application.Models.Results;
using Application.Models.ViewModels.Companies;

namespace Application.Features.Company.Queries.GetList
{
    public record GetCompanyListQuery
        : BaseGetListQuery,
        IQuery<ResponseModel<List<CompanyViewModel>>>
    {
    }
}
