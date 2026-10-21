using Application.Features.Abstractions.Commands.Interfaces;
using Application.Models.Dtos.Slides;
using Application.Models.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Slides.Commands.Delete
{
    public record DeleteSlideCommand
        : DeleteSlideDto,
        ICommand<ResponseModel<bool>>
    {
    }
}
