using Application.Interfaces.Files;
using AutoMapper;

namespace Application.Mappings
{
    /// <summary>
    /// Builds a file's public URL from its stored Path at read time, so a new FileStorage:PublicBaseUrl
    /// (another domain, a CDN) applies to every file, also those uploaded before the change.
    /// </summary>
    public sealed class FileUrlResolver(IFileService fileService) : IMemberValueResolver<object, object, string, string>
    {
        public string Resolve(object source, object destination, string sourceMember, string destMember, ResolutionContext context)
            => fileService.GetPublicUrl(sourceMember);
    }
}
