using Microsoft.AspNetCore.Mvc;
using AiErp.API.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq; // AllowedTables kontrolü için gerekli
using System;

namespace AiErp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Route: api/RawData
    public class RawDataController : ControllerBase
    {
        private readonly SqlExecutorService _sqlExecutorService;

        public RawDataController(SqlExecutorService sqlExecutorService)
        {
            _sqlExecutorService = sqlExecutorService;
        }

        // Güvenlik için izin verilen tablo adları listesi
        private static readonly string[] AllowedTables = 
            new[] { "Products", "Vendors", "Orders", "PurchaseOrderDetails", "GoodsReceipts" };

        // Frontend'ten gelen tablo adına göre veriyi çeker (GET api/RawData/Products)
        [HttpGet("{tableName}")]
        public async Task<IActionResult> GetTableData(string tableName)
        {
            // SQL Injection (Zararlı Yazılım) KORUMASI:
            // Sadece izin verilen tablo adlarını kabul et
            if (!AllowedTables.Any(t => t.Equals(tableName, StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest($"Hata: '{tableName}' adında bir tabloya erişim izni yok.");
            }

            // Dapper ile ham SQL sorgusunu oluştur ve çalıştır
            string sqlQuery = $"SELECT * FROM {tableName}";
            
            try
            {
                IEnumerable<dynamic> result = await _sqlExecutorService.ExecuteQueryAsync(sqlQuery);
                
                if (result == null || !result.Any())
                {
                    return NotFound($"Tablo '{tableName}' boş veya bulunamadı.");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Veritabanı hatasını yakala (Örn: Bağlantı hatası)
                return StatusCode(500, $"Veritabanı hatası: {ex.Message}");
            }
        }
    }
}