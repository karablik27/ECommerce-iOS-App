using System;
using System.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using MMLib.SwaggerForOcelot.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// 1) Грузим сначала ocelot.json (он лежит в корне проекта рядом с csproj)
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// 2) HTTP-клиенты для проксирования swagger.json downstream
builder.Services.AddHttpClient("OrdersSvc", c =>
{
    c.BaseAddress = new Uri("http://orderservice");
    c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});
builder.Services.AddHttpClient("PaymentsSvc", c =>
{
    c.BaseAddress = new Uri("http://paymentservice");
    c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

// 3) Ocelot + SwaggerForOcelot
builder.Services.AddOcelot(builder.Configuration);
builder.Services.AddSwaggerForOcelot(builder.Configuration);

// 4) Swagger-UI для самого Gateway
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("gateway", new OpenApiInfo { Title = "ECommerce API Gateway", Version = "v1" });
});

var app = builder.Build();
app.UseRouting();

// 5) Проксируем raw swagger.json downstream
app.MapWhen(ctx => ctx.Request.Path.Value!
        .Equals("/swagger/orderservice/swagger.json", StringComparison.OrdinalIgnoreCase),
    b => b.RunProxy("OrdersSvc", "/swagger/v1/swagger.json"));
app.MapWhen(ctx => ctx.Request.Path.Value!
        .Equals("/swagger/paymentservice/swagger.json", StringComparison.OrdinalIgnoreCase),
    b => b.RunProxy("PaymentsSvc", "/swagger/v1/swagger.json"));

// 6) UI Gateway на http://localhost:5003/swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.RoutePrefix = "swagger";
    c.SwaggerEndpoint("/swagger/gateway/swagger.json", "Gateway v1");
    c.SwaggerEndpoint("/swagger/orderservice/swagger.json", "Orders Service");
    c.SwaggerEndpoint("/swagger/paymentservice/swagger.json", "Payments Service");
});

// 7) Проксируем все Ocelot-маршруты
app.UseEndpoints(endpoints => endpoints.MapControllers());
await app.UseOcelot();
app.Run();

/// Прокси-хелпер с подробным логированием ошибок downstream
static class ProxyExtensions
{
    private static readonly string[] _hopHeaders = new[]
    {
        "connection", "keep-alive", "proxy-authenticate", "proxy-authorization",
        "te", "trailer", "transfer-encoding", "upgrade", "content-length"
    };

    public static void RunProxy(this IApplicationBuilder app, string clientName, string downstreamPath)
    {
        app.Run(async ctx =>
        {
            var factory = ctx.RequestServices.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient(clientName);

            var req = new HttpRequestMessage(new HttpMethod(ctx.Request.Method), downstreamPath)
            {
                Content = (ctx.Request.ContentLength > 0 ||
                           ctx.Request.Headers.ContainsKey("Transfer-Encoding"))
                    ? new StreamContent(ctx.Request.Body)
                    : null
            };
            if (req.Content != null && ctx.Request.ContentType != null)
                req.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(ctx.Request.ContentType);

            // копируем заголовки, кроме hop-by-hop
            foreach (var header in ctx.Request.Headers)
                if (!_hopHeaders.Contains(header.Key, StringComparer.OrdinalIgnoreCase))
                    req.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());

            HttpResponseMessage resp;
            try
            {
                resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ctx.RequestAborted);
            }
            catch (Exception ex)
            {
                ctx.Response.StatusCode = 502;
                await ctx.Response.WriteAsync($"[ProxyError:{clientName}] {ex.GetType().Name}: {ex.Message}");
                return;
            }

            // проксируем ответ upstream обратно клиенту
            ctx.Response.StatusCode = (int)resp.StatusCode;
            foreach (var header in resp.Headers)
                if (!_hopHeaders.Contains(header.Key, StringComparer.OrdinalIgnoreCase))
                    ctx.Response.Headers[header.Key] = header.Value.ToArray();
            foreach (var header in resp.Content.Headers)
                if (!_hopHeaders.Contains(header.Key, StringComparer.OrdinalIgnoreCase))
                    ctx.Response.Headers[header.Key] = header.Value.ToArray();

            await resp.Content.CopyToAsync(ctx.Response.Body);
        });
    }
}
