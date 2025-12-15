using Microsoft.AspNetCore.Authorization;

namespace WebApi.Filters.Attributes
{
    public class CrudAuthorizeAttribute
        : AuthorizeAttribute
    {
        public CrudAuthorizeAttribute(string action, string entity)
        {
            Policy = $"{action}_{entity}";
        }
    }
}
