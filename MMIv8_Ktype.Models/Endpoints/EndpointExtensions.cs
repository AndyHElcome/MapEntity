using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
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
                var postAttr = method.GetCustomAttribute<PostAttribute>();
                if (postAttr != null)
                {
                    var path = postAttr.Path;
                    var methodName = method.Name;

                    // Get actual MethodInfo from the implementation
                    var implMethod = implementation.GetType().GetMethod(methodName);

                    var x = method.GetParameters();
                    var y = method.GetParameters().Length;

                    var returnType = method.ReturnType;
                    var parameters = method.GetParameters();

                    // This assumes method returns Task<IResult> and takes a single parameter or none
                    //Delegate handler = method.GetParameters().Length switch
                    //{
                    //    0 => Delegate.CreateDelegate(typeof(Func<Task<IResult>>), implementation, implMethod),
                    //    1 => Delegate.CreateDelegate(typeof(Func<,>).MakeGenericType(method.GetParameters()[ 0 ].ParameterType, typeof(Task<MatchMakeModel?>)), implementation, implMethod),
                    //    _ => throw new NotSupportedException("Only methods with 0 or 1 parameter are supported")
                    //};

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

                    group.MapPost(path, handler).WithName(methodName);
                }

                // Handle other verbs (Get, Put, etc.) similarly
            }
        }

        public static List<(Type IType, Type CType)> GetEndpointServiceInterface(Assembly assembly) //FOR INTERFACE
        {
            List<(Type IType, Type CType)> result = new();

            var endpointTypes = assembly.DefinedTypes
                             .Where(type => type is { IsAbstract: false, IsInterface: false } && type.IsAssignableTo(typeof(IEndpoint)))
                             .Select(type => (Type)type)
                             .ToArray();

            foreach (var endpointType in endpointTypes)
            {
                var serviceDescriptor = endpointType.GetInterfaces()
                    .Where(interfaceType => interfaceType != typeof(IEndpoint) && interfaceType.IsAssignableTo(typeof(IEndpoint)))
                    .Select(interfaceType => (interfaceType, endpointType))
                    .ToArray();

                result.AddRange(serviceDescriptor);
            }

            return result;
        }

        //public static List<(Type IType, Type CType)> GetEndpointServiceInterface(Assembly assembly)
        //{
        //    List<(Type IType, Type CType)> result = new();

        //    var endpointTypes = assembly.DefinedTypes
        //                     .Where(type => type is { IsAbstract: false, IsInterface: false } && type.IsAssignableTo(typeof(IEndpoint)))
        //                     .Select(type => (Type)type)
        //                     .ToArray();

        //    foreach (var endpointType in endpointTypes)
        //    {
        //        var serviceDescriptor = Assembly.GetExecutingAssembly().DefinedTypes
        //                         .Where(type => type is { IsAbstract: true, IsInterface: false } && type.IsAssignableTo(typeof(IEndpoint)))
        //                         .Select(interfaceType => ((Type)interfaceType, endpointType))
        //                         .ToArray();

        //        result.AddRange(serviceDescriptor);
        //    }

        //    return result;
        //}

        public static TypeInfo[] GetEnpointInterfaces(Assembly assembly)
        {
            return assembly.DefinedTypes
                            .Where(type => type is { IsAbstract: false, IsInterface: false } && type.IsAssignableTo(typeof(IEndpoint)))
                            .ToArray();
        }

        public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
        {
            var serviceDescriptor = GetEndpointServiceInterface(assembly)
                .Select(interfaceType => ServiceDescriptor.Transient(interfaceType.IType, interfaceType.CType));

            services.TryAddEnumerable(serviceDescriptor);

            return services;
        }

        public static IApplicationBuilder MapEndpoints(this WebApplication app, Assembly assembly, RouteGroupBuilder? routeGroupBuilder = null)
        {
            IEndpointRouteBuilder builder = routeGroupBuilder is null ? app : routeGroupBuilder;

            using var scope = app.Services.CreateScope();
            foreach(var service in GetEndpointServiceInterface(assembly))
            {
                var impl = scope.ServiceProvider.GetRequiredService(service.IType);

                MapEndpointsFromInterface(builder, (IEndpoint)impl, service.IType);
            }
            
            return app;
        }
    }
}
