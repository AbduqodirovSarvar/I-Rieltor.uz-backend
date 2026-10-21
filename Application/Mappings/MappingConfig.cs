using Application.Interfaces.Files;
using Application.Models.Dtos.Auths;
using Application.Models.Dtos.Users;
using Application.Models.ViewModels.Companies;
using Application.Models.ViewModels.Public;
using Application.Models.ViewModels.Slides;
using Application.Models.ViewModels.UploadedFiles;
using Domain.Entities;
using Mapster;

namespace Application.Mappings
{
    /// <summary>
    /// Mapster (MIT) replaced AutoMapper (commercial licence from v15, P3-02). Pairs not listed here map by
    /// property name; only the ones with special rules are configured.
    /// </summary>
    public sealed class MappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // A file's public URL is built from its stored Path at read time, so a new FileStorage:PublicBaseUrl
            // (another domain, a CDN) applies to every file, also those uploaded before the change.
            // The service comes from the request scope (Mapster.DependencyInjection's ServiceMapper).
            config.NewConfig<UploadedFile, UploadedFileViewModel>()
                  .Map(d => d.Url, s => MapContext.Current!.GetService<IFileService>().GetPublicUrl(s.Path));
            config.NewConfig<UploadedFile, PublicFileViewModel>()
                  .Map(d => d.Url, s => MapContext.Current!.GetService<IFileService>().GetPublicUrl(s.Path));

            config.NewConfig<Slide, PublicSlideViewModel>()
                  .Map(d => d.Image, s => s.UploadedFile);
            config.NewConfig<Company, PublicCompanyViewModel>()
                  .Map(d => d.Logo, s => s.LogoFile);

            // The password is hashed by the service; the name is stored as one string.
            config.NewConfig<CreateUserDto, User>()
                  .Ignore(d => d.PasswordHash)
                  .Map(d => d.FullName, s => BuildFullName(s.LastName, s.FirstName, s.MiddleName));
            config.NewConfig<RegisterDto, User>()
                  .Ignore(d => d.PasswordHash)
                  .Map(d => d.FullName, s => BuildFullName(s.LastName, s.FirstName, s.MiddleName));
            // UpdateUserDto is a partial update and is applied field by field in UserService, not mapped.
        }

        /// <summary>
        /// "Familiya Ism Otasining ismi", skipping empty parts.
        /// </summary>
        public static string BuildFullName(string? lastName, string? firstName, string? middleName)
            => string.Join(' ', new[] { lastName, firstName, middleName }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));
    }
}
