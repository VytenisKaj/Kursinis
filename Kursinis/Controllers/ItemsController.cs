using Kursinis.Enums;
using Kursinis.Models;
using Kursinis.Services.AuthorizationHelper;
using Kursinis.Services.FileWriter;
using Kursinis.Services.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Kursinis.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemsController : ControllerBase
    {

        private readonly IRepositoryService _repositoryService;
        private readonly IAuthorizationHelper _aurhorizationHelper;
        private readonly IFileWriter _writer;


        public ItemsController()
        {
            _repositoryService = new RepositoryService();
            _aurhorizationHelper = new AuthorizationHelper();
            _writer = new FileWriter();
        }

        public ItemsController(IRepositoryService repositoryService, IAuthorizationHelper authorizationHelper, IFileWriter fileWriter)
        {
            _repositoryService = repositoryService;
            _aurhorizationHelper = authorizationHelper;
            _writer = fileWriter;
        }

        [HttpGet]
        public IActionResult GetItems()
        {
            if (_aurhorizationHelper.IsAuthenticated)
            {
                return Ok(_repositoryService.GetItems());
            }
            return Ok(_repositoryService.GetItems().Where( item => item.RequiresAuthorizedUser == false));
        }

        [HttpGet]  
        public IActionResult GetItemById(int id)
        {
            if (!_aurhorizationHelper.HasPermission(Permissions.ViewItem))
            {
                return Forbid();
            }
            try
            {
                var Item = _repositoryService.GetItem(id);

                if (Item == null)
                {
                    return NotFound($"Item with id {id} not found");
                }

                return Ok(Item);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public IActionResult GetItemPrice(int id)
        {
            if (!_aurhorizationHelper.HasPermission(Permissions.ViewItem))
            {
                return Forbid();
            }
            try
            {
                var item = _repositoryService.GetItem(id);

                if (item == null)
                {
                    return NotFound($"Item with id {id} not found");
                }

                return CalculatateDiscount(item) switch
                {
                    Discount.Small => Ok(item.Price * 0.9),
                    Discount.Medium => Ok(item.Price * 0.8),
                    Discount.Big => Ok(item.Price * 0.7),
                    Discount.None => Ok(item.Price),
                    _ => throw new ArgumentOutOfRangeException(nameof(CalculatateDiscount), "Not a valid discount")
                };

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public IActionResult AddItem([FromBody]ItemRequest request)
        {
            if (!_aurhorizationHelper.HasPermission(Permissions.CreateItems))
            {
                return Forbid();
            }

            if ((!_aurhorizationHelper.HasPermission(Permissions.Admin)) && (_aurhorizationHelper.WorkPlaceId != request.LocationId))
            {
                return BadRequest("Cannot add items to different location");
            }
            try
            {
                var item = _repositoryService.CreateItem(request);
                return CreatedAtAction(nameof(GetItemById), item, item.Id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        public IActionResult UpdateItem(int id, ItemRequest request)
        {
            if (!_aurhorizationHelper.HasPermission(Permissions.UpdateItems))
            {
                return Forbid();
            }
            try
            {
                var item = _repositoryService.GetItem(id);

                if (item == null)
                { 
                    return NotFound($"Item with id {id} not found"); 
                }

                item.Name = request.Name;
                item.Price = request.Price;
                item.LocationId = request.LocationId;
                item.RequiresAuthorizedUser = request.RequiresAuthorizedUser;

                _repositoryService.UpdateItem();

                _writer.Write($"{_aurhorizationHelper.WorkPlaceId}.log", $"user id={_aurhorizationHelper.UserId} updated item id={id}");

                return NoContent();
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete] IActionResult DeleteItem(int id)
        {
            if (!_aurhorizationHelper.HasPermission(Permissions.DeleteItems))
            {
                return Forbid();
            }
            try
            {
                var item = _repositoryService.GetItem(id);
                if (item == null)
                {
                    return NotFound($"Item with id {id} not found");
                }
                _repositoryService.DeleteItem(item);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private Discount CalculatateDiscount(Item item)
        {
            if (!_aurhorizationHelper.HasPermission(Permissions.GetDiscount))
            {
                return Discount.None;
            }

            if (item.Price > 200)
            {
                return Discount.Big;
            }

            if (item.Price > 100)
            {
                return Discount.Medium;
            }

            if (item.Price > 50)
            {
                return Discount.Small;
            }

            return Discount.None;
        }
    }
}
