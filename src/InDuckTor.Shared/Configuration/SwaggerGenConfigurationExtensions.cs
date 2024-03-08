using System.Diagnostics;
using InDuckTor.Shared.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace InDuckTor.Shared.Configuration;

public static class SwaggerGenConfigurationExtensions
{
    public static void ConfigureJwtAuth(SwaggerGenOptions options)
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

    public static void ConfigureEnumMemberValues(SwaggerGenOptions options)
    {
        options.SchemaFilter<EnumMemberSchemaFilter>();
    }
}

/// <summary>
/// Описывает в Swagger значения enum при помощи EnumMemberAttribute
/// </summary>
public class EnumMemberSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type is not { IsEnum: true } enumType) return;

        var enumMemberDefinitions = new List<IOpenApiAny>();
        var enumValues = Enum.GetValues(enumType);
        foreach (var enumValue in enumValues)
        {
            if (!EnumExtensions.TryGetEnumMemberName(enumType, (Enum)enumValue, out var enumMemberValue)) continue;
            enumMemberDefinitions.Add(new OpenApiString(enumMemberValue));
        }

        if (enumMemberDefinitions.Count > 0)
        {
            Debug.Assert(enumMemberDefinitions.Count == enumValues.Length, "Не все элементы enum не описаны с EnumMemberAttribute");
            schema.Enum = enumMemberDefinitions;
            schema.Type = "string";
            schema.Format = null;
        }
    }
}