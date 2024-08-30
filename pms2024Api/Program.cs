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

// �t�mlog4net
var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

// �K�[�A�Ȩ�e����
builder.Services.AddControllers();  // �K�[����A��
builder.Services.AddEndpointsApiExplorer();  // �K�[���I API �����A��
builder.Services.AddHttpClient();  // �K�[ HTTP �Ȥ�ݪA��
builder.Services.AddSwaggerGen(c =>
{
    // ���C�� API �����ͦ� Swagger ����
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "pms2024Api", Version = "v1" });
    c.SwaggerDoc("v2", new OpenApiInfo { Title = "pms2024Api", Version = "v2" });
    // �]�t XML ���Ѥ��
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
});

// �t�m API ��������
builder.Services.AddApiVersioning(config =>
{
    config.DefaultApiVersion = new ApiVersion(1, 0);  // �q�{ API ����
    config.AssumeDefaultVersionWhenUnspecified = true; // �������w�����ɨϥ��q�{����
    config.ReportApiVersions = true;  // ���i API ����
    config.ApiVersionReader = new QueryStringApiVersionReader("v");  // �q�d�ߦr�ŦꤤŪ�� API ����
});

// �[�� appsettings.json �t�m���
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// ���U DbContext �M�s���r�Ŧ�
builder.Services.AddDbContext<PmsContext>(options =>
    options.UseSqlServer(builder.Configuration.GetSection("Settings:SqlConn").Value));

// �K�[ CORS ����
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin()  // ���\����ӷ�
                   .AllowAnyMethod()  // ���\�����k
                   .AllowAnyHeader();  // ���\������Y
        });
});

var app = builder.Build();

// ���U Big5 �s�X���ѵ{��
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// �t�m HTTP �ШD�޹D
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();  // �ϥ� Swagger ������
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "pms2024Api v1");  // �t�m Swagger UI ���I
        c.SwaggerEndpoint("/swagger/v2/swagger.json", "pms2024Api v2");  // �t�m Swagger UI ���I
    });
}

app.UseCors("AllowAllOrigins");  // ���� CORS ����

app.UseHttpsRedirection();  // �j��ϥ� HTTPS

app.UseAuthorization();  // �ϥα��v������

app.MapControllers();  // �M�g�������

app.Run();  // �B�����ε{��