using Kursinis.Controllers;
using Kursinis.Enums;
using Kursinis.Models;
using Kursinis.Services.AuthorizationHelper;
using Kursinis.Services.FileWriter;
using Kursinis.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using FluentAssertions;

namespace Kursinis.Tests
{
    public class ItemsControllerTests
    {
        private readonly Mock<IRepositoryService> _repositoryServiceMock;
        private readonly Mock<IAuthorizationHelper> _authorizationHelperMock;
        private readonly Mock<IFileWriter> _fileWriterMock;

        private readonly ItemsController _controller;

        public ItemsControllerTests()
        {
            _repositoryServiceMock = new Mock<IRepositoryService>();
            _authorizationHelperMock = new Mock<IAuthorizationHelper>();
            _fileWriterMock = new Mock<IFileWriter>();
            _controller = new ItemsController(_repositoryServiceMock.Object, _authorizationHelperMock.Object, _fileWriterMock.Object);
        }

        [Fact]
        public void GetItems_ShouldReturnAllItems_WhenUserIsAuthenticated()
        {
            // Arrange
            var items = new List<Item>
        {
            new Item { Id = 1, Name = "Item 1", RequiresAuthorizedUser = true },
            new Item { Id = 2, Name = "Item 2", RequiresAuthorizedUser = false }
        };
            _repositoryServiceMock.Setup(r => r.GetItems()).Returns(items);
            _authorizationHelperMock.Setup(a => a.IsAuthenticated).Returns(true);

            // Act
            var result = _controller.GetItems();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().BeEquivalentTo(items);
        }

        [Fact]
        public void GetItems_ShouldReturnOnlyPublicItems_WhenUserIsNotAuthenticated()
        {
            // Arrange
            var items = new List<Item>
        {
            new Item { Id = 1, Name = "Item 1", RequiresAuthorizedUser = true },
            new Item { Id = 2, Name = "Item 2", RequiresAuthorizedUser = false }
        };
            var publicItems = items.Where(i => i.RequiresAuthorizedUser == false).ToList();
            _repositoryServiceMock.Setup(r => r.GetItems()).Returns(items);
            _authorizationHelperMock.Setup(a => a.IsAuthenticated).Returns(false);

            // Act
            var result = _controller.GetItems();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().BeEquivalentTo(publicItems);
        }

        [Fact]
        public void GetItemById_ShouldReturnForbidden_WhenUserDoesNotHavePermission()
        {
            // Arrange
            var id = 1;
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.ViewItem)).Returns(false);

