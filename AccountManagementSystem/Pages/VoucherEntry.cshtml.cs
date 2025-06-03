using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using System.Data.SqlClient;

public class VoucherEntryModel : PageModel
{
    private readonly IConfiguration _config;
    public List<AccountDropdown> Accounts { get; set; }

    [BindProperty]
    public VoucherInput Voucher { get; set; }

    public VoucherEntryModel(IConfiguration config)
    {
        _config = config;
    }

    public IActionResult OnGet()
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Accountant"))
        {
            return Forbid();
        }

        LoadAccounts();
        return Page();
    }

    public void LoadAccounts()
    {
        Accounts = new List<AccountDropdown>();
        using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
        {
            conn.Open();
            SqlCommand cmd = new SqlCommand("SELECT Id, Name FROM ChartOfAccounts", conn);
            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Accounts.Add(new AccountDropdown
                {
                    Id = (int)reader["Id"],
                    Name = reader["Name"].ToString()
                });
            }
        }
    }

    public IActionResult OnPost()
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Accountant"))
        {
            return Forbid();
        }
        using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
        {
            conn.Open();

            var table = new DataTable();
            table.Columns.Add("AccountId", typeof(int));
            table.Columns.Add("DebitAmount", typeof(decimal));
            table.Columns.Add("CreditAmount", typeof(decimal));

            foreach (var entry in Voucher.Entries)
            {
                table.Rows.Add(entry.AccountId, entry.DebitAmount, entry.CreditAmount);
            }

            SqlCommand cmd = new SqlCommand("sp_SaveVoucher", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@VoucherType", Voucher.VoucherType);
            cmd.Parameters.AddWithValue("@ReferenceNo", Voucher.ReferenceNo);
            cmd.Parameters.AddWithValue("@VoucherDate", Voucher.VoucherDate);

            var param = cmd.Parameters.AddWithValue("@Entries", table);
            param.SqlDbType = SqlDbType.Structured;
            param.TypeName = "VoucherEntryType";

            cmd.ExecuteNonQuery();
        }

        return RedirectToPage();
    }
}

public class AccountDropdown
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class VoucherInput
{
    public string VoucherType { get; set; }
    public string ReferenceNo { get; set; }
    public DateTime VoucherDate { get; set; }
    public List<VoucherEntry> Entries { get; set; }
}

public class VoucherEntry
{
    public int AccountId { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
}
