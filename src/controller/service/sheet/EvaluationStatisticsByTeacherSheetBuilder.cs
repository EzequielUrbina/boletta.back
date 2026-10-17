using ClosedXML.Excel;
using wsmcbl.src.model.academy;

namespace wsmcbl.src.controller.service.sheet;

public class EvaluationStatisticsByTeacherSheetBuilder : SheetBuilder
{
    private PartialEntity partial { get; set; } = null!;
    private string schoolyear { get; set; } = null!;
    private string enrollmentLabel { get; set; } = null!;
    private string userAlias { get; set; } = null!;
    private TeacherEntity teacher { get; set; } = null!;

    private List<StudentEntity> studentList { get; set; } = null!;
    private List<model.secretary.StudentEntity> initialStudentList { get; set; } = null!;
    private List<SubjectPartialEntity> subjectPartialList { get; set; } = null!;
    private List<model.secretary.SubjectEntity> subjectList { get; set; } = null!;

    protected override void setColumnQuantity()
    {
        columnQuantity = 16;
    }

    public override byte[] getSpreadSheet()
    {
        using var workbook = new XLWorkbook();
        initWorksheet(workbook, enrollmentLabel);

        setTitle();
        setDate(6, userAlias);
        const int headerRow = 10;
        
        setHeaderEnrollment(headerRow - 2);
        setHeaderSummary(headerRow);
        setSummary(headerRow + 1);
        setDistribution(headerRow + 13);
        
        setHeaderSubject(headerRow);
        setSubject(headerRow + 2);

        worksheet.Columns().AdjustToContents();
        
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return stream.ToArray();
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
        subTitle.Value = $"Datos estadísticos {partial.label} {schoolyear}";
        subTitle.Style.Font.Bold = true;
        subTitle.Style.Font.FontSize = 13;
        subTitle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        subTitle.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        worksheet.Row(titleRow + 1).Height = 18;

        var subTitleTeacher = worksheet.Range($"B{titleRow + 2}:{lastColumnName}{titleRow + 2}").Merge();
        subTitleTeacher.Value = $"Docente guía: {teacher.fullName()}";
        subTitleTeacher.Style.Font.FontSize = 13;
        subTitleTeacher.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        subTitleTeacher.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        worksheet.Row(titleRow + 1).Height = 18;
    }
    
    private void setHeaderEnrollment(int headerRow)
    {
        var enrollmentTitle = worksheet.Range($"B{headerRow}:{lastColumnName}{headerRow}").Merge();
        enrollmentTitle.Value = enrollmentLabel;
        enrollmentTitle.Style.Font.Bold = true;
        enrollmentTitle.Style.Font.FontSize = 13;
        enrollmentTitle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        enrollmentTitle.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        worksheet.Row(headerRow).Height = 18;
    }
    
    private void setHeaderSummary(int headerRow)
    {
        var headerColumn = 2;

        var headerStyle = worksheet.Range($"B{headerRow}:E{headerRow}");
        headerStyle.Style.Font.Bold = true;
        headerStyle.Style.Fill.BackgroundColor = XLColor.LightGray;

        worksheet.Cell(headerRow, headerColumn).Value = "Matrícula";
        worksheet.Cell(headerRow, headerColumn + 1).Value = "Total";
        worksheet.Cell(headerRow, headerColumn + 2).Value = "Varones";
        worksheet.Cell(headerRow, headerColumn + 3).Value = "Mujeres";

        headerRow += 5;
        headerStyle = worksheet.Range($"B{headerRow}:E{headerRow}");
        headerStyle.Style.Font.Bold = true;
        headerStyle.Style.Fill.BackgroundColor = XLColor.LightGray;

        worksheet.Cell(headerRow, headerColumn).Value = "Asignaturas";
        worksheet.Cell(headerRow, headerColumn + 1).Value = "Total";
        worksheet.Cell(headerRow, headerColumn + 2).Value = "Varones";
        worksheet.Cell(headerRow, headerColumn + 3).Value = "Mujeres";
        
        headerRow += 7;
        headerStyle = worksheet.Range($"B{headerRow}:E{headerRow}");
        headerStyle.Style.Font.Bold = true;
        headerStyle.Style.Fill.BackgroundColor = XLColor.LightGray;

        worksheet.Cell(headerRow, headerColumn).Value = "Promedios";
        worksheet.Cell(headerRow, headerColumn + 1).Value = "Total";
        worksheet.Cell(headerRow, headerColumn + 2).Value = "Varones";
        worksheet.Cell(headerRow, headerColumn + 3).Value = "Mujeres";
    }

