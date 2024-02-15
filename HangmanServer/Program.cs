
using Microsoft.AspNetCore.Hosting;

namespace HangmanServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //Task.Run(Server.CommandThread);

            // Specify the URLs to listen on
            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                // Setup a HTTP and HTTPS endpoint
                serverOptions.ListenAnyIP(6969); // Listen for HTTP connections on port 5000
                serverOptions.ListenAnyIP(6970, listenOptions =>
                {
                    listenOptions.UseHttps(); // Listen for HTTPS connections on port 5001
                });
            });

            Server.InitialiseServer();

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            //if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
