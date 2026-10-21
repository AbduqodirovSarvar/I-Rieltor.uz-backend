using Application.Models.ViewModels.Public;
using Application.Features.Abstractions.Queries.Interfaces;
using Application.Models.Results;
using Application.Models.ViewModels.Slides;

namespace Application.Features.Slides.Queries.GetPublicList
{
    public record GetPublicSlideListQuery
        : IQuery<ResponseModel<List<PublicSlideViewModel>>>
    {
    }
}
