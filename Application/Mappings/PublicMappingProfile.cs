using Application.Models.ViewModels.Public;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappings
{
    public class PublicMappingProfile : Profile
    {
        public PublicMappingProfile()
        {
            CreateMap<UploadedFile, PublicFileViewModel>()
                .ForMember(d => d.Url, o => o.MapFrom<FileUrlResolver, string>(s => s.Path));
            CreateMap<Slide, PublicSlideViewModel>()
                .ForMember(d => d.Image, o => o.MapFrom(s => s.UploadedFile));
            CreateMap<Company, PublicCompanyViewModel>()
                .ForMember(d => d.Logo, o => o.MapFrom(s => s.LogoFile));
        }
    }
}
