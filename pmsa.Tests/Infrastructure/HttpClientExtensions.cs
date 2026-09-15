using System.Net;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

namespace pmsa.Tests.Infrastructure;

/// <summary>
/// Drives the application the way a browser does: fetch the page, read its anti-forgery token,
/// post the form back. Tests that bypassed the form would not exercise what a real sign-in does.
/// </summary>
public static class HttpClientExtensions
{
    private static readonly HtmlParser Parser = new();

    public static async Task<IHtmlDocument> GetDocumentAsync(this HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await ParseAsync(response);
    }

    public static async Task<IHtmlDocument> ParseAsync(this HttpResponseMessage response) =>
        Parser.ParseDocument(await response.Content.ReadAsStringAsync());

    /// <summary>Posts a form with the anti-forgery token taken from a freshly fetched page.</summary>
    public static async Task<HttpResponseMessage> PostFormAsync(
        this HttpClient client, string url, IDictionary<string, string> fields, string? formUrl = null)
    {
        var document = await client.GetDocumentAsync(formUrl ?? url);
        var token = (document.QuerySelector("input[name='__RequestVerificationToken']") as IHtmlInputElement)?.Value
                    ?? throw new InvalidOperationException($"No anti-forgery token on {formUrl ?? url}.");

        var payload = new Dictionary<string, string>(fields) { ["__RequestVerificationToken"] = token };
        return await client.PostAsync(url, new FormUrlEncodedContent(payload));
    }

    /// <summary>Signs in through the sign-in page and returns the response, following no redirects.</summary>
    public static Task<HttpResponseMessage> SignInAsync(
        this HttpClient client, string email, string password, string? returnUrl = null)
    {
        var url = returnUrl is null
            ? "/Account/SignIn"
            : $"/Account/SignIn?returnUrl={Uri.EscapeDataString(returnUrl)}";

        return client.PostFormAsync(
            url,
            new Dictionary<string, string> { ["Input.Email"] = email, ["Input.Password"] = password },
            formUrl: url);
    }

    /// <summary>Signs in and asserts that it worked, for tests whose subject is what happens next.</summary>
    public static async Task SignInSuccessfullyAsync(this HttpClient client, string email, string password)
    {
        var response = await client.SignInAsync(email, password);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.False(
            response.Headers.Location?.OriginalString.Contains("SignIn", StringComparison.OrdinalIgnoreCase),
            "Sign-in was refused when the test expected it to succeed.");
    }

    public static bool RedirectsToSignIn(this HttpResponseMessage response) =>
        response.StatusCode is HttpStatusCode.Redirect &&
        response.Headers.Location?.OriginalString.Contains("/Account/SignIn", StringComparison.OrdinalIgnoreCase) is true;

    public static string Text(this IHtmlDocument document) => document.Body?.TextContent ?? string.Empty;
}
