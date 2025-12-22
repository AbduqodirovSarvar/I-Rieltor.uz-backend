using Application.Features.Abstractions.Commands.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Company.Commands.Delete
{
    public class DeleteCompanyCommandHandler(
        ICompanyService companyService)
        : ICommandHandler<DeleteCompanyCommand, ResponseModel<bool>>
    {
        private readonly ICompanyService _companyService = companyService;
        public async Task<ResponseModel<bool>> Handle(DeleteCompanyCommand request, CancellationToken cancellationToken)
        {
            return await _companyService.DeleteAsync(request.Id, cancellationToken);
        }
    }
}
