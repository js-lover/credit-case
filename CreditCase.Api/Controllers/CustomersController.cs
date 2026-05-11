using CreditCase.Application.DTOs.Customers;
using CreditCase.Application.DTOs.LoanEvaluation;
using CreditCase.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace CreditCase.Api.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ILoanEvaluationService _evaluationService;

    public CustomersController(ICustomerService customerService, ILoanEvaluationService evaluationService)
    {
        _customerService = customerService;
        _evaluationService = evaluationService;
    }

    /// <summary>
    /// Sistemdeki tüm müşterileri listeler.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var customers = await _customerService.GetAllAsync();
        return Ok(customers);
    }

    /// <summary>
    /// Belirtilen ID'ye sahip müşteriyi döner.
    /// </summary>
    /// <param name="id">Müşteri ID'si</param>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        return Ok(customer);
    }

    /// <summary>
    /// Müşterinin borç özetini döner: toplam kalan anapara, gecikmiş/ödenen/bekleyen
    /// taksit sayıları ve toplam ödenmemiş borç tutarı.
    /// </summary>
    /// <param name="id">Müşteri ID'si</param>
    [HttpGet("{id:int}/summary")]
    public async Task<IActionResult> GetSummary(int id)
    {
        var summary = await _customerService.GetSummaryAsync(id);
        return Ok(summary);
    }

    /// <summary>
    /// Yeni müşteri kaydı oluşturur. TC Kimlik Numarası benzersiz olmalıdır.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request)
    {
        var customer = await _customerService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
    }

    /// <summary>
    /// Müşterinin ad, soyad, e-posta ve telefon bilgilerini günceller.
    /// </summary>
    /// <param name="id">Müşteri ID'si</param>
    /// <param name="request">Güncellenecek alanlar</param>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerRequest request)
    {
        var customer = await _customerService.UpdateAsync(id, request);
        return Ok(customer);
    }

    /// <summary>
    /// Müşterinin tüm kredi değerlendirme geçmişini döner.
    /// </summary>
    /// <param name="id">Müşteri ID'si</param>
    [HttpGet("{id:int}/evaluations")]
    public async Task<ActionResult<IEnumerable<LoanEvaluationResponse>>> GetEvaluations(int id)
    {
        var evaluations = await _evaluationService.GetByCustomerIdAsync(id);
        return Ok(evaluations);
    }

    /// <summary>
    /// Müşteriyi siler. Bağlı kredi ve taksit kayıtları cascade olarak kaldırılır.
    /// </summary>
    /// <param name="id">Müşteri ID'si</param>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _customerService.DeleteAsync(id);
        return NoContent();
    }
}
