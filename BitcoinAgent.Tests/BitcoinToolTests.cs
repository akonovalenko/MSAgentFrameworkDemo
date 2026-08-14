using System.Net;
using System.Text;
using System.Text.Json;
using BitcoinAgent.Domain.Models;
using BitcoinAgent.Infrastructure.Services;
using BitcoinAgent.Infrastructure.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BitcoinAgent.Tests;

public class BitcoinToolTests
{
    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<HttpResponseMessage>(cancellationToken);
            }

            return Task.FromResult(_responder(request));
        }
    }

    [Fact]
    public async Task GetHistoricalPriceAsync_ReturnsBitcoinPriceModel()
    {
        // Arrange
        var expected = 1000.00m;
        var json = JsonSerializer.Serialize(new { market_data = new { current_price = new { usd = expected } } });

        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.coingecko.com/api/v3/")
        };

        var serviceLogger = NullLogger<BitcoinService>.Instance;
        var toolLogger = NullLogger<BitcoinTool>.Instance;

        var service = new BitcoinService(client, serviceLogger);
        var tool = new BitcoinTool(service, toolLogger);

        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var result = await tool.GetHistoricalPriceAsync(date);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("BTC", result.Symbol);
        Assert.Equal("USD", result.Currency);
        Assert.Equal(expected, result.Price);
        Assert.Equal(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), result.Timestamp);
    }

    [Fact]
    public async Task GetCurrentPriceAsync_ReturnsBitcoinPriceModel()
    {
        // Arrange
        var expected = 25000.50m;
        var json = JsonSerializer.Serialize(new { bitcoin = new { usd = expected } });

        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

    private sealed class FakeBitcoinService : IBitcoinService
    {
        private readonly Func<Task<decimal>> _currentPriceResponder;
        private readonly Func<DateOnly, Task<decimal>> _historicalResponder;

        public FakeBitcoinService(Func<Task<decimal>> currentPriceResponder, Func<DateOnly, Task<decimal>> historicalResponder)
        {
            _currentPriceResponder = currentPriceResponder;
            _historicalResponder = historicalResponder;
        }

        public Task<decimal> GetBitcoinPriceAsync(CancellationToken cancellationToken = default) => _currentPriceResponder();

        public Task<decimal> GetHistoricalBitcoinPriceAsync(DateOnly date, CancellationToken cancellationToken = default) => _historicalResponder(date);
    }

    [Fact]
    public async Task GetCurrentPriceAsync_ServiceThrows_PropagatesException()
    {
        // Arrange
        var service = new FakeBitcoinService(() => throw new InvalidOperationException("bad"), _ => Task.FromResult(0m));
        var tool = new BitcoinTool(service, NullLogger<BitcoinTool>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => tool.GetCurrentPriceAsync());
    }

    [Fact]
    public async Task GetHistoricalPriceAsync_ServiceCanceled_PropagatesOperationCanceled()
    {
        // Arrange
        var service = new FakeBitcoinService(() => Task.FromResult(0m), _ => throw new OperationCanceledException());
        var tool = new BitcoinTool(service, NullLogger<BitcoinTool>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => tool.GetHistoricalPriceAsync(DateOnly.FromDateTime(DateTime.UtcNow)));
    }

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.coingecko.com/api/v3/")
        };

        var serviceLogger = NullLogger<BitcoinService>.Instance;
        var toolLogger = NullLogger<BitcoinTool>.Instance;

        var service = new BitcoinService(client, serviceLogger);
        var tool = new BitcoinTool(service, toolLogger);

        // Act
        var result = await tool.GetCurrentPriceAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("BTC", result.Symbol);
        Assert.Equal("USD", result.Currency);
        Assert.Equal(expected, result.Price);
        Assert.True((DateTimeOffset.UtcNow - result.Timestamp).Duration() < TimeSpan.FromSeconds(10));
    }
}
