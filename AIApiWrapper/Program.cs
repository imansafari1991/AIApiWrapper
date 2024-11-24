
using Microsoft.Extensions.DependencyInjection;
using NSwag.AspNetCore;
using RestSharp;
namespace AIApiWrapper
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddAuthorization();

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            builder.Services.AddOpenApiDocument();
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseOpenApi();
                app.UseSwaggerUi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            var summaries = new[]
            {
                "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
            };

            app.MapGet("/weatherforecast", (HttpContext httpContext) =>
            {
                var forecast = Enumerable.Range(1, 5).Select(index =>
                    new WeatherForecast
                    {
                        Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                        TemperatureC = Random.Shared.Next(-20, 55),
                        Summary = summaries[Random.Shared.Next(summaries.Length)]
                    })
                    .ToArray();
                return forecast;
            })
            .WithName("GetWeatherForecast");
            app.MapPost("/CallGPT", async (string prompt, HttpContext httpContext) =>
            {
                var aiService = new AIService();
                return await aiService.GetChatGptResponse(prompt);
            })
            .WithName("CallGPT");
            app.MapPost("/ElevenLabs/TextToSpeech", async (string prompt, decimal stability = 0.5m,
                decimal similarityBoost = 0.5m,
                decimal style = 0.5m,
                string voice= "9BWtsMINqrJLrRacOk9x"
                ) =>
            {
                var aiService = new AIService();
                
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "output_audio.mp3");

                await aiService.ElevenLabsTextToSpeech(prompt,"output_audio.mp3", stability,similarityBoost,style);
                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                return Results.File(fileBytes, "audio/mpeg", "output_audio.mp3");


            })
            .WithName("TextToSpeech");
            app.Run();
        }
    }
    public class AIService
    {
        private readonly string _apiKey = "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";

        public async Task<string> GetChatGptResponse(string prompt)
        {
            var client = new RestClient("https://api.openai.com/v1/chat/completions");
            var request = new RestRequest("", Method.Post);

            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Authorization", $"Bearer {_apiKey}");

            var body = new
            {
                model = "gpt-4o",  // or another GPT model
                messages = new[]
                {
                new { role = "system", content = "You are a helpful assistant." },
                new { role = "user", content =  prompt}
            },
                max_tokens = 150
            };

            request.AddJsonBody(body);

            var response = await client.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                return response.Content; // return the response from the API
            }
            else
            {
                throw new Exception($"Error: {response.ErrorMessage}");
            }
        }

        public async Task ElevenLabsTextToSpeech(string prompt,string fileName,decimal stability ,
                decimal similarityBoost ,
                decimal style )
        {
            var client = new RestClient("https://api.elevenlabs.io/v1/text-to-speech/9BWtsMINqrJLrRacOk9x");
            var request = new RestRequest("",Method.Post);
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("xi-api-key", "sk_cab967f249e7c30dd24a4bf85de5e47d54009081e69f5a5e");
            request.AddJsonBody(new
            {
                text = prompt,
                model_id = "eleven_turbo_v2_5",
                language_code = "en",
                voice_settings = new
                {
                    stability = stability,
                    similarity_boost = similarityBoost,
                    style = style,
                    use_speaker_boost = true
                },
                seed = 123,
                use_pvc_as_ivc = true,
                apply_text_normalization = "auto"
            });

            var response = client.Execute(request);

            if (response.IsSuccessful)
            {
                // Save the file to disk
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "output_audio.mp3");
                File.WriteAllBytes(filePath, response.RawBytes);
                

                Console.WriteLine($"File saved at: {filePath}");
            }
            else
            {
                Console.WriteLine($"Error: {response.ErrorMessage}");
            }
        }
    }
}
