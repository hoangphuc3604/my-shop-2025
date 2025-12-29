using System.Threading.Tasks;
using MyShop.Data.Models;

namespace MyShop.Contracts;

public interface IAccountService
{
    Task<User?> LoginAsync(string username, string password);
}
