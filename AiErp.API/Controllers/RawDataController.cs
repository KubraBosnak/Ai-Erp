using Microsoft.AspNetCore.Mvc;
using AiErp.API.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

namespace AiErp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RawDataController : ControllerBase
    {
        private readonly SqlExecutorService _sqlExecutorService;

        // Kullanıcı dostu adları gerçek tablo adlarına çeviren map
        private static readonly Dictionary<string, string> TableMap =
            new(StringComparer.OrdinalIgnoreCase)
        {
            { "products", "Products" },
            { "vendors", "Vendors" },
            { "orders", "Orders" },
            { "details", "PurchaseOrderDetails" },
            { "receipts", "GoodsReceipts" }
        };

        // JOIN gerektiren tablolar için özel SQL sorguları
        private static readonly Dictionary<string, string> ComplexQueries =
            new(StringComparer.OrdinalIgnoreCase)
        {
            // SATIN ALMA DETAYLARI – Ürün adı + müşteri adı
            {
                "details",
                @"SELECT 
                    d.OrderDetailId,
                    d.OrderId,
                    o.CustomerName,
                    p.Name AS ProductName,
                    d.Quantity,
                    d.UnitPrice
                FROM PurchaseOrderDetails d
                JOIN Products p ON d.ProductId = p.Id
                JOIN Orders o ON d.OrderId = o.Id"
            },

            // FİŞLER – Müşteri adı + sipariş toplamı
            {
                "receipts",
                @"SELECT 
                    r.ReceiptId,
                    r.OrderId,
                    o.CustomerName,
                    r.ReceiptDate,
                    r.ReceivedQuantity,
                    o.TotalAmount AS OrderTotal
                FROM GoodsReceipts r
                JOIN Orders o ON r.OrderId = o.Id"
            },

            // Basit tablolar
            { "products", "SELECT * FROM Products" },
            { "vendors", "SELECT * FROM Vendors" },
            { "orders", "SELECT * FROM Orders" }
        };

        public RawDataController(SqlExecutorService sqlExecutorService)
        {
            _sqlExecutorService = sqlExecutorService;
        }

        // GET: api/RawData/{tableName}
        [HttpGet("{tableName}")]
        public async Task<IActionResult> GetTableData(string tableName)
        {
            // 1. Kullanıcının yazdığı isim map'te var mı?
            if (!TableMap.TryGetValue(tableName, out string? actualTableName))
            {
                return BadRequest($"Hata: '{tableName}' için veri erişimi tanımlı değil.");
            }

            string key = tableName.ToLowerInvariant();

            // 2. Özel bir JOIN sorgusu var mı?
            string sqlQuery = ComplexQueries.ContainsKey(key)
                ? ComplexQueries[key]
                : $"SELECT * FROM {actualTableName}";

            try
            {
                IEnumerable<dynamic> result = await _sqlExecutorService.ExecuteQueryAsync(sqlQuery);

                if (result == null || !result.Any())
                {
                    return NotFound($"Tablo '{tableName}' boş.");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Veritabanı hatası: {ex.Message}");
            }
        }
    }
}
