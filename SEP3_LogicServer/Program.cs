using System;
using Microsoft.Extensions.DependencyInjection;
using Grpc.Net.ClientFactory;
using Repositories;
using RepositoryContracts;
using SEP3_LogicServer.Services;
using Sep3_Proto;
// setting the developer environment manually
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    EnvironmentName = Environments.Development
});

// Allow HTTP/2 without TLS (only for local/dev) - add this if we don't use https
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);


// Register the gRPC generated client for homogenious service
builder.Services.AddGrpcClient<Sep3_Proto.homogeniousService.homogeniousServiceClient>(options =>
{
    options.Address = new Uri("http://localhost:7991"); // Java gRPC server address
});

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<GameService>();

var app = builder.Build();

// checking environment 
Console.WriteLine($"Environment: {app.Environment.EnvironmentName}");
Console.WriteLine(app.Environment.IsDevelopment());

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();
app.MapControllers();
app.Run();
