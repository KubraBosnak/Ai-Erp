using AiErp.API.Services;
using System.Data;
using Microsoft.Data.SqlClient;

try
{
    Console.WriteLine(">>> PROGRAM BAŞLATILIYOR...");

    var builder = WebApplication.CreateBuilder(args);

    // 1. Ayarları Oku
    Console.WriteLine(">>> Ayarlar okunuyor...");
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new Exception("HATA: ConnectionString 'DefaultConnection' appsettings.json içinde bulunamadı!");
    }

    // 2. Servisleri Ekle
    Console.WriteLine(">>> Servisler ekleniyor...");
    
    // SQL Bağlantısı
    builder.Services.AddTransient<IDbConnection>(sp => new SqlConnection(connectionString));

    // AI ve Dapper Servisleri
    builder.Services.AddTransient<OpenAiService>();
    builder.Services.AddTransient<SqlExecutorService>();
    
    // HTTP ve Controller Servisleri
    builder.Services.AddHttpClient();
    builder.Services.AddControllers();
    // Angular'ın (4200) konuşmasına izin veriyoruz
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAngular",
            policy =>
            {
                policy.WithOrigins("http://localhost:4200") // Angular adresi
                  .AllowAnyHeader()
                  .AllowAnyMethod();
            });
    });
    // Swagger/OpenAPI
    // builder.Services.AddEndpointsApiExplorer();
    // builder.Services.AddSwaggerGen();

    var app = builder.Build();
    Console.WriteLine(">>> Uygulama inşa edildi (Build Success).");
    app.UseCors("AllowAngular");
    // 3. Pipeline Ayarları
    //if (app.Environment.IsDevelopment())
    //{
      //  app.UseSwagger();
        //app.UseSwaggerUI();
    //}

    app.UseHttpsRedirection();
    app.MapControllers();

    Console.WriteLine(">>> SUNUCU AYAĞA KALKIYOR (RUN)...");
    app.Run();
}
catch (Exception ex)
{
    // HATA VARSA BURAYA DÜŞER VE SEBEBİNİ YAZAR
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("!!! KRİTİK BAŞLANGIÇ HATASI !!!");
    Console.WriteLine(ex.Message);
    Console.WriteLine(ex.StackTrace);
    Console.ResetColor();
}