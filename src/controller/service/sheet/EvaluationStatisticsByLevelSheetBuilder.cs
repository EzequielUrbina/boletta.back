using ClosedXML.Excel;
using wsmcbl.src.model.academy;
using wsmcbl.src.model.dao;
using wsmcbl.src.model.secretary;
using StudentEntity = wsmcbl.src.model.secretary.StudentEntity;

namespace wsmcbl.src.controller.service.sheet;

public class EvaluationStatisticsByLevelSheetBuilder : SheetBuilder
{
    private PartialEntity partial { get; set; } = null!;
    private string schoolyear { get; set; } = null!;
    private string userAlias { get; set; } = null!;
    private DaoFactory daoFactory { get; set; } = null!;
    private List<DegreeEntity> degreeList { get; set; } = null!;
    private List<model.secretary.SubjectEntity> subjectList { get; set; } = null!;
    
    protected override void setColumnQuantity()
    {
        columnQuantity = 10;
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
        
        var list = degreeList.Where(e => e.educationalLevel == educationalLevel).ToList();
        updateColumnQuantity(2 + 3 * list.Count);
        
        initWorksheet(workbook, educationalLevel);
        setTitle(educationalLevel);
        setDate(7, userAlias);
        
        await setSubjectListByLevel(level);
        
        var column = 2;
        setTitleRow(column++);
        foreach (var item in list)
        {
            await setListByDegree(item.degreeId!);
            setBodyByDegree(item, column);

            column += 3;
        }
        
        worksheet.Columns().AdjustToContents();
    }

    private void updateColumnQuantity(int value)
    {
        columnQuantity = value;
        lastColumnName = getColumnName(value);
    }

    private List<model.secretary.SubjectEntity> subjectNameList { get; set; } = null!;
    private async Task setSubjectListByLevel(int level)
    {
        subjectList = await daoFactory.subjectDao!.getListForCurrentSchoolyearByLevel(level, partial.semester);

        subjectNameList = subjectList.OrderBy(e => e.areaId)
            .ThenBy(e => e.number).DistinctBy(e => e.initials).ToList();
    }

    private List<model.academy.StudentEntity> studentList { get; set; } = null!;
    private List<StudentEntity> initialStudentList { get; set; } = null!;
    private List<SubjectPartialEntity> subjectPartialList { get; set; } = null!;
    
    private async Task setListByDegree(string degreeId)
    {
        studentList = await daoFactory
            .academyStudentDao!.getListWithGradesByDegreeId(degreeId, partial.partialId);
        
        initialStudentList = await getListBeforeFirstPartialByDegree(degreeId);
        subjectPartialList = await daoFactory.subjectPartialDao!.getListByPartialIdAndDegreeId(partial.partialId, degreeId);
    }

    private async Task<List<StudentEntity>> getListBeforeFirstPartialByDegree(string degreeId)
    {
        var result = await daoFactory.academyStudentDao!.getListBeforeFirstPartialByDegreeId(degreeId);
        var initialList = result.Select(e => e.student).ToList();
        
        var list = await daoFactory.withdrawnStudentDao!.getListByDegreeId(degreeId);
        var withdrawnStudentList = list.Select(e => e.student!).ToList();
        
        return initialList.Union(withdrawnStudentList).ToList();
    }

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
        subTitle.Value = $"Datos estadísticos {partial.label} {schoolyear}";
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

    private void setTitleRow(int column)
    {
        var row = 10;
        cell(row++, column, "Matrícula");
        worksheet.Cell(row++, column).Value = "Inicial";
        worksheet.Cell(row, column).Value = "Actual";
        
        var rangeString = $"{getColumnName(column)}{row - 2}:{lastColumnName}{row}";
        setBorderByRange(rangeString);

        row += 3;
        cell(row++, column,"Asignaturas");
        worksheet.Cell(row++, column).Value = "Aprobados limpios";
        worksheet.Cell(row++, column).Value = "Reprobados de 1 a 2";
        worksheet.Cell(row, column).Value = "Reprobados de 3 a más";
        
        rangeString = $"{getColumnName(column)}{row - 3}:{lastColumnName}{row}";
        setBorderByRange(rangeString);

        row += 3;
        cell(row++, column,"Asignatura/Aprobado");
        
        foreach (var item in subjectNameList)
        {
            worksheet.Cell(row++, column).Value = item.initials;
        }
        
        rangeString = $"{getColumnName(column)}{row - 1 - subjectNameList.Count}:{lastColumnName}{row - 1}";
        setBorderByRange(rangeString);

        row += 2;
        cell(row++, column, "Promedio");
        worksheet.Cell(row++, column).Value = "AA (100 - 90)";
        worksheet.Cell(row++, column).Value = "AS (89 - 76)";
        worksheet.Cell(row++, column).Value = "AF (75 - 60)";
        worksheet.Cell(row++, column).Value = "AI (59 - 0)";
        worksheet.Cell(row, column).Value = "Total";
        
        rangeString = $"{getColumnName(column)}{row - 5}:{lastColumnName}{row}";
        setBorderByRange(rangeString);
    }

