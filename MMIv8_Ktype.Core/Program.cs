using Microsoft.OpenApi;
using MMIv8_Ktype.Api;
using MMIv8_Ktype.Api.Endpoints;
using MMIv8_Ktype.Core.Contexts;
using MMIv8_Ktype.Core.Exceptions;
using MMIv8_Ktype.Core.Services;
using MMIv8_Ktype.Core.Services.Mapping;
using MMIv8_Ktype.Core.Services.Match;
using MMIv8_Ktype.Core.Services.Source;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models.Util;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Conventions;
using Serilog;
using Serilog.Events;
using System.Reflection;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        Log.Logger = new LoggerConfiguration() // Move to App settings
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message:l}{NewLine}{Exception}")
            .WriteTo.File($"..\\MMIv8_Ktype.Core\\Logs\\Log.txt",
                          rollOnFileSizeLimit: true,
                          fileSizeLimitBytes: 1048576,
                          shared: true,
                          outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:l}{NewLine}{Exception}",
                          restrictedToMinimumLevel: LogEventLevel.Information)
            .CreateLogger();

        builder.Services.AddSingleton(Log.Logger);

        //BsonDefaults.GuidRepresentationMode = GuidRepresentationMode.V3;
        JsonWriterSettings.Defaults.OutputMode = JsonOutputMode.Shell;

        ConventionRegistry.Register("EnumStringConvention", new ConventionPack { new EnumRepresentationConvention(BsonType.String) }, t => true);
        ConventionRegistry.Register("IgnoreIfNullConvention", new ConventionPack { new IgnoreIfNullConvention(true) }, t => true);
        

        //builder.Services.AddControllers().AddJsonOptions(opts => JsonSerializationOptions.ApplyJsonSettings(opts.JsonSerializerOptions)); // Not required if not controllers

        //builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(opts => JsonSerializationOptions.ApplyJsonSettings(opts.SerializerOptions));


        builder.Services.AddControllers().AddJsonOptions(opts => opts.JsonSerializerOptions.GetJsonSerializerOptions()); // Not required if not controllers

        builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(opts => opts.SerializerOptions.GetJsonSerializerOptions());

        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        builder.Services.AddSingleton<MongoDBContext>();

        builder.Services.AddScoped<VersionService>();
        builder.Services.AddScoped<EntityRelationService>();
        builder.Services.AddScoped<UserService>();

        builder.Services.AddScoped<IVersionProvider, VersionProviderMongo>();

        builder.Services.AddScoped<SourceTecDocPCService>();
        builder.Services.AddScoped<SourceMMIv8Service>();
        builder.Services.AddScoped<SourceTecDocEntityModelService>();
        builder.Services.AddScoped<SourceMMIv8EntityModelService>();

        builder.Services.AddScoped<MatchEntityService>();
        //builder.Services.AddScoped<MatchRefineService>();
        builder.Services.AddScoped<MatchBaseService>();
        builder.Services.AddScoped<MatchMakeModelService>();

        builder.Services.AddScoped<BulkMappingService>();
        builder.Services.AddScoped<MappingService>();

        builder.Services.AddScoped<SourceMMIv8MatchRefineService>();
        builder.Services.AddScoped<SourceTecDocPCMatchRefineService>();
        builder.Services.AddScoped<SourceMMIv8UpdateService>();
        builder.Services.AddScoped<SourceTecDocPCUpdateService>();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

 



        builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        //if (app.Environment.IsDevelopment())
        //{
        //    app.UseSwagger();
        //    app.UseSwaggerUI();
        //}

        app.UseSwagger();

        app.UseSwaggerUI();

        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MongoDBContext>();
        }

        app.UseHttpsRedirection();

        app.UseExceptionHandler();

        app.UseAuthorization();

        app.MapControllers();
        app.MapEndpoints();

        app.Run();
    }
}