using Application.Features.Abstractions.Commands.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Results;
using Application.Models.ViewModels.Companies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Company.Commands.Create
{
    public class CreateCompanyCommandHandler(
        ICompanyService companyService)
        : ICommandHandler<CreateCompanyCommand, ResponseModel<CompanyViewModel>>
    {
        private readonly ICompanyService _companyService = companyService;
        public async Task<ResponseModel<CompanyViewModel>> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
        {
            return await _companyService.CreateAsync(request, cancellationToken);
        }
    }
}
