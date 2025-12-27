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

        private static readonly Dictionary<string, string> TableMap =
            new(StringComparer.OrdinalIgnoreCase)
        {
            { "products", "Products" },
            { "vendors", "Vendors" },
            { "orders", "Orders" },
            { "details", "PurchaseOrderDetails" },
            { "receipts", "GoodsReceipts" }
        };

        // GÜNCELLEME: Tüm sorgulara 'AS [Türkçe İsim]' ekledik.
        private static readonly Dictionary<string, string> ComplexQueries =
            new(StringComparer.OrdinalIgnoreCase)
        {
            // 1. SATIN ALMA DETAYLARI
            {
                "details",
                @"SELECT 
                    d.OrderDetailId AS [Detay No],
                    d.OrderId AS [Sipariş No],
                    o.CustomerName AS [Müşteri Adı],
                    p.Name AS [Ürün Adı],
                    d.Quantity AS [Adet],
                    d.UnitPrice AS [Birim Fiyat],
                    (d.Quantity * d.UnitPrice) AS [Satır Toplamı]
                FROM PurchaseOrderDetails d
                JOIN Products p ON d.ProductId = p.Id
                JOIN Orders o ON d.OrderId = o.Id"
            },

            // 2. MAL KABUL FİŞLERİ
            {
                "receipts",
                @"SELECT 
                    r.ReceiptId AS [Fiş No],
                    r.OrderId AS [Sipariş No],
                    o.CustomerName AS [Müşteri],
                    r.ReceiptDate AS [İşlem Tarihi],
                    r.ReceivedQuantity AS [Teslim Alınan Miktar],
                    o.TotalAmount AS [Sipariş Toplamı]
                FROM GoodsReceipts r
                JOIN Orders o ON r.OrderId = o.Id"
            },

            // 3. ÜRÜNLER (En önemlisi bu)
            {
                "products",
                @"SELECT 
                    p.Id AS [Ürün No],
                    p.Name AS [Ürün Adı],
                    p.StockAmount AS [Stok Miktarı],
                    p.Price AS [Birim Fiyat],
                    v.VendorName AS [Tedarikçi Firma]
                FROM Products p
                JOIN Vendors v ON p.VendorId = v.Id"
            },

            // 4. TEDARİKÇİLER (SELECT * yerine özel seçim yaptık)
            { 
                "vendors", 
                @"SELECT 
                    Id AS [Firma No], 
                    VendorName AS [Firma Adı], 
                    ContactName AS [İlgili Kişi], 
                    City AS [Şehir], 
                    TaxId AS [Vergi No] 
                  FROM Vendors" 
            },

            // 5. SİPARİŞLER
            { 
                "orders", 
                @"SELECT 
                    Id AS [Sipariş No], 
                    CustomerName AS [Müşteri], 
                    OrderDate AS [Sipariş Tarihi], 
                    TotalAmount AS [Toplam Tutar] 
                  FROM Orders" 
            }
        };

        public RawDataController(SqlExecutorService sqlExecutorService)
        {
            _sqlExecutorService = sqlExecutorService;
        }

        [HttpGet("{tableName}")]
        public async Task<IActionResult> GetTableData(string tableName)
        {
            if (!TableMap.TryGetValue(tableName, out string? actualTableName))
            {
                return BadRequest($"Hata: '{tableName}' tablosu bulunamadı.");
            }

            string key = tableName.ToLowerInvariant();

            string sqlQuery = ComplexQueries.ContainsKey(key)
                ? ComplexQueries[key]
                : $"SELECT * FROM {actualTableName}";

            try
            {
                IEnumerable<dynamic> result = await _sqlExecutorService.ExecuteQueryAsync(sqlQuery);

                if (result == null)
                {
                    return Ok(new List<dynamic>());
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