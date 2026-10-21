using Application.Extentions;
using Application.Interfaces.Files;
using Application.Messaging;
using Application.Models.Dtos.Users;
using Application.Models.ViewModels.Public;
using Application.Models.ViewModels.Slides;
using Domain.Entities;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace IRieltor.Tests.Unit
{
    public class MediatorAndMappingTests
    {
        public sealed record Ping(string Text) : IRequest<string>;

        public sealed class PingHandler : IRequestHandler<Ping, string>
        {
            public Task<string> Handle(Ping request, CancellationToken cancellationToken) => Task.FromResult(request.Text);
        }

        public sealed class Wrap<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
        {
            public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
                => (TResponse)(object)$"[{await next(cancellationToken)}]";
        }

        public sealed class Exclaim<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
        {
            public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
                => (TResponse)(object)$"{await next(cancellationToken)}!";
        }

        private static IMediator CreateMediator()
            => new ServiceCollection()
                .AddMediator(typeof(MediatorAndMappingTests).Assembly, typeof(Wrap<,>), typeof(Exclaim<,>))
                .BuildServiceProvider().CreateScope().ServiceProvider.GetRequiredService<IMediator>();

        [Fact]
        public async Task Behaviors_run_in_registration_order_around_the_handler()
            => Assert.Equal("[hi!]", await CreateMediator().Send(new Ping("hi")));

        [Fact]
        public async Task A_request_known_only_as_object_is_dispatched_too()
            => Assert.Equal("[hi!]", await CreateMediator().Send((object)new Ping("hi")));

        private sealed class FakeFiles : IFileService
        {
            public string GetPublicUrl(string path) => "https://cdn.test" + path;
            public Task<UploadedFile> SaveFileAsync(Microsoft.AspNetCore.Http.IFormFile fileBytes, string category = "default") => throw new NotSupportedException();
            public Task DeleteFileAsync(string fileName, string category = "default") => throw new NotSupportedException();
            public Task DiscardFileAsync(string fileName, string category = "default") => throw new NotSupportedException();
        }

        private static IMapper CreateMapper()
        {
            var services = new ServiceCollection().AddApplicationServices();
            services.AddScoped<IFileService, FakeFiles>();
            return services.BuildServiceProvider().CreateScope().ServiceProvider.GetRequiredService<IMapper>();
        }

        [Fact]
        public void File_urls_are_built_from_the_path_also_in_nested_objects()
        {
            var slide = new Slide { Title = "T", UploadedFile = new UploadedFile { Path = "/uploads/slide/a.png", Alt = "alt" } };
            var mapper = CreateMapper();

            Assert.Equal("https://cdn.test/uploads/slide/a.png", mapper.Map<SlideViewModel>(slide).UploadedFile!.Url);
            var publicSlide = mapper.Map<PublicSlideViewModel>(slide);
            Assert.Equal("https://cdn.test/uploads/slide/a.png", publicSlide.Image!.Url);
            Assert.Equal("alt", publicSlide.Image.Alt);
        }

        [Fact]
        public void User_from_create_dto_gets_the_full_name_and_no_password()
        {
            var user = CreateMapper().Map<User>(new CreateUserDto { FirstName = "Ali", LastName = "Valiyev", Email = "a@b.uz", Password = "Strong@123" });

            Assert.Equal("Valiyev Ali", user.FullName);
            Assert.Equal(string.Empty, user.PasswordHash);
            Assert.Equal("a@b.uz", user.Email);
        }
    }
}