    private void setSummary(int headerRow)
    {
        const int bodyColumn = 2;

        var initialTotal = initialStudentList.Count;
        var initialMaleCount = initialStudentList.Count(e => e.sex);
        addRow(headerRow++, bodyColumn, "Inicial", initialMaleCount, initialTotal);
        
        var total = studentList.Count;
        var maleCount = studentList.Count(e => e.student.sex);
        addRow(headerRow++, bodyColumn, "Actual", maleCount, total);
        
        var malePercentage = (decimal) maleCount / initialMaleCount;
        var femalePercentage = (decimal) (total - maleCount) / (initialTotal - initialMaleCount);
        var totalPercentage = (decimal) total / initialTotal;
        addRowPercentage(headerRow, bodyColumn, "Retención (%)", malePercentage, femalePercentage, totalPercentage);
        
        var approvedList = studentList.Where(e => e.passedAllSubjects()).ToList();
        var approvedTotal = approvedList.Count;
        var approvedMaleCount = approvedList.Count(e => e.student.sex);
        headerRow += 3;
        addRow(headerRow++, bodyColumn, "Aprobados limpios", approvedMaleCount, approvedTotal);

        malePercentage = (decimal) approvedMaleCount / maleCount;
        femalePercentage = (decimal) (approvedTotal - approvedMaleCount) / (total - maleCount);
        totalPercentage = (decimal) approvedTotal / total;
        addRowPercentage(headerRow++, bodyColumn, "Rendimiento académico (%)", malePercentage, femalePercentage, totalPercentage);
        
        var failedFromOneToTwoList = studentList.Where(e => e.isFailed(1)).ToList();
        total = failedFromOneToTwoList.Count;
        maleCount = failedFromOneToTwoList.Count(e => e.student.sex);
        addRow(headerRow++, bodyColumn,"Reprobados de 1 a 2", maleCount, total);
        
        var failedFromThreeToMoreList = studentList.Where(e => e.isFailed(2)).ToList();
        total = failedFromThreeToMoreList.Count;
        maleCount = failedFromThreeToMoreList.Count(e => e.student.sex);
        addRow(headerRow++, bodyColumn,"Reprobados de 3 a más", maleCount, total);
        
        var notEvaluatedList = studentList.Where(e => e.hasNotEvaluated()).ToList();
        total = notEvaluatedList.Count;
        maleCount = notEvaluatedList.Count(e => e.student.sex);
        addRow(headerRow, bodyColumn,"No evaluados", maleCount, total);

        setBorderByRange("B10:E13");
        setBorderByRange("B15:E20");
    }
    
    private void setDistribution(int headerRow)
    {
        const int bodyColumn = 2;
        
        var list = studentList.Where(e => e.isWithInRange("AA", partial.partialId)).ToList();
        var total = list.Count;
        var maleCount = list.Count(e => e.student.sex);
        addRow(headerRow++, bodyColumn, "AA (100 - 90)", maleCount, total);
        
        list = studentList.Where(e => e.isWithInRange("AS", partial.partialId)).ToList();
        total = list.Count;
        maleCount = list.Count(e => e.student.sex);
        addRow(headerRow++, bodyColumn, "AS (89  - 76)", maleCount, total);
        
        list = studentList.Where(e => e.isWithInRange("AF", partial.partialId)).ToList();
        total = list.Count;
        maleCount = list.Count(e => e.student.sex);
        addRow(headerRow++, bodyColumn, "AF (75  - 60)", maleCount, total);
        
        list = studentList.Where(e => e.isWithInRange("AI", partial.partialId)).ToList();
        total = list.Count;
        maleCount = list.Count(e => e.student.sex);
        addRow(headerRow++, bodyColumn, "AI (59  - 0)", maleCount, total);
        
        total = studentList.Count;
        maleCount = studentList.Count(e => e.student.sex);
        addRow(headerRow, bodyColumn, "Totales", maleCount, total);
        
        setBorderByRange("B22:E27");
    }

    private void setHeaderSubject(int headerRow)
    {
        var headerColumn = 7;

        var rangeString = $"{getColumnName(headerColumn)}{headerRow}:{getColumnName(columnQuantity)}{headerRow + 1}";
        var headerStyle = worksheet.Range(rangeString);
        headerStyle.Style.Font.Bold = true;
        headerStyle.Style.Fill.BackgroundColor = XLColor.LightGray;
        headerStyle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        setBorderByRange(rangeString);

        worksheet.Cell(headerRow, headerColumn).Value = "Asignaturas";

        rangeString = $"{getColumnName(headerColumn + 1)}{headerRow}:{getColumnName(headerColumn + 3)}{headerRow}";
        var cells = worksheet.Range(rangeString).Merge();
        cells.Value = "Aprobados";
        cells.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        cells.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        rangeString = $"{getColumnName(headerColumn + 4)}{headerRow}:{getColumnName(headerColumn + 6)}{headerRow}";
        cells = worksheet.Range(rangeString).Merge();
        cells.Value = "Reprobados";
        cells.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        cells.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        rangeString = $"{getColumnName(headerColumn + 7)}{headerRow}:{getColumnName(headerColumn + 9)}{headerRow++}";
        cells = worksheet.Range(rangeString).Merge();
        cells.Value = "No evaluados";
        cells.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        cells.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        headerColumn++;
        worksheet.Cell(headerRow, headerColumn++).Value = "  Total  ";
        worksheet.Cell(headerRow, headerColumn++).Value = "Varones";
        worksheet.Cell(headerRow, headerColumn++).Value = "Mujeres";
        worksheet.Cell(headerRow, headerColumn++).Value = "  Total  ";
        worksheet.Cell(headerRow, headerColumn++).Value = "Varones";
        worksheet.Cell(headerRow, headerColumn++).Value = "Mujeres";
        worksheet.Cell(headerRow, headerColumn++).Value = "  Total  ";
        worksheet.Cell(headerRow, headerColumn++).Value = "Varones";
        worksheet.Cell(headerRow, headerColumn).Value = "Mujeres";
    }

