using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace RevenueRecognitionSystem.Api.OpenApi;

public class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header
        };

        var bearerRef = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        };
        document.SecurityRequirements.Add(new OpenApiSecurityRequirement
        {
            [bearerRef] = Array.Empty<string>()
        });

        return Task.CompletedTask;
    }
}
