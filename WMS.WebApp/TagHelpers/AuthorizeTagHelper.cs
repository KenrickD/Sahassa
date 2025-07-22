using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace WMS.WebApp.TagHelpers
{
    [HtmlTargetElement("*", Attributes = "asp-authorize")]
    [HtmlTargetElement("*", Attributes = "asp-authorize-policy")]
    [HtmlTargetElement("*", Attributes = "asp-authorize-roles")]
    [HtmlTargetElement("*", Attributes = "asp-authorize-permission")]
    public class AuthorizeTagHelper : TagHelper
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthorizeTagHelper(
            IAuthorizationService authorizationService,
            IHttpContextAccessor httpContextAccessor)
        {
            _authorizationService = authorizationService;
            _httpContextAccessor = httpContextAccessor;
        }

        [HtmlAttributeName("asp-authorize")]
        public bool Authorize { get; set; }

        [HtmlAttributeName("asp-authorize-policy")]
        public string Policy { get; set; }

        [HtmlAttributeName("asp-authorize-roles")]
        public string Roles { get; set; }

        [HtmlAttributeName("asp-authorize-permission")]
        public string Permission { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var user = _httpContextAccessor.HttpContext.User;

            if (!user.Identity.IsAuthenticated && Authorize)
            {
                output.SuppressOutput();
                return;
            }

            if (!string.IsNullOrEmpty(Policy))
            {
                var authorized = await _authorizationService.AuthorizeAsync(user, Policy);
                if (!authorized.Succeeded)
                {
                    output.SuppressOutput();
                    return;
                }
            }

            if (!string.IsNullOrEmpty(Roles))
            {
                var roleArray = Roles.Split(',').Select(r => r.Trim());
                if (!roleArray.Any(role => user.IsInRole(role)))
                {
                    output.SuppressOutput();
                    return;
                }
            }

            if (!string.IsNullOrEmpty(Permission))
            {
                if (!user.HasClaim("Permission", Permission))
                {
                    output.SuppressOutput();
                    return;
                }
            }
        }
    }
}