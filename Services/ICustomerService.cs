using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechSolutions.Models;

namespace TechSolutions.Services
{
    internal interface ICustomerService
    {
        List<Customer> GetAllCustomers();
        List<Customer> GetCustomers(string searchTerm, int page, int pageSize);
        int GetTotalCustomers(string searchTerm);
        Customer getCustomerById(int id);
        void CreateCustomer(Customer customer);
        void UpdateCustomer(Customer customer);
        void DeleteCustomer(int id);
        string GenerateCustomerCode();
        bool CustomerCodeExists(string customerCode);

    }
}
