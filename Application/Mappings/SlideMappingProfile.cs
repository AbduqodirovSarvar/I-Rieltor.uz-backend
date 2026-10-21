using Application.Models.Dtos.Slides;
using Application.Models.ViewModels.Slides;
using AutoMapper;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Mappings
{
    public class SlideMappingProfile : Profile
    {
        public SlideMappingProfile() 
        {
            CreateMap<Slide, SlideViewModel>();
            CreateMap<CreateSlideDto, Slide>();
            CreateMap<UpdateSlideDto, Slide>();
        }
    }
}
