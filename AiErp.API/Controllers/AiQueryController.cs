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
                // (Ekleme özelliğini iptal ettiğimiz için burası sadece SELECT üretecek)
                string sqlQuery = await _openAiService.GenerateSqlFromText(request.Question);

                if (string.IsNullOrEmpty(sqlQuery) || sqlQuery.StartsWith("HATA"))
                    return BadRequest(new { message = "SQL üretilemedi veya yetkisiz işlem." });

                // 3. ADIM: SQL'İ ÇALIŞTIR (Veritabanından veriyi çek)
                // SqlExecutorService'in görseldeki hali (sadece ExecuteQueryAsync) buna uygun.
                var rawData = await _sqlExecutorService.ExecuteQueryAsync(sqlQuery);

                // 4. ADIM: VERİYİ JSON'A ÇEVİR
                string jsonData = JsonSerializer.Serialize(rawData);
                
                // 5. ADIM: ANALİZ (Kahin Modu)
                string aiAnalysis = await _openAiService.AnalyzeResultSet(request.Question, jsonData);

                // 6. ADIM: HEPSİNİ GÖNDER
                return Ok(new 
                { 
                    originalQuestion = request.Question,
                    generatedSql = sqlQuery,
                    data = rawData,
                    analysis = aiAnalysis
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    // --- SORUNU ÇÖZEN KISIM BURASI ---
    public class UserQuery
    {
        // = string.Empty; diyerek "bu asla null olmayacak, başlangıçta boş metin olsun" diyoruz.
        // Böylece o sarı uyarı çizgisi ve hata kayboluyor.
        public string Question { get; set; } = string.Empty;
    }
}