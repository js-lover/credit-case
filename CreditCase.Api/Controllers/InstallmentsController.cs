using CreditCase.Application.DTOs.Installments;
using CreditCase.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace CreditCase.Api.Controllers;

[ApiController]
[Route("api/installments")]
public class InstallmentsController : ControllerBase
{
    private readonly IInstallmentService _installmentService;

    public InstallmentsController(IInstallmentService installmentService)
    {
        _installmentService = installmentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var installments = await _installmentService.GetAllAsync();
        return Ok(installments);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var installment = await _installmentService.GetByIdAsync(id);
        return Ok(installment);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateInstallmentRequest request)
    {
        var installment = await _installmentService.UpdateAsync(id, request);
        return Ok(installment);
    }
}
