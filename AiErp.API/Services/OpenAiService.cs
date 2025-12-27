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
            _apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API Key bulunamadı veya boş.");
        }

        public async Task<string> GenerateSqlFromText(string userPrompt)
        {
        
            var schema = @"
                -- TABLOLAR VE KOLONLAR
                Products (Id, Name, StockAmount, Price, VendorId)
                Vendors (Id, VendorName, ContactName, City, TaxId)
                Orders (Id, OrderDate, TotalAmount, CustomerName)
                PurchaseOrderDetails (OrderDetailId, OrderId, ProductId, Quantity, UnitPrice)
                GoodsReceipts (ReceiptId, OrderId, ReceiptDate, ReceivedQuantity)

                -- İLİŞKİLER (JOIN Mantığı)
                Products.VendorId -> Vendors.Id (Ürünün tedarikçisi)
                PurchaseOrderDetails.OrderId -> Orders.Id
                PurchaseOrderDetails.ProductId -> Products.Id
                GoodsReceipts.OrderId -> Orders.Id
            ";

            var systemMessage = $@"
                Sen uzman bir MSSQL (T-SQL) veritabanı asistanısın.
                Görevin: Kullanıcının doğal dil sorularını aşağıdaki şemaya uygun T-SQL sorgularına çevirmek.

                VERİTABANI ŞEMASI:
                {schema}

                KURALLAR:
                1. SADECE SQL kodu döndür. Açıklama, ```sql etiketi veya yorum satırı ASLA ekleme.
                2. KOLON İSİMLERİNE DİKKAT ET:
                   - Products ve Vendors tablolarının birincil anahtarı 'Id' dir. 'ProductId' veya 'VendorId' diye uydurma.
                   - Bağlantı kurarken: Products.VendorId = Vendors.Id
                3. ANLAM AYRIMI (Çok Önemli):
                   - 'Kaç çeşit', 'kaç farklı', 'kaç satır' denirse -> COUNT(*) kullan.
                   - 'Toplam stok', 'kaç adet ürün' (miktar olarak) denirse -> SUM(StockAmount) kullan.
                4. CRUD İŞLEMLERİ:
                   - Kullanıcı veri eklemek, silmek veya güncellemek isterse (INSERT, UPDATE, DELETE) buna izin ver ve uygun SQL'i üret.
                5. TARİH:
                   - Tarih filtrelerinde DATEADD(DAY, -X, GETDATE()) kullan.

                ÖRNEKLER (Few-Shot Learning):
                
                Kullanıcı: 'Toplam kaç çeşit ürünümüz var?'
                AI: SELECT COUNT(*) FROM Products

                Kullanıcı: 'Depoda toplam kaç adet (miktar) ürün var?'
                AI: SELECT SUM(StockAmount) FROM Products

                Kullanıcı: 'Hangi ürünü hangi tedarikçiden alıyoruz?'
                AI: SELECT p.Name, v.VendorName FROM Products p JOIN Vendors v ON p.VendorId = v.Id

                Kullanıcı: 'Yeni bir tedarikçi ekle: Adı Global Lojistik, Şehir İstanbul'
                AI: INSERT INTO Vendors (VendorName, City) VALUES ('Global Lojistik', 'İstanbul')

                Kullanıcı: 'Mouse fiyatlarını %10 arttır'
                AI: UPDATE Products SET Price = Price * 1.10 WHERE Name LIKE '%Mouse%'
            ";

            var requestBody = new
            {
                model = "gpt-4o-mini", // Veya gpt-3.5-turbo
                messages = new[]
                {
                    new { role = "system", content = systemMessage },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.1, // Tutarlı olması için düşük sıcaklık
                max_tokens = 500
            };

            var requestJson = JsonSerializer.Serialize(requestBody);
            
            // OpenAI API Çağrısı
            var request = new HttpRequestMessage(HttpMethod.Post, "[https://api.openai.com/v1/chat/completions](https://api.openai.com/v1/chat/completions)");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                // Hata durumunda SQL yorum satırı olarak hata döndür ki frontend patlamasın
                return $"-- API HATASI: {response.StatusCode} - {errorContent}";
            }

            var responseString = await response.Content.ReadAsStringAsync();
            
            using (JsonDocument doc = JsonDocument.Parse(responseString))
            {
                var sqlText = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                
                // Temizlik: AI bazen ```sql ... ``` formatında atar, onları siliyoruz.
                return sqlText?
                    .Replace("```sql", "")
                    .Replace("```", "")
                    .Trim() ?? "SELECT 1"; 
            }
        }
    }
}