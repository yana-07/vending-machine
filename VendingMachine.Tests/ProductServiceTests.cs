using Moq;
using NUnit.Framework.Legacy;
using System.Linq.Expressions;
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
        public async Task GetByCodeAsync_ShouldReturnCorrectProduct_ForGivenCode(
            int id, string code, string name, int price, byte quantity)
        {
            var product = new Product
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
                .ReturnsAsync(product);

            var realProduct = await _cut.GetByCodeAsync(code);

            Assert.Multiple(() =>
            {
                Assert.That(realProduct.Id, Is.EqualTo(product.Id));
                Assert.That(realProduct.Code, Is.EqualTo(product.Code));
                Assert.That(realProduct.Name, Is.EqualTo(product.Name));
                Assert.That(realProduct.Quantity, Is.EqualTo(product.Quantity));
                Assert.That(realProduct.Price, Is.EqualTo(product.Price));
            });
        }
    }
}
