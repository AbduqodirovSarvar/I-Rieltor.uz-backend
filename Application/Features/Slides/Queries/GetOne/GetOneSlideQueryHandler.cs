using Application.Features.Abstractions.Queries.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Results;
using Application.Models.ViewModels.Slides;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Slides.Queries.GetOne
{
    public class GetOneSlideQueryHandler(
        ISlideService slideService)
        : IQueryHandler<GetOneSlideQuery, ResponseModel<SlideViewModel?>>
    {
        private readonly ISlideService _slideService = slideService;
        public async Task<ResponseModel<SlideViewModel?>> Handle(GetOneSlideQuery request, CancellationToken cancellationToken)
        {
            return await _slideService.GetByIdAsync(request.Id, cancellationToken);
        }
    }
}
