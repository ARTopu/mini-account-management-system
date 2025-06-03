using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using System.Data.SqlClient;

public class ChartOfAccountsModel : PageModel
{
    private readonly IConfiguration _config;

    public ChartOfAccountsModel(IConfiguration config)
    {
        _config = config;
    }

    [BindProperty]
    public AccountModel Account { get; set; }

    public List<AccountModel> Accounts { get; set; }

    public void OnGet()
    {
        LoadAccounts();
    }

    public IActionResult OnPostCreate()
    {
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
