using CYarp.Client;
using CYarp.Client.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CYarpClientAspNetCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.Configure<CYarpClientOptions>(builder.Configuration.GetSection(nameof(CYarpClientOptions)));

            // ע��cyarp�ļ�����
            builder.Services.AddCYarpListener();

            builder.WebHost.ConfigureKestrel(kestrel =>
            {
                kestrel.ListenLocalhost(5000);

                // ����һ��cyarp
                var endPoint = new CYarpEndPoint(kestrel.ApplicationServices.GetRequiredService<IOptions<CYarpClientOptions>>().Value);
                kestrel.Listen(endPoint);
            });

            var app = builder.Build();
            app.UseStaticFiles();
            app.UseRouting();
            app.MapGet("/", context =>
            {
                return context.Response.WriteAsync("ok");
            });
            app.Run();
        }
    }
}
