using Application.Features.Abstractions.Queries.Interfaces;
using Application.Models.Requests.Abstractions;
using Application.Models.Results;
using Application.Models.ViewModels.Slides;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Slides.Queries.GetList
{
    public record GetSlideListQuery
        : BaseGetListQuery,
        IQuery<ResponseModel<List<SlideViewModel>>>
    {
    }
}
