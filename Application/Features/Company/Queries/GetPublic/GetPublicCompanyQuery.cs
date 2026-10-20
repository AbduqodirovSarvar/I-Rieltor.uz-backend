using Application.Features.Abstractions.Queries.Interfaces;
using Application.Models.Results;
using Application.Models.ViewModels.Companies;

namespace Application.Features.Company.Queries.GetPublic
{
    public record GetPublicCompanyQuery
        : IQuery<ResponseModel<CompanyViewModel?>>
    {
    }
}
