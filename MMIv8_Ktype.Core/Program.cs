using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson;
using Serilog;
using Serilog.Events;
using MMIv8_Ktype.Models.Collections;
using MMIv8_Ktype.Models;
using MMIv8_Ktype.Core.Services;
using MMIv8_Ktype.Core.Contexts;
using MMIv8_Ktype.Core.Services.Match;
using MMIv8_Ktype.Core.Services.Mapping;
using MMIv8_Ktype.Core.Services.Source;
using System.Reflection;
using MMIv8_Ktype.Models.Util;
using MMIv8_Ktype.Api.Endpoints;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        //BsonDefaults.GuidRepresentationMode = GuidRepresentationMode.V3;

        ConventionRegistry.Register("EnumStringConvention", new ConventionPack { new EnumRepresentationConvention(BsonType.String) }, t => true);
        ConventionRegistry.Register("IgnoreIfNullConvention", new ConventionPack { new IgnoreIfNullConvention(true) }, t => true);


        builder.Services.AddControllers().AddJsonOptions(opts => JsonSerializationOptions.ApplyJsonSettings(opts.JsonSerializerOptions));

        builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(opts => JsonSerializationOptions.ApplyJsonSettings(opts.SerializerOptions));

        builder.Services.AddSingleton<MongoDBContext>();
        builder.Services.AddScoped<MongoBaseContext>();
        builder.Services.AddScoped<MatchEntityContext>();

        builder.Services.AddScoped<VersionService>();
        builder.Services.AddScoped<UserService>();

        builder.Services.AddScoped<IVersionProvider, VersionProviderMongo>();

        builder.Services.AddScoped<SourceTecDocPCService>();
        builder.Services.AddScoped<SourceMMIv8Service>();
        builder.Services.AddScoped<SourceTecDocEntityModelService>();
        builder.Services.AddScoped<SourceMMIv8EntityModelService>();


        builder.Services.AddScoped<MatchEntityService>();
        builder.Services.AddScoped<MatchBaseService>();
        builder.Services.AddScoped<MatchMakeModelService>();

        builder.Services.AddScoped<BulkMappingService>();
        builder.Services.AddScoped<MappingService>();

        builder.Services.AddScoped<SourceMMIv8UpdateService>();
        builder.Services.AddScoped<SourceTecDocPCUpdateService>();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        Log.Logger = new LoggerConfiguration() // Move to App settings
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