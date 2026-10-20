using Application.Extentions;
using Application.Features.Abstractions.Queries.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Results;
using Application.Models.ViewModels.Slides;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Slides.Queries.GetList
{
    public class GetSlideLIstQueryHandler(
        ISlideService slideService)
        : IQueryHandler<GetSlideListQuery, ResponseModel<List<SlideViewModel>>>
    {
        private readonly ISlideService _slideService = slideService;
        public async Task<ResponseModel<List<SlideViewModel>>> Handle(GetSlideListQuery request, CancellationToken cancellationToken)
        {
            Expression<Func<Slide, bool>>? predicate = null;
            if (request.Filters != null && request.Filters.Any())
            {
                predicate = request.Filters.BuildPredicate<Slide>();
            }
            return await _slideService.GetAllAsync(predicate, request.PageIndex, request.PageSize, cancellationToken);
        }
    }
}
