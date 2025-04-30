using System.Net.Http;
using Microsoft.AspNetCore.Mvc; // Needed for [FromHeader] if used, but HttpRequest works directly

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient(); // Enable HttpClientFactory
// Optional: If you needed IHttpContextAccessor elsewhere, add it: builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// !!! === IMPORTANT: REPLACE THE URL BELOW === !!!
// Use the URL of YOUR backend app (e.g., https://my-unique-backend-app-net8-123.azurewebsites.net/api/data)
string backendUrl = "https://my-unique-apim-net8-789.azure-api.net/backend-net8/data";

// Use HttpRequest parameter to access headers directly (preferred in .NET 7+)
app.MapGet("/callbackend", async (IHttpClientFactory clientFactory, HttpRequest request) =>
{
    var client = clientFactory.CreateClient();
    string backendResponse = "Error calling backend."; // Default message
    try
    {
        // Forward traceparent and tracestate headers from incoming request
        if (request.Headers.TryGetValue("traceparent", out var traceparentValue))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("traceparent", traceparentValue.ToString());
        }
        if (request.Headers.TryGetValue("tracestate", out var tracestateValue))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("tracestate", tracestateValue.ToString());
        }

        var response = await client.GetAsync(backendUrl);
        response.EnsureSuccessStatusCode();
        backendResponse = await response.Content.ReadAsStringAsync();
        return Results.Ok(new { FrontendSays = "Called backend VIA APIM! (.NET 8)", BackendSays = backendResponse });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error calling backend: {ex.ToString()}");
        return Results.Problem($"Error calling backend: {ex.Message}. Backend URL tried: {backendUrl}. Response: {backendResponse}");
    }
});

app.MapGet("/", () => "Hello from Frontend (.NET 8)! Go to /callbackend to test.");

app.Run();
