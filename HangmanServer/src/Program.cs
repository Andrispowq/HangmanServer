
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

            /*builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.ListenAnyIP(6969);
                serverOptions.ListenAnyIP(6970, listenOptions =>
                {
                    listenOptions.UseHttps();
                });
            });*/

            if(!Server.InitialiseServer())
            {
                Console.WriteLine("Error while starting server!\nShutting down...");
                return;
            }

            builder.Services.AddControllers();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            /*builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowedOrigins", policy =>
                {
                    policy.WithOrigins("http://localhost:8000")
                                 .AllowAnyMethod()
                                 .AllowAnyHeader()
                                 .AllowCredentials();
                });
            });*/

            builder.Services.AddSignalR();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHsts();
            //app.UseCors("AllowedOrigins");

            //app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            app.MapHub<MultiplayerHub>("api/v1/MultiplayerHub");

            Task.Run(Server.UpdateThread);
            Task.Run(Server.CommandThread);

            app.Run();
        }
    }
}
