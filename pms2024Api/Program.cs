using log4net.Config;
using log4net;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using pms2024Api.Data;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// 配置log4net
var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

// 添加服務到容器中
builder.Services.AddControllers();  // 添加控制器服務
builder.Services.AddEndpointsApiExplorer();  // 添加端點 API 探索服務
builder.Services.AddHttpClient();  // 添加 HTTP 客戶端服務
builder.Services.AddSwaggerGen(c =>
{
    // 為每個 API 版本生成 Swagger 文檔
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "pms2024Api", Version = "v1" });
    c.SwaggerDoc("v2", new OpenApiInfo { Title = "pms2024Api", Version = "v2" });
    // 包含 XML 註解文件
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
});

// 配置 API 版本控制
builder.Services.AddApiVersioning(config =>
{
    config.DefaultApiVersion = new ApiVersion(1, 0);  // 默認 API 版本
    config.AssumeDefaultVersionWhenUnspecified = true; // 當未指定版本時使用默認版本
    config.ReportApiVersions = true;  // 報告 API 版本
    config.ApiVersionReader = new QueryStringApiVersionReader("v");  // 從查詢字符串中讀取 API 版本
});

// 加載 appsettings.json 配置文件
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// 註冊 DbContext 和連接字符串
builder.Services.AddDbContext<PmsContext>(options =>
    options.UseSqlServer(builder.Configuration.GetSection("Settings:SqlConn").Value));

// 添加 CORS 策略
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin()  // 允許任何來源
                   .AllowAnyMethod()  // 允許任何方法
                   .AllowAnyHeader();  // 允許任何標頭
        });
});

var app = builder.Build();

// 註冊 Big5 編碼提供程序
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// 配置 HTTP 請求管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();  // 使用 Swagger 中間件
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "pms2024Api v1");  // 配置 Swagger UI 端點
        c.SwaggerEndpoint("/swagger/v2/swagger.json", "pms2024Api v2");  // 配置 Swagger UI 端點
    });
}

app.UseCors("AllowAllOrigins");  // 應用 CORS 策略

app.UseHttpsRedirection();  // 強制使用 HTTPS

app.UseAuthorization();  // 使用授權中間件

app.MapControllers();  // 映射控制器路由

app.Run();  // 運行應用程式
