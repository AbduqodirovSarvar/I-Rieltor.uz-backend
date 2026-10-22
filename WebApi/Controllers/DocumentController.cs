using Application.Features.PrivateDocuments;
using Application.Interfaces.EntityServices;
using Application.Messaging;
using Domain.Behaviours;
using Domain.Enums.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;
using WebApi.Controllers.Abstractions;
using WebApi.Extensions;

namespace WebApi.Controllers
{
    /// <summary>
    /// Personal documents (identity papers, powers of attorney, payment receipts): kept in a folder no URL leads to
    /// and given back only to their owner or to staff with READ_PRIVATEDOCUMENT.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DocumentController(IMediator mediator, IPrivateDocumentService documents) : BaseController(mediator)
    {
        /// <summary>Uploads a document (multipart form): pdf, png, jpg or docx, up to 10 MB.</summary>
        [HttpPost]
        [EnableRateLimiting(WebApiDependencyInjection.SessionRateLimitPolicy)]
        public async Task<IActionResult> Upload([FromForm] UploadPrivateDocumentCommand command)
            => Ok(await _mediator.Send(command));

        /// <summary>Your own documents.</summary>
        [HttpGet("my/list")]
        public async Task<IActionResult> GetMine([FromQuery] GetMyPrivateDocumentsQuery query)
            => Ok(await _mediator.Send(query, HttpContext.RequestAborted));

        /// <summary>
        /// Everyone's documents. Filter by auction: <c>Filters[auctionId.equals]=...</c>, by owner: <c>Filters[ownerId.equals]=...</c>.
        /// </summary>
        [Authorize(Policy = nameof(PrivateDocumentActions.READ_PRIVATEDOCUMENT))]
        [HttpGet("list")]
        public async Task<IActionResult> GetAll([FromQuery] GetPrivateDocumentListQuery query)
            => Ok(await _mediator.Send(query, HttpContext.RequestAborted));

        /// <summary>
        /// The file itself (as an attachment, never shown by the browser on its own). The owner or staff only; for others the
        /// answer is 404. Every download is recorded in the audit log.
        /// </summary>
        [HttpGet("download")]
        public async Task<IActionResult> Download([FromQuery, Required, NotEmptyGuid] Guid id)
        {
            var file = await documents.DownloadAsync(id, HttpContext.RequestAborted);
            Response.Headers.CacheControl = "private, no-store";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            // The file may be an image or pdf from a stranger: it must not run anything in our origin.
            Response.Headers["Content-Security-Policy"] = "sandbox; default-src 'none'";
            return File(file.Content, file.ContentType, file.FileName);
        }

        /// <summary>Deletes your own document (staff with DELETE_PRIVATEDOCUMENT: anyone's).</summary>
        [HttpDelete]
        public async Task<IActionResult> Delete(DeletePrivateDocumentCommand command)
            => Ok(await _mediator.Send(command));
    }
}
