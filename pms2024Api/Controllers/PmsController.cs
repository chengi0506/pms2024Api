using Azure.Core;
using log4net;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.SqlServer.Server;
using Microsoft.VisualBasic;
using pms2024Api.Data;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;
using System.Reflection;
using System.Runtime;

namespace pms2024Api.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]  // 版本 1.0
    [ApiVersion("2.0", Deprecated = true)]  // 版本 2.0 已棄用

    public class PmsController : ControllerBase
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ILogger<PmsController> _logger;
        private readonly PmsContext _context;

        public PmsController(ILogger<PmsController> logger, PmsContext context)
        {
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// 登入
        /// </summary>
        /// <returns></returns>
        [HttpPost("Login")]
        [EnableCors("AllowAllOrigins")]
        [SwaggerOperation(Summary = "登入", Description = "這個方法用於登入。")]
        [SwaggerResponse((int)HttpStatusCode.OK, "操作成功", typeof(IEnumerable<Cuser>))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, "無效的請求")]
        public IActionResult Login([FromBody] Login request)
        {
            if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("無效的帳號或密碼");
            }

            // 驗證使用者代號和密碼
            var user = _context.Cuser
                .FirstOrDefault(u => u.使用者代號 == request.UserId && u.使用者密碼 == request.Password);

            if (user == null)
            {
                return Unauthorized("帳號或密碼錯誤");
            }

            // 返回成功響應
            return Ok(new
            {
                message = "登入成功",
                userId = user.使用者代號,
                userName = user.使用者名稱,
                groupId = user.群組代號
            });
        }

        /// <summary>
        /// 根據代號取得對應區域列表
        /// </summary>
        /// <param name="code">代號</param>
        /// <returns>對應區域列表</returns>
        [HttpGet("GetAreasByCode/{code}")]
        [SwaggerOperation(Summary = "根據代號取得對應區域列表", Description = "這個方法用於根據代號取得對應區域列表。")]
        [SwaggerResponse((int)HttpStatusCode.OK, "操作成功", typeof(IEnumerable<Coded>))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, "無效的請求")]
        public ActionResult<IEnumerable<Coded>> GetAreasByCode(string code)
        {
            try
            {
                var result = _context.Coded
                    .Where(c => c.類別 == "02" && c.代號.StartsWith(code))
                    .ToList();

                if (result == null || result.Count == 0)
                {
                    return NotFound("沒有找到對應的區域");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                Log.Error($"取得對應區域列表異常:{ex.Message}");
                return BadRequest($"取得對應區域列表異常:{ex.Message}");
            }
        }

        /// <summary>
        /// 取得下一個代號
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        [HttpGet("GetNextCodeId")]
        [SwaggerOperation(Summary = "取得下一個代號", Description = "這個方法回傳類別代號最大值加 1。")]
        [SwaggerResponse((int)HttpStatusCode.OK, "操作成功", typeof(string))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, "無效的請求")]
        public ActionResult<string> GetNextCodeId(string category)
        {
            try
            {
                // 取得特定類別下的所有代號
                var codes = _context.Coded
                    .Where(c => c.類別 == category)
                    .Select(c => c.代號)
                    .ToList();

                // 如果沒有找到任何代號，則回傳錯誤或初始代號
                if (codes == null || codes.Count == 0)
                {
                    return Ok("001"); // 可根據需求設定初始值
                }

                // 根據代號進行排序，並找出最大代號
                string maxCode = codes.OrderByDescending(c => c).FirstOrDefault().Trim();

                // 提取代號中的數字部分
                string prefix = new string(maxCode.TakeWhile(c => !char.IsDigit(c)).ToArray());
                string numberPart = new string(maxCode.SkipWhile(c => !char.IsDigit(c)).ToArray());

                // 如果沒有數字部分，則將代號設為 1
                int nextNumber = string.IsNullOrEmpty(numberPart) ? 1 : int.Parse(numberPart) + 1;

                // 設定數字部分的位數，補足二位數
                string formattedNumberPart = nextNumber.ToString("D2");

                // 組合新的代號
                string nextCodeId = $"{prefix}{formattedNumberPart}";

                return Ok(nextCodeId);
            }
            catch (Exception ex)
            {
                Log.Error($"取得下一個代號異常: {ex.Message}");
                return BadRequest($"取得下一個代號異常: {ex.Message}");
            }
        }

        /// <summary>
        /// 取得財產編號流水號
        /// </summary>
        /// <param name="科目">科目</param>
        /// <param name="子目">子目</param>
        /// <param name="類別">類別</param>
        /// <returns></returns>
        [HttpGet("GetProNo")]
        [SwaggerOperation(Summary = "取得財產編號流水號", Description = "這個方法回傳取得財產編號流水號最大值加 1。")]
        [SwaggerResponse((int)HttpStatusCode.OK, "操作成功", typeof(string))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, "無效的請求")]
        public ActionResult<string> GetProNo(string 科目, string 子目, string 類別)
        {
            try
            {
                // 取得財產編號流水號
                var reault = _context.Pros
                    .Where(c => c.科目 == 科目 && c.子目== 子目.Substring(1,2) && c.類別 == 類別)
                    .Select(c => c.總項)
                    .ToList();

                // 如果沒有找到任何流水號，則回傳錯誤或初始流水號
                if (reault == null || reault.Count == 0)
                {
                    return Ok("001"); // 可根據需求設定初始值
                }

                // 根據代號進行排序，並找出最大代號
                string maxCode = reault.OrderByDescending(c => c).FirstOrDefault().Trim();

                // 提取代號中的數字部分
                string prefix = new string(maxCode.TakeWhile(c => !char.IsDigit(c)).ToArray());
                string numberPart = new string(maxCode.SkipWhile(c => !char.IsDigit(c)).ToArray());

                // 如果沒有數字部分，則將代號設為 1
                int nextNumber = string.IsNullOrEmpty(numberPart) ? 1 : int.Parse(numberPart) + 1;

                // 設定數字部分的位數，補足二位數
                string formattedNumberPart = nextNumber.ToString("D3");

                // 組合新的代號
                string nextCodeId = $"{prefix}{formattedNumberPart}";

                return Ok(nextCodeId);
            }
            catch (Exception ex)
            {
                Log.Error($"取得財產編號流水號異常: {ex.Message}");
                return BadRequest($"取得財產編號流水號異常: {ex.Message}");
            }
        }

        /// <summary>
        /// 取得系統代碼
        /// </summary>
        /// <param name="category">類別</param>
        /// <returns></returns>
        [HttpGet("GetCode")]
        [SwaggerOperation(Summary = "取得系統代碼", Description = "這個方法用於取得系統代碼。")]
        [SwaggerResponse((int)HttpStatusCode.OK, "操作成功", typeof(IEnumerable<Coded>))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, "無效的請求")]
        public ActionResult<IEnumerable<Coded>> GetCode(string category)
        {
            try
            {
                var codeds = _context.Coded
                   .Where(c => c.類別 == category)
                   .ToList();

                if (codeds == null || codeds.Count == 0)
                {
                    return NotFound("沒有找到對應的代碼");
                }

                var result = codeds.Select(c => new
                {
                    類別 = c.類別?.Trim(),
                    代號 = c.代號?.Trim(),
                    名稱 = (c.名稱 ?? string.Empty).Trim(),
                    內容 = (c.內容 ?? string.Empty).Trim()
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                Log.Error($"取得系統代碼異常:{ex.Message}");
                return BadRequest($"取得系統代碼異常:{ex.Message}");
            }
        }

        /// <summary>
        /// 新增代碼
        /// </summary>
        /// <param name="coded">代碼</param>
        /// <returns></returns>
        [HttpPost("AddCode")]
        [SwaggerOperation(Summary = "新增代碼", Description = "這個方法用於新增代碼。")]
        [SwaggerResponse((int)HttpStatusCode.Created, "新增成功", typeof(Coded))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, "無效的請求")]
        public IActionResult AddCode([FromBody] Coded coded)
        {
            if (coded == null)
            {
                return BadRequest("代碼資料不得為空");
            }

            _context.Coded.Add(coded);

            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateException e)
            {
                Log.Error(e);
                return StatusCode((int)HttpStatusCode.InternalServerError, "Error saving data.");
            }

            return Ok(coded);
        }

        /// <summary>
        /// 修改代碼
        /// </summary>
        /// <param name="coded">代碼</param>
        /// <returns></returns>
        [HttpPut("UpdateCode")]
        [SwaggerOperation(Summary = "修改代碼", Description = "這個方法用於修改代碼。")]
        [SwaggerResponse((int)HttpStatusCode.OK, "操作成功", typeof(Coded))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, "無效的請求")]
        public async Task<IActionResult> UpdateCode([FromBody] Coded coded)
        {
            if (coded == null || string.IsNullOrEmpty(coded.代號) || string.IsNullOrEmpty(coded.類別) || string.IsNullOrEmpty(coded.名稱))
            {
                return BadRequest("代碼資料不得為空");
            }


            var result = await _context.Coded
                .Where(c => c.類別 == coded.類別 &&  c.代號 == coded.代號)
                .FirstOrDefaultAsync();


            if (result == null)
            {
                return NotFound("找不到代碼");
            }

            //result.類別 = request.類別;
            //result.代號 = request.代號;
            result.名稱 = coded.名稱;
            result.內容 = coded.內容;

            try
            {
                await _context.SaveChangesAsync();

                return Ok(result);
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, $"修改代碼異常:{ex.Message}");
            }
        }

        /// <summary>
        /// 取得財產筆數
        /// </summary>
        /// <param name="proId">財產類別</param>
        /// <returns>財產筆數</returns>
        [HttpGet("GetProCount/{proId}")]
        [SwaggerOperation(Summary = "取得財產筆數", Description = "這個方法用於取得財產類別的筆數。")]
        [SwaggerResponse((int)HttpStatusCode.OK, "操作成功", typeof(int))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, "無效的請求")]
        public ActionResult<int> GetProCount(string proId)
        {
            try
            {
                var result = _context.Pros
                    .Where(c => c.科目 == proId && c.財產狀態 != "2")
                    .Count();

                return Ok(result);
            }
            catch (Exception ex)
            {
                Log.Error($"取得財產筆數異常:{ex.Message}");
                return BadRequest($"取得財產筆數異常:{ex.Message}");
            }
        }

        /// <summary>
        /// 取得使用者
        /// </summary>
        /// <returns></returns>
        [HttpPost("GetCuser")]
        [EnableCors("AllowAllOrigins")]
        [SwaggerOperation(Summary = "取得使用者", Description = "這個方法用於取得使用者。")]
        [SwaggerResponse((int)HttpStatusCode.OK, "操作成功", typeof(IEnumerable<Cuser>))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, "無效的請求")]
        public ActionResult<IEnumerable<Cuser>> GetCuser()
        {
            try
            {
                var Cuser = _context.Cuser.ToList();


                if (Cuser == null || Cuser.Count == 0)
                {
                    return NotFound("沒有找到對應的代碼");
                }

                // 遍歷 Cuser 列表，確保每個屬性都處理好 null 值
                var result = Cuser.Select(c => new
                {
                    使用者代號 = c.使用者代號?.Trim(),
                    使用者名稱 = c.使用者名稱?.Trim(),
                    使用者密碼 = c.使用者密碼?.Trim(),
                    群組代號 = c.群組代號?.Trim(),
                    程式名稱 = c.程式名稱?.Trim(),
                    程式時間 = c.程式時間,
                    ip = c.ip?.Trim()
                }).OrderByDescending(r=> r.程式時間);

                return Ok(result);
            }
            catch (Exception ex)
            {
                Log.Error($"取得使用者異常:{ex.Message}");
                return BadRequest($"取得使用者異常:{ex.Message}");
            }
        }

        /// <summary>
        /// 新增使用者
        /// </summary>
        /// <param name="cuser">使用者</param>
        /// <returns></returns>
        [HttpPost("AddCuser")]
        [SwaggerOperation(Summary = "新增使用者", Description = "這個方法用於新增使用者。")]
        [SwaggerResponse((int)HttpStatusCode.Created, "新增成功", typeof(Cuser))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, "無效的請求")]
        public IActionResult AddCuser([FromBody] Cuser cuser)
        {
            if (cuser == null)
            {
                return BadRequest("使用者資料不得為空");
            }

            _context.Cuser.Add(cuser);

            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateException e)
            {
                Log.Error(e);
                return StatusCode((int)HttpStatusCode.InternalServerError, "Error saving data.");
            }

            return CreatedAtAction(nameof(GetCuserById), new { id = cuser.使用者代號 }, cuser);
        }

        /// <summary>
        /// 修改使用者資訊
        /// </summary>
        /// <param name="cuser">使用者</param>
        /// <returns></returns>
        [HttpPut("UpdateCuserInfo")]
        [SwaggerOperation(Summary = "修改使用者資訊", Description = "這個方法用於修改使用者資訊。")]
        [SwaggerResponse((int)HttpStatusCode.OK, "操作成功", typeof(Cuser))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, "無效的請求")]
        public async Task<IActionResult> UpdateCuserInfo([FromBody] Cuser cuser)
        {
            if (cuser == null || string.IsNullOrEmpty(cuser.使用者代號) || string.IsNullOrEmpty(cuser.群組代號))
            {
                return BadRequest("使用者資料不得為空");
            }

            var user = await _context.Cuser.FindAsync(cuser.使用者代號);
            if (user == null)
            {
                return NotFound("找不到使用者資料");
            }

            user.群組代號 = cuser.群組代號;
            user.使用者密碼 = cuser.使用者密碼;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(user);
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, $"修改使用者資訊異常:{ex.Message}");
            }
        }

        /// <summary>
        /// 查詢使用者
        /// </summary>
        /// <param name="id">科目</param>
        /// <returns></returns>
        [HttpGet("GetCuserById/{id}")]
        [SwaggerOperation(Summary = "查詢財產主檔", Description = "這個方法用於查詢財產主檔。")]
        [SwaggerResponse((int)HttpStatusCode.OK, "操作成功", typeof(IEnumerable<Cuser>))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, "無效的請求")]
        public async Task<ActionResult<Cuser>> GetCuserById(string id)
        {
            var result = await _context.Cuser.FindAsync(id);
            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        /// <summary>
        /// 刪除使用者
        /// </summary>
        /// <param name="使用者代號">使用者代號</param>
        /// <returns></returns>
        [HttpDelete("DeleteCuser/{使用者代號}")]
        [SwaggerOperation(Summary = "刪除財產主檔", Description = "這個方法用於刪除財產主檔。")]
        [SwaggerResponse((int)HttpStatusCode.OK, "操作成功")]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, "無效的請求")]
        public async Task<IActionResult> DeleteCuser(string 使用者代號)
        {
            var result = await _context.Cuser.FindAsync(使用者代號);
            if (result == null)
            {
                return NotFound("使用者不存在");
            }

            _context.Cuser.Remove(result);

            try
            {
                await _context.SaveChangesAsync();
                return Ok("使用者已刪除");
            }
            catch (DbUpdateException e)
            {
                Log.Error(e);
                return StatusCode((int)HttpStatusCode.InternalServerError, "刪除使用者異常");
            }
        }

        /// <summary>
        /// 修改密碼
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("UpdatePassword")]
        [SwaggerOperation(Summary = "修改密碼", Description = "這個方法用於修改密碼。")]
        [SwaggerResponse((int)HttpStatusCode.OK, "操作成功", typeof(Login))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, "無效的請求")]
        public async Task<IActionResult> UpdatePassword([FromBody] PasswordChangeRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.OldPassword) || string.IsNullOrEmpty(request.NewPassword))
            {
                return BadRequest("請求內容不得為空");
            }

            var user = await _context.Cuser.FindAsync(request.UserId);
            if (user == null)
            {
                return NotFound("找不到使用者資料");
            }

            // 檢查舊密碼是否正確
            if (user.使用者密碼 != request.OldPassword)
            {
                return BadRequest("舊密碼不正確");
            }

            user.使用者密碼 = request.NewPassword;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(user);
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, $"修改密碼異常:{ex.Message}");
            }
        }


        /// <summary>
        /// 取得系統基本設定
        /// </summary>
        /// <returns></returns>
        [HttpPost("GetSysSetting")]
        [EnableCors("AllowAllOrigins")]
        [SwaggerOperation(Summary = "取得系統基本設定", Description = "這個方法用於取得系統基本設定。")]
        [SwaggerResponse((int)HttpStatusCode.OK, "操作成功", typeof(IEnumerable<Basedat>))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, "無效的請求")]
        public ActionResult<IEnumerable<Basedat>> GetBasedat()
        {
            try
            {
                var result = _context.Basedat.ToList();
                return Ok(result);
            }
            catch (Exception ex)
            {
                Log.Error($"取得系統基本設定異常:{ex.Message}");
                return BadRequest($"取得系統基本設定異常:{ex.Message}");
            }
        }

        /// <summary>
        /// 取得財產匯總
        /// </summary>
        /// <returns></returns>
        [HttpPost("GetSubjectSummary")]
        [EnableCors("AllowAllOrigins")]
        [SwaggerOperation(Summary = "取得財產匯總", Description = "此API用於取得各科目的財產匯總資訊。")]
        [SwaggerResponse((int)HttpStatusCode.OK, "操作成功", typeof(IEnumerable<SubjectSummaryDto>))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, "無效的請求")]
        public ActionResult<IEnumerable<SubjectSummaryDto>> GetSubjectSummary()
        {
            try
            {
                // 取得科目和取得財產匯總
                var result = _context.Pros
                    .Where(p => p.財產狀態 != "2")
                    .GroupBy(p => p.科目)
                    .Select(g => new
                    {
                        科目代號 = g.Key,
                        取得價值 = g.Sum(p => p.取得價值 ?? 0)
                    })
                    .OrderBy(p => p.科目代號)
                    .ToList();

                // 執行查詢結果後，將科目代號轉為科目名稱
                var subjectSummaryList = result.Select(g => new SubjectSummaryDto
                {
                    科目代號 = g.科目代號,
                    科目名稱 = g.科目代號 switch
                    {
                        "1" => "土地",
                        "2" => "房屋及建築",
                        "3" => "機器及設備",
                        "4" => "電腦設備",
                        "5" => "農林設備",
                        "6" => "畜產設備",
                        "7" => "交通運輸設備",
                        "8" => "雜項設備",
                        "9" => "未完工程",
                        _ => "未知科目"
                    },
                    取得價值 = g.取得價值
                }).ToList();

                return Ok(subjectSummaryList);
            }
            catch (Exception ex)
            {
                Log.Error($"取得財產匯總異常: {ex.Message}");
                return BadRequest($"取得財產匯總異常: {ex.Message}");
            }
        }


        /// <summary>
        /// 取得群組代號
        /// </summary>
        /// <returns></returns>
        [HttpPost("GetGpassGroupID")]
        [EnableCors("AllowAllOrigins")]
        [SwaggerOperation(Summary = "取得群組代號", Description = "這個方法用於取得群組代號。")]
        [SwaggerResponse((int)HttpStatusCode.OK, "操作成功", typeof(IEnumerable<int>))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, "無效的請求")]
        public ActionResult<IEnumerable<Gpass>> GetGpassGroupID()
        {
            try
            {
                var result = _context.Gpass
                .GroupBy(g => g.群組代號)
                .Select(g => g.Key)
                .ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                Log.Error($"取得群組代號異常:{ex.Message}");
                return BadRequest($"取得群組代號異常:{ex.Message}");
            }
        }

        /// <summary>
        /// 取得群組權限
        /// </summary>
        /// <returns></returns>
        [HttpPost("GetGpass")]
        [EnableCors("AllowAllOrigins")]
        [SwaggerOperation(Summary = "取得群組權限", Description = "這個方法用於取得群組權限。")]
        [SwaggerResponse((int)HttpStatusCode.OK, "操作成功", typeof(IEnumerable<Gpass>))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, "無效的請求")]
        public ActionResult<IEnumerable<Gpass>> GetGpass()
        {
            try
            {
                var result = _context.Gpass.ToList();


                return Ok(result);
            }
            catch (Exception ex)
            {
                Log.Error($"取得群組權限異常:{ex.Message}");
                return BadRequest($"取得群組權限異常:{ex.Message}");
            }
        }

        /// <summary>
        /// 取得群組權限(依群組代號)
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("GetGpassById")]
        [EnableCors("AllowAllOrigins")]
        [SwaggerOperation(Summary = "取得群組權限(依群組代號)", Description = "這個方法用於取得群組權限(依群組代號)。")]
        [SwaggerResponse((int)HttpStatusCode.OK, "操作成功", typeof(IEnumerable<Gpass>))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, "無效的請求")]
        public ActionResult<IEnumerable<Gpass>> GetGpassById([FromBody] GetGpassRequest request)
        {
            try
            {
                var query = _context.Gpass.AsQueryable();

                if (!string.IsNullOrEmpty(request.GroupId))
                {
                    query = query.Where(g => g.群組代號 == request.GroupId);
                }

                var result = query.ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                Log.Error($"取得群組權限異常:{ex.Message}");
                return BadRequest($"取得群組權限異常:{ex.Message}");
            }
        }

        /// <summary>
        /// 建立群組
        /// </summary>
        /// <param name="groupid"></param>
        /// <returns></returns>
        [HttpPut("CreateGroup")]
        [SwaggerOperation(Summary = "建立群組", Description = "這個方法用於建立群組。")]
        [SwaggerResponse((int)HttpStatusCode.Created, "操作成功", typeof(Gpass))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, "無效的請求")]
        public async Task<IActionResult> CreateGroup([FromQuery] string groupid)
        {
            if (string.IsNullOrEmpty(groupid))
            {
                return BadRequest("群組代號不得為空");
            }

            try
            {
                // 檢查群組代號是否已存在
                var existingGroup = await _context.Gpass
                    .AsNoTracking()
                    .AnyAsync(g => g.群組代號 == groupid);

                if (existingGroup)
                {
                    return BadRequest("群組代號已存在");
                }

                // 使用 AsNoTracking 來防止 EF Core 跟蹤查詢到的實體
                var referenceGroup = await _context.Gpass
                    .AsNoTracking()
                    .Where(g => g.群組代號 == "01")
                    .ToListAsync();

                if (referenceGroup == null || !referenceGroup.Any())
                {
                    return NotFound("參考的群組不存在");
                }

                var newGroupEntries = referenceGroup.Select(g => new Gpass
                {
                    群組代號 = groupid,
                    程式代號 = g.程式代號,
                    權限等級 = g.權限等級
                }).ToList();

                _context.Gpass.AddRange(newGroupEntries);

                await _context.SaveChangesAsync();
                return Ok(groupid);
            }
            catch (DbUpdateException ex)
            {
                Log.Error($"資料庫更新失敗:{ex.Message}");
                return StatusCode((int)HttpStatusCode.InternalServerError, "資料庫更新失敗。");
            }
            catch (Exception ex)
            {
                Log.Error($"未預期的錯誤:{ex.Message}");
                return StatusCode((int)HttpStatusCode.InternalServerError, "伺服器內部錯誤。");
            }
        }


        /// <summary>
        /// 查詢群組
        /// </summary>
        /// <param name="id">科目</param>
        /// <returns></returns>
        [HttpGet("GetGpassById/{id}")]
        [SwaggerOperation(Summary = "查詢群組", Description = "這個方法用於查詢群組。")]
        [SwaggerResponse((int)HttpStatusCode.OK, "操作成功", typeof(IEnumerable<Gpass>))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, "無效的請求")]
        public async Task<ActionResult<Gpass>> GetGpassById(string id)
        {
            var pro = await _context.Gpass.FindAsync(id);
            if (pro == null)
            {
                return NotFound();
            }

            return Ok(pro);
        }


        /// <summary>
        /// 更新更新權限等級
        /// </summary>
        /// <param name="_Gpass"></param>
        /// <returns></returns>
        [HttpPut("UpdatePermissionLevel")]
        [SwaggerOperation(Summary = "更新權限等級", Description = "這個方法用於更新權限等級。")]
        [SwaggerResponse((int)HttpStatusCode.OK, "操作成功", typeof(Gpass))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, "無效的請求")]
        public async Task<IActionResult> UpdatePermissionLevel([FromBody] Gpass _Gpass)
        {
            if (_Gpass == null)
            {
                return BadRequest("權限等級不得為空");
            }

            var result = await _context.Gpass
                .FirstOrDefaultAsync(g => g.程式代號 == _Gpass.程式代號 && g.群組代號 == _Gpass.群組代號);

            if (result == null)
            {
                return NotFound("Settings not found");
            }

            result.權限等級 = _Gpass.權限等級;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(result);
            }
            catch (DbUpdateException e)
            {
                Log.Error(e);
                return StatusCode((int)HttpStatusCode.InternalServerError, "更新權限等級異常");
            }

        }

    /// <summary>
    /// 更新系統基本設定
    /// </summary>
    /// <param name="_basedat"></param>
    /// <returns></returns>
    [HttpPut("UpdateSysSetting")]
        [SwaggerOperation(Summary = "更新系統基本設定", Description = "這個方法用於更新系統基本設定。")]
        [SwaggerResponse((int)HttpStatusCode.OK, "操作成功", typeof(Basedat))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, "無效的請求")]
        public async Task<ActionResult<Basedat>> UpdateSysSetting([FromBody] Basedat _basedat)
        {
            if (_basedat == null)
            {
                return BadRequest("系統基本設定不得為空");
            }

            var result = _context.Basedat.FirstOrDefault();

            if (result == null)
            {
                return NotFound("Settings not found");
            }

            result.農會名稱 = _basedat.農會名稱;
            result.農會電話 = _basedat.農會電話;
            result.郵遞區號 = _basedat.郵遞區號;
            result.縣市 = _basedat.縣市;
            result.鄉鎮 = _basedat.鄉鎮;
            result.農會地址 = _basedat.農會地址;
            result.註冊碼 = _basedat.註冊碼;
            result.農會代號 = _basedat.農會代號;
            result.折舊週期 = _basedat.折舊週期;
            result.表尾一 = _basedat.表尾一;
            result.表尾二 = _basedat.表尾二;
            result.表尾三 = _basedat.表尾三;
            result.表尾四 = _basedat.表尾四;
            result.表尾五 = _basedat.表尾五;
            result.控管時間 = _basedat.控管時間;
            result.報表格式 = _basedat.報表格式;
            result.折舊法 = _basedat.折舊法;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(result);
            }
            catch (DbUpdateException e)
            {
                Log.Error(e);
                return StatusCode((int)HttpStatusCode.InternalServerError, "修改系統基本設定異常");
            }
        }

        /// <summary>
        /// 取得財產主檔清單
        /// </summary>
        /// <returns></returns>
        [HttpPost("GetProLists")]
        [EnableCors("AllowAllOrigins")]
        [SwaggerOperation(Summary = "取得財產主檔清單", Description = "這個方法用於取得財產主檔清單。")]
        [SwaggerResponse((int)HttpStatusCode.OK, "操作成功", typeof(IEnumerable<Pro>))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, "無效的請求")]
        public ActionResult<IEnumerable<Pro>> GetProLists()
        {
            try
            {
                var result = _context.Pros.ToList();
                return Ok(result);
            }
            catch (Exception ex)
            {
                Log.Error($"取得財產主檔清單異常:{ex.Message}");
                return BadRequest($"取得財產主檔清單異常:{ex.Message}");
            }
        }

        /// <summary>
        /// 新增財產主檔
        /// </summary>
        /// <param name="pro"></param>
        /// <returns></returns>
        [HttpPost("AddPro")]
        [SwaggerOperation(Summary = "新增財產主檔", Description = "這個方法用於新增財產主檔。")]
        [SwaggerResponse((int)HttpStatusCode.Created, "新增成功", typeof(Pro))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, "無效的請求")]
        public IActionResult AddPro([FromBody] Pro pro)
        {
            if (pro == null)
            {
                return BadRequest("財產主檔資料不得為空");
            }

            _context.Pros.Add(pro);

            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateException e)
            {
                Log.Error(e);
                return StatusCode((int)HttpStatusCode.InternalServerError, "Error saving data.");
            }

            return Ok(new { 科目 = pro.科目, 子目 = pro.子目, 類別 = pro.類別, 總項 = pro.總項 });
        }

        /// <summary>
        /// 查詢財產主檔
        /// </summary>
        /// <param name="科目">科目</param>
        /// <param name="子目">子目</param>
        /// <param name="類別">類別</param>
        /// <param name="總項">總項</param>
        /// <returns></returns>
        [HttpGet("GetProById/{科目}/{子目}/{類別}/{總項}")]
        [SwaggerOperation(Summary = "查詢財產主檔", Description = "這個方法用於查詢財產主檔。")]
        [SwaggerResponse((int)HttpStatusCode.OK, "操作成功", typeof(IEnumerable<Pro>))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, "無效的請求")]
        public async Task<ActionResult<Pro>> GetProById(string 科目, string 子目, string 類別, string 總項)
        {
            var pro = await _context.Pros.FindAsync(科目, 子目, 類別, 總項);

            if (pro == null)
            {
                return NotFound();
            }

            return Ok(pro);
        }


        /// <summary>
        /// 修改財產主檔
        /// </summary>
        /// <param name="_pro">財產主檔</param>
        /// <returns></returns>
        [HttpPut("EditProList")]
        [SwaggerOperation(Summary = "修改財產主檔", Description = "這個方法用於修改財產主檔。")]
        [SwaggerResponse((int)HttpStatusCode.OK, "操作成功", typeof(Pro))]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, "無效的請求")]
        public async Task<ActionResult<Pro>> EditProList([FromBody] Pro _pro)
        {
            if (_pro == null)
            {
                return BadRequest("財產主檔資料不得為空");
            }

            var Pro = await _context.Pros.FindAsync(_pro.科目, _pro.子目, _pro.類別, _pro.總項);
            if (Pro == null)
            {
                return NotFound("財產主檔不存在");
            }

            Pro.財產名稱 = _pro.財產名稱;
            Pro.使用單位編號 = _pro.使用單位編號;
            Pro.規格程式 = _pro.規格程式;
            Pro.使用單位 = _pro.使用單位;
            Pro.來源 = _pro.來源;
            Pro.機器號碼 = _pro.機器號碼;
            Pro.置放地點 = _pro.置放地點;
            Pro.廠牌年式 = _pro.廠牌年式;
            Pro.數量 = _pro.數量;
            Pro.計算單位 = _pro.計算單位;
            Pro.取得日期 = _pro.取得日期;
            Pro.開始折舊日期 = _pro.開始折舊日期;
            Pro.耐用年限 = _pro.耐用年限;
            Pro.開支部門 = _pro.開支部門;
            Pro.證號 = _pro.證號;
            Pro.資料袋編號 = _pro.資料袋編號;
            Pro.安裝情形 = _pro.安裝情形;
            Pro.使用情形一 = _pro.使用情形一;
            Pro.使用情形二 = _pro.使用情形二;
            Pro.取得價值 = _pro.取得價值;
            Pro.改良修理 = _pro.改良修理;
            Pro.預留殘值 = _pro.預留殘值;
            Pro.補助款 = _pro.補助款;
            Pro.備註 = _pro.備註;
            Pro.保管者 = _pro.保管者;
            Pro.財產狀態 = _pro.財產狀態;
            Pro.報廢日期 = _pro.報廢日期;
            Pro.報廢申請人 = _pro.報廢申請人;
            Pro.廢轉數量 = _pro.廢轉數量;
            Pro.未折減餘額 = _pro.未折減餘額;
            Pro.折舊累計 = _pro.折舊累計;
            Pro.明細數量 = _pro.明細數量;
            Pro.登錄者 = _pro.登錄者;
            Pro.更新時間 = _pro.更新時間;
            Pro.總量 = _pro.總量;
            Pro.已完成註記 = _pro.已完成註記;
            Pro.使用者 = _pro.使用者;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(Pro);
            }
            catch (DbUpdateException e)
            {
                Log.Error(e);
                return StatusCode((int)HttpStatusCode.InternalServerError, "修改財產主檔異常");
            }
        }

        /// <summary>
        /// 刪除財產主檔
        /// </summary>
        /// <param name="科目">科目</param>
        /// <param name="子目">子目</param>
        /// <param name="類別">類別</param>
        /// <param name="總項">總項</param>
        /// <returns></returns>
        [HttpDelete("DeleteProList/{科目}/{子目}/{類別}/{總項}")]
        [SwaggerOperation(Summary = "刪除財產主檔", Description = "這個方法用於刪除財產主檔。")]
        [SwaggerResponse((int)HttpStatusCode.OK, "操作成功")]
        [SwaggerResponse((int)HttpStatusCode.BadRequest, "無效的請求")]
        public async Task<IActionResult> DeleteProList(string 科目, string 子目, string 類別, string 總項)
        {
            var existingPro = await _context.Pros.FindAsync(科目, 子目, 類別, 總項);
            if (existingPro == null)
            {
                return NotFound("財產主檔不存在");
            }

            _context.Pros.Remove(existingPro);

            try
            {
                await _context.SaveChangesAsync();
                return Ok("財產主檔已刪除");
            }
            catch (DbUpdateException e)
            {
                Log.Error(e);
                return StatusCode((int)HttpStatusCode.InternalServerError, "刪除財產主檔異常");
            }
        }
    }
}
