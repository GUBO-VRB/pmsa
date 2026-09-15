using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;

namespace pmsa.Security;

/// <summary>
/// NFR-005: pages are served with <c>Cache-Control: no-store</c> so that pressing Back after
/// signing out cannot reveal data from the previous session (SC-007).
/// </summary>
/// <remarks>
/// Applied to every Razor Page rather than only to authenticated ones — the sign-in page carries
/// an anti-forgery token and a submitted email address, neither of which should be cached either.
/// Static assets are served outside the page pipeline and keep their own long-lived caching.
/// </remarks>
public class NoStoreFilter : IAsyncPageFilter
{
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var headers = context.HttpContext.Response.Headers;
        headers[HeaderNames.CacheControl] = "no-store, no-cache, must-revalidate";
        headers[HeaderNames.Pragma] = "no-cache";
        await next();
    }
}
