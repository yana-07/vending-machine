using MockQueryable;
using Moq;
using System.Linq.Expressions;
using VendingMachine.Common.Exceptions;
using VendingMachine.Data.Models;
using VendingMachine.Data.Repositories;
using VendingMachine.Services.Services;

namespace VendingMachine.Tests
{
    [TestFixture]
    public class ProductServiceTests
    {
        private Mock<IRepository<Product>> _repositoryMock;
        private ProductService _cut;

        [SetUp]
        public void Setup()
        {
            _repositoryMock = new Mock<IRepository<Product>>();
            _cut = new ProductService(_repositoryMock.Object);
        }

        [TestCase(1, "A1", "Lorem", 70, 2)]
        [TestCase(2, "B7", "Ipsum", 150, 10)]
        [TestCase(3, "01", "Dolor", 230, 7)]
        public async Task GetByCodeAsync_ShallReturnCorrectProduct_ForGivenExistingCode(
            int id, string code, string name, int price, byte quantity)
        {
            var expected = new Product
            {
                Id = id,
                Code = code,
                Name = name,
                Price = price,
                Quantity = quantity
            };

            _repositoryMock.Setup(
                mock => mock.FirstOrDefaultAsync(
                    It.IsAny<Expression<Func<Product, bool>>>()))
                .ReturnsAsync(expected);

            var actual = await _cut.GetByCodeAsync(code);

            Assert.Multiple(() =>
            {
                Assert.That(actual.Id, Is.EqualTo(expected.Id));
                Assert.That(actual.Code, Is.EqualTo(expected.Code));
                Assert.That(actual.Name, Is.EqualTo(expected.Name));
                Assert.That(actual.Price, Is.EqualTo(expected.Price));
                Assert.That(actual.Quantity, Is.EqualTo(expected.Quantity));
            });
        }

        [Test]
        public void GetByCodeAsync_ShallThrowProductNotFoundException_ForNonExistingProductCode()
        {
            var expected = new Product
            {
                Id = 1,
                Code = "C4",
                Name = "Some Name",
                Price = 120,
                Quantity = 8
            };

            _repositoryMock.Setup(
                mock => mock.FirstOrDefaultAsync(
                    productEntity => productEntity.Code == expected.Code))
                .ReturnsAsync(expected);

            Assert.ThrowsAsync<ProductNotFoundException>(async () => await _cut.GetByCodeAsync("A7"));
        }

        [Test]
        public async Task GetAllAsNoTrackingAsync_ShallReturnCorrectCollection()
        {
            Product[] expectedProducts = [
                new Product { Id = 1, Code = "01", Name = "Some Name", Price = 199, Quantity = 1 },
                new Product { Id = 8, Code = "03", Name = "Test", Price = 170, Quantity = 10 },
                new Product { Id = 12, Code = "05", Name = "Name", Price = 100, Quantity = 7 }];

            var asyncMock = expectedProducts.AsQueryable().BuildMock();

            _repositoryMock.Setup(
                mock => mock.AllAsNoTracking())
                .Returns(asyncMock);

            var actualProducts = await _cut.GetAllAsNoTrackingAsync();
            var actualProductsAsList = actualProducts.ToList();

            Assert.Multiple(() =>
            {
                for (int i = 0; i < expectedProducts.Length; i++)
                {
                    var expected = expectedProducts[i];
                    var actual = actualProductsAsList[i];

                    Assert.That(actual.Code, Is.EqualTo(expected.Code));
                    Assert.That(actual.Name, Is.EqualTo(expected.Name));
                    Assert.That(actual.Price, Is.EqualTo(expected.Price));
                    Assert.That(actual.Quantity, Is.EqualTo(expected.Quantity));
                }
            });
        }

        [Test]
        public async Task GetAllCodesAsync_ShallReturnCorrectStringCollection()
        {
            Product[] expectedProducts = [
                new Product { Id = 1, Code = "A1", Name = "Lorem", Price = 100, Quantity = 6 },
                new Product { Id = 8, Code = "B4", Name = "Ipsum", Price = 200, Quantity = 4 },
                new Product { Id = 12, Code = "T1", Name = "Dolor", Price = 300, Quantity = 7 }];

            var asyncMock = expectedProducts.AsQueryable().BuildMock();

            _repositoryMock.Setup(
                mock => mock.AllAsNoTracking())
                .Returns(asyncMock);

            var actualProductCodes = await _cut.GetAllCodesAsync();
            var expectedProductCodes = expectedProducts.Select(product => product.Code);

           Assert.That(actualProductCodes, Is.EqualTo(expectedProductCodes));
        }

        [TestCase(1, "A1", "Lorem", 70, 2)]
        [TestCase(2, "B7", "Ipsum", 150, 10)]
        [TestCase(3, "01", "Dolor", 230, 7)]
        public async Task DecreaseInventory_ShallDecreaseInventoryByOne_IfCalledOnce
            (int id, string code, string name, int price, byte quantity)
        {
            var expected = new Product
            {
                Id = id,
                Code = code,
                Name = name,
                Price = price,
                Quantity = quantity
            };

            _repositoryMock.Setup(
                mock => mock.FirstOrDefaultAsync(
                    It.IsAny<Expression<Func<Product, bool>>>()))
                .ReturnsAsync(expected);

            await _cut.DecreaseInventoryAsync(code);

            Assert.That(expected.Quantity, Is.EqualTo(quantity - 1));
        }

        [TestCase(1, "A1", "Lorem", 70, 5)]
        [TestCase(2, "B7", "Ipsum", 150, 10)]
        [TestCase(3, "01", "Dolor", 230, 7)]
        public async Task DecreaseInventory_ShallDecreaseInventoryByThreee_IfCalledThreeTimes
            (int id, string code, string name, int price, byte quantity)
        {
            var expected = new Product
            {
                Id = id,
                Code = code,
                Name = name,
                Price = price,
                Quantity = quantity
            };

            _repositoryMock.Setup(
                mock => mock.FirstOrDefaultAsync(
                    It.IsAny<Expression<Func<Product, bool>>>()))
                .ReturnsAsync(expected);

            await _cut.DecreaseInventoryAsync(code);
            await _cut.DecreaseInventoryAsync(code);          
            await _cut.DecreaseInventoryAsync(code);

            Assert.That(expected.Quantity, Is.EqualTo(quantity - 3));
        }
    }
}
