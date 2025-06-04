using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MMIv8_Ktype.Models.Attributes;
using Refit;

namespace MMIv8_Ktype.Api.Endpoints
{
    public static class EndpointExtensions
    {
        public static string GetGroupName<T>()
            where T : IEndpoint
        {
            return typeof(T).GetCustomAttribute<GroupName>()?.Name ?? typeof(T).Name;
        }

        public static string GetGroupName(this Type type)
        {
            return type.GetCustomAttribute<GroupName>()?.Name ?? type.Name;
        }

        public static List<MethodInfo> GetInterfaceMethods(this Type type)
        {
            List<MethodInfo> methods = [.. type.GetMethods()];
            foreach (var interfaceType in type.GetInterfaces())
            {
                methods.AddRange(interfaceType.GetMethods());
            }

            return methods;
        }

        public static void MapEndpointsFromInterface(IEndpointRouteBuilder routeBuilder, IEndpoint implementation, Type abstraction)
        {
            var methods = abstraction.GetInterfaceMethods();

            var groupName = abstraction.GetGroupName() ?? abstraction.Name;
            var group = routeBuilder.MapGroup(groupName).WithTags(groupName);

            foreach (var method in methods)
            {
                foreach(var httpMethodAttribute in method.GetCustomAttributes<HttpMethodAttribute>())
                {
                    if (httpMethodAttribute is null)
                        continue;

                    var path = httpMethodAttribute.Path;

                    // Get actual MethodInfo from the implementation
                    var implMethod = implementation.GetType().GetMethod(method.Name);

                    var returnType = method.ReturnType;
                    var parameters = method.GetParameters();

                    Delegate handler = parameters.Length switch
                    {
                        0 => Delegate.CreateDelegate(
                                Expression.GetDelegateType(returnType),
                                implementation,
                                implMethod),

                        1 => Delegate.CreateDelegate(
                                Expression.GetDelegateType([parameters[ 0 ].ParameterType, returnType]),
                                implementation,
                                implMethod),

                        _ => Delegate.CreateDelegate(
                                Expression.GetDelegateType([.. parameters.Select(c => c.ParameterType), returnType]),
                                implementation,
                                implMethod)
                    };

                    var methodName = $"{groupName}_{method.Name}";

                    _ = httpMethodAttribute.Method.Method switch
                    {
                        "GET" => group.MapGet(path, handler).WithName(methodName).AddEndpointFilter<ResultEndpointFilter>(),
                        "PUT" => group.MapPut(path, handler).WithName(methodName).AddEndpointFilter<ResultEndpointFilter>(),
                        "POST" => group.MapPost(path, handler).WithName(methodName).AddEndpointFilter<ResultEndpointFilter>(),
                        "DELETE" => group.MapDelete(path, handler).WithName(methodName).AddEndpointFilter<ResultEndpointFilter>(),
                        //"HEAD" => _,
                        //"OPTIONS" => _,
                        //"TRACE" => _,
                        "PATCH" => group.MapPatch(path, handler).WithName(methodName).AddEndpointFilter<ResultEndpointFilter>(),
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
