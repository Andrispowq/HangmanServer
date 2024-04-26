
using HangmanServer.src.Multiplayer.SignalR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace HangmanServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            if (!Server.InitialiseServer())
            {
                Console.WriteLine("Error while starting server!\nShutting down...");
                return;
            }

            if(!Directory.Exists("HangmanServerData/secret"))
            {
                Directory.CreateDirectory("HangmanServerData/secret");
            }
            if(!File.Exists("HangmanServerData/secret/jwt_key"))
            {
                string secret = Utils.GenerateEncryptionKey();
                File.WriteAllText("HangmanServerData/secret/jwt_key", secret);
            }

            string JWTSecret = File.ReadAllText("HangmanServerData/secret/jwt_key");
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JWTSecret)),
                        ValidateIssuer = true,
                        ValidIssuer = "https://hangman.mptrdev.com",
                        ValidateAudience = false,
                        ClockSkew = TimeSpan.Zero
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            if (context.Request.Cookies.ContainsKey("AuthToken"))
                            {
                                context.Token = context.Request.Cookies["AuthToken"];
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddControllers();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddSignalR();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHsts();

            app.MapControllers();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<MultiplayerHub>("api/v1/MultiplayerHub");

            Task.Run(Server.UpdateThread);
            Task.Run(Server.CommandThread);

            app.Run();
        }
    }
}
