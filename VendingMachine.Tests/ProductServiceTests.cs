using MockQueryable;
using Moq;
using System.Linq.Expressions;
using VendingMachine.Common.Constants;
using VendingMachine.Common.Exceptions;
using VendingMachine.Data.Models;
using VendingMachine.Data.Repositories;
using VendingMachine.Services.DTOs;
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
        public async Task GetByCodeAsync_ShallReturnCorrectProduct_ForExistingProductCode(
            int id, string code, string name, int price, byte quantity)
        {
            var expectedProduct = new Product
            {
                Id = id,
                Code = code,
                Name = name,
                PriceInStotinki = price,
                Quantity = quantity
            };

            _repositoryMock.Setup(
                mock => mock.FirstOrDefaultAsync(
                    It.IsAny<Expression<Func<Product, bool>>>()))
                .ReturnsAsync(expectedProduct);

            var actualProduct = await _cut.GetByCodeAsync(code);

            Assert.Multiple(() =>
            {
                Assert.That(actualProduct.Id, Is.EqualTo(expectedProduct.Id));
                Assert.That(actualProduct.Code, Is.EqualTo(expectedProduct.Code));
                Assert.That(actualProduct.Name, Is.EqualTo(expectedProduct.Name));
                Assert.That(actualProduct.PriceInStotinki, Is.EqualTo(expectedProduct.PriceInStotinki));
                Assert.That(actualProduct.Quantity, Is.EqualTo(expectedProduct.Quantity));
            });
        }

        [Test]
        public void GetByCodeAsync_ShallThrowException_ForNonExistentProductCode()
        {
            var product = new Product
            {
                Id = 1,
                Code = "C4",
                Name = "Some Name",
                PriceInStotinki = 120,
                Quantity = 8
            };

            _repositoryMock.Setup(
                mock => mock.FirstOrDefaultAsync(
                    productEntity => productEntity.Code == product.Code))
                .ReturnsAsync(product);

            const string Code = "A7";

            var ex = Assert.ThrowsAsync<ProductNotFoundException>(
                async () => await _cut.GetByCodeAsync(Code));

            Assert.That(ex.Message, Is.EqualTo($"Product with code {Code} does not exist."));
        }

        [Test]
        public async Task GetAllAsNoTrackingAsync_ShallReturnCorrectCollection()
        {
            Product[] expectedProducts = [
                new Product { Id = 1, Code = "01", Name = "Some Name", PriceInStotinki = 199, Quantity = 1 },
                new Product { Id = 8, Code = "03", Name = "Test", PriceInStotinki = 170, Quantity = 10 },
                new Product { Id = 12, Code = "05", Name = "Name", PriceInStotinki = 100, Quantity = 7 }];

            var asyncMock = expectedProducts.BuildMock();

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
                    Assert.That(actual.PriceInStotinki, Is.EqualTo(expected.PriceInStotinki));
                    Assert.That(actual.Quantity, Is.EqualTo(expected.Quantity));
                }
            });
        }

        [Test]
        public async Task GetAllCodesAsync_ShallReturnCorrectStringCollection()
        {
            Product[] expectedProducts = [
                new Product { Id = 1, Code = "A1", Name = "Lorem", PriceInStotinki = 100, Quantity = 6 },
                new Product { Id = 8, Code = "B4", Name = "Ipsum", PriceInStotinki = 200, Quantity = 4 },
                new Product { Id = 12, Code = "T1", Name = "Dolor", PriceInStotinki = 300, Quantity = 7 }];

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
        public async Task DecreaseInventoryAsync_ShallDecreaseInventoryByOne_WhenCalledOnce
            (int id, string code, string name, int price, byte quantity)
        {
            var product = new Product
            {
                Id = id,
                Code = code,
                Name = name,
                PriceInStotinki = price,
                Quantity = quantity
            };

            _repositoryMock.Setup(
                mock => mock.FirstOrDefaultAsync(
                    It.IsAny<Expression<Func<Product, bool>>>()))
                .ReturnsAsync(product);

            await _cut.DecreaseInventoryAsync(code);

            Assert.That(product.Quantity, Is.EqualTo(quantity - 1));
        }

        [TestCase(1, "A1", "Lorem", 70, 5)]
        [TestCase(2, "B7", "Ipsum", 150, 10)]
        [TestCase(3, "01", "Dolor", 230, 7)]
        public async Task DecreaseInventoryAsync_ShallDecreaseInventoryByThreee_WhenCalledThreeTimes
            (int id, string code, string name, int price, byte quantity)
        {
            var product = new Product
            {
                Id = id,
                Code = code,
                Name = name,
                PriceInStotinki = price,
                Quantity = quantity
            };

            _repositoryMock.Setup(
                mock => mock.FirstOrDefaultAsync(
                    It.IsAny<Expression<Func<Product, bool>>>()))
                .ReturnsAsync(product);

            await _cut.DecreaseInventoryAsync(code);
            await _cut.DecreaseInventoryAsync(code);          
            await _cut.DecreaseInventoryAsync(code);

            Assert.That(product.Quantity, Is.EqualTo(quantity - 3));
        }

        [TestCase(7, "02", "Lorem", 120, 0)]
        [TestCase(8, "B3", "Ipsum", 50, 0)]
        [TestCase(8, "04", "Dolor", 90, 0)]
        public async Task DecreaseInventoryAsync_ShallReturnFailureOperationResult_WhenProductIsOutOfStock
            (int id, string code, string name, int price, byte quantity)
        {
            var product = new Product
            {
                Id = id,
                Code = code,
                Name = name,
                PriceInStotinki = price,
                Quantity = quantity
            };

            _repositoryMock.Setup(
                mock => mock.FirstOrDefaultAsync(
                    It.IsAny<Expression<Func<Product, bool>>>()))
                .ReturnsAsync(product);

            var operationResult = await _cut.DecreaseInventoryAsync(code);

            Assert.Multiple(() =>
            {
                Assert.That(operationResult.IsSuccess, Is.False);
                Assert.That(operationResult.ErrorMessage,
                    Is.EqualTo($"Product with code {code} is out of stock."));
            });
        }

        [TestCase("02", ProductConstants.MinQuantity)]
        [TestCase("B3", ProductConstants.MaxQuantity)]
        [TestCase("04", 1)]
        [TestCase("05", 9)]
        public async Task UpdateQuantityAsync_ShallWorkCorrectly_ForValidQuantity
            (string code, byte quantity)
        {
            var product = new Product
            {
                Id = 1,
                Code = code,
                Name = "Name",
                PriceInStotinki = 120,
                Quantity = 5
            };

            _repositoryMock.Setup(
                mock => mock.FirstOrDefaultAsync(
                    It.IsAny<Expression<Func<Product, bool>>>()))
                .ReturnsAsync(product);


            var quantityUpdateDto = new ProductQuantityUpdateDto
            {
                Code = code,
                Quantity = quantity
            };

            await _cut.UpdateQuantityAsync(quantityUpdateDto);

            Assert.That(product.Quantity, Is.EqualTo(quantityUpdateDto.Quantity));
        }

        [TestCase(ProductConstants.MaxQuantity + 1)]
        [TestCase(ProductConstants.MaxQuantity + 5)]
        [TestCase(ProductConstants.MaxQuantity + 10)]
        public async Task UpdateQuantityAsync_ShallReturnFailureOperationResult_WhenQuantityIsAboveMaximum(byte quantity)
        {
            var product = new Product
            {
                Id = 2,
                Code = "B56",
                Name = "Some Name",
                PriceInStotinki = 140,
                Quantity = 2
            };

            _repositoryMock.Setup(
                mock => mock.FirstOrDefaultAsync(
                    It.IsAny<Expression<Func<Product, bool>>>()))
                .ReturnsAsync(product);


            var quantityUpdateDto = new ProductQuantityUpdateDto
            {
                Code = "A7",
                Quantity = quantity
            };

            var operationResult = await _cut.UpdateQuantityAsync(quantityUpdateDto);

            Assert.Multiple(() =>
            {
                Assert.That(operationResult.IsSuccess, Is.False);
                Assert.That(operationResult.ErrorMessage,
                    Is.EqualTo($"Product quantity cannot exceed {ProductConstants.MaxQuantity}."));
            });
        }

        [TestCase(7, "02", "Lorem", 120, 0)]
        [TestCase(8, "B3", "Ipsum", 50, 1)]
        [TestCase(8, "04", "Dolor", 90, 2)]
        public async Task RemoveAsync_ShallWorkCorrectly_ForExistingProduct
           (int id, string code, string name, int price, byte quantity)
        {
            var product = new Product
            {
                Id = id,
                Code = code,
                Name = name,
                PriceInStotinki = price,
                Quantity = quantity
            };

            _repositoryMock.Setup(
                mock => mock.FirstOrDefaultAsync(
                    It.IsAny<Expression<Func<Product, bool>>>()))
                .ReturnsAsync(product);

            await _cut.RemoveAsync(code);

            _repositoryMock.Verify(repo => repo.Delete(product), Times.Once);
            _repositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
        }

        [Test]
        public void RemoveAsync_ShallThrowException_ForNonExistentProduct()
        {
            const string ProductCode = "NONEXISTENT";

            _repositoryMock.Setup(
                mock => mock.FirstOrDefaultAsync(
                    It.IsAny<Expression<Func<Product, bool>>>()))
                .ThrowsAsync(new ProductNotFoundException(ProductCode));

            var ex = Assert.ThrowsAsync<ProductNotFoundException>(
                async () => await _cut.RemoveAsync(ProductCode));

            Assert.That(ex.Message, Is.EqualTo($"Product with code {ProductCode} does not exist."));
        }

        [TestCase("02")]
        [TestCase("B3")]
        [TestCase("04")]
        public async Task AddAsync_ShallReturnFailureOperationResult_ForExistingProduct(string code)
        {
            var product = new Product
            {
                Id = 3,
                Code = code,
                Name = "Lorem Impsum",
                PriceInStotinki = 75,
                Quantity = 5
            };

            _repositoryMock.Setup(
                mock => mock.FirstOrDefaultAsync(
                    It.IsAny<Expression<Func<Product, bool>>>()))
                .ReturnsAsync(product);

            var productDto = new ProductDto
            {
                Code = code,
                Name = "Test",
                PriceInStotinki = 50,
                Quantity = 8
            };

            var  operationResult = await _cut.AddAsync(productDto);

            Assert.Multiple(() =>
            {
                Assert.That(operationResult.IsSuccess, Is.False);
                Assert.That(operationResult.ErrorMessage,
                    Is.EqualTo($"Product with code {code} already exists."));
            });

            _repositoryMock.Verify(repo => repo.Delete(It.IsAny<Product>()), Times.Never);
            _repositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Never);
        }

        [TestCase(ProductConstants.MaxQuantity + 1)]
        [TestCase(ProductConstants.MaxQuantity + 5)]
        [TestCase(ProductConstants.MaxQuantity + 10)]
        public async Task AddAsync_ShallReturnFailureOperationResult_WhenQuantityIsAboveMaximum(byte quantity)
        {
            _repositoryMock.Setup(
                mock => mock.FirstOrDefaultAsync(
                    It.IsAny<Expression<Func<Product, bool>>>()))
                .ReturnsAsync(It.IsAny<Product>);

            var productDto = new ProductDto
            {
                Code = "A1",
                Name = "Test",
                PriceInStotinki = 50,
                Quantity = quantity
            };

            var operationResult = await _cut.AddAsync(productDto);

            Assert.Multiple(() =>
            {
                Assert.That(operationResult.IsSuccess, Is.False);
                Assert.That(operationResult.ErrorMessage,
                    Is.EqualTo($"Product quantity cannot exceed {ProductConstants.MaxQuantity}."));
            });
        }

        [TestCase(-1)]
        [TestCase(-122)]
        [TestCase(-250)]
        public async Task AddAsync_ShallReturnFailureOperationResult_ForNegativePrice(int price)
        {
            _repositoryMock.Setup(
                mock => mock.FirstOrDefaultAsync(
                    It.IsAny<Expression<Func<Product, bool>>>()))
                .ReturnsAsync(It.IsAny<Product>);

            var productDto = new ProductDto
            {
                Code = "A1",
                Name = "Test",
                PriceInStotinki = price,
                Quantity = 7
            };

            var operationResult = await _cut.AddAsync(productDto);

            Assert.Multiple(() =>
            {
                Assert.That(operationResult.IsSuccess, Is.False);
                Assert.That(operationResult.ErrorMessage,
                    Is.EqualTo("Product price cannot be negative."));
            });
        }

        [TestCase("B5", "Lorem", 120, 1)]
        [TestCase("B6", "Impsum", 130, 2)]
        [TestCase("B7", "Dolor", 140, 3)]
        public async Task AddAsync_ShallWorkCorrectly_ForCorrectInput(
            string code, string name, int price, byte quantity)
        {
            _repositoryMock.Setup(
                mock => mock.FirstOrDefaultAsync(
                    It.IsAny<Expression<Func<Product, bool>>>()))
                .ReturnsAsync(It.IsAny<Product>());

            var productToAdd = new ProductDto
            {
                Code = code,
                Name = name,
                PriceInStotinki = price,
                Quantity = quantity
            };

            await _cut.AddAsync(productToAdd);

            _repositoryMock.Verify(repo => repo.Add(It.IsAny<Product>()), Times.Once);
            _repositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
        }

        [TestCase("02", 10)]
        [TestCase("B3", 150)]
        [TestCase("04", 200)]
        public async Task UpdatePriceAsync_ShallWorkCorrectly
            (string code, int price)
        {
            var product = new Product
            {
                Id = 1,
                Code = code,
                Name = "Name",
                PriceInStotinki = 120,
                Quantity = 5
            };

            _repositoryMock.Setup(
                mock => mock.FirstOrDefaultAsync(
                    It.IsAny<Expression<Func<Product, bool>>>()))
                .ReturnsAsync(product);

            var priceUpdateDto = new ProductPriceUpdateDto
            {
                Code = code,
                Price = price
            };

            await _cut.UpdatePriceAsync(priceUpdateDto);

            Assert.That(product.PriceInStotinki, Is.EqualTo(priceUpdateDto.Price));
        }

        [TestCase("02", -1)]
        [TestCase("B3", -70)]
        [TestCase("04", -150)]
        public async Task UpdatePriceAsync_ShallReturnFailureOperationResult_ForNegativePrice
            (string code, int price)
        {
            var product = new Product
            {
                Id = 1,
                Code = code,
                Name = "Name",
                PriceInStotinki = 120,
                Quantity = 5
            };

            _repositoryMock.Setup(
                mock => mock.FirstOrDefaultAsync(
                    It.IsAny<Expression<Func<Product, bool>>>()))
                .ReturnsAsync(product);

            var priceUpdateDto = new ProductPriceUpdateDto
            {
                Code = code,
                Price = price
            };

            var operationResult = await _cut.UpdatePriceAsync(priceUpdateDto);

            Assert.Multiple(() =>
            {
                Assert.That(operationResult.IsSuccess, Is.False);
                Assert.That(operationResult.ErrorMessage,
                    Is.EqualTo("Product price cannot be negative."));
            });
        }

        [Test]
        public async Task CanAddAsync_ShallReturnTrue_WhenProductCountIsLessThanSlotLimit()
        {
            Product[] expectedProducts = [
                new Product { Id = 1, Code = "01", Name = "Some Name", PriceInStotinki = 199, Quantity = 1 },
                new Product { Id = 8, Code = "03", Name = "Test", PriceInStotinki = 170, Quantity = 10 },
                new Product { Id = 12, Code = "05", Name = "Name", PriceInStotinki = 100, Quantity = 7 }];

            var asyncMock = expectedProducts.AsQueryable().BuildMock();

            _repositoryMock.Setup(
                mock => mock.AllAsNoTracking())
                .Returns(asyncMock);

            Assert.That(await _cut.CanAddAsync(), Is.True);
        }

        [Test]
        public async Task CanAddAsync_ShallReturnFalse_WhenProductCountIsEqualToSlotLimit()
        {
            var expectedProducts = new List<Product>(VendingMachineConstants.SlotLimit);

            for (int i = 0; i < VendingMachineConstants.SlotLimit; i++)
            {
                expectedProducts.Add(
                    new Product 
                    { 
                        Id = i, 
                        Code = $"A{i}", 
                        Name = $"Name{i}", 
                        PriceInStotinki = 100 + i, 
                        Quantity = 7 
                    });
            }

            var asyncMock = expectedProducts.AsQueryable().BuildMock();

            _repositoryMock.Setup(
                mock => mock.AllAsNoTracking())
                .Returns(asyncMock);

            Assert.That(await _cut.CanAddAsync(), Is.False);
        }
    }
}
