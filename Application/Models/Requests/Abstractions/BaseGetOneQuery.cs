using Domain.Behaviours;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models.Requests.Abstractions
{
    public record BaseGetOneQuery
    {
        [Required(ErrorMessage = "Id is required.")]
        [NotEmptyGuid(ErrorMessage = "Id is required.")]
        public Guid Id { get; init; }
    }
}
