namespace SchedulingApp.Api.Infrastructure;

/// <summary>Swagger UI page served from a CDN, pointing at the built-in .NET OpenAPI document.</summary>
internal static class SwaggerUi
{
    public const string Html = """
        <!doctype html>
        <html lang="en">
        <head>
          <meta charset="utf-8" />
          <title>Scheduling App API</title>
          <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/swagger-ui-dist@5.17.14/swagger-ui.css" />
        </head>
        <body>
          <div id="swagger"></div>
          <script src="https://cdn.jsdelivr.net/npm/swagger-ui-dist@5.17.14/swagger-ui-bundle.js"></script>
          <script>
            window.ui = SwaggerUIBundle({ url: '/openapi/v1.json', dom_id: '#swagger', persistAuthorization: true });
          </script>
        </body>
        </html>
        """;
}
