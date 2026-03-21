using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TechSolutions.Models;

namespace TechSolutions.Services
{
    public class CustomerService : ICustomerService
    {
        private ApplicationDbContext _context;
        public CustomerService()
        {
            _context = new ApplicationDbContext();
        }

        public List<Customer> GetAllCustomers()
        {
            return _context.Customers.Where(c => !c.IsDeleted).OrderByDescending(c => c.CreatedDate).ToList();
        }

        public List<Customer> GetCustomers(string searchTerm, int page, int pageSize)
        {
            var query = _context.Customers.Where(c => !c.IsDeleted);
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(c => 
                c.FirstName.ToLower().Contains(searchTerm) || 
                c.LastName.ToLower().Contains(searchTerm) || 
                c.Email.ToLower().Contains(searchTerm) || 
                c.Phone.ToLower().Contains(searchTerm) || 
                c.CustomerCode.ToLower().Contains(searchTerm)
                );
            }
            return query.OrderByDescending(c => c.CreatedDate).Skip((page - 1) * pageSize).Take(pageSize).ToList();
        }

        public int GetTotalCustomers(string searchTerm)
        { 
            var query = _context.Customers.Where(c => !c.IsDeleted);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(c => 
                c.FirstName.ToLower().Contains(searchTerm) ||
                c.LastName.ToLower().Contains(searchTerm) ||
                c.Email.ToLower().Contains(searchTerm) ||
                c.Phone.ToLower().Contains(searchTerm) ||
                c.CustomerCode.ToLower().Contains(searchTerm)
                );
            }

            return query.Count();
        }

        public Customer getCustomerById(int id)
        {
            return _context.Customers.Where(c => !c.IsDeleted).FirstOrDefault(c => c.CustomerID == id);
        }

        public void CreateCustomer(Customer customer)
        {
            if (string.IsNullOrWhiteSpace(customer.CustomerCode))
            {
                customer.CustomerCode = GenerateCustomerCode();
            }

            customer.CreatedDate = DateTime.Now;

            customer.DataQualityScore = CalculateDataQualityScore(customer);

            _context.Customers.Add(customer);
            _context.SaveChanges();
        }

        public void UpdateCustomer(Customer customer)
        {
            var existingCustomer = _context.Customers.Find(customer.CustomerID);

            if (existingCustomer != null)
            {
                existingCustomer.FirstName = customer.FirstName;
                existingCustomer.LastName = customer.LastName;
                existingCustomer.DateOfBirth = customer.DateOfBirth;
                existingCustomer.Gender = customer.Gender;
                existingCustomer.CustomerType = customer.CustomerType;
                existingCustomer.Email = customer.Email;
                existingCustomer.Phone = customer.Phone;
                existingCustomer.Address = customer.Address;
                existingCustomer.City = customer.City;
                existingCustomer.Province = customer.Province;
                existingCustomer.PostalCode = customer.PostalCode;
                existingCustomer.IsActive = customer.IsActive;
                existingCustomer.ModifiedDate = DateTime.Now;

                existingCustomer.DataQualityScore = CalculateDataQualityScore(existingCustomer);

                _context.SaveChanges();
            }
        }

        public void DeleteCustomer(int id)
        {
            try
            {
                // Temporarily disable validation for delete operation
                var originalValidateOnSaveEnabled = _context.Configuration.ValidateOnSaveEnabled;
                _context.Configuration.ValidateOnSaveEnabled = false;

                var customer = _context.Customers.Find(id);

                if (customer != null)
                {
                    // Soft delete - only update these two fields
                    customer.IsDeleted = true;
                    customer.ModifiedDate = DateTime.Now;

                    _context.SaveChanges();
                }

                // Re-enable validation
                _context.Configuration.ValidateOnSaveEnabled = originalValidateOnSaveEnabled;
            }
            catch (Exception ex)
            {
                // Re-enable validation even if error occurs
                _context.Configuration.ValidateOnSaveEnabled = true;
                throw new Exception("Error deleting customer: " + ex.Message, ex);
            }
        }

        public string GenerateCustomerCode()
        {
            var lastCustomer = _context.Customers
                .OrderByDescending(c => c.CustomerID)
                .FirstOrDefault();

            int nextNumber = 1;
            if (lastCustomer != null)
            {
                var lastCode = lastCustomer.CustomerCode;
                if (lastCode != null && lastCode.StartsWith("CUS-"))
                {
                    var numberPart = lastCode.Substring(4);
                    if (int.TryParse(numberPart, out int lastNumber))
                    {
                        nextNumber = lastNumber + 1;
                    }
                }
                else
                {
                    nextNumber = lastCustomer.CustomerID + 1;
                }
            }

            return $"CUS-{nextNumber:D4}";
        }

        public bool CustomerCodeExists(string customerCode)
        {
            return _context.Customers.Any(c => c.CustomerCode == customerCode && !c.IsDeleted);
        }

        private decimal CalculateDataQualityScore(Customer customer)
        {
            int score = 0;
            int totalFields = 10;

            // Required fields (10 points each)
            if (!string.IsNullOrWhiteSpace(customer.FirstName)) score += 10;
            if (!string.IsNullOrWhiteSpace(customer.LastName)) score += 10;
            if (!string.IsNullOrWhiteSpace(customer.Email)) score += 10;
            if (!string.IsNullOrWhiteSpace(customer.Phone)) score += 10;
            if (!string.IsNullOrWhiteSpace(customer.CustomerType)) score += 10;

            // Optional but important fields (10 points each)
            if (customer.DateOfBirth.HasValue) score += 10;
            if (!string.IsNullOrWhiteSpace(customer.Address)) score += 10;
            if (!string.IsNullOrWhiteSpace(customer.City)) score += 10;
            if (!string.IsNullOrWhiteSpace(customer.Province)) score += 10;
            if (!string.IsNullOrWhiteSpace(customer.PostalCode)) score += 10;

            return score;
        }

        // Dispose context when done
        public void Dispose()
        {
            _context?.Dispose();
        }

    }
}