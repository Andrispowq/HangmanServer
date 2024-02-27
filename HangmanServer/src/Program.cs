
using HangmanServer.src.Multiplayer.SignalR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Net.WebSockets;

namespace HangmanServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            Task.Run(Server.CommandThread);

            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.ListenAnyIP(6969);
                serverOptions.ListenAnyIP(6970, listenOptions =>
                {
                    listenOptions.UseHttps();
                });
            });

            Server.InitialiseServer();

            builder.Services.AddControllers();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", policyBuilder =>
                    policyBuilder.WithOrigins("http://localhost:8000") // Replace with the client's origin
                                 .AllowAnyMethod()
                                 .AllowAnyHeader()
                                 .AllowCredentials());
            });

            builder.Services.AddSignalR();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHsts();
            app.UseCors("CorsPolicy");

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            app.MapHub<MultiplayerHub>("/MultiplayerHub");

            app.Run();
        }
    }
}
