using System.Text;
using ClosedXML.Excel;
using wsmcbl.src.utilities;

namespace wsmcbl.src.controller.service.sheet;

public abstract class SheetBuilder
{
    protected int columnQuantity { get; set; }
    protected IXLWorksheet worksheet { get; set; } = null!;
    protected string lastColumnName { get; set; } = null!;
    protected XLColor redColor { get; set; } = XLColor.FromHtml("#FFA6A6");
    protected XLColor blueColor { get; set; } = XLColor.FromHtml("#a6c8ed");
    protected XLColor grayColor { get; set; } = XLColor.FromHtml("#D9D9D9");

    protected abstract void setColumnQuantity();
    public abstract byte[] getSpreadSheet();

    public virtual async Task loadSpreadSheetAsync()
    {
        await Task.CompletedTask;
    }

    protected void initWorksheet(XLWorkbook workbook, string title)
    {
        lastColumnName = getColumnName(columnQuantity);
        
        worksheet = workbook.Worksheets.Add(title);
        worksheet.Style.Font.FontSize = 12;
        
        worksheet.CellsUsed().Style.NumberFormat.SetFormat("@");
    }
    
    protected static string getColumnName(int value)
    {
        var result = new StringBuilder();
        while (value > 0)
        {
            var modulo = (value - 1) % 26;
            result.Insert(0, Convert.ToChar(65 + modulo));
            value = (value - 1) / 26;
        }

        return result.ToString();
    }
    
    protected void setBorder(int lastRow, int headerRow)
    {
        setBorderByRange($"B{headerRow}:{lastColumnName}{lastRow}");
    }

    protected void setBorderByRange(string stringRange)
    {
        var ixlRange = worksheet.Range(stringRange); 
        
        ixlRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
        ixlRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        ixlRange.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
        ixlRange.Style.Border.RightBorder = XLBorderStyleValues.Thin;
        ixlRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }
    
    protected void setDate(int row, string userAlias)
    {
        var dateCell = worksheet.Range($"B{row}:{lastColumnName}{row}").Merge();
        dateCell.Value = $"Generado por Boletta el {DateTime.UtcNow.toStringFull(false)}, {userAlias}.";
        dateCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }
}