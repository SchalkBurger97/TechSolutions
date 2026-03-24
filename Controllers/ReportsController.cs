using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2010.PowerPoint;
using iText.Kernel.Pdf;
using iText.StyledXmlParser.Jsoup.Nodes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using TechSolutions.Models;

namespace TechSolutions.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController()
        {
            _context = new ApplicationDbContext();
        }

        // ── Portal landing page ──
        public ActionResult Index()
        {
            var reports = _context.Reports
                .Where(r => r.IsActive)
                .OrderBy(r => r.SortOrder)
                .ToList();

            // Group by category for the portal view
            var grouped = reports
                .GroupBy(r => r.Category)
                .ToDictionary(g => g.Key, g => g.ToList());

            ViewBag.Grouped = grouped;
            ViewBag.TotalReports = reports.Count;
            return View(reports);
        }

        // ── Run a report and display results ──
        public ActionResult Run(int id, FormCollection parameters)
        {
            var returnUrl = Request.QueryString["returnUrl"];
            var returnLabel = Request.QueryString["returnLabel"];

            ViewBag.ReturnUrl = !string.IsNullOrEmpty(returnUrl) ? returnUrl : Url.Action("Index", "Reports");
            ViewBag.ReturnLabel = !string.IsNullOrEmpty(returnLabel) ? returnLabel : "Report Portal";

            var report = _context.Reports.Find(id);
            if (report == null)
            {
                TempData["ErrorMessage"] = "Report not found.";
                return RedirectToAction("Index");
            }

            var paramDefs = ParseParamDefs(report.ParameterDefinitions);
            var paramValues = BuildParamValues(paramDefs, parameters);

            DataTable results = null;
            string error = null;

            try
            {
                results = ExecuteReport(report.StoredProcedureName, paramValues);
            }
            catch (Exception ex)
            {
                error = "Error running report: " + ex.Message;
            }

            ViewBag.Report = report;
            ViewBag.ParamDefs = paramDefs;
            ViewBag.ParamValues = paramValues;
            ViewBag.Results = results;
            ViewBag.Error = error;
            ViewBag.RowCount = results?.Rows.Count ?? 0;
            ViewBag.RunAt = DateTime.Now.ToString("dd MMM yyyy HH:mm:ss");
            ViewBag.ReturnUrl = !string.IsNullOrEmpty(returnUrl) ? returnUrl : Url.Action("Index", "Reports");
            return View(report);
        }

        // ── Export: Excel ──
        public ActionResult ExportExcel(int id, FormCollection parameters)
        {
            var report = _context.Reports.Find(id);
            if (report == null) return HttpNotFound();

            var paramDefs = ParseParamDefs(report.ParameterDefinitions);
            var paramValues = BuildParamValues(paramDefs, parameters);
            var data = ExecuteReport(report.StoredProcedureName, paramValues);

            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add(Truncate(report.ReportName, 31));

                // Title row
                ws.Cell(1, 1).Value = report.ReportName;
                ws.Cell(1, 1).Style.Font.Bold = true;
                ws.Cell(1, 1).Style.Font.FontSize = 14;
                ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#0477bf");
                ws.Range(1, 1, 1, data.Columns.Count).Merge();

                // Generated line
                ws.Cell(2, 1).Value = "Generated: " + DateTime.Now.ToString("dd MMM yyyy HH:mm");
                ws.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;
                ws.Range(2, 1, 2, data.Columns.Count).Merge();

                // Header row
                int headerRow = 4;
                for (int c = 0; c < data.Columns.Count; c++)
                {
                    var cell = ws.Cell(headerRow, c + 1);
                    cell.Value = data.Columns[c].ColumnName;
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0477bf");
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                }

                // Data rows
                for (int r = 0; r < data.Rows.Count; r++)
                {
                    for (int c = 0; c < data.Columns.Count; c++)
                    {
                        var cell = ws.Cell(headerRow + 1 + r, c + 1);
                        var val = data.Rows[r][c];
                        cell.Value = val == DBNull.Value ? "" : val.ToString();

                        // Alternate row shading
                        if (r % 2 == 0)
                            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#f0f5f9");

                        // Highlight danger values
                        if (val?.ToString() == "EXPIRED" || val?.ToString() == "No")
                            cell.Style.Font.FontColor = XLColor.FromHtml("#c0392b");
                        else if (val?.ToString() == "Verified" || val?.ToString() == "Yes")
                            cell.Style.Font.FontColor = XLColor.FromHtml("#2e9e6b");
                    }
                }

                // Auto-fit columns
                ws.Columns().AdjustToContents();

                // Add border to table
                var tableRange = ws.Range(headerRow, 1, headerRow + data.Rows.Count, data.Columns.Count);
                tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                tableRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#d6e4ef");
                tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
                tableRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#d6e4ef");

                using (var ms = new MemoryStream())
                {
                    wb.SaveAs(ms);
                    string fileName = SafeFileName(report.ReportName) + "_" + DateTime.Now.ToString("yyyyMMdd") + ".xlsx";
                    return File(ms.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
            }
        }

        // ── Export: PDF ──
        public ActionResult ExportPdf(int id, FormCollection parameters)
        {
            var report = _context.Reports.Find(id);
            if (report == null) return HttpNotFound();

            var paramDefs = ParseParamDefs(report.ParameterDefinitions);
            var paramValues = BuildParamValues(paramDefs, parameters);
            var data = ExecuteReport(report.StoredProcedureName, paramValues);

            using (var ms = new MemoryStream())
            {
                bool landscape = data.Columns.Count > 6;

                var writer = new iText.Kernel.Pdf.PdfWriter(ms);
                var pdfDoc = new iText.Kernel.Pdf.PdfDocument(writer);
                var pageSize = landscape
                    ? iText.Kernel.Geom.PageSize.A4.Rotate()
                    : iText.Kernel.Geom.PageSize.A4;

                var doc = new iText.Layout.Document(pdfDoc, pageSize);
                doc.SetMargins(36, 28, 28, 28);

                // ── Fonts ──
                var boldFont = iText.Kernel.Font.PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);
                var normalFont = iText.Kernel.Font.PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);

                var primaryColor = new iText.Kernel.Colors.DeviceRgb(4, 119, 191);
                var mutedColor = new iText.Kernel.Colors.DeviceRgb(107, 124, 141);
                var dangerColor = new iText.Kernel.Colors.DeviceRgb(192, 57, 43);
                var successColor = new iText.Kernel.Colors.DeviceRgb(46, 158, 107);
                var altRowColor = new iText.Kernel.Colors.DeviceRgb(240, 245, 249);
                var borderColor = new iText.Kernel.Colors.DeviceRgb(214, 228, 239);
                var whiteColor = iText.Kernel.Colors.ColorConstants.WHITE;

                // ── Title ──
                doc.Add(new iText.Layout.Element.Paragraph(report.ReportName)
                    .SetFont(boldFont).SetFontSize(16).SetFontColor(primaryColor)
                    .SetMarginBottom(4));

                if (!string.IsNullOrEmpty(report.Description))
                {
                    doc.Add(new iText.Layout.Element.Paragraph(report.Description)
                        .SetFont(normalFont).SetFontSize(9).SetFontColor(mutedColor)
                        .SetMarginBottom(2));
                }

                doc.Add(new iText.Layout.Element.Paragraph(
                        "Generated: " + DateTime.Now.ToString("dd MMM yyyy HH:mm") +
                        "   |   " + data.Rows.Count + " records")
                    .SetFont(normalFont).SetFontSize(9).SetFontColor(mutedColor)
                    .SetMarginBottom(14));

                // ── Table ──
                var table = new iText.Layout.Element.Table(data.Columns.Count)
                    .UseAllAvailableWidth();

                // Header row
                for (int c = 0; c < data.Columns.Count; c++)
                {
                    var cell = new iText.Layout.Element.Cell()
                        .Add(new iText.Layout.Element.Paragraph(data.Columns[c].ColumnName)
                            .SetFont(boldFont).SetFontSize(9).SetFontColor(whiteColor))
                        .SetBackgroundColor(primaryColor)
                        .SetPadding(6)
                        .SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                    table.AddHeaderCell(cell);
                }

                // Data rows
                for (int r = 0; r < data.Rows.Count; r++)
                {
                    var rowBg = r % 2 == 0 ? altRowColor : whiteColor;
                    for (int c = 0; c < data.Columns.Count; c++)
                    {
                        string val = data.Rows[r][c] == DBNull.Value ? "" : data.Rows[r][c].ToString();
                        var fontColor = val == "EXPIRED" || val == "No" ? dangerColor :
                                           val == "Verified" || val == "Yes" ? successColor :
                                           iText.Kernel.Colors.ColorConstants.BLACK;

                        var cell = new iText.Layout.Element.Cell()
                            .Add(new iText.Layout.Element.Paragraph(val)
                            .SetFont(normalFont).SetFontSize(8.5f).SetFontColor(fontColor))
                            .SetBackgroundColor(rowBg)
                            .SetPadding(5)
                            .SetBorder(new iText.Layout.Borders.SolidBorder(borderColor, 0.5f));
                        table.AddCell(cell);
                    }
                }

                doc.Add(table);
                doc.Close();

                string fileName = SafeFileName(report.ReportName) + "_" +
                                  DateTime.Now.ToString("yyyyMMdd") + ".pdf";
                return File(ms.ToArray(), "application/pdf", fileName);
            }
        }

        // ── Export: Print HTML ──
        public ActionResult PrintHtml(int id, FormCollection parameters)
        {
            var report = _context.Reports.Find(id);
            if (report == null) return HttpNotFound();

            var paramDefs = ParseParamDefs(report.ParameterDefinitions);
            var paramValues = BuildParamValues(paramDefs, parameters);
            var data = ExecuteReport(report.StoredProcedureName, paramValues);

            ViewBag.Report = report;
            ViewBag.Data = data;
            ViewBag.ParamValues = paramValues;
            ViewBag.RunAt = DateTime.Now.ToString("dd MMM yyyy HH:mm:ss");

            return View();
        }

        // PRIVATE HELPERS

        private DataTable ExecuteReport(string spName, Dictionary<string, string> paramValues)
        {
            var connStr = System.Configuration.ConfigurationManager
                .ConnectionStrings["DefaultConnection"].ConnectionString;

            using (var conn = new SqlConnection(connStr))
            using (var cmd = new SqlCommand(spName, conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 60;

                foreach (var kv in paramValues)
                {
                    cmd.Parameters.AddWithValue("@" + kv.Key,
                        string.IsNullOrWhiteSpace(kv.Value) ? (object)DBNull.Value : kv.Value);
                }

                conn.Open();
                var dt = new DataTable();
                var reader = cmd.ExecuteReader();
                dt.Load(reader);
                return dt;
            }
        }

        private List<Dictionary<string, string>> ParseParamDefs(string json)
        {
            var result = new List<Dictionary<string, string>>();
            if (string.IsNullOrWhiteSpace(json) || json == "[]") return result;

            // Simple manual JSON parser — avoids Newtonsoft dependency issues
            json = json.Trim().TrimStart('[').TrimEnd(']');
            var objects = SplitJsonObjects(json);
            foreach (var obj in objects)
            {
                var dict = new Dictionary<string, string>();
                var clean = obj.Trim().TrimStart('{').TrimEnd('}');
                foreach (var pair in clean.Split(new[] { ",\"" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = pair.TrimStart('"').Split(new[] { "\":\"", "\":" }, StringSplitOptions.None);
                    if (kv.Length >= 2)
                        dict[kv[0].Trim('"')] = kv[1].Trim('"').TrimEnd('"');
                }
                if (dict.Count > 0) result.Add(dict);
            }
            return result;
        }

        private List<string> SplitJsonObjects(string json)
        {
            var result = new List<string>();
            int depth = 0, start = 0;
            for (int i = 0; i < json.Length; i++)
            {
                if (json[i] == '{') { if (depth == 0) start = i; depth++; }
                else if (json[i] == '}') { depth--; if (depth == 0) result.Add(json.Substring(start, i - start + 1)); }
            }
            return result;
        }

        private Dictionary<string, string> BuildParamValues(
            List<Dictionary<string, string>> defs,
            FormCollection form)
        {
            var result = new Dictionary<string, string>();
            foreach (var def in defs)
            {
                if (!def.ContainsKey("Name")) continue;
                string name = def["Name"];
                string value = form != null && form[name] != null
                    ? form[name]
                    : (def.ContainsKey("Default") ? def["Default"] : "");
                result[name] = value;
            }
            return result;
        }

        private string SafeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name.Replace(' ', '_');
        }

        private string Truncate(string s, int max)
        {
            return s.Length <= max ? s : s.Substring(0, max);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _context.Dispose();
            base.Dispose(disposing);
        }
    }
}
