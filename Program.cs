using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

builder.Services.AddScoped<PagamentoService>();

builder.Services.AddScoped<ClienteService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    app.UseSwagger();

    app.UseSwaggerUI();
}

// ENDPOINTS
app.MapClienteEndpoints();
app.MapPagamentoEndpoints();
app.MapRelatorioEndpoints();

app.Run();