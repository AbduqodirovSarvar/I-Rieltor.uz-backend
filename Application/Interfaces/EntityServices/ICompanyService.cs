using Application.Models.ViewModels.Public;
using Application.Interfaces.EntityServices.Abstractions;
using Application.Models.Dtos.Companies;
using Application.Models.Results;
using Application.Models.ViewModels.Companies;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.EntityServices
{
    public interface ICompanyService
        : IGenericEntityService<Company,
                                CreateCompanyDto,
                                UpdateCompanyDto,
                                CompanyViewModel>
    {
        /// <summary>
        /// The site owner's company for the "About" and "Contacts" pages; no permission required.
        /// </summary>
        Task<ResponseModel<PublicCompanyViewModel?>> GetPublicAsync(CancellationToken cancellationToken = default);
    }
}
