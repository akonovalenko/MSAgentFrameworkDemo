global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
global using System.Text.Json;
global using System.Text.Json.Serialization;

namespace BitcoinAgent.Application;

public delegate Task AgentDelegate(
    AgentContext context,
    CancellationToken cancellationToken);