    private void setBodyByDegree(DegreeEntity degree, int column)
    {
        var row = 9;
        degreeTitle(row, column, degree.label);

        row += 2;
        var initialTotal = initialStudentList.Count;
        var initialMaleCount = initialStudentList.Count(e => e.sex);
        var initialFemaleCount = initialTotal - initialMaleCount;
        setCellValues(row++, column, initialTotal,initialMaleCount, initialFemaleCount);
        
        var total = studentList.Count;
        var maleCount = studentList.Count(e => e.student.sex);
        var femaleCount = total - maleCount;
        setCellValues(row, column, total,maleCount, femaleCount);

        row += 2;
        degreeTitle(row, column, degree.label);
        
        row += 2;
        var approvedList = studentList.Where(e => e.passedAllSubjects()).ToList();
        var approvedTotal = approvedList.Count;
        var approvedMaleCount = approvedList.Count(e => e.student.sex);
        var approvedFemaleCount = approvedTotal - approvedMaleCount;
        setCellValues(row++, column, approvedTotal,approvedMaleCount, approvedFemaleCount);
        
        var failedFromOneToTwoList = studentList.Where(e => e.isFailed(1)).ToList();
        total = failedFromOneToTwoList.Count;
        maleCount = failedFromOneToTwoList.Count(e => e.student.sex);
        femaleCount = total - maleCount;
        setCellValues(row++, column, total, maleCount, femaleCount);
        
        var failedFromThreeToMoreList = studentList.Where(e => e.isFailed(2)).ToList();
        total = failedFromThreeToMoreList.Count;
        maleCount = failedFromThreeToMoreList.Count(e => e.student.sex);
        femaleCount = total - maleCount;
        setCellValues(row, column, total, maleCount, femaleCount);

        row += 2;
        degreeTitle(row, column, degree.label);
        
        row += 2;
        setSubjectDetail(row, column, degree.degreeId!);

        row += 1 + subjectNameList.Count;
        degreeTitle(row, column, degree.label);
        
        row += 2;
        var list = studentList.Where(e => e.isWithInRange("AA", partial.partialId)).ToList();
        total = list.Count;
        maleCount = list.Count(e => e.student.sex);
        femaleCount = total - maleCount;
        setCellValues(row++, column, total,maleCount, femaleCount);
        
        list = studentList.Where(e => e.isWithInRange("AS", partial.partialId)).ToList();
        total = list.Count;
        maleCount = list.Count(e => e.student.sex);
        femaleCount = total - maleCount;
        setCellValues(row++, column, total,maleCount, femaleCount);
        
        list = studentList.Where(e => e.isWithInRange("AF", partial.partialId)).ToList();
        total = list.Count;
        maleCount = list.Count(e => e.student.sex);
        femaleCount = total - maleCount;
        setCellValues(row++, column, total,maleCount, femaleCount);
        
        list = studentList.Where(e => e.isWithInRange("AI", partial.partialId)).ToList();
        total = list.Count;
        maleCount = list.Count(e => e.student.sex);
        femaleCount = total - maleCount;
        setCellValues(row++, column, total,maleCount, femaleCount);
        
        total = studentList.Count;
        maleCount = studentList.Count(e => e.student.sex);
        femaleCount = total - maleCount;
        setCellValues(row, column, total,maleCount, femaleCount);
    }

    private void setSubjectDetail(int row, int column, string degreeId)
    {
        foreach (var item in subjectNameList)
        {
            var id = subjectList.FirstOrDefault(e => e.initials == item.initials && e.degreeId == degreeId)?.subjectId;
            if (id == null)
            {
                row++;
                continue;
            }
            
            var list = subjectPartialList.Where(e => e.subjectId == id).ToList();
            if (list.Count == 0)
            {
                row++;
                continue;
            }

            List<GradeEntity> approvedList = [];
            foreach (var sp in list)
            {
                approvedList.AddRange(sp.gradeList.Where(e => e.isApproved()).ToList());
            }

            var total = approvedList.Count;
            var male = approvedList.Count(e => e.student!.sex);
            var female = total - male;
            setCellValues(row++, column, total, male,  female);
        }
    }

    private void setCellValues(int row, int column, XLCellValue total, XLCellValue males, XLCellValue females)
    {
        worksheet.Cell(row, column).Value = total;
        worksheet.Cell(row, column + 1).Value = males;
        worksheet.Cell(row, column + 2).Value = females;

        worksheet.Cell(row, column).Style.Fill.BackgroundColor = blueColor;
    }

    private void degreeTitle(int row, int column, string value)
    {
        var rangeString = $"{getColumnName(column)}{row}:{getColumnName(column + 2)}{row}";
        setBorderByRange(rangeString);

        worksheet.Range(rangeString).Merge().Value = value.Split(" ")[0];
        worksheet.Cell(row + 1, column).Value = "Tot.";
        worksheet.Cell(row + 1, column + 1).Value = "Var.";
        worksheet.Cell(row + 1, column + 2).Value = "Muj.";
        
        rangeString = $"{getColumnName(column)}{row}:{getColumnName(column + 2)}{row + 1}";
        var headerStyle = worksheet.Range(rangeString);
        headerStyle.Style.Font.Bold = true;
        headerStyle.Style.Fill.BackgroundColor = grayColor;
        headerStyle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private void cell(int row, int column, string value)
    {
        var headerStyle = worksheet.Cell(row, column);
        headerStyle.Style.Font.Bold = true;
        headerStyle.Style.Fill.BackgroundColor = grayColor;
        headerStyle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerStyle.Value = value;
    }

    public class Builder
    {
        private readonly EvaluationStatisticsByLevelSheetBuilder sheetBuilder;

        public Builder()
        {
            sheetBuilder = new EvaluationStatisticsByLevelSheetBuilder();
        }

        public EvaluationStatisticsByLevelSheetBuilder build() => sheetBuilder;

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