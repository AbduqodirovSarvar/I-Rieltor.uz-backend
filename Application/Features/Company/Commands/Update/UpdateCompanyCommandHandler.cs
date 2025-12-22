using Application.Features.Abstractions.Commands.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Results;
using Application.Models.ViewModels.Companies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Company.Commands.Update
{
    public class UpdateCompanyCommandHandler(
        ICompanyService companyService)
        : ICommandHandler<UpdateCompanyCommand, ResponseModel<CompanyViewModel>>
    {
        private readonly ICompanyService _companyService = companyService;
        public async Task<ResponseModel<CompanyViewModel>> Handle(UpdateCompanyCommand request, CancellationToken cancellationToken)
        {
            return await _companyService.UpdateAsync(request, cancellationToken);
        }
    }
}
