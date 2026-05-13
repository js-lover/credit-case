using CreditCase.Application.DTOs.Loans;
using CreditCase.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace CreditCase.Api.Controllers;

[ApiController]
[Route("api/loans")]
public class LoansController : ControllerBase
{
    private readonly ILoanService _loanService;

    public LoansController(ILoanService loanService)
    {
        _loanService = loanService;
    }

    /// <summary>
    /// Sistemdeki tüm kredileri listeler.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var loans = await _loanService.GetAllAsync();
        return Ok(loans);
    }

    /// <summary>
    /// Belirtilen ID'ye sahip krediyi taksit planı ve ödeme bilgileriyle birlikte döner.
    /// </summary>
    /// <param name="id">Kredi ID'si</param>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var loan = await _loanService.GetByIdAsync(id);
        return Ok(loan);
    }

    /// <summary>
    /// Yeni kredi oluşturur. Kredi skoru onaylandıktan sonra taksit planı otomatik üretilir.
    /// Taksit tutarı düz vade farkı (flat-rate) yöntemiyle hesaplanır.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLoanRequest request)
    {
        var loan = await _loanService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = loan.Id }, loan);
    }
}