            // Act
            var result = _controller.GetItemById(id);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public void GetItemById_ShouldReturnNotFound_WhenItemDoesNotExist()
        {
            // Arrange
            var id = 1;
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.ViewItem)).Returns(true);
            _repositoryServiceMock.Setup(r => r.GetItem(id)).Returns((Item)null);

            // Act
            var result = _controller.GetItemById(id);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void GetItemById_ShouldReturnOkWithItem_WhenItemExists()
        {
            // Arrange
            var id = 1;
            var item = new Item { Id = id, Name = "Item 1", RequiresAuthorizedUser = true };
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.ViewItem)).Returns(true);
            _repositoryServiceMock.Setup(r => r.GetItem(id)).Returns(item);

            // Act
            var result = _controller.GetItemById(id);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().BeEquivalentTo(item);
        }

        [Fact]
        public void AddItem_ShouldReturnForbidden_WhenUserDoesNotHavePermission()
        {
            // Arrange
            var request = new ItemRequest { Name = "New Item", LocationId = 1 };
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.CreateItems)).Returns(false);

            // Act
            var result = _controller.AddItem(request);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public void AddItem_ShouldReturnBadRequest_WhenUserCannotAddItemsToDifferentLocation()
        {
            // Arrange
            var request = new ItemRequest { Name = "New Item", LocationId = 2 };
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.CreateItems)).Returns(true);
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.Admin)).Returns(false);
            _authorizationHelperMock.Setup(a => a.WorkPlaceId).Returns(1);

            // Act
            var result = _controller.AddItem(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void AddItem_ShouldReturnCreatedWithItem_WhenItemIsCreated()
        {
            // Arrange
            var request = new ItemRequest { Name = "New Item", LocationId = 1 };
            var item = new Item { Id = 1, Name = "New Item", LocationId = 1 };
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.CreateItems)).Returns(true);
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.Admin)).Returns(true);
            _repositoryServiceMock.Setup(r => r.CreateItem(request)).Returns(item);

            // Act
            var result = _controller.AddItem(request);

            // Assert
            result.Should().BeOfType<CreatedAtActionResult>();
            var createdResult = result as CreatedAtActionResult;
            createdResult.ActionName.Should().Be(nameof(_controller.GetItemById));
            createdResult.RouteValues["id"].Should().Be(item.Id);
            createdResult.Value.Should().BeEquivalentTo(item);
        }

        [Theory]
        [InlineData(Discount.None, 50, 50)]
        [InlineData(Discount.Small, 60, 54)]
        [InlineData(Discount.Medium, 120, 96)]
        [InlineData(Discount.Big, 250, 175)]
        public void GetItemPrice_ShouldReturnOkWithCorrectPrice_WhenItemExistsAndUserHasPermission(Discount discount, double price, double expectedPrice)
        {
            // Arrange
            var id = 1;
            var item = new Item { Price = price };
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.ViewItem)).Returns(true);
            _repositoryServiceMock.Setup(x => x.GetItem(id)).Returns(item);
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.GetDiscount)).Returns(true);

            // Act
            var result = _controller.GetItemPrice(id);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            result.As<OkObjectResult>().Value.Should().Be(expectedPrice);
        }

        [Fact]
        public void GetItemPrice_ShouldReturnOkWithOriginalPrice_WhenUserHasNoDiscountPermission()
        {
            // Arrange
            var id = 1;
            var item = new Item { Price = 100 };
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.ViewItem)).Returns(true);
            _repositoryServiceMock.Setup(x => x.GetItem(id)).Returns(item);
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.GetDiscount)).Returns(false);

            // Act
            var result = _controller.GetItemPrice(id);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            result.As<OkObjectResult>().Value.Should().Be(item.Price);
        }

        [Fact]
        public void GetItemPrice_ShouldReturnBadRequest_WhenExceptionIsThrown()
        {
            // Arrange
            var id = 1;
            var exceptionMessage = "Something went wrong";
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.ViewItem)).Returns(true);
            _repositoryServiceMock.Setup(x => x.GetItem(id)).Throws(new Exception(exceptionMessage));

            // Act
            var result = _controller.GetItemPrice(id);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            result.As<BadRequestObjectResult>().Value.Should().Be(exceptionMessage);
        }

        [Fact]
        public void DeleteItem_ShouldReturnNoContent_WhenItemIsDeletedSuccessfully()
        {
            // Arrange
            var id = 1;
            var item = new Item();
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.DeleteItems)).Returns(true);
            _repositoryServiceMock.Setup(x => x.GetItem(id)).Returns(item);

            // Act
            var result = _controller.DeleteItem(id);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            _repositoryServiceMock.Verify(x => x.DeleteItem(item), Times.Once);
        }

        [Fact]
        public void DeleteItem_ShouldReturnBadRequest_WhenExceptionIsThrown()
        {
            // Arrange
            var id = 1;
            var exceptionMessage = "Something went wrong";
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.DeleteItems)).Returns(true);
            _repositoryServiceMock.Setup(x => x.GetItem(id)).Throws(new Exception(exceptionMessage));

            // Act
            var result = _controller.DeleteItem(id);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            result.As<BadRequestObjectResult>().Value.Should().Be(exceptionMessage);
        }

        [Fact]
        public void GetItemPrice_ShouldReturnForbid_WhenUserHasNoPermission()
        {
            // Arrange
            var id = 1;
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.ViewItem)).Returns(false);

            // Act
            var result = _controller.GetItemPrice(id);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public void GetItemPrice_ShouldReturnNotFound_WhenItemDoesNotExist()
        {
            // Arrange
            var id = 1;
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.ViewItem)).Returns(true);
            _repositoryServiceMock.Setup(x => x.GetItem(id)).Returns((Item)null);

            // Act
            var result = _controller.GetItemPrice(id);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void UpdateItem_ShouldReturnBadRequest_WhenExceptionIsThrown()
        {
            // Arrange
            var id = 1;
            var request = new ItemRequest();
            var exceptionMessage = "Something went wrong";
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.UpdateItems)).Returns(true);
            _repositoryServiceMock.Setup(x => x.GetItem(id)).Throws(new Exception(exceptionMessage));

            // Act
            var result = _controller.UpdateItem(id, request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            result.As<BadRequestObjectResult>().Value.Should().Be(exceptionMessage);
        }

        [Fact]
        public void DeleteItem_ShouldReturnForbid_WhenUserHasNoPermission()
        {
            // Arrange
            var id = 1;
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.DeleteItems)).Returns(false);

            // Act
            var result = _controller.DeleteItem(id);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public void DeleteItem_ShouldReturnNotFound_WhenItemDoesNotExist()
        {
            // Arrange
            var id = 1;
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.DeleteItems)).Returns(true);
            _repositoryServiceMock.Setup(x => x.GetItem(id)).Returns((Item)null);

            // Act
            var result = _controller.DeleteItem(id);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void UpdateItem_ShouldReturnForbid_WhenUserHasNoPermission()
        {
            // Arrange
            var id = 1;
            var request = new ItemRequest();
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.UpdateItems)).Returns(false);

            // Act
            var result = _controller.UpdateItem(id, request);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public void UpdateItem_ShouldReturnNotFound_WhenItemDoesNotExist()
        {
            // Arrange
            var id = 1;
            var request = new ItemRequest();
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.UpdateItems)).Returns(true);
            _repositoryServiceMock.Setup(x => x.GetItem(id)).Returns((Item)null);

            // Act
            var result = _controller.UpdateItem(id, request);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void UpdateItem_ShouldReturnNoContent_WhenItemIsUpdatedSuccessfully()
        {
            // Arrange
            var id = 1;
            var request = new ItemRequest();
            var item = new Item();
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.UpdateItems)).Returns(true);
            _repositoryServiceMock.Setup(x => x.GetItem(id)).Returns(item);

            // Act
            var result = _controller.UpdateItem(id, request);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            item.Name.Should().Be(request.Name);
            item.Price.Should().Be(request.Price);
            item.LocationId.Should().Be(request.LocationId);
            item.RequiresAuthorizedUser.Should().Be(request.RequiresAuthorizedUser);

            _repositoryServiceMock.Verify(x => x.UpdateItem(), Times.Once);

            _fileWriterMock.Verify(x => x.Write(It.IsAny<string>(), It.Is<string>(s => s.Contains($"user id={_authorizationHelperMock.Object.UserId} updated item id={id}"))), Times.Once);
        }
    }
}
