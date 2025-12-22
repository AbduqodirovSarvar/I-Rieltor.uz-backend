using Application.Features.Abstractions.Queries.Interfaces;
using Application.Models.Requests.Abstractions;
using Application.Models.Results;
using Application.Models.ViewModels.Slides;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Slides.Queries.GetOne
{
    public record GetOneSlideQuery
        : BaseGetOneQuery,
        IQuery<ResponseModel<SlideViewModel?>>
    {
    }
}
