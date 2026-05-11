using CreditCase.Application.DTOs.Payments;
using CreditCase.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace CreditCase.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var payments = await _paymentService.GetAllAsync();
        return Ok(payments);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request)
    {
        var payment = await _paymentService.CreateAsync(request);
        return CreatedAtAction(nameof(GetAll), payment);
    }
}
