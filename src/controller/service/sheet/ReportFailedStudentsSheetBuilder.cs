using System.Text;
using ClosedXML.Excel;
using wsmcbl.src.model.academy;
using wsmcbl.src.model.dao;
using wsmcbl.src.model.secretary;
using wsmcbl.src.utilities;
using StudentEntity = wsmcbl.src.model.academy.StudentEntity;

namespace wsmcbl.src.controller.service.sheet;

public class ReportFailedStudentsSheetBuilder : SheetBuilder
{
    private PartialEntity partial { get; set; } = null!;
    private string schoolyear { get; set; } = null!;
    private string userAlias { get; set; } = null!;
    private DaoFactory daoFactory { get; set; } = null!;
    private List<DegreeEntity> degreeList { get; set; } = null!;
    private List<model.secretary.SubjectEntity> subjectList { get; set; } = null!;

    protected override void setColumnQuantity()
    {
        columnQuantity = 7;
    }

    private XLWorkbook workbook { get; set; } = null!;

    public override async Task loadSpreadSheetAsync()
    {
        workbook = new XLWorkbook();

        await setLevelBody(2);
        await setLevelBody(3);
    }

    public override byte[] getSpreadSheet()
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return stream.ToArray();
    }

    private async Task setLevelBody(int level)
    {
        var educationalLevel = level == 2 ? "Primaria" : "Secundaria";

        setColumnQuantity();
        initWorksheet(workbook, educationalLevel);
        setTitle(educationalLevel);
        setDate(7, userAlias);

        subjectList = await daoFactory.subjectDao!.getListForCurrentSchoolyearByLevel(level, partial.semester);
        setTitle();

        counter = 1;
        row = 11;
        var list = degreeList.Where(e => e.educationalLevel == educationalLevel).ToList();
        foreach (var item in list)
        {
            studentList = await daoFactory.academyStudentDao!
                .getListWithGradesByDegreeId(item.degreeId!, partial.partialId);

            var enrollmentList = item.enrollmentList!.Where(e => e.quantity != 0)
                .OrderBy(e => e.tag).ToList();
            
            foreach (var enr in enrollmentList)
            {
                setBodyByDegree(enr);
            }
        }
        
        var rangeString = $"B9:{lastColumnName}{counter + 9}";
        setBorderByRange(rangeString);

        worksheet.Columns().AdjustToContents();
    }

    private List<StudentEntity> studentList { get; set; } = null!;

    private void setTitle(string level)
    {
        const int titleRow = 2;
        var title = worksheet.Range($"B{titleRow}:{lastColumnName}{titleRow}").Merge();
        title.Value = Environment.GetEnvironmentVariable("BOLETTA_SCHOOL_NAME") ?? "N/A";
        title.Style.Font.Bold = true;
        title.Style.Font.FontSize = 14;
        title.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        title.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        worksheet.Row(titleRow).Height = 25;

        var subTitle = worksheet.Range($"B{titleRow + 1}:{lastColumnName}{titleRow + 1}").Merge();
        subTitle.Value = $"Reporte de reporbados {partial.label} {schoolyear}";
        subTitle.Style.Font.Bold = true;
        subTitle.Style.Font.FontSize = 13;
        subTitle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        subTitle.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        worksheet.Row(titleRow + 1).Height = 18;

        subTitle = worksheet.Range($"B{titleRow + 3}:{lastColumnName}{titleRow + 3}").Merge();
        subTitle.Value = level;
        subTitle.Style.Font.Bold = true;
        subTitle.Style.Font.FontSize = 13;
        subTitle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        subTitle.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        worksheet.Row(titleRow + 3).Height = 18;
    }

    private void setTitle()
    {
        row = 9;
        var column = 2;

        worksheet.Range($"B{row}:E{row}").Merge().Value = "Datos";
        worksheet.Range($"F{row}:G{row}").Merge().Value = "Asignaturas";

        row++;
        worksheet.Cell(row, column++).Value = "N°";
        worksheet.Cell(row, column++).Value = "Nombre";
        worksheet.Cell(row, column++).Value = "Código";
        worksheet.Cell(row, column++).Value = "Grado";
        worksheet.Cell(row, column++).Value = "De 1 a 2";
        worksheet.Cell(row, column).Value = "De 3 a más";
        
        var rangeString = $"B{row - 1}:{lastColumnName}{row}";
        var headerStyle = worksheet.Range(rangeString);
        headerStyle.Style.Font.Bold = true;
        headerStyle.Style.Fill.BackgroundColor = grayColor;
        headerStyle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private int counter { get; set; }
    private int row { get; set; }
    private void setBodyByDegree(EnrollmentEntity enrollment)
    {
        var failedList = studentList
            .Where(e => e.enrollmentId == enrollment.enrollmentId)
            .Where(e => !e.passedAllSubjects())
            .OrderBy(e => e.student.sex).ThenBy(e => e.student.name).ToList();

        if (failedList.Count == 0)
        {
            return;
        }

        foreach (var std in failedList)
        {
            var column = 2;
            worksheet.Cell(row, column++).Value = counter++;
            worksheet.Cell(row, column++).Value = std.fullName();
            worksheet.Cell(row, column++).Value = std.student.minedId.getOrDefault();
            worksheet.Cell(row, column++).Value = enrollment.label;
            worksheet.Cell(row, column++).Value = getSubjectName(std, 1);
            worksheet.Cell(row, column).Value = getSubjectName(std, 2);

            row++;
        }
    }

    private string getSubjectName(StudentEntity std, int type)
    {
        if (!std.isFailed(type))
        {
            return string.Empty;
        }
        
        var idList = std.getSubjectFailedIdList();
        
        var listName = subjectList
            .Where(e => idList.Contains(e.subjectId!))
            .Select(e => e.name).ToList();
        
        return string.Join(", ", listName);
    }

    public class Builder
    {
        private readonly ReportFailedStudentsSheetBuilder sheetBuilder;

        public Builder()
        {
            sheetBuilder = new ReportFailedStudentsSheetBuilder();
        }

        public ReportFailedStudentsSheetBuilder build() => sheetBuilder;

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

        public Builder withUserAlias(string parameter)
        {
            sheetBuilder.userAlias = parameter;
            return this;
        }

        public Builder witDegreeList(List<DegreeEntity> parameter)
        {
            sheetBuilder.degreeList = parameter
                .OrderBy(e => e.educationalLevel).ThenBy(e => e.tag).ToList();
            return this;
        }

        public Builder withDaoFactory(DaoFactory parameter)
        {
            sheetBuilder.daoFactory = parameter;
            return this;
        }
    }
}