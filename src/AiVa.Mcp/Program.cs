using AiVa.Mcp;
using AiVa.Mcp.Calendar;
using AiVa.Mcp.Mail;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// stdout carries the MCP protocol, so every log line must go to stderr.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddSingleton(AiVaOptions.Load());
builder.Services.AddSingleton<CredentialProvider>();
builder.Services.AddSingleton<MailService>();
builder.Services.AddSingleton<CalDavClient>();

builder.Services
    .AddMcpServer(o => o.ServerInfo = new() { Name = "ai-va", Version = "1.0.0" })
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
