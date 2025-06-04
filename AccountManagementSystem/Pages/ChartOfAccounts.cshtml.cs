using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using System.Data.SqlClient;
using OfficeOpenXml;
using Microsoft.AspNetCore.Authorization;

[Authorize(Roles = "Admin,Accountant,Viewer")]
public class ChartOfAccountsModel : PageModel
{
    private readonly IConfiguration _config;

    public ChartOfAccountsModel(IConfiguration config)
    {
        _config = config;
    }
    public List<AccountModel> Accounts { get; set; }
    [BindProperty]
    public AccountModel Account { get; set; }

    
    public bool IsEditor { get; set; }
    
    public void OnGet()
    {
        IsEditor = User.IsInRole("Admin") || User.IsInRole("Accountant");
        
        LoadAccounts();
    }

    public IActionResult OnPostCreate()
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Accountant"))
        {
            return Forbid();
        }
        using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
        {
            conn.Open();
            SqlCommand cmd = new SqlCommand("sp_ManageChartOfAccounts", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Action", "CREATE");
            cmd.Parameters.AddWithValue("@Name", Account.Name);
            cmd.Parameters.AddWithValue("@ParentId", (object)Account.ParentId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@AccountType", Account.AccountType);
            cmd.ExecuteNonQuery();
        }

        return RedirectToPage();
    }

    public IActionResult OnPostDelete(int id)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Accountant"))
            return Forbid();

        // Call stored procedure to delete the account
        // Example:
        using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
        {
            conn.Open();
            using (var cmd = new SqlCommand("sp_ManageChartOfAccounts", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Action", "Delete");
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.ExecuteNonQuery();
            }
        }

        return RedirectToPage();
    }
    [BindProperty]
    public AccountModel EditAccount { get; set; }

    public IActionResult OnPostEdit()
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Accountant"))
            return Forbid();

        // Update logic via stored procedure
        using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
        {
            conn.Open();
            using (var cmd = new SqlCommand("sp_ManageChartOfAccounts", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Action", "Update");
                cmd.Parameters.AddWithValue("@Id", EditAccount.Id);
                cmd.Parameters.AddWithValue("@Name", EditAccount.Name);
                cmd.Parameters.AddWithValue("@ParentId", EditAccount.ParentId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@AccountType", EditAccount.AccountType);
                cmd.ExecuteNonQuery();
            }
        }

        return RedirectToPage();
    }


    public async Task<IActionResult> OnPostExportAsync()
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Accountant"))
            return Forbid();

        DataTable dt = new DataTable();
        string connectionString = _config.GetConnectionString("DefaultConnection");

        using (SqlConnection conn = new SqlConnection(connectionString))
        using (SqlCommand cmd = new SqlCommand("sp_ManageChartOfAccounts", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Action", "GET_ALL");

            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(dt);
        }

        if (dt.Rows.Count == 0)
        {
            TempData["ExportMessage"] = "No data found to export.";
            return RedirectToPage();
        }

        // Remove using block to prevent auto-disposing
        var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("ChartOfAccounts");
        worksheet.Cells["A1"].LoadFromDataTable(dt, true);

        // Write to memory stream without disposing it
        var stream = new MemoryStream();
        package.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"ChartOfAccounts_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        return File(stream, contentType, fileName);
    }




    public void LoadAccounts()
    {
        Accounts = new List<AccountModel>();

        using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
        {
            conn.Open();
            SqlCommand cmd = new SqlCommand("SELECT * FROM ChartOfAccounts", conn);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                Accounts.Add(new AccountModel
                {
                    Id = (int)reader["Id"],
                    Name = reader["Name"].ToString(),
                    ParentId = reader["ParentId"] as int?,
                    AccountType = reader["AccountType"].ToString()
                });
            }
        }
    }
}

public class AccountModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int? ParentId { get; set; }
    public string AccountType { get; set; }
}
