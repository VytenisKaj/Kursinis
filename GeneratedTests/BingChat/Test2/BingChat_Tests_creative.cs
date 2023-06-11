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
    public class ItemsControllerTests2
    {
        private readonly Mock<IRepositoryService> _repositoryServiceMock;
        private readonly Mock<IAuthorizationHelper> _authorizationHelperMock;
        private readonly Mock<IFileWriter> _fileWriterMock;
        private readonly ItemsController _controller;

        public ItemsControllerTests2()
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
            var expectedItems = items.Where(i => i.RequiresAuthorizedUser == false).ToList();
            _repositoryServiceMock.Setup(r => r.GetItems()).Returns(items);
            _authorizationHelperMock.Setup(a => a.IsAuthenticated).Returns(false);

            // Act
            var result = _controller.GetItems();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().BeEquivalentTo(expectedItems);
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
            _repositoryServiceMock.Setup(r => r.GetItem(id)).Returns((Item)null);
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.ViewItem)).Returns(true);

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
            _repositoryServiceMock.Setup(r => r.GetItem(id)).Returns(item);
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.ViewItem)).Returns(true);

            // Act
            var result = _controller.GetItemById(id);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().BeEquivalentTo(item);
        }

        [Fact]
        public void GetItemById_ShouldReturnBadRequestWithExceptionMessage_WhenExceptionOccurs()
        {
            // Arrange
            var id = 1;
            var exceptionMessage = "Something went wrong";
            _repositoryServiceMock.Setup(r => r.GetItem(id)).Throws(new Exception(exceptionMessage));
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.ViewItem)).Returns(true);

            // Act
            var result = _controller.GetItemById(id);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Value.Should().Be(exceptionMessage);
        }

        [Fact]
        public void AddItem_ShouldReturnForbidden_WhenUserDoesNotHavePermission()
        {
            // Arrange
            var request = new ItemRequest { Name = "Item 1", LocationId = 1 };
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
            var request = new ItemRequest { Name = "Item 1", LocationId = 2 };
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.CreateItems)).Returns(true);
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.Admin)).Returns(false);
            _authorizationHelperMock.Setup(a => a.WorkPlaceId).Returns(1);

            // Act
            var result = _controller.AddItem(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void AddItem_ShouldReturnCreatedAtActionWithItem_WhenItemIsCreatedSuccessfully()
        {
            // Arrange
            var request = new ItemRequest { Name = "Item 1", LocationId = 1 };
            var item = new Item { Id = 1, Name = "Item 1", LocationId = 1 };
            _repositoryServiceMock.Setup(r => r.CreateItem(request)).Returns(item);
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.CreateItems)).Returns(true);
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.Admin)).Returns(true);

            // Act
            var result = _controller.AddItem(request);

            // Assert
            result.Should().BeOfType<CreatedAtActionResult>();
            var createdAtActionResult = result as CreatedAtActionResult;
            createdAtActionResult.ActionName.Should().Be(nameof(_controller.GetItemById));
            createdAtActionResult.RouteValues["id"].Should().Be(item.Id);
            createdAtActionResult.Value.Should().BeEquivalentTo(item);
        }

        [Fact]
        public void AddItem_ShouldReturnBadRequestWithExceptionMessage_WhenExceptionOccurs()
        {
            // Arrange
            var request = new ItemRequest { Name = "Item 1", LocationId = 1 };
            var exceptionMessage = "Something went wrong";
            _repositoryServiceMock.Setup(r => r.CreateItem(request)).Throws(new Exception(exceptionMessage));
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.CreateItems)).Returns(true);
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.Admin)).Returns(true);

            // Act
            var result = _controller.AddItem(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Value.Should().Be(exceptionMessage);
        }

        [Fact]
        public void UpdateItem_ShouldReturnForbidden_WhenUserDoesNotHavePermission()
        {
            // Arrange
            var id = 1;
            var request = new ItemRequest { Name = "Item 1", LocationId = 1 };
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.UpdateItems)).Returns(false);

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
            var request = new ItemRequest { Name = "Item 1", LocationId = 1 };
            _repositoryServiceMock.Setup(r => r.GetItem(id)).Returns((Item)null);
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.UpdateItems)).Returns(true);

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
            var request = new ItemRequest { Name = "Item 1", LocationId = 1 };
            var item = new Item { Id = id, Name = "Item 2", LocationId = 2 };
            _repositoryServiceMock.Setup(r => r.GetItem(id)).Returns(item);
            _repositoryServiceMock.Setup(r => r.UpdateItem()).Verifiable();
            _fileWriterMock.Setup(w => w.Write(It.IsAny<string>(), It.IsAny<string>())).Verifiable();
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.UpdateItems)).Returns(true);
            _authorizationHelperMock.Setup(a => a.WorkPlaceId).Returns(1);
            _authorizationHelperMock.Setup(a => a.UserId).Returns(1);

            // Act
            var result = _controller.UpdateItem(id, request);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            item.Name.Should().Be(request.Name);
            item.LocationId.Should().Be(request.LocationId);
            _repositoryServiceMock.Verify();
            _fileWriterMock.Verify();
        }

        [Fact]
        public void UpdateItem_ShouldReturnBadRequestWithExceptionMessage_WhenExceptionOccurs()
        {
            // Arrange
            var id = 1;
            var request = new ItemRequest { Name = "Item 1", LocationId = 1 };
            var exceptionMessage = "Something went wrong";
            _repositoryServiceMock.Setup(r => r.GetItem(id)).Throws(new Exception(exceptionMessage));
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.UpdateItems)).Returns(true);

            // Act
            var result = _controller.UpdateItem(id, request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Value.Should().Be(exceptionMessage);
        }

        [Fact]
        public void DeleteItem_ShouldReturnForbidden_WhenUserDoesNotHavePermission()
        {
            // Arrange
            var id = 1;
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.DeleteItems)).Returns(false);

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
            _repositoryServiceMock.Setup(r => r.GetItem(id)).Returns((Item)null);
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.DeleteItems)).Returns(true);

            // Act
            var result = _controller.DeleteItem(id);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void DeleteItem_ShouldReturnNoContent_WhenItemIsDeletedSuccessfully()
        {
            // Arrange
            var id = 1;
            var item = new Item { Id = id, Name = "Item 1", LocationId = 1 };
            _repositoryServiceMock.Setup(r => r.GetItem(id)).Returns(item);
            _repositoryServiceMock.Setup(r => r.DeleteItem(item)).Verifiable();
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.DeleteItems)).Returns(true);

            // Act
            var result = _controller.DeleteItem(id);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            _repositoryServiceMock.Verify();
        }

        [Fact]
        public void DeleteItem_ShouldReturnBadRequestWithExceptionMessage_WhenExceptionOccurs()
        {
            // Arrange
            var id = 1;
            var exceptionMessage = "Something went wrong";
            _repositoryServiceMock.Setup(r => r.GetItem(id)).Throws(new Exception(exceptionMessage));
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.DeleteItems)).Returns(true);

            // Act
            var result = _controller.DeleteItem(id);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Value.Should().Be(exceptionMessage);
        }

        [Fact]
        public void GetItemPrice_ShouldReturnForbidden_WhenUserDoesNotHavePermission()
        {
            // Arrange
            var id = 1;
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.ViewItem)).Returns(false);

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
            _repositoryServiceMock.Setup(r => r.GetItem(id)).Returns((Item)null);
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.ViewItem)).Returns(true);

            // Act
            var result = _controller.GetItemPrice(id);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Theory]
        [InlineData(50, 50)]
        [InlineData(75, 67.5)]
        [InlineData(150, 120)]
        [InlineData(250, 175)]
        public void GetItemPrice_ShouldReturnOkWithCorrectPrice_WhenItemExists(double originalPrice, double expectedPrice)
        {
            // Arrange
            var id = 1;
            var item = new Item { Id = id, Name = "Item 1", Price = originalPrice };
            _repositoryServiceMock.Setup(r => r.GetItem(id)).Returns(item);
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.ViewItem)).Returns(true);
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.GetDiscount)).Returns(true);

            // Act
            var result = _controller.GetItemPrice(id);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().Be(expectedPrice);
        }

        [Fact]
        public void GetItemPrice_ShouldReturnBadRequestWithExceptionMessage_WhenExceptionOccurs()
        {
            // Arrange
            var id = 1;
            var exceptionMessage = "Something went wrong";
            _repositoryServiceMock.Setup(r => r.GetItem(id)).Throws(new Exception(exceptionMessage));
            _authorizationHelperMock.Setup(a => a.HasPermission(Permissions.ViewItem)).Returns(true);

            // Act
            var result = _controller.GetItemPrice(id);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Value.Should().Be(exceptionMessage);
        }
    }
}
