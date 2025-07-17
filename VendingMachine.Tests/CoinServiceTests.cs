using MockQueryable;
using Moq;
using NUnit.Framework.Legacy;
using System.Linq.Expressions;
using VendingMachine.Data.Models;
using VendingMachine.Data.Repositories;
using VendingMachine.Services.DTOs;
using VendingMachine.Services.Services;

namespace VendingMachine.Tests;

[TestFixture]
public class CoinServiceTests
{
    private Mock<IRepository<Coin>> _repositoryMock;
    private CoinService _cut;

    [SetUp]
    public void Setup()
    {
        _repositoryMock = new Mock<IRepository<Coin>>();
        _cut = new CoinService(_repositoryMock.Object);
    }

    [Test]

    public async Task GetAllAsync_ShallReturnAllCoins_WhenNoPredicateIsPassed()
    {
        var sampleCoins = GetSampleCoins();

        _repositoryMock
            .Setup(mock => mock.All())
            .Returns(sampleCoins.BuildMock());

        var actualCoins = await _cut.GetAllAsync();

        Assert.Multiple(() =>
        {
            Assert.That(actualCoins.Count(), Is.EqualTo(sampleCoins.Count()));
            Assert.That(actualCoins, Is.EquivalentTo(sampleCoins));
        });
    }

    [Test]

    public async Task GetAllAsync_ShallFilterCoins_WhenAPredicateIsPassed()
    {
        var sampleCoins = GetSampleCoins();

        _repositoryMock
            .Setup(mock => mock.All())
            .Returns(sampleCoins.BuildMock());

        Expression<Func<Coin, bool>> wherePredicate = coin => coin.Value > 50;

        var filteredCoins = sampleCoins.AsQueryable().Where(wherePredicate);

        var actualCoins = await _cut.GetAllAsync(wherePredicate);

        Assert.Multiple(() =>
        {
            Assert.That(actualCoins.Count(), Is.EqualTo(filteredCoins.Count()));
            Assert.That(actualCoins, Is.EquivalentTo(filteredCoins));
        });
    }

    [Test]

    public async Task GetAllAsync_ShallReturnEmpty_WhenNoCoinsMatchPassedPredicate()
    {
        var sampleCoins = GetSampleCoins();
        _repositoryMock
            .Setup(mock => mock.All())
            .Returns(sampleCoins.BuildMock());

        Expression<Func<Coin, bool>> wherePredicate = coin => coin.Quantity > 100;

        var filteredCoins = sampleCoins.AsQueryable().Where(wherePredicate);

        var actualCoins = await _cut.GetAllAsync(wherePredicate);

        Assert.Multiple(() =>
        {
            Assert.That(actualCoins.Count(), Is.EqualTo(filteredCoins.Count()));
            Assert.That(actualCoins, Is.EquivalentTo(filteredCoins));
        });
    }

    [Test]

    public async Task GetAllAsNoTrackingAsync_ShallReturnAllCoins_WhenNoOrderByDescKeySelectorIsPassed()
    {
        var sampleCoins = GetSampleCoins();

        _repositoryMock
            .Setup(mock => mock.AllAsNoTracking())
            .Returns(sampleCoins.BuildMock());

        var sampleCoinDtos = sampleCoins
            .Select(coin => 
                new CoinDto { 
                    Value = coin.Value, 
                    Quantity = coin.Quantity 
                });

        var actualCoins = await _cut.GetAllAsNoTrackingAsync();

        Assert.Multiple(() =>
        {
            Assert.That(actualCoins.Count(), Is.EqualTo(sampleCoinDtos.Count()));
            foreach (var sample in sampleCoinDtos)
            {
                var actualCoin = actualCoins.FirstOrDefault(
                    actual => actual.Value == sample.Value);

                Assert.That(actualCoin, Is.Not.Null);
                Assert.That(actualCoin?.Value, Is.EqualTo(sample.Value));
            }
        });
    }

    [Test]
    public async Task GetAllAsNoTrackingAsync_ShallReturnAllCoinsInDescOrder_WhenOrderByDescKeySelectorIsPassed()
    {
        var sampleCoins = GetSampleCoins();

        _repositoryMock
            .Setup(mock => mock.AllAsNoTracking())
            .Returns(sampleCoins.BuildMock());

        Expression<Func<Coin, byte>> orderByDescKeySelector = coin => coin.Value;

        var sampleCoinDtos = sampleCoins
            .AsQueryable()
            .OrderByDescending(orderByDescKeySelector)
            .Select(coin =>
                new CoinDto
                {
                    Value = coin.Value,
                    Quantity = coin.Quantity
                })
            .ToList();

        var actualCoins = (await _cut.GetAllAsNoTrackingAsync(orderByDescKeySelector)).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(actualCoins, Has.Count.EqualTo(sampleCoinDtos.Count));

            for (int i = 0; i < sampleCoinDtos.Count; i++)
            {
                Assert.That(actualCoins[i].Value, Is.EqualTo(sampleCoinDtos[i].Value));
                Assert.That(actualCoins[i].Quantity, Is.EqualTo(sampleCoinDtos[i].Quantity));
            }
        });
    }

    [Test]
    public async Task GetAllAsDenominationToValueMap_ShallReturnCorrectMap()
    {
        var sampleCoins = GetSampleCoinDtos()
            .ToDictionary(
                coin => coin.Denomination,
                coin => coin.Value);

        _repositoryMock
            .Setup(mock => mock.AllAsNoTracking())
            .Returns(GetSampleCoins().BuildMock());

        var actualCoins = await _cut.GetAllAsDenominationToValueMap();

        Assert.Multiple(() =>
        {
            Assert.That(actualCoins, Has.Count.EqualTo(sampleCoins.Count));

            foreach (var (denomination, value) in sampleCoins)
            {
                var actualContainsKey = actualCoins
                    .TryGetValue(denomination, out var actualValue);

                Assert.That(actualContainsKey, Is.True);
                Assert.That(actualValue, Is.EqualTo(value));
            }
        });
    }

    private static IEnumerable<CoinDto> GetSampleCoinDtos()
    {
        return
        [
            new CoinDto { Value = 10, Quantity = 100 },
            new CoinDto { Value = 20, Quantity = 20 },
            new CoinDto { Value = 50, Quantity = 30 },
            new CoinDto { Value = 100, Quantity = 17 },
            new CoinDto { Value = 200, Quantity = 20 }
        ];
    }

    private static IEnumerable<Coin> GetSampleCoins()
    {
        return 
        [
            new Coin { Id = 1, Value = 10, Quantity = 100 },
            new Coin { Id = 2, Value = 20, Quantity = 20 },
            new Coin { Id = 3, Value = 50, Quantity = 30 },
            new Coin { Id = 4, Value = 100, Quantity = 17 },
            new Coin { Id = 5, Value = 200, Quantity = 20 }
        ];
    }

}
