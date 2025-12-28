using Microsoft.AspNetCore.Mvc;
using AiErp.API.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using System;

namespace AiErp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AiQueryController : ControllerBase
    {
        private readonly OpenAiService _openAiService;
        private readonly SqlExecutorService _sqlExecutorService;

        public AiQueryController(OpenAiService openAiService, SqlExecutorService sqlExecutorService)
        {
            _openAiService = openAiService;
            _sqlExecutorService = sqlExecutorService;
        }

        [HttpPost("query")]
        public async Task<IActionResult> Ask([FromBody] UserQuery request)
        {
            try
            {
                // 1. GÜVENLİK KONTROLÜ
                if (request == null || string.IsNullOrWhiteSpace(request.Question))
                    return BadRequest(new { message = "Soru boş olamaz." });

                // 2. ADIM: AI SQL ÜRETSİN
                // (Senin OpenAiService.cs içindeki GenerateSqlFromText metodunu çağırır)
                string sqlQuery = await _openAiService.GenerateSqlFromText(request.Question);

                // Konsola yazalım ki SQL'i görelim
                Console.WriteLine("AI SQL: " + sqlQuery);

                if (string.IsNullOrEmpty(sqlQuery) || sqlQuery.StartsWith("HATA"))
                    return BadRequest(new { message = "SQL üretilemedi." });

                // 3. ADIM: SQL'İ ÇALIŞTIR (Veriyi Çek)
                var rawData = await _sqlExecutorService.ExecuteQueryAsync(sqlQuery);

                // 4. ADIM: VERİYİ JSON'A ÇEVİR (Analiz için hazırlık)
                string jsonData = JsonSerializer.Serialize(rawData);

                // -----------------------------------------------------------
                // 5. ADIM: İŞTE BURASI EKSİK OLABİLİR! (ANALİZİ ÇAĞIRMA)
                // -----------------------------------------------------------
                string aiAnalysis = await _openAiService.AnalyzeResultSet(request.Question, jsonData);

                // 6. ADIM: HEPSİNİ GÖNDER
                return Ok(new 
                { 
                    originalQuestion = request.Question,
                    generatedSql = sqlQuery,
                    data = rawData,
                    analysis = aiAnalysis // Frontend'in beklediği HTML analiz buraya gider
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class UserQuery
    {
        public string Question { get; set; } = string.Empty;
    }
}