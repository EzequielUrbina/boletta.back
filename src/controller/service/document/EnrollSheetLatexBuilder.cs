using System.Globalization;
using System.Text;
using wsmcbl.src.model.secretary;
using wsmcbl.src.utilities;

namespace wsmcbl.src.controller.service.document;

public class EnrollSheetLatexBuilder(string templatesPath, string outputPath) : LatexBuilder(templatesPath, outputPath)
{
    private string? grade { get; set; }
    private string? userName { get; set; }
    private string? newSchoolyear { get; set; }
    private StudentEntity? entity { get; set; }
    private model.academy.StudentEntity? academyStudent { get; set; }

    private string schoolName { get; set; } = null!;
    private string schoolEmail { get; set; } = null!;
    
    protected override string getTemplateName() => "enroll-sheet";
    
    protected override string updateContent(string value)
    {
        var content = new StringBuilder(value);
        content.ReplaceInLatexFormat("schoolyear.value", newSchoolyear);
        
        content.ReplaceInLatexFormat("logo.value", $"{getImagesPath()}/logo.png");
        content.ReplaceInLatexFormat("enroll.date.value", academyStudent!.getCreateAtByDateOnly().toString("dddd dd/MMM/yyyy"));
        content.ReplaceInLatexFormat("student.name.value", entity!.fullName());
        content.ReplaceInLatexFormat("degree.value", grade!);
        content.ReplaceInLatexFormat("repeating.value", getTextByBool(academyStudent!.isRepeating));
        content.ReplaceInLatexFormat("mined.id.value", entity.minedId ?? "N/A");
        content.ReplaceInLatexFormat("student.age.value", $"{entity.getAge()} años");
        content.ReplaceInLatexFormat("student.sex.value", getTextBySex(entity.sex));
        content.ReplaceInLatexFormat("student.birthday.value", entity.birthday.toString("dd/MMMM/yyyy"));
        content.ReplaceInLatexFormat("tutor.value", entity.tutor.name);
        content.ReplaceInLatexFormat("diseases.value", entity.diseases!);
        content.ReplaceInLatexFormat("phones.value", entity.tutor.phone);
        content.ReplaceInLatexFormat("tutor.email.value", entity.tutor.email);
        content.ReplaceInLatexFormat("religion.value", entity.religion);
        content.ReplaceInLatexFormat("address.value", entity.address);
        
        content.ReplaceInLatexFormat("secretary.name.value", userName);
        content.ReplaceInLatexFormat("current.datetime.value", DateTime.UtcNow.toStringFull());
        content.ReplaceInLatexFormat("student.id.value", entity.studentId);
        content.ReplaceInLatexFormat("student.token.value", entity.accessToken);
        
        content.ReplaceInLatexFormat("school.name.value", schoolName);
        content.ReplaceInLatexFormat("school.email.value", schoolEmail);

        setParents(content, entity.parents);
        setFile(content, entity.file!);
        
        content.ReplaceInLatexFormat("current.year.value", getYearLabel());
        
        return content.ToString();
    }

    private static string getYearLabel()
    {
        var year = DateTime.Today.Month > 10 ? DateTime.Today.Year : DateTime.Today.Year - 1;
        return year.ToString();
    }
    
    private static void setParents(StringBuilder content, ICollection<StudentParentEntity>? entityParents)
    {
        if (entityParents == null)
        {
            setParent(content, "father", null);
            setParent(content, "mother", null);
            return;
        }
        
        var father = entityParents.FirstOrDefault(e => e.sex);
        setParent(content, "father", father);
        
        var mother = entityParents.FirstOrDefault(e => !e.sex);
        setParent(content, "mother", mother);
    }
    
    private static void setParent(StringBuilder content, string typeParent, StudentParentEntity? parent)
    {
        if (parent == null)
        {
            content.ReplaceInLatexFormat($"{typeParent}.name.value", "");
            content.ReplaceInLatexFormat($"{typeParent}.idcard.value", "");
            content.ReplaceInLatexFormat($"{typeParent}.occupation.value", "");
            return;
        }
        
        content.ReplaceInLatexFormat($"{typeParent}.name.value", parent.name);
        content.ReplaceInLatexFormat($"{typeParent}.idcard.value", parent.idCard!);
        content.ReplaceInLatexFormat($"{typeParent}.occupation.value", parent.occupation!);
    }

    private static void setFile(StringBuilder content, StudentFileEntity file)
    {
        content.ReplaceInLatexFormat("birth.document.value", getTextByBool(file.birthDocument));
        content.ReplaceInLatexFormat("conduct.document.value", getTextByBool(file.conductDocument));
        content.ReplaceInLatexFormat("financial.solvency.value", getTextByBool(file.financialSolvency));
        content.ReplaceInLatexFormat("parent.idcard.value", getTextByBool(file.parentIdentifier));
        content.ReplaceInLatexFormat("transfer.sheet.value", getTextByBool(file.transferSheet));
        content.ReplaceInLatexFormat("updated.grade.report.value", getTextByBool(file.updatedDegreeReport));
    }

    private static string getTextByBool(bool isSomething) => isSomething ? "Sí" : "No";
    private static string getTextBySex(bool sex) => sex ? "Hombre" : "Mujer";

    public class Builder
    {
        private readonly EnrollSheetLatexBuilder latexBuilder;

        public Builder(string templatesPath, string outPath)
        {
            latexBuilder = new EnrollSheetLatexBuilder(templatesPath, outPath)
            {
                entity = new StudentEntity()
            };
        }

        public EnrollSheetLatexBuilder build() => latexBuilder;
        
        public Builder withStudent(StudentEntity parameter)
        {
            latexBuilder.entity = parameter;
            return this;
        }
        
        public Builder withGrade(string parameter)
        {
            latexBuilder.grade = parameter;
            return this;
        }
        
        public Builder withUserAlias(string parameter)
        {
            latexBuilder.userName = parameter;
            return this;
        }
        
        public Builder withSchoolyear(string? parameter)
        {
            latexBuilder.newSchoolyear = parameter ?? string.Empty;
            return this;
        }
        
        public Builder withAcademyStudent(model.academy.StudentEntity parameter)
        {
            latexBuilder.academyStudent = parameter;
            return this;
        }
        
        public Builder withSchoolName(string parameter)
        {
            latexBuilder.schoolName = parameter;
            return this;
        }
        
        public Builder withSchoolEmail(string parameter)
        {
            latexBuilder.schoolEmail = parameter;
            return this;
        }
    }
}
