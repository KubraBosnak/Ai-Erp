using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using System.Collections.Generic;

namespace AiErp.API.Services
{
    public class OpenAiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _modelName = "gpt-4o-mini"; 
        private readonly OpenAIClient _client;
        public OpenAiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API Key eksik.");
            _client = new OpenAIClient(_apiKey);
        }

        // 1. ADIM: SQL ÜRETME
        public async Task<string> GenerateSqlFromText(string userPrompt)
        {
            var schema = @"
                -- TABLOLAR VE KOLONLAR
                Products (Id, Name, StockAmount, Price, VendorId)
                Vendors (Id, VendorName, ContactName, City, TaxId)
                Orders (Id, OrderDate, TotalAmount, CustomerName)
                PurchaseOrderDetails (OrderDetailId, OrderId, ProductId, Quantity, UnitPrice)
                GoodsReceipts (ReceiptId, OrderId, ReceiptDate, ReceivedQuantity)

                -- İLİŞKİLER
                Products.VendorId -> Vendors.Id
                PurchaseOrderDetails.OrderId -> Orders.Id
                PurchaseOrderDetails.ProductId -> Products.Id
            ";

            var systemMessage = $@"
                Sen uzman bir MSSQL veritabanı mühendisisin.
                Görevin: Kullanıcı sorusunu T-SQL sorgusuna çevirmek.
                ŞEMA: {schema}
                VERİTABANI ŞEMASI VE KURALLAR:
                1. Sadece SQL kodu döndür. Markdown yok, yorum yok.
                2. Sadece SELECT, INSERT, UPDATE, DELETE kullan.
                3. Tarih analizlerinde 'yyyy-MM' formatında gruplama yap ki trend analizi yapabilelim.
                4. Tarih sorulursa 'Orders' tablosundaki 'OrderDate' sütununu kullan.
                5. En çok satılan ürün gibi sorgularda şu 3 tabloyu JOIN yap:
                  FROM PurchaseOrderDetails d
                  JOIN Orders o ON d.OrderId = o.Id
                  JOIN Products p ON d.ProductId = p.Id

                MEVSİM FİLTRELERİ:
                - Yaz (Summer): MONTH(o.OrderDate) IN (6, 7, 8)
                - Kış (Winter): MONTH(o.OrderDate) IN (12, 1, 2)
                - İlkbahar: 3, 4, 5
                - Sonbahar: 9, 10, 11
 
                ÇIKTI FORMATI:
                Sadece SQL kodunu döndür. Markdown (```sql) veya açıklama metni EKLEME.
            ";

            // SDK Kullanımı (Daha kararlı ve temiz)
            var options = new ChatCompletionsOptions()
            {
                DeploymentName = _modelName,
                Messages = { 
                    new ChatRequestSystemMessage(systemMessage), 
                    new ChatRequestUserMessage(userPrompt) 
                },
                Temperature = 0 // SQL için kesinlik şart
            };
            var response = await _client.GetChatCompletionsAsync(options);
            string sql = response.Value.Choices[0].Message.Content;

            // Temizlik
            return sql.Replace("```sql", "").Replace("```", "").Trim();
        }

        // 2. ADIM: ANALİZ VE TAHMİN
        public async Task<string> AnalyzeResultSet(string userQuestion, string jsonResultData)
        {
            // Eğer veri boşsa AI'yı yorma
            if (string.IsNullOrEmpty(jsonResultData) || jsonResultData == "[]")
                return "Analiz edilecek veri bulunamadı.";

            var systemMessage = @"
            Sen kıdemli bir ERP İş Analisti, Finansal Analist, Veri Bilimcisi, Sınıflandırma Uzmanısın.
            Gelen soruyu analiz et ve eğer kullanıcı sohbet etmek istiyorsa sadece tek kelime cevap ver.
            Sana kullanıcı sorusu ve veritabanından gelen ham veri (JSON) verilecek.

            GÖREVLERİN:
            1. Veriyi yorumla: Rakamlar ne anlatıyor? Artış mı var, düşüş mü?
            2. Verilerdeki matematiksel eğilimi (trend) hesaplayıp geleceği öngörmek.
            3. Trend Analizi: Verideki dalgalanmaları açıkla.
            4. TAHMİN (Predictive Analysis): Mevcut trende bakarak gelecek dönem için mantıklı bir öngörüde bulun.
            5. Aksiyon Önerisi: Bu veriye dayanarak yönetici ne yapmalı? (Örn: Stok artır, kampanyayı durdur vb.)

            ÖNEMLİ KURALLAR - İLERİ SEVİYE ANALİZ:
            1. Tarih sorulursa MUTLAKA 'Orders' tablosundaki 'OrderDate' alanını kullan.
            2. En çok satılan/temin edilen ürün sorulursa şu 3 tabloyu birleştirerek analiz yap:
             - Tablo 1: PurchaseOrderDetails d
             - Tablo 2: Orders o (Bağlantı: d.OrderId = o.Id)
             - Tablo 3: Products p (Bağlantı: d.ProductId = p.Id)
            3. Gruplamayı 'p.Name' sütununa göre yap.
            4. Sıralamayı 'SUM(d.Quantity) DESC' (çoktan aza) şeklinde yap.
            5. Mevsim sorulursa şu ayları filtrele:
           - Yaz Ayları: 6, 7, 8 (Haziran, Temmuz, Ağustos) 
           - Kış Ayları: 12, 1, 2
           - İlkbahar: 3, 4, 5
           - Sonbahar: 9, 10, 11
           6. Eğer kullanıcı veritabanından veri istiyorsa (satışlar, stok, ciro, kaç adet, listele vb.) -> 'SQL' yaz.
           7. Eğer kullanıcı selam veriyorsa, yeteneklerini soruyorsa veya veritabanı dışı sohbet ediyorsa -> 'CHAT' yaz.
           8. 'Ekleme yapabilir misin', 'Silme yapabilir misin' gibi sorular yetenek sorusudur -> 'CHAT' yaz.

            HESAPLAMA KURALLARI (BUNLARI UYGULA):
            1. Sadece son aya bakma! Son 3-4 ayın değişim oranını (yüzde kaç artmış/azalmış) kafanda hesapla.
            2. ASLA son ayın verisini aynen 'Gelecek ay tahmini' olarak yazma.
            3. HESAPLAMA MANTIĞI:
               - Eğer trend sürekli düşüyorsa (Örn: 100 -> 80 -> 70), gelecek ayı son aydan DAHA DÜŞÜK tahmin et (Örn: 60-65 arası).
               - Eğer trend artıyorsa, artış hızı kadar ekle.
               - Veri dalgalıysa (bir artıp bir azalıyorsa), son 3 ayın ORTALAMASINI al.

            ANALİZ KURALLARI:
            1. Sadece sayıları okuma, ne anlama geldiğini söyle. (Örn: 'Satışlar %20 artmış, bu iyiye işaret.')
            2. TREND ANALİZİ: Verilerdeki artış/azalış eğilimini tespit et.
            3. TAHMİN (PREDICTION): Son aylardaki trende bakarak gelecek ay için sayısal bir tahmin aralığı ver.
            - Örnek: 'Düşüş trendi devam ederse gelecek ay ciro 55.000 - 60.000 bandında olabilir.'
            4. UYARI: Eğer trend kötüyse yöneticiyi uyar.
            5. AKSİYON: Yöneticiye ne yapmasını önerirsin? (Stok artır, kampanya yap vb.)

            ANALİZ İÇERİĞİ (Satır Satır):
            - <tr><td><b>Durum Analizi:</b></td><td>Geçmiş verileri özetle, rakamların yanına 'TL' ekle.</td></tr>
            - <tr><td><b>Trend Yorumu:</b></td><td>Verilerdeki yüzde (%) değişim oranını ve ivmeyi profesyonelce yorumla.</td></tr>
            - <tr><td><b>Gelecek Tahmini (Forecast):</b></td><td>Matematiksel trende göre gelecek ay için net bir ARALIK ver (Örn: 55.000 TL - 60.000 TL).</td></tr>
            - <tr><td><b>Öneri:</b></td><td>Yöneticiye yönelik aksiyon planını (stok, kampanya vb.) yaz.</td></tr>

            ÇIKTI FORMATI:
            - Kısa, profesyonel ve madde madde olsun.
            - HTML formatında (<b>, <ul>, <li>) döndür ki frontend'de güzel görünsün.
            - Kullanıcı sohbet etmek istiyorsa tek kelimelik ceap ver.
            ";

            var userMessage = $"KULLANICI SORUSU: {userQuestion}\n\nVERİTABANI SONUCU (JSON): {jsonResultData}";


          var chatOptions = new ChatCompletionsOptions()
            {
                DeploymentName = _modelName, 
                Messages = {
                    new ChatRequestSystemMessage(systemMessage),
                    new ChatRequestUserMessage(userMessage)
                },
                Temperature = 0.7f,
                MaxTokens = 800
            };






          try
            {
                var response = await _client.GetChatCompletionsAsync(chatOptions);
                string content = response.Value.Choices[0].Message.Content;
                
                // Temizlik
                content = content.Replace("```html", "").Replace("```", "").Trim();        
                
                // HTML Tablo Garantisi
                if (!content.Contains("<table")) {
                    content = "<table style='width:100%; border:1px solid #ddd;'>" + content + "</table>";
                }
                
                return content;             
            }
            catch (Exception ex)
            {
                Console.WriteLine("AI Analiz Hatası: " + ex.Message);
                return "AI analizi şu an yapılamadı. (Hata: " + ex.Message + ")";
            }
        }





        // Manuel HTTP çağrısı (Sadece ihtiyaç duyulursa yedek olarak kalabilir)
        private async Task<string> CallOpenAiAsync(string systemMsg, string userMsg, double temperature)
        {
            var options = new ChatCompletionsOptions()
            {
                DeploymentName = _modelName,
                Messages = { 
                    new ChatRequestSystemMessage(systemMsg), 
                    new ChatRequestUserMessage(userMsg) 
                },
                Temperature = (float)temperature
            };

            var response = await _client.GetChatCompletionsAsync(options);
            return response.Value.Choices[0].Message.Content;
        }
    }
    }
