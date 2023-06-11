using Xunit;
using Kursinis.Controllers;
using Kursinis.Models;
using Kursinis.Services.AuthorizationHelper;
using Kursinis.Services.Repositories;
using Kursinis.Services.FileWriter;
using Microsoft.AspNetCore.Mvc;
using Moq;
using FluentAssertions;
using Kursinis.Enums;

namespace GeneratedTests.Phind.Test2
{
    

    public class ItemsControllerTests
    {
        private readonly Mock<IRepositoryService> _repositoryServiceMock;
        private readonly Mock<IAuthorizationHelper> _authorizationHelperMock;
        private readonly Mock<IFileWriter> _mockFileWriter;
        private readonly ItemsController _controller;

        public ItemsControllerTests()
        {
            _repositoryServiceMock = new Mock<IRepositoryService>();
            _authorizationHelperMock = new Mock<IAuthorizationHelper>();
            _mockFileWriter = new Mock<IFileWriter>();
            _controller = new ItemsController(_repositoryServiceMock.Object, _authorizationHelperMock.Object, _mockFileWriter.Object);
        }

        [Fact]
        public void AddItem_NoPermission_ReturnsForbidResult()
        {
            // Arrange
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.CreateItems)).Returns(false);

            // Act
            var result = _controller.AddItem(new ItemRequest());

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }


        [Fact]
        public void AddItem_NoAdminPermissionAndDifferentLocation_ReturnsBadRequest()
        {
            // Arrange
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.CreateItems)).Returns(true);
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.Admin)).Returns(false);
            _authorizationHelperMock.Setup(x => x.WorkPlaceId).Returns(1);
            var request = new ItemRequest { LocationId = 2 };

            // Act
            var result = _controller.AddItem(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>().Which.Value.Should().Be("Cannot add items to different location");
        }

        [Fact]
        public void AddItem_WithPermissionAndSuccess_ReturnsCreatedResult()
        {
            // Arrange
            var itemRequest = new ItemRequest { LocationId = 1 };
            var createdItem = new Item { Id = 1, LocationId = 1 };
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.CreateItems)).Returns(true);
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.Admin)).Returns(true);
            _repositoryServiceMock.Setup(x => x.CreateItem(itemRequest)).Returns(createdItem);

            // Act
            var result = _controller.AddItem(itemRequest);

            // Assert
            result.Should().BeOfType<CreatedAtActionResult>().Which.Value.Should().BeEquivalentTo(createdItem);
        }

        [Fact]
        public void AddItem_ExceptionOccured_ReturnsBadRequest()
        {
            // Arrange
            var itemRequest = new ItemRequest { LocationId = 1 };
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.CreateItems)).Returns(true);
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.Admin)).Returns(true);
            _repositoryServiceMock.Setup(x => x.CreateItem(itemRequest)).Throws(new Exception("Error creating item"));

            // Act
            var result = _controller.AddItem(itemRequest);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>().Which.Value.Should().Be("Error creating item");
        }

        [Fact]
        public void AddItem_NonAdminWithPermissionAndSameLocation_ReturnsCreatedResult()
        {
            // Arrange
            var itemRequest = new ItemRequest { LocationId = 1 };
            var createdItem = new Item { Id = 1, LocationId = 1 };
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.CreateItems)).Returns(true);
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.Admin)).Returns(false);
            _authorizationHelperMock.Setup(x => x.WorkPlaceId).Returns(1);
            _repositoryServiceMock.Setup(x => x.CreateItem(itemRequest)).Returns(createdItem);

            // Act
            var result = _controller.AddItem(itemRequest);

            // Assert
            result.Should().BeOfType<CreatedAtActionResult>().Which.Value.Should().BeEquivalentTo(createdItem);
        }

        [Fact]
        public void GetItemPrice_NoPermissionToViewItem_ReturnsForbid()
        {
            // Arrange
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.ViewItem)).Returns(false);

            // Act
            var result = _controller.GetItemPrice(1);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public void GetItemPrice_ItemNotFound_ReturnsNotFound()
        {
            // Arrange
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.ViewItem)).Returns(true);
            _repositoryServiceMock.Setup(x => x.GetItem(It.IsAny<int>())).Returns((Item)null);

            // Act
            var result = _controller.GetItemPrice(1);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void GetItemPrice_NoPermissionToGetDiscount_ReturnsItemPriceWithoutDiscount()
        {
            // Arrange
            var item = new Item { Id = 1, Price = 100 };
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.ViewItem)).Returns(true);
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.GetDiscount)).Returns(false);
            _repositoryServiceMock.Setup(x => x.GetItem(It.IsAny<int>())).Returns(item);

            // Act
            var result = _controller.GetItemPrice(1);

            // Assert
            result.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().Be(item.Price);
        }

        [Fact]
        public void GetItemPrice_ItemHasDiscount_ReturnsItemPriceWithDiscount()
        {
            // Arrange
            var item = new Item { Id = 1, Price = 150 };
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.ViewItem)).Returns(true);
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.GetDiscount)).Returns(true);
            _repositoryServiceMock.Setup(x => x.GetItem(It.IsAny<int>())).Returns(item);

            // Act
            var result = _controller.GetItemPrice(1);

            // Assert
            result.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().Be(item.Price * 0.9); // 10% discount
        }

        [Fact]
        public void UpdateItem_NoPermission_ReturnsForbid()
        {
            // Arrange
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.UpdateItems)).Returns(false);

            // Act
            var result = _controller.UpdateItem(1, new ItemRequest());

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public void UpdateItem_ItemNotFound_ReturnsNotFound()
        {
            // Arrange
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.UpdateItems)).Returns(true);
            _repositoryServiceMock.Setup(x => x.GetItem(It.IsAny<int>())).Returns((Item)null);

            // Act
            var result = _controller.UpdateItem(1, new ItemRequest());

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void UpdateItem_SuccessfulUpdate_ReturnsNoContent()
        {
            // Arrange
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.UpdateItems)).Returns(true);
            _repositoryServiceMock.Setup(x => x.GetItem(It.IsAny<int>())).Returns(new Item());

            // Act
            var result = _controller.UpdateItem(1, new ItemRequest());

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public void UpdateItem_Exception_ReturnsBadRequest()
        {
            // Arrange
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.UpdateItems)).Returns(true);
            _repositoryServiceMock.Setup(x => x.GetItem(It.IsAny<int>())).Throws(new Exception("Error"));

            // Act
            var result = _controller.UpdateItem(1, new ItemRequest());

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void DeleteItem_UserWithoutPermission_ReturnsForbidResult()
        {
            // Arrange
            int itemId = 1;
            _authorizationHelperMock
                .Setup(a => a.HasPermission(Permissions.DeleteItems))
                .Returns(false);

            // Act
            IActionResult result = _controller.DeleteItem(itemId);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public void DeleteItem_ItemNotFound_ReturnsNotFoundResult()
        {
            // Arrange
            int itemId = 1;
            _authorizationHelperMock
                .Setup(a => a.HasPermission(Permissions.DeleteItems))
                .Returns(true);
            _repositoryServiceMock
                .Setup(r => r.GetItem(itemId))
                .Returns((Item)null);

            // Act
            IActionResult result = _controller.DeleteItem(itemId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void DeleteItem_ItemDeletedSuccessfully_ReturnsNoContentResult()
        {
            // Arrange
            int itemId = 1;
            var item = new Item { Id = itemId };
            _authorizationHelperMock
                .Setup(a => a.HasPermission(Permissions.DeleteItems))
                .Returns(true);
            _repositoryServiceMock
                .Setup(r => r.GetItem(itemId))
                .Returns(item);

            // Act
            IActionResult result = _controller.DeleteItem(itemId);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            _repositoryServiceMock.Verify(r => r.DeleteItem(item), Times.Once);
        }

        [Fact]
        public void DeleteItem_ExceptionOccurs_ReturnsBadRequestResult()
        {
            // Arrange
            int itemId = 1;
            var exceptionMessage = "An error occurred";
            _authorizationHelperMock
                .Setup(a => a.HasPermission(Permissions.DeleteItems))
                .Returns(true);
            _repositoryServiceMock
                .Setup(r => r.GetItem(itemId))
                .Throws(new Exception(exceptionMessage));

            // Act
            IActionResult result = _controller.DeleteItem(itemId);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            ((BadRequestObjectResult)result).Value.Should().Be(exceptionMessage);
        }

        [Fact]
        public void GetItems_AuthenticatedUser_ReturnsAllItems()
        {
            // Arrange
            var items = new List<Item>
            {
                new Item { Id = 1, RequiresAuthorizedUser = false },
                new Item { Id = 2, RequiresAuthorizedUser = true },
            };

            _repositoryServiceMock.Setup(repo => repo.GetItems()).Returns(items);
            _authorizationHelperMock.Setup(auth => auth.IsAuthenticated).Returns(true);

            // Act
            var result = _controller.GetItems();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedItems = okResult.Value.Should().BeAssignableTo<IEnumerable<Item>>().Subject;
            returnedItems.Should().BeEquivalentTo(items);
        }
        [Fact]

        public void GetItems_UnauthenticatedUser_ReturnsOnlyNonAuthorizedItems()
        {
            // Arrange
            var items = new List<Item>
            {
                new Item { Id = 1, RequiresAuthorizedUser = false },
                new Item { Id = 2, RequiresAuthorizedUser = true },
            };

            var expectedItems = new List<Item>
            {
                new Item { Id = 1, RequiresAuthorizedUser = false }
            };

            _repositoryServiceMock.Setup(repo => repo.GetItems()).Returns(items);
            _authorizationHelperMock.Setup(auth => auth.IsAuthenticated).Returns(false);

            // Act
            var result = _controller.GetItems();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedItems = okResult.Value.Should().BeAssignableTo<IEnumerable<Item>>().Subject;
            returnedItems.Should().BeEquivalentTo(expectedItems);
        }

        [Fact]
        public void GetItemById_UserDoesNotHavePermission_ReturnsForbidResult()
        {
            // Arrange
            int id = 1;
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.ViewItem)).Returns(false);

            // Act
            IActionResult result = _controller.GetItemById(id);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }
        [Fact]
        public void GetItemById_ItemNotFound_ReturnsNotFoundResult()
        {
            // Arrange
            int id = 1;
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.ViewItem)).Returns(true);
            _repositoryServiceMock.Setup(x => x.GetItem(id)).Returns((Item)null);

            // Act
            IActionResult result = _controller.GetItemById(id);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }
        [Fact]
        public void GetItemById_ItemFound_ReturnsOkResult()
        {
            // Arrange
            int id = 1;
            var item = new Item { Id = id, Name = "Test Item" };
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.ViewItem)).Returns(true);
            _repositoryServiceMock.Setup(x => x.GetItem(id)).Returns(item);

            // Act
            IActionResult result = _controller.GetItemById(id);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            (result as OkObjectResult).Value.Should().BeEquivalentTo(item);
        }
        [Fact]
        public void GetItemById_ExceptionThrown_ReturnsBadRequestResult()
        {
            // Arrange
            int id = 1;
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.ViewItem)).Returns(true);
            _repositoryServiceMock.Setup(x => x.GetItem(id)).Throws(new Exception("Test exception"));

            // Act
            IActionResult result = _controller.GetItemById(id);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            (result as BadRequestObjectResult).Value.Should().Be("Test exception");
        }
    }

}
