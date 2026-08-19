using SummerProject.Server.Bootstrap;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServerServices(builder.Configuration);

var app = builder.Build();

app.MapServerEndpoints();

app.Run();

public partial class Program;