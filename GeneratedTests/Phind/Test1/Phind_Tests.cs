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

namespace Kursinis.UnitTests.Controllers
{
    public class ItemsControllerTests
    {
        private readonly Mock<IRepositoryService> _repositoryServiceMock = new();
        private readonly Mock<IAuthorizationHelper> _authorizationHelperMock = new();
        private readonly Mock<IFileWriter> _fileWriterMock = new();

        private readonly ItemsController _controller;

        public ItemsControllerTests()
        {
            _controller = new ItemsController(_repositoryServiceMock.Object, _authorizationHelperMock.Object, _fileWriterMock.Object);
        }

        [Fact]
        public void GetItemById_ReturnsOkObjectResult_WithExpectedItem()
        {
            // Arrange
            var expectedItem = new Item { Id = 1, Name = "Test Item" };
            var mockRepository = new Mock<IRepositoryService>();
            mockRepository.Setup(repo => repo.GetItem(1)).Returns(expectedItem);
            var mockAuthorizationHelper = new Mock<IAuthorizationHelper>();
            mockAuthorizationHelper.Setup(helper => helper.HasPermission(Permissions.ViewItem)).Returns(true);
            var mockFileWriter = new Mock<IFileWriter>();
            var controller = new ItemsController(mockRepository.Object, mockAuthorizationHelper.Object, mockFileWriter.Object);

            // Act
            var result = controller.GetItemById(1);

            // Assert
            result.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().Be(expectedItem);
        }

        [Fact]
        public void GetItemById_ReturnsForbidResult_WhenUserDoesNotHavePermission()
        {
            // Arrange
            var mockRepository = new Mock<IRepositoryService>();
            var mockAuthorizationHelper = new Mock<IAuthorizationHelper>();
            mockAuthorizationHelper.Setup(helper => helper.HasPermission(Permissions.ViewItem)).Returns(false);
            var mockFileWriter = new Mock<IFileWriter>();
            var controller = new ItemsController(mockRepository.Object, mockAuthorizationHelper.Object, mockFileWriter.Object);

            // Act
            var result = controller.GetItemById(1);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public void GetItemById_ReturnsNotFoundResult_WhenItemDoesNotExist()
        {
            // Arrange
            Item nullItem = null;
            var mockRepository = new Mock<IRepositoryService>();
            mockRepository.Setup(repo => repo.GetItem(1)).Returns(nullItem);
            var mockAuthorizationHelper = new Mock<IAuthorizationHelper>();
            mockAuthorizationHelper.Setup(helper => helper.HasPermission(Permissions.ViewItem)).Returns(true);
            var mockFileWriter = new Mock<IFileWriter>();
            var controller = new ItemsController(mockRepository.Object, mockAuthorizationHelper.Object, mockFileWriter.Object);

            // Act
            var result = controller.GetItemById(1);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>()
                .Which.Value.Should().Be("Item with id 1 not found");
        }

        [Fact]
        public void AddItem_ReturnsCreatedResponse()
        {
            // Arrange
            var repositoryMock = new Mock<IRepositoryService>();
            var authorizationHelperMock = new Mock<IAuthorizationHelper>();
            var fileWriterMock = new Mock<IFileWriter>();
            var controller = new ItemsController(repositoryMock.Object, authorizationHelperMock.Object, fileWriterMock.Object);
            var itemRequest = new ItemRequest { Name = "Test Item", LocationId = 1 };

            authorizationHelperMock.Setup(x => x.HasPermission(Permissions.CreateItems)).Returns(true);
            repositoryMock.Setup(x => x.CreateItem(itemRequest)).Returns(new Item { Id = 1, Name = "Test Item", LocationId = 1 });

            // Act
            var result = controller.AddItem(itemRequest);

            // Assert
            result.Should().BeOfType<CreatedAtActionResult>()
                  .Subject.Value.Should().BeOfType<Item>()
                  .Subject.Name.Should().Be("Test Item");
        }

        [Fact]
        public void UpdateItem_ReturnsNoContent_WhenAuthorized()
        {
            // Arrange
            var id = 1;
            var request = new ItemRequest { Name = "New Name", Price = 10, LocationId = 1, RequiresAuthorizedUser = true };
            var mockRepo = new Mock<IRepositoryService>();
            var mockAuth = new Mock<IAuthorizationHelper>();
            mockAuth.Setup(a => a.HasPermission(Permissions.UpdateItems)).Returns(true);
            var mockWriter = new Mock<IFileWriter>();
            var controller = new ItemsController(mockRepo.Object, mockAuth.Object, mockWriter.Object);

            // Act
            var result = controller.UpdateItem(id, request);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            mockRepo.Verify(r => r.GetItem(id), Times.Once);
            mockRepo.Verify(r => r.UpdateItem(), Times.Once);
            mockWriter.Verify(w => w.Write(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void DeleteItem_ReturnsNoContent_WhenRequestIsSuccessful()
        {
            // Arrange
            var itemId = 1;
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.DeleteItems)).Returns(true);
            _repositoryServiceMock.Setup(x => x.GetItem(itemId)).Returns(new Item { Id = itemId });

            // Act
            var result = _controller.DeleteItem(itemId);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            _repositoryServiceMock.Verify(x => x.DeleteItem(It.IsAny<Item>()), Times.Once);
        }

        [Fact]
        public void DeleteItem_ReturnsNotFound_WhenItemDoesNotExist()
        {
            // Arrange
            var itemId = 1;
            _authorizationHelperMock.Setup(x => x.HasPermission(Permissions.DeleteItems)).Returns(true);
            _repositoryServiceMock.Setup(x => x.GetItem(itemId)).Returns((Item)null);

            // Act
            var result = _controller.DeleteItem(itemId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>()
                .Which.Value.Should().Be($"Item with id {itemId} not found");
            _repositoryServiceMock.Verify(x => x.DeleteItem(It.IsAny<Item>()), Times.Never);
        }

        [Fact]
        public void GetItemPrice_ReturnsOkObjectResult_WithDiscountedPrice()
        {
            // Arrange
            var itemId = 1;
            var item = new Item { Id = itemId, Price = 100 };
            var repositoryMock = new Mock<IRepositoryService>();
            repositoryMock.Setup(repo => repo.GetItem(itemId)).Returns(item);
            var authorizationMock = new Mock<IAuthorizationHelper>();
            authorizationMock.Setup(helper => helper.HasPermission(Permissions.ViewItem)).Returns(true);
            authorizationMock.Setup(helper => helper.HasPermission(Permissions.GetDiscount)).Returns(true);
            var fileWriterMock = new Mock<IFileWriter>();
            var controller = new ItemsController(repositoryMock.Object, authorizationMock.Object, fileWriterMock.Object);

            // Act
            var result = controller.GetItemPrice(itemId);

            // Assert
            result.Should().BeOfType<OkObjectResult>()
                  .Which.Value.Should().Be(90);
        }


    }
}

