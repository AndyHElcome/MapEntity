using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson;
using Serilog;
using Serilog.Events;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Models.Status;
using MMIv8_Ktype.Core.Services;
using MMIv8_Ktype.Core.Contexts;
using MMIv8_Ktype.Core.Services.Match;
using MMIv8_Ktype.Core.Services.Mapping;
using MMIv8_Ktype.Core.Services.Source;
using MMIv8_Ktype.Core.Endpoints;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;
using MMIv8_Ktype.Models.Util;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        //BsonDefaults.GuidRepresentationMode = GuidRepresentationMode.V3;

        ConventionRegistry.Register("EnumStringConvention", new ConventionPack { new EnumRepresentationConvention(BsonType.String) }, t => true);
        ConventionRegistry.Register("IgnoreIfNullConvention", new ConventionPack { new IgnoreIfNullConvention(true) }, t => true);

        JsonOptions jsonOptions = new();
        jsonOptions.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        jsonOptions.JsonSerializerOptions.Converters.Add(new JsonObjectIdConverter());
        jsonOptions.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        jsonOptions.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
        jsonOptions.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        jsonOptions.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        jsonOptions.JsonSerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver();
        jsonOptions.JsonSerializerOptions.WriteIndented = true;



        builder.Services.AddControllers().AddJsonOptions(opts => JsonSerializationOptions.ApplyJsonSettings(opts.JsonSerializerOptions));

        builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(opts => JsonSerializationOptions.ApplyJsonSettings(opts.SerializerOptions));

        //builder.Services.AddControllers().AddJsonOptions(x =>
        //{
        //    // serialize enums as strings in api responses 
        //    x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        //    x.JsonSerializerOptions.Converters.Add(new JsonObjectIdConverter());

        //    // ignore omitted parameters on models to enable optional params 
        //    x.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

        //    x.JsonSerializerOptions.WriteIndented = true;
        //});

        builder.Services.AddSingleton<MongoDBContext>();
        builder.Services.AddScoped<MongoBaseContext>();
        builder.Services.AddScoped<MatchEntityContext>();

        builder.Services.AddScoped<VersionService>();
        builder.Services.AddScoped<UserService>();

        builder.Services.AddScoped<IVersionProvider, VersionProviderMongo>();

        builder.Services.AddScoped<ISourceEntityService<MongoSourceTecDocPC>, SourceTecDocPCService>();
        builder.Services.AddScoped<ISourceEntityService<MongoSourceMMIv8>, SourceMMIv8Service>();
        builder.Services.AddScoped<SourceTecDocEntityModelService>();
        builder.Services.AddScoped<SourceMMIv8EntityModelService>();

        builder.Services.AddScoped<MatchEntityService>();
        builder.Services.AddScoped<MatchBaseService>();
        builder.Services.AddScoped<MatchMakeModelService>();

        builder.Services.AddScoped<BulkMappingService>();
        builder.Services.AddScoped<MappingService>();


        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate: "[{Level:u3}] {Message:l}{NewLine}{Exception}")
            .WriteTo.File($"..\\MMIv8_Ktype.Core\\Logs\\Log.txt",
                          rollOnFileSizeLimit: true,
                          fileSizeLimitBytes: 1048576,
                          shared: true,
                          outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:l}{NewLine}{Exception}",
                          restrictedToMinimumLevel: LogEventLevel.Information)
            .CreateLogger();

        builder.Services.AddSingleton(Log.Logger);

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

        app.UseAuthorization();

        app.MapControllers();
        app.MapEndpoints();

        app.Run();
    }
}