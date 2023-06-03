using Kursinis.Models;

namespace Kursinis.Services.Repositories
{
    public interface IRepositoryService
    {
        List<Item> GetItems();

        Item? GetItem(int id);

        void DeleteItem(Item item);
        void UpdateItem();

        Item CreateItem(ItemRequest item);
    }
}
