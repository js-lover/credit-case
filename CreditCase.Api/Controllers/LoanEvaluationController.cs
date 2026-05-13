using CreditCase.Application.DTOs.LoanEvaluation;
using CreditCase.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace CreditCase.Api.Controllers;

/// <summary>
/// Kredi değerlendirme endpoint'leri. CLAUDE.md §18 API Layer.
/// </summary>
[ApiController]
[Route("api/loans")]
[Produces("application/json")]
public class LoanEvaluationController : ControllerBase
{
    private readonly ILoanEvaluationService _service;

    public LoanEvaluationController(ILoanEvaluationService service)
    {
        _service = service;
    }

    /// <summary>
    /// Kredi başvurusunu değerlendirir: risk analizi, vade farkı hesabı, onay/red kararı.
    /// Sonuç her durumda (onay veya red) veritabanına kaydedilir.
    /// </summary>
    /// <response code="200">Değerlendirme tamamlandı (onay veya red bilgisi içerir)</response>
    /// <response code="400">Geçersiz istek parametresi</response>
    /// <response code="404">Müşteri bulunamadı</response>
    [HttpPost("evaluate")]
    [ProducesResponseType(typeof(LoanEvaluationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LoanEvaluationResponse>> Evaluate([FromBody] LoanApplicationRequest request)
    {
        var result = await _service.EvaluateAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Belirli bir değerlendirme kaydını döner.
    /// </summary>
    /// <response code="200">Değerlendirme kaydı</response>
    /// <response code="404">Kayıt bulunamadı</response>
    [HttpGet("evaluation/{evaluationId:int}")]
    [ProducesResponseType(typeof(LoanEvaluationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LoanEvaluationResponse>> GetById(int evaluationId)
    {
        var result = await _service.GetByIdAsync(evaluationId);
        return Ok(result);
    }

    /// <summary>
    /// Müşterinin mevcut profiliyle alabileceği maksimum krediyi hesaplar.
    /// Gerçek bir başvuru yapmadan ön bilgi almak için kullanılır.
    /// </summary>
    /// <response code="200">Maksimum kredi uygunluk sonucu</response>
    /// <response code="404">Müşteri bulunamadı</response>
    [HttpGet("maximum-eligibility/{customerId:int}")]
    [ProducesResponseType(typeof(LoanEvaluationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LoanEvaluationResponse>> GetMaximumEligibility(int customerId)
    {
        var result = await _service.GetMaximumEligibilityAsync(customerId);
        return Ok(result);
    }
}
