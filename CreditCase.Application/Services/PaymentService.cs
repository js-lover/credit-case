using CreditCase.Application.DTOs.Payments;
using CreditCase.Application.Exceptions;
using CreditCase.Application.Interfaces.Repositories;
using CreditCase.Application.Interfaces.Services;
using CreditCase.Domain.Entities;
using CreditCase.Domain.Enums;
using FluentValidation;

namespace CreditCase.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IInstallmentRepository _installmentRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly IValidator<CreatePaymentRequest> _createValidator;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IInstallmentRepository installmentRepository,
        ILoanRepository loanRepository,
        IValidator<CreatePaymentRequest> createValidator)
    {
        _paymentRepository = paymentRepository;
        _installmentRepository = installmentRepository;
        _loanRepository = loanRepository;
        _createValidator = createValidator;
    }

    public async Task<IEnumerable<PaymentResponse>> GetAllAsync()
    {
        var payments = await _paymentRepository.GetAllAsync();
        return payments.Select(MapToResponse);
    }

    public async Task<PaymentResponse> CreateAsync(CreatePaymentRequest request)
    {
        await _createValidator.ValidateAndThrowAsync(request);

        var installment = await _installmentRepository.GetByIdAsync(request.InstallmentId);
        if (installment is null)
            throw new NotFoundException($"Installment with ID {request.InstallmentId} not found.");

        if (installment.Status == InstallmentStatus.Paid)
            throw new BusinessRuleException("This installment has already been paid.");

        var existingPayment = await _paymentRepository.GetByInstallmentIdAsync(request.InstallmentId);
        if (existingPayment is not null)
            throw new BusinessRuleException("A payment already exists for this installment.");

        var payment = new Payment
        {
            InstallmentId = request.InstallmentId,
            PaymentAmount = request.PaymentAmount,
            PaymentDate = DateTime.UtcNow,
            Status = PaymentStatus.Successful
        };

        var created = await _paymentRepository.AddAsync(payment);

        installment.Status = InstallmentStatus.Paid;
        await _installmentRepository.UpdateAsync(installment);

        var loan = await _loanRepository.GetByIdWithInstallmentsAsync(installment.LoanId);
        if (loan is not null)
        {
            // RemainingPrincipal = sum of all installments not yet paid
            loan.RemainingPrincipal = loan.Installments
                .Where(i => i.Status != InstallmentStatus.Paid)
                .Sum(i => i.Amount);

            if (loan.RemainingPrincipal == 0)
                loan.Status = LoanStatus.Closed;

            await _loanRepository.UpdateAsync(loan);
        }

        return MapToResponse(created);
    }

    private static PaymentResponse MapToResponse(Payment payment) => new()
    {
        Id = payment.Id,
        InstallmentId = payment.InstallmentId,
        PaymentAmount = payment.PaymentAmount,
        PaymentDate = payment.PaymentDate,
        Status = payment.Status
    };
}
