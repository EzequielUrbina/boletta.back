using ClosedXML.Excel;
using wsmcbl.src.exception;
using wsmcbl.src.model.config;
using wsmcbl.src.model.secretary;
using wsmcbl.src.utilities;

namespace wsmcbl.src.controller.service.sheet;

public class StudentRegisterSheetBuilder : SheetBuilder
{
    private UserEntity user { get; set; } = null!;
    private SchoolyearEntity schoolyear { get; set; } = null!;
    private List<StudentRegisterView> registerList { get; set; } = null!;
    
    public override byte[] getSpreadSheet()
    {
        var title = $"Padrón {schoolyear.label}";
        
        using var workbook = new XLWorkbook();
        initWorksheet(workbook, title);
        
        setTitle(title);
        setDate(5, user.getAlias());
        const int headerRow = 7;
        setHeader(headerRow);
        
        var list = registerList
            .OrderBy(e => e.degreePosition)
            .ThenBy(e => e.sectionPosition)
            .ThenBy(e => e.fullName);
        
        var counter = headerRow + 1;
        foreach (var item in list)
        {
            setBody(counter, item, counter - headerRow);
            counter++;
        }

        var lastRow = registerList.Count + headerRow;
        
        setBorder(lastRow, headerRow);
        
        worksheet.Columns().AdjustToContents();
        worksheet.SheetView.FreezeRows(headerRow);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return stream.ToArray();
    }

    private void setTitle(string title)
    { 
        var titleRow = 2;
        var titleCell = worksheet!.Range($"B{titleRow}:{lastColumnName}{titleRow}").Merge();
        titleCell.Value = Environment.GetEnvironmentVariable("BOLETTA_SCHOOL_NAME") ?? "N/A";
        titleCell.Style.Font.Bold = true;
        titleCell.Style.Font.FontSize = 14;
        titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        titleCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        worksheet.Row(titleRow).Height = 25;
        
        
        var titleCell1 = worksheet.Range($"B{titleRow + 1}:{lastColumnName}{titleRow + 1}").Merge();
        titleCell1.Value = title;
        titleCell1.Style.Font.Bold = true;
        titleCell1.Style.Font.FontSize = 13;
        titleCell1.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        titleCell1.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        worksheet.Row(titleRow + 1).Height = 20;
    }

    private void setBody(int counter, StudentRegisterView item, int pos)
    {
        var bodyColumn = 2;
        worksheet!.Cell(counter, bodyColumn++).Value = pos;
        worksheet.Cell(counter, bodyColumn++).Value = item.studentId; 
        worksheet.Cell(counter, bodyColumn++).Value = item.minedId.getOrDefault();
        worksheet.Cell(counter, bodyColumn++).Value = item.educationalLevel; 
        worksheet.Cell(counter, bodyColumn++).Value = item.degree; 
        worksheet.Cell(counter, bodyColumn++).Value = item.section;  
        worksheet.Cell(counter, bodyColumn++).Value = item.fullName; 
        worksheet.Cell(counter, bodyColumn++).Value = item.birthday.ToString("dd-MM-yyyy");
        worksheet.Cell(counter, bodyColumn++).Value = item.getAge();  
        worksheet.Cell(counter, bodyColumn++).Value = item.sex ? "M" : "F"; 
        worksheet.Cell(counter, bodyColumn++).Value = item.weight; 
        worksheet.Cell(counter, bodyColumn++).Value = item.height;  
        worksheet.Cell(counter, bodyColumn++).Value = item.address;  
        worksheet.Cell(counter, bodyColumn++).Value = item.mother; 
        worksheet.Cell(counter, bodyColumn++).Value = item.motherIdCard; 
        worksheet.Cell(counter, bodyColumn++).Value = item.father; 
        worksheet.Cell(counter, bodyColumn++).Value = item.fatherIdCard;
        worksheet.Cell(counter, bodyColumn++).Value = item.tutor; 
        worksheet.Cell(counter, bodyColumn++).Value = item.phone; 
        worksheet.Cell(counter, bodyColumn++).Value = item.getIsRepeatingString();
        worksheet.Cell(counter, bodyColumn).Value = item.getEnrollDateString();
    }

    private void setHeader(int headerRow)
    { 
        var headerStyle = worksheet!.Range($"B{headerRow}:{lastColumnName}{headerRow}");
        headerStyle.Style.Font.Bold = true;
        headerStyle.Style.Fill.BackgroundColor = XLColor.LightGray;
        headerStyle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerStyle.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        
        var headerColumn = 2;
        worksheet.Cell(headerRow, headerColumn++).Value = "N°";
        worksheet.Cell(headerRow, headerColumn++).Value = "Código";
        worksheet.Cell(headerRow, headerColumn++).Value = "Cód. mined";
        worksheet.Cell(headerRow, headerColumn++).Value = "Modalidad";
        worksheet.Cell(headerRow, headerColumn++).Value = "Nivel";
        worksheet.Cell(headerRow, headerColumn++).Value = "Sección";
        worksheet.Cell(headerRow, headerColumn++).Value = "Nombre completo";
        worksheet.Cell(headerRow, headerColumn++).Value = "F. nacimiento";
        worksheet.Cell(headerRow, headerColumn++).Value = "Edad";
        worksheet.Cell(headerRow, headerColumn++).Value = "Sexo";
        worksheet.Cell(headerRow, headerColumn++).Value = "Peso";
        worksheet.Cell(headerRow, headerColumn++).Value = "Talla";
        worksheet.Cell(headerRow, headerColumn++).Value = "Dirección";
        worksheet.Cell(headerRow, headerColumn++).Value = "Madre";
        worksheet.Cell(headerRow, headerColumn++).Value = "Céd. madre";
        worksheet.Cell(headerRow, headerColumn++).Value = "Padre";
        worksheet.Cell(headerRow, headerColumn++).Value = "Céd. padre";
        worksheet.Cell(headerRow, headerColumn++).Value = "Tutor";
        worksheet.Cell(headerRow, headerColumn++).Value = "Teléfono";
        worksheet.Cell(headerRow, headerColumn++).Value = "Repitente";
        worksheet.Cell(headerRow, headerColumn).Value = "F. matrícula";
    }

    protected override void setColumnQuantity()
    {
        columnQuantity = 22;
    }

    public class Builder
    {
        private readonly StudentRegisterSheetBuilder sheetBuilder;

        public Builder()
        {
            sheetBuilder = new StudentRegisterSheetBuilder();
            sheetBuilder.setColumnQuantity();
        }

        public StudentRegisterSheetBuilder build() => sheetBuilder;
        
        public Builder withUser(UserEntity parameter)
        {
            sheetBuilder.user = parameter;
            return this;
        }
        
        public Builder withSchoolyear(SchoolyearEntity parameter)
        {
            sheetBuilder.schoolyear = parameter;
            return this;
        }
        
        public Builder withRegisterList(List<StudentRegisterView>? parameter)
        {
            if (parameter == null)
            {
                throw new InternalException("There is not student register.");
            }
            
            sheetBuilder.registerList = parameter;
            return this;
        }
    }
}