using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BitcoinAgent.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BitcoinAgent.Tests;

public class BitcoinServiceTests
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
    public async Task GetHistoricalBitcoinPriceAsync_ReturnsExpectedPrice()
    {
        // Arrange
        var expected = 54321.21m;
        var json = JsonSerializer.Serialize(new { market_data = new { current_price = new { usd = expected } } });

        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.coingecko.com/api/v3/")
        };

        var logger = NullLogger<BitcoinService>.Instance;

        var service = new BitcoinService(client, logger);

        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var actual = await service.GetHistoricalBitcoinPriceAsync(date);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task GetBitcoinPriceAsync_NonSuccess_ThrowsHttpRequestException()
    {
        // Arrange
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.coingecko.com/api/v3/")
        };

        var logger = NullLogger<BitcoinService>.Instance;
        var service = new BitcoinService(client, logger);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => service.GetBitcoinPriceAsync());
    }

    [Fact]
    public async Task GetBitcoinPriceAsync_MissingJson_ThrowsInvalidOperationException()
    {
        // Arrange
        var json = JsonSerializer.Serialize(new { });

        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.coingecko.com/api/v3/")
        };

        var logger = NullLogger<BitcoinService>.Instance;
        var service = new BitcoinService(client, logger);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetBitcoinPriceAsync());
    }

    [Fact]
    public async Task GetBitcoinPriceAsync_Canceled_ThrowsOperationCanceledException()
    {
        // Arrange
        var expected = 32100.12m;
        var json = JsonSerializer.Serialize(new { bitcoin = new { usd = expected } });

        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.coingecko.com/api/v3/")
        };

        var logger = NullLogger<BitcoinService>.Instance;
        var service = new BitcoinService(client, logger);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => service.GetBitcoinPriceAsync(cts.Token));
    }

    [Fact]
    public async Task GetBitcoinPriceAsync_ReturnsExpectedPrice()
    {
        // Arrange
        var expected = 32100.12m;
        var json = JsonSerializer.Serialize(new { bitcoin = new { usd = expected } });

        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.coingecko.com/api/v3/")
        };

        var logger = NullLogger<BitcoinService>.Instance;

        var service = new BitcoinService(client, logger);

        // Act
        var actual = await service.GetBitcoinPriceAsync();

        // Assert
        Assert.Equal(expected, actual);
    }
}
