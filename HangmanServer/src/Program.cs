
using HangmanServer.src.Multiplayer.SignalR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Net.WebSockets;
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
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ClockSkew = TimeSpan.Zero
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            if (context.Request.Cookies.ContainsKey("AuthCookie"))
                            {
                                context.Token = context.Request.Cookies["AuthCookie"];
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
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHsts();

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            app.MapHub<MultiplayerHub>("api/v1/MultiplayerHub");

            Task.Run(Server.UpdateThread);
            Task.Run(Server.CommandThread);

            app.Run();
        }
    }
}
