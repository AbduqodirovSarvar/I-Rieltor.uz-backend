using Application.Interfaces.EntityServices.Abstractions;
using Application.Models.Dtos.Slides;
using Application.Models.Results;
using Application.Models.ViewModels.Slides;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.EntityServices
{
    public interface ISlideService
        : IGenericEntityService<Slide,
                                CreateSlideDto,
                                UpdateSlideDto,
                                SlideViewModel>
    {
        /// <summary>
        /// Slides for the home page carousel, in display order; no permission required.
        /// </summary>
        Task<ResponseModel<List<SlideViewModel>>> GetPublicListAsync(CancellationToken cancellationToken = default);
    }
}
