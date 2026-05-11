using CreditCase.Application.DTOs.Customers;
using CreditCase.Application.Exceptions;
using CreditCase.Application.Interfaces.Repositories;
using CreditCase.Application.Interfaces.Services;
using CreditCase.Domain.Entities;
using FluentValidation;

namespace CreditCase.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IValidator<CreateCustomerRequest> _createValidator;
    private readonly IValidator<UpdateCustomerRequest> _updateValidator;

    public CustomerService(
        ICustomerRepository customerRepository,
        IValidator<CreateCustomerRequest> createValidator,
        IValidator<UpdateCustomerRequest> updateValidator)
    {
        _customerRepository = customerRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IEnumerable<CustomerResponse>> GetAllAsync()
    {
        var customers = await _customerRepository.GetAllAsync();
        return customers.Select(MapToResponse);
    }

    public async Task<CustomerResponse> GetByIdAsync(int id)
    {
        var customer = await _customerRepository.GetByIdAsync(id);
        if (customer is null)
            throw new NotFoundException($"Customer with ID {id} not found.");
        return MapToResponse(customer);
    }

    public async Task<CustomerResponse> CreateAsync(CreateCustomerRequest request)
    {
        await _createValidator.ValidateAndThrowAsync(request);

        var existing = await _customerRepository.GetByIdentityNumberAsync(request.IdentityNumber);
        if (existing is not null)
            throw new BusinessRuleException("A customer with this identity number already exists.");

        var existingEmail = await _customerRepository.GetByEmailAsync(request.Email);
        if (existingEmail is not null)
            throw new BusinessRuleException("A customer with this email already exists.");

        var customer = new Customer
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            IdentityNumber = request.IdentityNumber,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _customerRepository.AddAsync(customer);
        return MapToResponse(created);
    }

    public async Task<CustomerResponse> UpdateAsync(int id, UpdateCustomerRequest request)
    {
        await _updateValidator.ValidateAndThrowAsync(request);

        var customer = await _customerRepository.GetByIdAsync(id);
        if (customer is null)
            throw new NotFoundException($"Customer with ID {id} not found.");

        if (!string.Equals(customer.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existingEmail = await _customerRepository.GetByEmailAsync(request.Email);
            if (existingEmail is not null)
                throw new BusinessRuleException("A customer with this email already exists.");
        }

        customer.FirstName = request.FirstName;
        customer.LastName = request.LastName;
        customer.Email = request.Email;
        customer.PhoneNumber = request.PhoneNumber;

        var updated = await _customerRepository.UpdateAsync(customer);
        return MapToResponse(updated);
    }

    public async Task DeleteAsync(int id)
    {
        var customer = await _customerRepository.GetByIdAsync(id);
        if (customer is null)
            throw new NotFoundException($"Customer with ID {id} not found.");

        await _customerRepository.DeleteAsync(customer);
    }

    public async Task<CustomerSummaryResponse> GetSummaryAsync(int id)
    {
        var customer = await _customerRepository.GetByIdWithLoansAndInstallmentsAsync(id);
        if (customer is null)
            throw new NotFoundException($"Customer with ID {id} not found.");

        var allInstallments = customer.Loans.SelectMany(l => l.Installments).ToList();

        return new CustomerSummaryResponse
        {
            CustomerId = customer.Id,
            FullName = $"{customer.FirstName} {customer.LastName}",
            TotalLoans = customer.Loans.Count,
            TotalRemainingPrincipal = customer.Loans.Sum(l => l.RemainingPrincipal),
            TotalOutstandingDebt = allInstallments
                .Where(i => i.Status != Domain.Enums.InstallmentStatus.Paid)
                .Sum(i => i.Amount),
            PaidInstallments = allInstallments.Count(i => i.Status == Domain.Enums.InstallmentStatus.Paid),
            UnpaidInstallments = allInstallments.Count(i => i.Status == Domain.Enums.InstallmentStatus.Unpaid),
            OverdueInstallments = allInstallments.Count(i => i.Status == Domain.Enums.InstallmentStatus.Overdue)
        };
    }

    private static CustomerResponse MapToResponse(Customer customer) => new()
    {
        Id = customer.Id,
        FirstName = customer.FirstName,
        LastName = customer.LastName,
        IdentityNumber = customer.IdentityNumber,
        Email = customer.Email,
        PhoneNumber = customer.PhoneNumber,
        CreatedAt = customer.CreatedAt
    };
}
