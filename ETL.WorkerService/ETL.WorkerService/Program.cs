using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using ETL.Core;
using ETL.Extractors;
using ETL.Staging;
using ETL.WorkerService;

var builder = Host.CreateApplicationBuilder(args);

// 1. Inyeccion de IHttpClientFactory para consumo de API REST
builder.Services.AddHttpClient();

// 2. Inyeccion del servicio de Staging y DataLoader de Dimensiones
var connectionString = builder.Configuration.GetConnectionString("OpinionsDb") 
    ?? "Server=MSI\\SQLEXPRESS;Database=OpinionsBd;Trusted_Connection=True;TrustServerCertificate=True;";
builder.Services.AddSingleton(new StagingService(connectionString));
builder.Services.AddSingleton(new DimensionLoaderService(connectionString));

// 3. Inyeccion de los 3 Extractores (Inversion de dependencias)
var surveysPath = builder.Configuration["EtlSettings:SurveysFilePath"] ?? "data/surveys_part1.csv";
var reviewsTable = builder.Configuration["EtlSettings:ReviewsTableName"] ?? "Web";
var apiUrl = builder.Configuration["EtlSettings:SocialCommentsApiUrl"] ?? "https://api.mock.local/social-comments";

builder.Services.AddTransient<IExtractor<SurveyRawRecord>>(sp => new CsvExtractor(surveysPath));
builder.Services.AddTransient<IExtractor<WebReviewRawRecord>>(sp => new DatabaseExtractor(connectionString, reviewsTable));
builder.Services.AddTransient<IExtractor<SocialCommentRawRecord>>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    return new ApiExtractor(httpClientFactory.CreateClient(), apiUrl);
});

// 4. Registrar el Worker Service como HostedService
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
