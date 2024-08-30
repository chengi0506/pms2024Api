using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pms2024Api.Data
{
    public class PmsContext : DbContext
    {
        public PmsContext(DbContextOptions<PmsContext> options) : base(options) { }

        public DbSet<Pro> Pros { get; set; }

        public DbSet<Basedat> Basedat { get; set; }

        public DbSet<Coded> Coded { get; set; }

        public DbSet<Gpass> Gpass { get; set; }

        public DbSet<Cuser> Cuser { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Pro>()
                .ToTable("PRO")
                .HasKey(p => new { p.科目, p.子目, p.類別, p.總項 });

            modelBuilder.Entity<Basedat>()
                .ToTable("basedat")
                .HasKey(p => new { p.農會名稱 });

            modelBuilder.Entity<Coded>(entity =>
            {
                entity.ToTable("coded");
                entity.HasKey(c => new { c.類別, c.代號 });
                entity.Property(c => c.名稱)
                    .IsRequired(false);
                entity.Property(c => c.內容)
                    .IsRequired(false);
            });

            modelBuilder.Entity<Gpass>()
                .ToTable("gpass")
                .HasKey(p => new { p.群組代號, p.程式代號 });

            modelBuilder.Entity<Cuser>()
                .ToTable("cuser")
                .HasKey(p => new { p.使用者代號 });

            base.OnModelCreating(modelBuilder);
        }
    }

    public class Login
    {
        public string UserId { get; set; }
        public string Password { get; set; }
    }

    public class PasswordChangeRequest
    {
        public string UserId { get; set; }
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
    }

    public class GetGpassRequest
    {
        public string GroupId { get; set; }
    }

    [Table("gpass")]
    public class Gpass
    {
        public string 群組代號 { get; set; }
        public string? 程式代號 { get; set; }
        public string? 權限等級 { get; set; }
    }

    [Table("cuser")]
    public class Cuser
    {
        public string 使用者代號 { get; set; }
        public string 使用者名稱 { get; set; }
        public string 使用者密碼 { get; set; }
        public string 群組代號 { get; set; }
        public string? 程式名稱 { get; set; }
        public DateTime? 程式時間 { get; set; }
        public string? ip { get; set; }
    }

    [Table("coded")]
    public class Coded
    {
        public string 類別 { get; set; }
        public string 代號 { get; set; }
        public string 名稱 { get; set; }
        public string 內容 { get; set; }
    }

    [Table("basedat")]
    public class Basedat
    {
        [Key]
        public string 農會名稱 { get; set; }
        public string 郵遞區號 { get; set; }
        public string 縣市 { get; set; }
        public string 鄉鎮 { get; set; }
        public string 農會地址 { get; set; }
        public string 農會電話 { get; set; }
        public string 註冊碼 { get; set; }
        public string 農會代號 { get; set; }
        public int 折舊週期 { get; set; }
        public string 表尾一 { get; set; }
        public string 表尾二 { get; set; }
        public string 表尾三 { get; set; }
        public string 表尾四 { get; set; }
        public string 表尾五 { get; set; }
        public int? 控管時間 { get; set; }
        public char? 報表格式 { get; set; }
        public char? 折舊法 { get; set; }
        public byte[] permisstime { get; set; }
    }


    [Table("PRO")]
    public class Pro
    {
        [Key]
        public string 科目 { get; set; }
        [Key]
        public string 子目 { get; set; }
        [Key]
        public string 類別 { get; set; }
        [Key]
        public string 總項 { get; set; }
        public string 財產名稱 { get; set; }
        public string? 使用單位編號 { get; set; }
        public string? 規格程式 { get; set; }
        public string? 使用單位 { get; set; }
        public string? 來源 { get; set; }
        public string? 機器號碼 { get; set; }
        public string? 置放地點 { get; set; }
        public string? 廠牌年式 { get; set; }
        public double? 數量 { get; set; }
        public string? 計算單位 { get; set; }
        public string? 取得日期 { get; set; }
        public string? 開始折舊日期 { get; set; }
        public int? 耐用年限 { get; set; }
        public string? 開支部門 { get; set; }
        public string? 證號 { get; set; }
        public string? 資料袋編號 { get; set; }
        public string? 安裝情形 { get; set; }
        public string? 使用情形一 { get; set; }
        public string? 使用情形二 { get; set; }
        public double? 取得價值 { get; set; }
        public double? 改良修理 { get; set; }
        public double? 預留殘值 { get; set; }
        public double? 補助款 { get; set; }
        public string? 備註 { get; set; }
        public string? 保管者 { get; set; }
        public string? 財產狀態 { get; set; }
        public string? 報廢日期 { get; set; }
        public string? 報廢申請人 { get; set; }
        public int? 廢轉數量 { get; set; }
        public double? 未折減餘額 { get; set; }
        public double? 折舊累計 { get; set; }
        public int? 明細數量 { get; set; }
        public string? 登錄者 { get; set; }
        public DateTime? 更新時間 { get; set; }
        public double? 總量 { get; set; }
        public char? 已完成註記 { get; set; }
        public string? 使用者 { get; set; }
    }

}
