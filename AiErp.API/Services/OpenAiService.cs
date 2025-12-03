using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace AiErp.API.Services
{
    public class OpenAiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public OpenAiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            // API Key'i appsettings.json'dan okuyoruz.
            _apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API Key bulunamadı veya boş.");
        }

        public async Task<string> GenerateSqlFromText(string userPrompt)
        {
            // Veritabanı Şeması (AI'a çoklu tablo bağlantılarını öğretiyoruz)
            var schema = @"
                CREATE TABLE Products (ProductId INT PRIMARY KEY, Name VARCHAR(100), StockAmount INT, Price DECIMAL(10, 2));
                CREATE TABLE Vendors (VendorId INT PRIMARY KEY, VendorName VARCHAR(150), City VARCHAR(50));
                
                -- FK: VendorId'yi Orders'a ekledik
                CREATE TABLE Orders (
                    OrderId INT PRIMARY KEY, 
                    VendorId INT, 
                    OrderDate DATETIME, 
                    TotalAmount DECIMAL(10, 2),
                    FOREIGN KEY (VendorId) REFERENCES Vendors(VendorId)
                );

                -- FK: Ürün ve Sipariş Kalemlerini bağlıyoruz
                CREATE TABLE PurchaseOrderDetails (
                    OrderDetailId INT PRIMARY KEY, 
                    OrderId INT, 
                    ProductId INT, 
                    Quantity INT, 
                    UnitPrice DECIMAL,
                    FOREIGN KEY (OrderId) REFERENCES Orders(OrderId),
                    FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
                );

                -- FK: Mal kabul kayıtlarını siparişe bağlıyoruz
                CREATE TABLE GoodsReceipts (
                    ReceiptId INT PRIMARY KEY, 
                    OrderId INT, 
                    ReceiptDate DATETIME, 
                    ReceivedQuantity INT,
                    FOREIGN KEY (OrderId) REFERENCES Orders(OrderId)
                );
            ";

            var systemMessage = @$"
                Sen bir T-SQL uzmanısın. Kullanıcı ne sorarsa sadece ham SQL döndür. Açıklama, yorum, kod bloğu ekleme.

Aşağıdaki tablo şemasını kullan:

Products(ProductId, Name, StockAmount, Price)
Vendors(VendorId, VendorName, City)
Orders(OrderId, VendorId, OrderDate, TotalAmount)
PurchaseOrderDetails(OrderDetailId, OrderId, ProductId, Quantity, UnitPrice)
GoodsReceipts(ReceiptId, OrderId, ReceiptDate, ReceivedQuantity)

Zorunlu JOIN Kuralları:
- Orders.OrderId = PurchaseOrderDetails.OrderId
- Products.ProductId = PurchaseOrderDetails.ProductId
- Vendors.VendorId = Orders.VendorId

Kurallar:
1. Kullanıcı “kaç tane”, “stok”, “stock amount”, “stok durumu” derse:
   → Products.StockAmount döndür. COUNT(*) kullanma.

2. Birden fazla tabloyu ilgilendiren her soruda JOIN kullanmak zorunludur.

3. Tarihli sorularda DATEADD kullan:
   Örnek: OrderDate >= DATEADD(DAY, -60, GETDATE())

4. Çıktın sadece SQL olsun. Başka hiçbir ekstra karakter olmasın.

                
                Şema: {schema}
            ";

            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new { role = "system", content = systemMessage },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.1, // Mantıklı ve tutarlı cevap için düşük sıcaklık
                max_tokens = 500
            };

            var requestJson = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

            if (!response.IsSuccessStatusCode)
            {
                // API'dan hata gelirse (401 Unauthorized, 400 Bad Request vb.), hatayı döndür
                var errorContent = await response.Content.ReadAsStringAsync();
                return $"-- HATA: API Başarısız. Durum Kodu: {response.StatusCode}. Hata içeriği: {errorContent}";
            }

            var responseString = await response.Content.ReadAsStringAsync();
            
            // JSON'dan sadece AI cevabını alıp temizliyoruz
            using (JsonDocument doc = JsonDocument.Parse(responseString))
            {
                var sqlText = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                // AI'ın eklediği kod bloklarını temizle
                return sqlText?.Replace("```sql", "").Replace("```", "").Trim() ?? "SELECT 1;";
            }
        }
    }
}