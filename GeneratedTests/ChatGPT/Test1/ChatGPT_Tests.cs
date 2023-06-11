using System;
using System.Collections.Generic;
using System.Linq;
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
    public class ItemsControllerTests
    {
        private readonly Mock<IRepositoryService> _repositoryServiceMock;
        private readonly Mock<IAuthorizationHelper> _authorizationHelperMock;
        private readonly Mock<IFileWriter> _fileWriterMock;
        private readonly ItemsController _itemsController;

        public ItemsControllerTests()
        {
            _repositoryServiceMock = new Mock<IRepositoryService>();
            _authorizationHelperMock = new Mock<IAuthorizationHelper>();
            _fileWriterMock = new Mock<IFileWriter>();
            _itemsController = new ItemsController(
                _repositoryServiceMock.Object,
                _authorizationHelperMock.Object,
                _fileWriterMock.Object
            );
        }

        [Fact]
        public void GetItems_WithAuthenticatedUser_ReturnsAllItems()
        {
            // Arrange
            _authorizationHelperMock.Setup(h => h.IsAuthenticated).Returns(true);
            var items = new List<Item>
            {
                new Item { Id = 1, Name = "Item 1", Price = 10, RequiresAuthorizedUser = false },
                new Item { Id = 2, Name = "Item 2", Price = 20, RequiresAuthorizedUser = true },
                new Item { Id = 3, Name = "Item 3", Price = 30, RequiresAuthorizedUser = false }
            };
            _repositoryServiceMock.Setup(r => r.GetItems()).Returns(items);

            // Act
            var result = _itemsController.GetItems() as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(200);
            result.Value.Should().BeEquivalentTo(items);
        }

        [Fact]
        public void GetItems_WithUnauthenticatedUser_ReturnsItemsWithoutAuthorization()
        {
            // Arrange
            _authorizationHelperMock.Setup(h => h.IsAuthenticated).Returns(false);
            var items = new List<Item>
            {
                new Item { Id = 1, Name = "Item 1", Price = 10, RequiresAuthorizedUser = false },
                new Item { Id = 2, Name = "Item 2", Price = 20, RequiresAuthorizedUser = true },
                new Item { Id = 3, Name = "Item 3", Price = 30, RequiresAuthorizedUser = false }
            };
            _repositoryServiceMock.Setup(r => r.GetItems()).Returns(items);

            // Act
            var result = _itemsController.GetItems() as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(200);
            result.Value.Should().BeEquivalentTo(items.Where(i => !i.RequiresAuthorizedUser));
        }

        [Fact]
        public void GetItemById_WithViewPermission_ReturnsItem()
        {
            // Arrange
            _authorizationHelperMock.Setup(h => h.HasPermission(Permissions.ViewItem)).Returns(true);
            var item = new Item { Id = 1, Name = "Item 1", Price = 10, RequiresAuthorizedUser = false };
            _repositoryServiceMock.Setup(r => r.GetItem(item.Id)).Returns(item);

            // Act
            var result = _itemsController.GetItemById(item.Id) as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(200);
            result.Value.Should().BeEquivalentTo(item);
        }

        [Fact]
        public void GetItemById_WithoutViewPermission_ReturnsForbidResult()
        {
            // Arrange
            _authorizationHelperMock.Setup(h => h.HasPermission(Permissions.ViewItem)).Returns(false);

            // Act
            var result = _itemsController.GetItemById(1);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public void GetItemById_WithInvalidItemId_ReturnsNotFoundResult()
        {
            // Arrange
            _authorizationHelperMock.Setup(h => h.HasPermission(Permissions.ViewItem)).Returns(true);
            _repositoryServiceMock.Setup(r => r.GetItem(It.IsAny<int>())).Returns((Item)null);

            // Act
            var result = _itemsController.GetItemById(1);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void GetItemPrice_WithViewPermissionAndValidItemId_ReturnsCorrectPrice()
        {
            // Arrange
            _authorizationHelperMock.Setup(h => h.HasPermission(Permissions.ViewItem)).Returns(true);
            var item = new Item { Id = 1, Name = "Item 1", Price = 100, RequiresAuthorizedUser = false };
            _repositoryServiceMock.Setup(r => r.GetItem(item.Id)).Returns(item);

            // Act
            var result = _itemsController.GetItemPrice(item.Id) as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(200);
            result.Value.Should().Be(item.Price);
        }

        [Fact]
        public void GetItemPrice_WithoutViewPermission_ReturnsForbidResult()
        {
            // Arrange
            _authorizationHelperMock.Setup(h => h.HasPermission(Permissions.ViewItem)).Returns(false);

            // Act
            var result = _itemsController.GetItemPrice(1);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public void GetItemPrice_WithInvalidItemId_ReturnsNotFoundResult()
        {
            // Arrange
            _authorizationHelperMock.Setup(h => h.HasPermission(Permissions.ViewItem)).Returns(true);
            _repositoryServiceMock.Setup(r => r.GetItem(It.IsAny<int>())).Returns((Item)null);

            // Act
            var result = _itemsController.GetItemPrice(1);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        /*[Fact]
        public void AddItem_WithCreateItemsPermissionAndMatchingWorkplace_AddsItemAndReturnsCreatedAtAction()
        {
            // Arrange
            _authorizationHelperMock.Setup(h => h.HasPermission(Permissions.CreateItems)).Returns(true);
            _authorizationHelperMock.Setup(h => h.HasPermission(Permissions.Admin)).Returns(false);
            _authorizationHelperMock.Setup(h => h.WorkPlaceId).Returns(1);
            var request = new ItemRequest { Name = "New Item", Price = 50, LocationId = 1 };
            var createdItem = new Item { Id = 1, Name = request.Name, Price = request.Price, RequiresAuthorizedUser = false };
            _repositoryServiceMock.Setup(r => r.CreateItem(request)).Returns(createdItem);

            // Act
            var result = _itemsController.AddItem(request) as CreatedAtActionResult;

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(201);
            result.ActionName.Should().Be(nameof(ItemsController.GetItemById));
            result.RouteValues.Should().ContainKey("id").WhichValue.Should().Be(createdItem.Id);
            result.Value.Should().BeEquivalentTo(createdItem);
        }*/

        [Fact]
        public void AddItem_WithoutCreateItemsPermission_ReturnsForbidResult()
        {
            // Arrange
            _authorizationHelperMock.Setup(h => h.HasPermission(Permissions.CreateItems)).Returns(false);

            // Act
            var result = _itemsController.AddItem(new ItemRequest());

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public void AddItem_WithDifferentWorkplaceAndNotAdmin_ReturnsBadRequestResult()
        {
            // Arrange
            _authorizationHelperMock.Setup(h => h.HasPermission(Permissions.CreateItems)).Returns(true);
            _authorizationHelperMock.Setup(h => h.HasPermission(Permissions.Admin)).Returns(false);
            _authorizationHelperMock.Setup(h => h.WorkPlaceId).Returns(2);

            // Act
            var result = _itemsController.AddItem(new ItemRequest { LocationId = 1 });

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().Be("Cannot add items to different location");
        }

        [Fact]
        public void UpdateItem_WithUpdateItemsPermissionAndValidItemId_UpdatesItemAndReturnsNoContent()
        {
            // Arrange
            _authorizationHelperMock.Setup(h => h.HasPermission(Permissions.UpdateItems)).Returns(true);
            var itemId = 1;
            var request = new ItemRequest { Name = "Updated Item", Price = 100, LocationId = 1, RequiresAuthorizedUser = true };
            var item = new Item { Id = itemId, Name = "Item 1", Price = 50, LocationId = 2, RequiresAuthorizedUser = false };
            _repositoryServiceMock.Setup(r => r.GetItem(itemId)).Returns(item);

            // Act
            var result = _itemsController.UpdateItem(itemId, request) as NoContentResult;

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(204);
            _repositoryServiceMock.Verify(r => r.UpdateItem(), Times.Once);
            _fileWriterMock.Verify(w => w.Write($"{_authorizationHelperMock.Object.WorkPlaceId}.log", $"user id={_authorizationHelperMock.Object.UserId} updated item id={itemId}"), Times.Once);
            item.Name.Should().Be(request.Name);
            item.Price.Should().Be(request.Price);
            item.LocationId.Should().Be(request.LocationId);
            item.RequiresAuthorizedUser.Should().Be(request.RequiresAuthorizedUser);
        }

        [Fact]
        public void UpdateItem_WithoutUpdateItemsPermission_ReturnsForbidResult()
        {
            // Arrange
            _authorizationHelperMock.Setup(h => h.HasPermission(Permissions.UpdateItems)).Returns(false);

            // Act
            var result = _itemsController.UpdateItem(1, new ItemRequest());

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public void UpdateItem_WithInvalidItemId_ReturnsNotFoundResult()
        {
            // Arrange
            _authorizationHelperMock.Setup(h => h.HasPermission(Permissions.UpdateItems)).Returns(true);
            _repositoryServiceMock.Setup(r => r.GetItem(It.IsAny<int>())).Returns((Item)null);

            // Act
            var result = _itemsController.UpdateItem(1, new ItemRequest());

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void DeleteItem_WithDeleteItemsPermissionAndValidItemId_DeletesItemAndReturnsNoContent()
        {
            // Arrange
            _authorizationHelperMock.Setup(h => h.HasPermission(Permissions.DeleteItems)).Returns(true);
            var itemId = 1;
            var item = new Item { Id = itemId, Name = "Item 1", Price = 50, LocationId = 2, RequiresAuthorizedUser = false };
            _repositoryServiceMock.Setup(r => r.GetItem(itemId)).Returns(item);

            // Act
            var result = _itemsController.DeleteItem(itemId) as NoContentResult;

            // Assert
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(204);
            _repositoryServiceMock.Verify(r => r.DeleteItem(item), Times.Once);
        }

        [Fact]
        public void DeleteItem_WithoutDeleteItemsPermission_ReturnsForbidResult()
        {
            // Arrange
            _authorizationHelperMock.Setup(h => h.HasPermission(Permissions.DeleteItems)).Returns(false);

            // Act
            var result = _itemsController.DeleteItem(1);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public void DeleteItem_WithInvalidItemId_ReturnsNotFoundResult()
        {
            // Arrange
            _authorizationHelperMock.Setup(h => h.HasPermission(Permissions.DeleteItems)).Returns(true);
            _repositoryServiceMock.Setup(r => r.GetItem(It.IsAny<int>())).Returns((Item)null);

            // Act
            var result = _itemsController.DeleteItem(1);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
