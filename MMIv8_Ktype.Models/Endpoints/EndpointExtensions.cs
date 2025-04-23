using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MMIv8_Ktype.Models.ApiServices;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Util;
using Refit;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MMIv8_Ktype.Models.Endpoints
{
    public static class EndpointExtensions
    {
        public static T Refit<T>()
            where T : IEndpoint
        {
            var groupName = GetGroupName<T>();
            var test = RestService.For<T>("https://localhost:44304/" + groupName, new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer(new System.Text.Json.JsonSerializerOptions().GetJsonSerializerOptions())
            });
            return RestService.For<T>("https://localhost:44304/"+ groupName, new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer( new System.Text.Json.JsonSerializerOptions().GetJsonSerializerOptions())
            });
        }

        public static string GetGroupName<T>()
            where T : IEndpoint
        {
            return typeof(T).GetCustomAttribute<GroupName>()?.Name ?? typeof(T).Name;
        }

        public static string GetGroupName(this Type type)
        {
            return type.GetCustomAttribute<GroupName>()?.Name ?? type.Name;
        }

        public static void MapEndpointsFromInterface(IEndpointRouteBuilder routeBuilder, IEndpoint implementation, Type abstraction)
        {
            var methods = abstraction.GetMethods();

            var groupName = abstraction.GetGroupName() ?? abstraction.Name;
            var group = routeBuilder.MapGroup(groupName).WithTags(groupName);

            foreach (var method in methods)
            {
                var test = method.GetCustomAttributes<HttpMethodAttribute>();

                foreach(var httpMethodAttribute in method.GetCustomAttributes<HttpMethodAttribute>())
                {
                    if (httpMethodAttribute is null)
                        continue;

                    var path = httpMethodAttribute.Path;
                    var methodName = method.Name;

                    // Get actual MethodInfo from the implementation
                    var implMethod = implementation.GetType().GetMethod(methodName);

                    var returnType = method.ReturnType;
                    var parameters = method.GetParameters();

                    Delegate handler = parameters.Length switch
                    {
                        0 => Delegate.CreateDelegate(
                            Expression.GetDelegateType(returnType),
                            implementation,
                            implMethod),

                        1 => Delegate.CreateDelegate(
                            Expression.GetDelegateType(new[] { parameters[ 0 ].ParameterType, returnType }),
                            implementation,
                            implMethod),

                        _ => throw new NotSupportedException("Only methods with 0 or 1 parameter are supported")
                    };

                    _ = httpMethodAttribute.Method.Method switch
                    {
                        "GET" => group.MapGet(path, handler).WithName(methodName),
                        "PUT" => group.MapPut(path, handler).WithName(methodName),
                        "POST" => group.MapPost(path, handler).WithName(methodName),
                        "DELETE" => group.MapDelete(path, handler).WithName(methodName),
                        //"HEAD" => _,
                        //"OPTIONS" => _,
                        //"TRACE" => _,
                        "PATCH" => group.MapPatch(path, handler).WithName(methodName),
                        //"CONNECT" => _,
                        _ => throw new NotSupportedException($"Do not recognise HttpMethod {httpMethodAttribute.Method}")
                    };
                }
            }
        }

        public static TypeInfo[] GetEnpointInterfaces()
        {
            return Assembly.GetExecutingAssembly().DefinedTypes
                            .Where(type => type.IsInterface && 
                                   type != typeof(IEndpoint) &&
                                   type.IsAssignableTo(typeof(IEndpoint)))
                            .ToArray();
        }

        public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
        {
            foreach(var interfaceType in GetEnpointInterfaces())
            {
                var serviceDescriptors = assembly.DefinedTypes
                 .Where(type => type is { IsAbstract: false, IsInterface: false } && type.IsAssignableTo(interfaceType))
                 .Select(type => ServiceDescriptor.Transient(interfaceType, type));

                services.TryAddEnumerable(serviceDescriptors);
            }

            return services;
        }

        public static IApplicationBuilder MapEndpoints(this WebApplication app, RouteGroupBuilder? routeGroupBuilder = null)
        {
            IEndpointRouteBuilder builder = routeGroupBuilder is null ? app : routeGroupBuilder;

            using var scope = app.Services.CreateScope();
            foreach(var service in GetEnpointInterfaces())
            {
                var impl = scope.ServiceProvider.GetRequiredService(service);

                MapEndpointsFromInterface(builder, (IEndpoint)impl, service);
            }
            
            return app;
        }
    }
}