    private void setSubject(int headerRow)
    {
        foreach (var item in subjectList)
        {
            setBody(item, headerRow++);
        }
        
        setBorderByRange($"G12:P{subjectList.Count + 11}");
    }

    private void setBody(model.secretary.SubjectEntity item, int headerRow)
    {
        var bodyColumn = 7;
        var result = subjectPartialList.FirstOrDefault(e => e.subjectId == item.subjectId);
        if (result == null)
        {
            return;
        }
        
        var approvedList = result.gradeList.Where(e => e.isApproved()).ToList();
        var failedList = result.gradeList.Where(e => !e.isApproved()).ToList();
        var notEvaluatedList = result.gradeList.Where(e => e.isNotEvaluated()).ToList();

        var total = approvedList.Count;
        var male = approvedList.Count(e => e.student!.sex);
        addRow(headerRow, bodyColumn, item.initials, male, total);
        
        total = failedList.Count;
        male = failedList.Count(e => e.student!.sex);
        bodyColumn += 3;
        addRow(headerRow, bodyColumn, null, male, total);
        
        total = notEvaluatedList.Count;
        male = notEvaluatedList.Count(e => e.student!.sex);
        bodyColumn += 3;
        addRow(headerRow, bodyColumn, null, male, total);
    }
    
    
    private void addRow(int headerRow, int bodyColumn, string? title, int male, int total)
    {
        if (title != null)
        {
            worksheet.Cell(headerRow, bodyColumn).Value = title; 
        }
        
        worksheet.Cell(headerRow, bodyColumn + 1).Value = total;
        worksheet.Cell(headerRow, bodyColumn + 2).Value = male;
        worksheet.Cell(headerRow, bodyColumn + 3).Value = total - male;
    }

    private void addRowPercentage(int headerRow, int bodyColumn, string title, decimal male, decimal female, decimal total)
    {
        worksheet.Cell(headerRow, bodyColumn).Value = title;
        worksheet.Cell(headerRow, bodyColumn + 1).Value = $"{Math.Round(100 * total, 2)} %";
        worksheet.Cell(headerRow, bodyColumn + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            
        worksheet.Cell(headerRow, bodyColumn + 2).Value = $"{Math.Round(100 * male, 2)} %";
        worksheet.Cell(headerRow, bodyColumn + 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        
        worksheet.Cell(headerRow, bodyColumn + 3).Value = $"{Math.Round(100 * female, 2)} %";
        worksheet.Cell(headerRow, bodyColumn + 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
    }

    public class Builder
    {
        private readonly EvaluationStatisticsByTeacherSheetBuilder sheetBuilder;

        public Builder()
        {
            sheetBuilder = new EvaluationStatisticsByTeacherSheetBuilder();
        }

        public EvaluationStatisticsByTeacherSheetBuilder build() => sheetBuilder;

        public Builder withPartial(PartialEntity parameter)
        {
            sheetBuilder.partial = parameter;
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

        public Builder withUserAlias(string parameter)
        {
            sheetBuilder.userAlias = parameter;
            return this;
        }

        public Builder withEnrollment(string parameter)
        {
            sheetBuilder.enrollmentLabel = parameter;
            return this;
        }

        public Builder withSubjectList(List<SubjectEntity> parameter)
        {
            sheetBuilder.subjectList = parameter.Select(e => e.secretarySubject!).ToList();
            sheetBuilder.setColumnQuantity();
            return this;
        }

        public Builder withSubjectPartialList(List<SubjectPartialEntity> parameter)
        {
            sheetBuilder.subjectPartialList = parameter;
            return this;
        }
        
        public Builder withStudentList(List<StudentEntity> parameter)
        {
            sheetBuilder.studentList = parameter;
            return this;
        }
        
        public Builder withInitialStudentList(List<model.secretary.StudentEntity> parameter)
        {
            sheetBuilder.initialStudentList = parameter;
            return this;
        }
    }
}