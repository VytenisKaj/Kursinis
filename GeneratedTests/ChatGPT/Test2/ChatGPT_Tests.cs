using FluentAssertions;
using Kursinis.Controllers;
using Kursinis.Enums;
using Kursinis.Models;
using Kursinis.Services.AuthorizationHelper;
using Kursinis.Services.FileWriter;
using Kursinis.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Kursinis.Tests.Controllers
{
    public class ItemsControllerTests2
    {
        private readonly Mock<IRepositoryService> _repositoryServiceMock;
        private readonly Mock<IAuthorizationHelper> _authorizationHelperMock;
        private readonly Mock<IFileWriter> _fileWriterMock;
        private readonly ItemsController _itemsController;

        public ItemsControllerTests2()
        {
            _repositoryServiceMock = new Mock<IRepositoryService>();
            _authorizationHelperMock = new Mock<IAuthorizationHelper>();
            _fileWriterMock = new Mock<IFileWriter>();
            _itemsController = new ItemsController(
                _repositoryServiceMock.Object,
                _authorizationHelperMock.Object,
                _fileWriterMock.Object);
        }

        [Fact]
        public void GetItems_WhenAuthenticated_ShouldReturnAllItems()
        {
            // Arrange
            _authorizationHelperMock.Setup(a => a.IsAuthenticated).Returns(true);
            var items = new List<Item> { new Item(), new Item() };
            _repositoryServiceMock.Setup(r => r.GetItems()).Returns(items);

            // Act
            var result = _itemsController.GetItems();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            result.As<OkObjectResult>().Value.Should().BeEquivalentTo(items);
        }

        [Fact]
        public void GetItems_WhenNotAuthenticated_ShouldReturnNonAuthorizedItems()
        {
            // Arrange
            _authorizationHelperMock.Setup(a => a.IsAuthenticated).Returns(false);
            var items = new List<Item>
            {
                new Item { RequiresAuthorizedUser = false },
                new Item { RequiresAuthorizedUser = true },
                new Item { RequiresAuthorizedUser = false }
            };
            _repositoryServiceMock.Setup(r => r.GetItems()).Returns(items);

            // Act
            var result = _itemsController.GetItems();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            result.As<OkObjectResult>().Value.Should().BeEquivalentTo(items);
        }

        [Fact]
        public void GetItemById_WhenHasPermission_ShouldReturnItem()
        {
            // Arrange
            const int itemId = 1;
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.ViewItem)).Returns(true);
            var item = new Item { Id = itemId };
            _repositoryServiceMock.Setup(r => r.GetItem(itemId)).Returns(item);

            // Act
            var result = _itemsController.GetItemById(itemId);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            result.As<OkObjectResult>().Value.Should().BeEquivalentTo(item);
        }

        [Fact]
        public void GetItemById_WhenNoPermission_ShouldReturnForbidResult()
        {
            // Arrange
            const int itemId = 1;
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.ViewItem)).Returns(false);

            // Act
            var result = _itemsController.GetItemById(itemId);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public void GetItemById_WhenItemNotFound_ShouldReturnNotFoundResult()
        {
            // Arrange
            const int itemId = 1;
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.ViewItem)).Returns(true);
            _repositoryServiceMock.Setup(r => r.GetItem(itemId)).Returns((Item)null);

            // Act
            var result = _itemsController.GetItemById(itemId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            result.As<NotFoundObjectResult>().Value.Should().Be($"Item with id {itemId} not found");
        }

        [Theory]
        [InlineData(1, 100, Discount.Small)]
        [InlineData(2, 150, Discount.Medium)]
        [InlineData(3, 250, Discount.Big)]
        [InlineData(4, 50, Discount.None)]
        public void GetItemPrice_WhenHasPermission_ShouldReturnCorrectDiscountedPrice(
            int itemId, double itemPrice, Discount expectedDiscount)
        {
            // Arrange
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.ViewItem)).Returns(true);
            var item = new Item { Id = itemId, Price = itemPrice };
            _repositoryServiceMock.Setup(r => r.GetItem(itemId)).Returns(item);

            // Act
            var result = _itemsController.GetItemPrice(itemId);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            result.As<OkObjectResult>().Value.Should().Be(item.Price * GetDiscountMultiplier(expectedDiscount));
        }

        [Fact]
        public void GetItemPrice_WhenNoPermission_ShouldReturnForbidResult()
        {
            // Arrange
            const int itemId = 1;
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.ViewItem)).Returns(false);

            // Act
            var result = _itemsController.GetItemPrice(itemId);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public void GetItemPrice_WhenItemNotFound_ShouldReturnNotFoundResult()
        {
            // Arrange
            const int itemId = 1;
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.ViewItem)).Returns(true);
            _repositoryServiceMock.Setup(r => r.GetItem(itemId)).Returns((Item)null);

            // Act
            var result = _itemsController.GetItemPrice(itemId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            result.As<NotFoundObjectResult>().Value.Should().Be($"Item with id {itemId} not found");
        }

        /*[Fact]
        public void GetItemPrice_WhenInvalidDiscount_ShouldThrowException()
        {
            // Arrange
            const int itemId = 1;
            const double itemPrice = 100;
            const Discount invalidDiscount = (Discount)10;
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.ViewItem)).Returns(true);
            var item = new Item { Id = itemId, Price = itemPrice };
            _repositoryServiceMock.Setup(r => r.GetItem(itemId)).Returns(item);

            // Act
            var action = new System.Action(() => _itemsController.GetItemPrice(itemId));

            // Assert
            action.Should().Throw<ArgumentOutOfRangeException>()
                .WithMessage($"Not a valid discount (Parameter '{nameof(CalculatateDiscount)}')");
        }*/

        [Fact]
        public void AddItem_WhenHasPermissionAndMatchingLocation_ShouldCreateItemAndReturnCreatedAtAction()
        {
            // Arrange
            var request = new ItemRequest
            {
                LocationId = 1
            };
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.CreateItems)).Returns(true);
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.Admin)).Returns(false);
            _authorizationHelperMock.Setup(a => a.WorkPlaceId).Returns(1);
            var createdItem = new Item { Id = 1 };
            _repositoryServiceMock.Setup(r => r.CreateItem(request)).Returns(createdItem);

            // Act
            var result = _itemsController.AddItem(request);

            // Assert
            result.Should().BeOfType<CreatedAtActionResult>();
            result.As<CreatedAtActionResult>().ActionName.Should().Be(nameof(ItemsController.GetItemById));
            result.As<CreatedAtActionResult>().RouteValues["id"].Should().Be(createdItem.Id);
            result.As<CreatedAtActionResult>().Value.Should().BeEquivalentTo(createdItem);
        }

        [Fact]
        public void AddItem_WhenNoPermission_ShouldReturnForbidResult()
        {
            // Arrange
            var request = new ItemRequest();
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.CreateItems)).Returns(false);

            // Act
            var result = _itemsController.AddItem(request);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public void AddItem_WhenDifferentLocationAndNoAdminPermission_ShouldReturnBadRequestResult()
        {
            // Arrange
            var request = new ItemRequest
            {
                LocationId = 2
            };
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.CreateItems)).Returns(true);
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.Admin)).Returns(false);
            _authorizationHelperMock.Setup(a => a.WorkPlaceId).Returns(1);

            // Act
            var result = _itemsController.AddItem(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            result.As<BadRequestObjectResult>().Value.Should().Be("Cannot add items to different location");
        }

        [Fact]
        public void UpdateItem_WhenHasPermissionAndItemExists_ShouldUpdateItemAndReturnNoContentResult()
        {
            // Arrange
            const int itemId = 1;
            var request = new ItemRequest { Name = "Updated Item", Price = 99, LocationId = 1, RequiresAuthorizedUser = false };
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.UpdateItems)).Returns(true);
            var existingItem = new Item { Id = itemId };
            _repositoryServiceMock.Setup(r => r.GetItem(itemId)).Returns(existingItem);

            // Act
            var result = _itemsController.UpdateItem(itemId, request);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            existingItem.Name.Should().Be(request.Name);
            existingItem.Price.Should().Be(request.Price);
            existingItem.LocationId.Should().Be(request.LocationId);
            existingItem.RequiresAuthorizedUser.Should().Be(request.RequiresAuthorizedUser);
            _repositoryServiceMock.Verify(r => r.UpdateItem(), Times.Once);
            _fileWriterMock.Verify(w => w.Write($"{_authorizationHelperMock.Object.WorkPlaceId}.log",
                $"user id={_authorizationHelperMock.Object.UserId} updated item id={itemId}"), Times.Once);
        }

        [Fact]
        public void UpdateItem_WhenNoPermission_ShouldReturnForbidResult()
        {
            // Arrange
            const int itemId = 1;
            var request = new ItemRequest();
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.UpdateItems)).Returns(false);

            // Act
            var result = _itemsController.UpdateItem(itemId, request);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public void UpdateItem_WhenItemNotFound_ShouldReturnNotFoundResult()
        {
            // Arrange
            const int itemId = 1;
            var request = new ItemRequest();
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.UpdateItems)).Returns(true);
            _repositoryServiceMock.Setup(r => r.GetItem(itemId)).Returns((Item)null);

            // Act
            var result = _itemsController.UpdateItem(itemId, request);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            result.As<NotFoundObjectResult>().Value.Should().Be($"Item with id {itemId} not found");
        }

        [Fact]
        public void DeleteItem_WhenHasPermissionAndItemExists_ShouldDeleteItemAndReturnNoContentResult()
        {
            // Arrange
            const int itemId = 1;
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.DeleteItems)).Returns(true);
            var existingItem = new Item { Id = itemId };
            _repositoryServiceMock.Setup(r => r.GetItem(itemId)).Returns(existingItem);

            // Act
            var result = _itemsController.DeleteItem(itemId);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            _repositoryServiceMock.Verify(r => r.DeleteItem(existingItem), Times.Once);
        }

        [Fact]
        public void DeleteItem_WhenNoPermission_ShouldReturnForbidResult()
        {
            // Arrange
            const int itemId = 1;
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.DeleteItems)).Returns(false);

            // Act
            var result = _itemsController.DeleteItem(itemId);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public void DeleteItem_WhenItemNotFound_ShouldReturnNotFoundResult()
        {
            // Arrange
            const int itemId = 1;
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.DeleteItems)).Returns(true);
            _repositoryServiceMock.Setup(r => r.GetItem(itemId)).Returns((Item)null);

            // Act
            var result = _itemsController.DeleteItem(itemId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            result.As<NotFoundObjectResult>().Value.Should().Be($"Item with id {itemId} not found");
        }

        private double GetDiscountMultiplier(Discount discount)
        {
            switch (discount)
            {
                case Discount.Small:
                    return 0.9;
                case Discount.Medium:
                    return 0.8;
                case Discount.Big:
                    return 0.7;
                case Discount.None:
                default:
                    return 1.0;
            }
        }
    }
}
