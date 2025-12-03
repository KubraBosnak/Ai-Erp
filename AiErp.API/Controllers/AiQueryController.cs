using Microsoft.AspNetCore.Mvc;
using AiErp.API.Services;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AiErp.API.Controllers // Namespace'in doğru olduğundan emin ol!
{
    [ApiController]
    [Route("api/[controller]")]
    public class AiQueryController : ControllerBase
    {
        private readonly OpenAiService _openAiService;
        private readonly SqlExecutorService _sqlExecutorService;

        public AiQueryController(
            OpenAiService openAiService, 
            SqlExecutorService sqlExecutorService)
        {
            _openAiService = openAiService;
            _sqlExecutorService = sqlExecutorService;
        }

        [HttpPost("query")]
        public async Task<IActionResult> GetAiQuery([FromBody] string userQuestion)
        {
            if (string.IsNullOrWhiteSpace(userQuestion))
            {
                return BadRequest("Sorgu metni boş olamaz.");
            }

            // 1. AI'dan SQL Sorgusunu Al 
            string sqlQuery = await _openAiService.GenerateSqlFromText(userQuestion);

            // 2. SQL Sorgusunu Veritabanında Çalıştır 
            IEnumerable<dynamic> result = await _sqlExecutorService.ExecuteQueryAsync(sqlQuery);

            // 3. Sonucu Frontend'e Gönder
            return Ok(new { 
                query = userQuestion,
                sqlExecuted = sqlQuery, 
                data = result 
            });
        }
    }
}