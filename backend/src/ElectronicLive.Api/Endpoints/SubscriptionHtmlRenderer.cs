using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ElectronicLive.Api.Endpoints;

internal static class SubscriptionHtmlRenderer
{
    public static ContentHttpResult Success(string message) =>
        Render("Unsubscribed", message, isSuccess: true, StatusCodes.Status200OK);

    public static ContentHttpResult NotFound(string message) =>
        Render("Unsubscribe Error", message, isSuccess: false, StatusCodes.Status404NotFound);

    public static ContentHttpResult BadRequest(string message) =>
        Render("Unsubscribe Error", message, isSuccess: false, StatusCodes.Status400BadRequest);

    private static ContentHttpResult Render(string title, string message, bool isSuccess, int statusCode)
    {
        var icon = isSuccess ? "✅" : "⚠️";
        var encodedTitle = WebUtility.HtmlEncode(title);
        var encodedMessage = WebUtility.HtmlEncode(message);

        var html = $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>ElectronicLive - {{encodedTitle}}</title>
              <style>
                body {
                  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
                  background-color: #0f172a;
                  color: #f8fafc;
                  display: flex;
                  justify-content: center;
                  align-items: center;
                  min-height: 100vh;
                  margin: 0;
                  padding: 1rem;
                  box-sizing: border-box;
                }
                .card {
                  background-color: #1e293b;
                  border: 1px solid #334155;
                  border-radius: 12px;
                  padding: 2rem;
                  max-width: 480px;
                  width: 100%;
                  text-align: center;
                  box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.3);
                }
                .icon {
                  font-size: 2.5rem;
                  margin-bottom: 1rem;
                }
                h1 {
                  font-size: 1.5rem;
                  font-weight: 600;
                  margin: 0 0 0.75rem 0;
                  color: #f8fafc;
                }
                p {
                  color: #94a3b8;
                  font-size: 1rem;
                  line-height: 1.5;
                  margin: 0 0 1.5rem 0;
                }
                .brand {
                  font-size: 0.875rem;
                  color: #64748b;
                  font-weight: 500;
                }
              </style>
            </head>
            <body>
              <div class="card">
                <div class="icon">{{icon}}</div>
                <h1>{{encodedTitle}}</h1>
                <p>{{encodedMessage}}</p>
                <div class="brand">ElectronicLive London EDM Tracker</div>
              </div>
            </body>
            </html>
            """;

        return TypedResults.Content(html, "text/html; charset=utf-8", Encoding.UTF8, statusCode: statusCode);
    }
}
