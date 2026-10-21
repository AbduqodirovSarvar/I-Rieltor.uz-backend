using Application.Models.ViewModels.Public;
using Application.Features.Abstractions.Queries.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Results;
using Application.Models.ViewModels.Slides;

namespace Application.Features.Slides.Queries.GetPublicList
{
    public class GetPublicSlideListQueryHandler(ISlideService slideService)
        : IQueryHandler<GetPublicSlideListQuery, ResponseModel<List<PublicSlideViewModel>>>
    {
        private readonly ISlideService _slideService = slideService;
        public async Task<ResponseModel<List<PublicSlideViewModel>>> Handle(GetPublicSlideListQuery request, CancellationToken cancellationToken)
        {
            return await _slideService.GetPublicListAsync(cancellationToken);
        }
    }
}
