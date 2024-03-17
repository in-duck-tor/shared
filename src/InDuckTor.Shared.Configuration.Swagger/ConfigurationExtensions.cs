using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace InDuckTor.Shared.Configuration.Swagger;

public static class ConfigurationExtensions
{
    public static void ConfigureJwtAuth(this SwaggerGenOptions options)
    {
        options.AddSecurityDefinition("auth", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                        { Type = ReferenceType.SecurityScheme, Id = "auth" }
                },
                [ ]
            }
        });
    }

    public static void ConfigureEnumMemberValues(this SwaggerGenOptions options)
    {
        options.SchemaFilter<EnumMemberSchemaFilter>();
    }
}