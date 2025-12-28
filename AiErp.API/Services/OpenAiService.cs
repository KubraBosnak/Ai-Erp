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
                Sen uzman bir T-SQL veritabanı mühendisisin.
                ŞEMA: {schema}

                ---------- KESİN KURALLAR (HATA YAPMA) ----------

                SENARYO A: GENEL SATIŞ / CİRO / KASA (Sales)
                - Soru: 'Ciro ne kadar?', 'Toplam satış', 'Kasa durumu'
                - KURAL: SADECE 'Orders' tablosunu kullan.
                - FORMÜL: SUM(TotalAmount)
                - YASAK: Asla detay tablolarını JOIN yapma (Veri çoğalır!).

                SENARYO B: TEDARİKÇİ / SATINALMA ANALİZİ (Procurement)
                - Soru: 'En çok ödeme yapılan tedarikçi', 'Hangi tedarikçiden ne aldık', 'Maliyet analizi'
                - KURAL: Burada JOIN yapman ŞARTTIR.
                - YOL: Vendors -> Products -> PurchaseOrderDetails
                - FORMÜL (Çok Önemli): SUM(PurchaseOrderDetails.Quantity * PurchaseOrderDetails.UnitPrice) 
                - YASAK: Tedarikçi analizinde 'Orders.TotalAmount' sütununu kullanma! Satır bazlı hesapla.

                SENARYO C: TARİH FİLTRELERİ
                - Eğer kullanıcı tarih belirtmediyse (örn: 'son 6 ay' demediyse), TÜM ZAMANLARI getir. 
                - Kendi kafandan 'WHERE Year(OrderDate) = 2025' gibi filtreler ASLA EKLEME.

                ÇIKTI FORMATI:
                - Sadece SQL kodu döndür. Markdown yok.
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
            Sen kıdemli bir ERP İş Analisti, Finansal Analist, Veri Bilimcisi, Sınıflandırma ve Satınalma Uzmanısın.
            Gelen soruyu analiz et ve eğer kullanıcı sohbet etmek istiyorsa sadece tek kelime cevap ver.
            Sana kullanıcı sorusu ve veritabanından gelen ham veri (JSON) verilecek.

            AMACIN:
            Bu verileri yöneticiye sunulacak kısa, net ve profesyonel bir rapora dönüştürmek.
            ASLA 'SQL' veya 'Chat' gibi tek kelimelik cevaplar verme. Doğrudan analize başla.

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
           - Kış Ayları: 12, 1, 2 (Aralık, Ocak, Şubat)
           - İlkbahar: 3, 4, 5 (Mart, Nisan, Mayıs)
           - Sonbahar: 9, 10, 11 (Eylül, Ekim, Kasım)
           6. GÖREVİN ROUTING DEĞİL ANALİZDİR: Sana gelen JSON verisi, SQL sorgusuyla zaten çekilmiştir. Sen bu veriyi yorumlamakla yükümlüsün. ASLA 'SQL' yazma. 
           7. ÇIKTI GARANTİSİ: Kullanıcı ne sorarsa sorsun, elindeki JSON verisine dayanarak mutlaka aşağıda belirtilen HTML Tablo formatında bir analiz üret.
           8. 'Ekleme yapabilir misin', 'Silme yapabilir misin' gibi sorular yetenek sorusudur -> 'CHAT' yaz.
           ÇOK KRİTİK KURALLAR (BUNLARA UYMAZSAN SİSTEM ÇÖKER):
    
    1. CİRO, TOPLAM TUTAR veya KAZANÇ SORULURSA:
       - SADECE ve SADECE 'Orders' tablosunu kullan.
       - Sütun: SUM(TotalAmount)
       - ASLA 'PurchaseOrderDetails' tablosunu JOIN YAPMA!
       - SEBEP: Bir siparişin içinde birden fazla ürün detayı vardır. Eğer detay tablosunu birleştirirsen, sipariş tutarını ürün sayısı kadar mükerrer toplarsın ve sonuç yanlış çıkar.
    
    2. EN ÇOK SATILAN ÜRÜN SORULURSA:
       - İşte o zaman PurchaseOrderDetails, Orders ve Products tablolarını JOIN yap.
       - Çünkü ürün bazlı adet bilgisi detay tablosundadır.
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
