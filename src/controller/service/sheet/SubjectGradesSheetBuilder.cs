using ClosedXML.Excel;
using wsmcbl.src.model.academy;

namespace wsmcbl.src.controller.service.sheet;

public class SubjectGradesSheetBuilder : SheetBuilder
{
    private string userAlias { get; set; } = null!;
    private string schoolyear { get; set; } = null!;
    private TeacherEntity teacher { get; set; } = null!;
    private EnrollmentEntity enrollment { get; set; } = null!;

    private string partialLabel { get; set; } = null!;
    private List<StudentEntity> studentList { get; set; } = null!;
    private List<model.secretary.SubjectEntity> subjectList { get; set; } = null!;
    
    public override byte[] getSpreadSheet()
    {
        using var workbook = new XLWorkbook();
        initWorksheet(workbook, $"Calificaciones {enrollment.label}");
        
        setTitle();
        setDate(6, userAlias);
        const int headerRow = 10;
        setHeader(headerRow);
        
        var counter = headerRow + 1;
        foreach (var item in studentList)
        {
            setBody(counter, item, counter - headerRow);
            counter++;
        }
        
        var lastRow = studentList.Count + headerRow;
        
        setBorder(lastRow, headerRow);
        
        worksheet.Columns().AdjustToContents();
        adjustToContents(headerRow, 6);
        worksheet.SheetView.FreezeRows(headerRow);
        
        hideAndProtectCells(headerRow - 1);
        
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return stream.ToArray();
    }

    private void adjustToContents(int headerRow, int columnSubject)
    {
        foreach (var column in worksheet.ColumnsUsed())
        {
            if (column.ColumnNumber() < columnSubject)
            {
                continue;
            }
            
            column.Width = column.Cell(headerRow).GetString().Length + 2;
        }
    }

    private void hideAndProtectCells(int headerRow)
    {
        worksheet.Row(headerRow).Hide();
        worksheet.Column(columnQuantity + 1).Hide();
    }

    private void setTitle()
    { 
        var titleRow = 2;
        var title = worksheet.Range($"B{titleRow}:{lastColumnName}{titleRow}").Merge();
        title.Value = Environment.GetEnvironmentVariable("BOLETTA_SCHOOL_NAME") ?? "N/A";
        title.Style.Font.Bold = true;
        title.Style.Font.FontSize = 14;
        title.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        title.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        worksheet.Row(titleRow).Height = 25;
        
        var subTitle = worksheet.Range($"B{titleRow + 1}:{lastColumnName}{titleRow + 1}").Merge();
        subTitle.Value = $"Calificaciones {partialLabel} {schoolyear}";
        subTitle.Style.Font.Bold = true;
        subTitle.Style.Font.FontSize = 13;
        subTitle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        subTitle.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        worksheet.Row(titleRow + 1).Height = 18;
        
        var subTitleTeacher = worksheet.Range($"B{titleRow + 2}:{lastColumnName}{titleRow + 2}").Merge();
        subTitleTeacher.Value = $"Docente: {teacher.fullName()}";
        subTitleTeacher.Style.Font.FontSize = 13;
        subTitleTeacher.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        subTitleTeacher.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        worksheet.Row(titleRow + 1).Height = 18;
    }
    
    private List<string> orderedSubjectIdList { get; set; } = [];
    
    private void setBody(int row, StudentEntity student, int pos)
    {
        var column = 2;
        worksheet.Cell(row, column++).Value = pos;
        worksheet.Cell(row, column++).Value = student.studentId;  
        worksheet.Cell(row, column++).Value = student.fullName(); 
        worksheet.Cell(row, column++).Value = student.student.sex ? "M" : "F";
        
        var gradeList = student.gradeList!
            .Where(e => orderedSubjectIdList.Contains(e.subjectId))
            .OrderBy(e => orderedSubjectIdList.IndexOf(e.subjectId))
            .ToList();
        
        foreach (var grade in gradeList)
        {
            worksheet.Cell(row, column++).Value = grade.grade;
        }

        var first = gradeList.FirstOrDefault();
        if (first == null)
        {
            return;
        }
        
        worksheet.Cell(row, column++).Value = first.conductGrade;
        if (first.conductGrade < 60)
        {
            worksheet.Cell(row, column - 1).Style.Fill.BackgroundColor = redColor;
        }
        
        worksheet.Cell(row, column).Value = student.studentId;
    }

