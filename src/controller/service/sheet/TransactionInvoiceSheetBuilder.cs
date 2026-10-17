using ClosedXML.Excel;
using wsmcbl.src.model.accounting;

namespace wsmcbl.src.controller.service.sheet;

public class TransactionInvoiceSheetBuilder : SheetBuilder
{
    private List<TransactionInvoiceView> _transactions { get; set; } = [];
    private string _userAlias { get; set; } = string.Empty;

    public TransactionInvoiceSheetBuilder()
    {
        setColumnQuantity();
    }

    public TransactionInvoiceSheetBuilder(List<TransactionInvoiceView> transactions)
    {
        _transactions = transactions;
        setColumnQuantity();
    }

    protected override void setColumnQuantity()
    {
        // Cantidad de columnas (A, B, C, D, E, F, G, H) = Total 8 columnas
        columnQuantity = 8;
    }

    public override byte[] getSpreadSheet()
    {
        using var workbook = new XLWorkbook();
        initWorksheet(workbook, "Transacciones");

        const int titleRow = 2;
        const int headerRow = 4;

        setTitle(titleRow);
        setHeader(headerRow);

        int currentRow = headerRow + 1;
        int firstDataRow = currentRow;

        foreach (var item in _transactions)
        {
            setRowData(currentRow, item);
            currentRow++;
        }

        int lastDataRow = currentRow - 1;

        // Fila de Total (solo si existen transacciones)
        if (_transactions.Count > 0)
        {
            setTotalRow(currentRow, firstDataRow, lastDataRow);
            currentRow++;
        }

        // Bordes de la tabla
        int borderLastRow = _transactions.Count > 0 ? currentRow - 1 : headerRow;
        setBorder(borderLastRow, headerRow);

        // Pie de página con fecha de generación y usuario
        setDate(currentRow + 1, _userAlias);

        // Ajuste automático de columnas
        worksheet.Columns($"B:{lastColumnName}").AdjustToContents();

        // Congelar encabezados al hacer scroll
        worksheet.SheetView.FreezeRows(headerRow);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private void setTitle(int titleRow)
    {
        var schoolName = Environment.GetEnvironmentVariable("BOLETTA_SCHOOL_NAME") ?? "N/A";

        var titleRange = worksheet.Range($"B{titleRow}:{lastColumnName}{titleRow}").Merge();
        titleRange.Value = $"Reporte de Transacciones - {schoolName}";
        titleRange.Style.Font.Bold = true;
        titleRange.Style.Font.FontSize = 15;
        titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        titleRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        worksheet.Row(titleRow).Height = 25;
    }

    private void setHeader(int headerRow)
    {
        string[] headers = { "Fecha", "Estudiante", "N° Recibo", "Monto", "Concepto", "Estado", "Cajero" };

        var headerRange = worksheet.Range($"B{headerRow}:{lastColumnName}{headerRow}");
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = blueColor;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        worksheet.Row(headerRow).Height = 22;

        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell($"{getColumnName(i + 2)}{headerRow}").Value = headers[i];
        }
    }

    private void setRowData(int row, TransactionInvoiceView item)
    {
        // Fecha
        var dateCell = worksheet.Cell($"B{row}");
        dateCell.Value = item.date;
        dateCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Estudiante
        worksheet.Cell($"C{row}").Value = item.student;

        // N° Recibo
        var numberCell = worksheet.Cell($"D{row}");
        numberCell.Value = item.number;
        numberCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Monto
        var totalCell = worksheet.Cell($"E{row}");
        totalCell.Value = item.total;
        totalCell.Style.NumberFormat.SetFormat("C$ #,##0.00");
        totalCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        // Concepto
        worksheet.Cell($"F{row}").Value = item.concept;

        // Estado
        var statusCell = worksheet.Cell($"G{row}");
        statusCell.Value = item.isValid ? "Válida" : "Anulada";
        statusCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        statusCell.Style.Font.Bold = true;

        // Cajero
        worksheet.Cell($"H{row}").Value = item.cashier;

        // Resaltado elegante para transacciones anuladas
        if (!item.isValid)
        {
            var rowRange = worksheet.Range($"B{row}:H{row}");
            rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFEBEE"); // Rojo pastel suave
            statusCell.Style.Font.FontColor = XLColor.DarkRed;
        }
    }

    private void setTotalRow(int row, int firstDataRow, int lastDataRow)
    {
        // Etiqueta "Total" en la columna N° Recibo (D)
        var labelCell = worksheet.Cell($"D{row}");
        labelCell.Value = "Total:";
        labelCell.Style.Font.Bold = true;
        labelCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        // Fórmula SUMA en la columna Monto (E)
        var totalCell = worksheet.Cell($"E{row}");
        totalCell.FormulaA1 = $"=SUM(E{firstDataRow}:E{lastDataRow})";
        totalCell.Style.Font.Bold = true;
        totalCell.Style.NumberFormat.SetFormat("C$ #,##0.00");
        totalCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        // Estilar la fila de total
        var totalRange = worksheet.Range($"B{row}:{lastColumnName}{row}");
        totalRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        totalRange.Style.Font.Bold = true;
    }

    public class Builder
    {
        private readonly TransactionInvoiceSheetBuilder _sheetBuilder = new();

        public Builder withTransactions(List<TransactionInvoiceView> transactions)
        {
            _sheetBuilder._transactions = transactions;
            return this;
        }

        public Builder withUserAlias(string userAlias)
        {
            _sheetBuilder._userAlias = userAlias;
            return this;
        }

        public TransactionInvoiceSheetBuilder build()
        {
            _sheetBuilder.setColumnQuantity();
            return _sheetBuilder;
        }
    }
}