    private void setHeader(int headerRow)
    {
        var headerColumn = 2;
        setHeaderId(headerRow - 1, headerColumn + 2);

        setHeaderEnrollment(headerRow - 2);
        
        var headerStyle = worksheet.Range($"B{headerRow}:{lastColumnName}{headerRow}");
        headerStyle.Style.Font.Bold = true;
        headerStyle.Style.Fill.BackgroundColor = XLColor.LightGray;
        headerStyle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerStyle.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        
        worksheet.Cell(headerRow, headerColumn++).Value = "N°";
        worksheet.Cell(headerRow, headerColumn++).Value = "Código";
        worksheet.Cell(headerRow, headerColumn++).Value = "Nombre completo";
        worksheet.Cell(headerRow, headerColumn++).Value = "Sexo";
        setHeaderForSubject(headerRow, headerColumn);
    }

    private void setHeaderEnrollment(int headerRow)
    {
        var enrollmentTitle = worksheet.Range($"B{headerRow}:{lastColumnName}{headerRow}").Merge();
        enrollmentTitle.Value = enrollment.label;
        enrollmentTitle.Style.Font.Bold = true;
        enrollmentTitle.Style.Font.FontSize = 13;
        enrollmentTitle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        enrollmentTitle.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        worksheet.Row(headerRow).Height = 18;
    }

    private void setHeaderId(int headerRow, int headerColumn)
    {
        worksheet.Cell(headerRow, headerColumn).Value = teacher.teacherId;

        headerColumn += 2;
        foreach (var item in subjectList)
        {
            worksheet.Cell(headerRow, headerColumn++).Value = item.subjectId;
        }
    }

    private void setHeaderForSubject(int headerRow, int headerColumn)
    {
        foreach (var item in subjectList)
        {
            worksheet.Cell(headerRow, headerColumn++).Value = item.initials;
        }
        
        worksheet.Cell(headerRow, headerColumn).Value = "Conducta";
    }
    
    protected override void setColumnQuantity()
    {
        var quantity = subjectList.Count;
        columnQuantity = quantity + 6;
    }
    
    public class Builder
    {
        private readonly SubjectGradesSheetBuilder sheetBuilder;

        public Builder()
        {
            sheetBuilder = new SubjectGradesSheetBuilder();
        }

        public SubjectGradesSheetBuilder build() => sheetBuilder;
        
        public Builder withUserAlias(string parameter)
        {
            sheetBuilder.userAlias = parameter;
            return this;
        }
        
        public Builder withSchoolyear(string parameter)
        {
            sheetBuilder.schoolyear = parameter;
            return this;
        }
        
        public Builder withTeacher(TeacherEntity parameter)
        {
            sheetBuilder.teacher = parameter;
            return this;
        }
        
        public Builder withPartialLabel(string parameter)
        {
            sheetBuilder.partialLabel = parameter;
            return this;
        }
        
        public Builder withEnrollment(EnrollmentEntity parameter)
        {
            sheetBuilder.enrollment = parameter;
            return this;
        }
        
        public Builder withSubjectList(List<SubjectEntity> parameter)
        {
            sheetBuilder.subjectList = parameter.Select(e => e.secretarySubject!).ToList();
            sheetBuilder.setColumnQuantity();
            sheetBuilder.setOrderedSubjectIdList();
            return this;
        }
        
        public Builder withStudentList(List<StudentEntity> parameter)
        {
            sheetBuilder.studentList = parameter;
            return this;
        }
    }
    
    private void setOrderedSubjectIdList()
    {
        orderedSubjectIdList = subjectList.Select(e => e.subjectId).ToList()!;
    }
}