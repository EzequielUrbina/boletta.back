using System.Text;
using wsmcbl.src.model.academy;
using wsmcbl.src.model.secretary;
using wsmcbl.src.utilities;
using StudentEntity = wsmcbl.src.model.academy.StudentEntity;
using SubjectEntity = wsmcbl.src.model.academy.SubjectEntity;

namespace wsmcbl.src.controller.service.document;

public class AcademicRecordLatexBuilder(string templatesPath, string outPath) : LatexBuilder(templatesPath, outPath)
{
    private string schoolName { get; set; } = null!;
    private string schoolEmail { get; set; } = null!;
    private string userAlias { get; set; } = null!;
    private string enrollmentLabel { get; set; } = null!;
    private string schoolyearLabel { get; set; } = null!;
    
    private StudentEntity student { get; set; } = null!;
    private List<SubjectEntity> subjectList { get; set; } = null!;
    private List<SubjectAreaEntity> subjectAreaList { get; set; } = null!;
    private List<PartialEntity> partialList { get; set; } = null!;
    
    protected override string getTemplateName() => "academic-record";

    protected override string updateContent(string value)
    {
        var content = new StringBuilder(value);
        
        content.Replace("logo.value", $"{getImagesPath()}/Boletta500x500.png");
        
        content.Replace("schoolyear.value", schoolyearLabel);
        
        content.Replace("student.id.value", student.studentId);
        content.Replace("student.name.value", student.fullName());
        content.Replace("mined.id.value", student.student.minedId.getOrDefault());
        content.Replace("enrollment.value", enrollmentLabel.ToUpper());
        
        content.Replace("detail.value", getDetail());

        var averageList = getAverageList();
        content.Replace("first.average.value", averageList[0]);
        content.Replace("second.average.value", averageList[1]);
        content.Replace("third.average.value", averageList[2]);
        content.Replace("fourth.average.value", averageList[3]);
        content.Replace("final.average.value", averageList[4]);
        
        content.Replace("user.alias.value", userAlias);
        content.Replace("current.date.value", DateTime.UtcNow.toString());
        content.Replace("current.datetime.value", DateTime.UtcNow.toStringFull());
        
        content.Replace("school.name.value", schoolName);
        content.Replace("school.email.value", schoolEmail);

        return content.ToString();
    }
    
    private List<string> getAverageList()
    {
        var result = new List<string>();

        var quantity = 0;
        var gradeAccumulator = 0m;
        
        foreach (var partial in partialList)
        {
            var grade = student.averageList?.FirstOrDefault(e => e.partialId == partial.partialId);

            if (grade != null)
            {
                quantity++;
                gradeAccumulator += grade.grade;
                result.Add($"{grade.grade:F2}");
            }
            else
            {
                result.Add("");
            }
        }

        var average = quantity is 2 or 4 ? $"{gradeAccumulator / quantity:F2}" : string.Empty;  
        result.Add(average);
        return result;
    }

    private string getDetail()
    {
        var content = new StringBuilder();
        
        foreach (var item in subjectAreaList)
        {
            if (subjectList.All(e => e.secretarySubject!.areaId != item.areaId))
            {
                continue;
            }
            
            content.Append($"\\multicolumn{{11}}{{|l|}}{{\\textbf{{\\footnotesize {item.name}}}}}\\\\\\hline");
            getSubjectDetail(content, item.areaId);
        }

        addConductGrade(content);
        
        return content.ToString();
    }

    private void addConductGrade(StringBuilder content)
    {
        content.Append("{{\\footnotesize Conducta}}");

        var conduct = 0m;
        var quantity = 0;

        foreach (var partial in partialList)
        {
            var grade = student.averageList?.FirstOrDefault(e => e.partialId == partial.partialId);
            if (grade == null)
            {
                content.Append(gradeFormat(null));
                continue;
            }
            
            quantity++;
            conduct += grade.conductGrade;
            content.Append(gradeFormat(grade.conductGrade));
        }

        content.Append(quantity is 2 or 4 ? gradeFormat(conduct / quantity) : gradeFormat(null));
        content.Append("\\\\\\hline");
    }
    
    private void getSubjectDetail(StringBuilder content, int areaId)
    {
        var list = subjectList.Where(e => e.secretarySubject!.areaId == areaId)
            .OrderBy(e => e.secretarySubject!.number).ToList();
            
        foreach (var item in list)
        {
            content.Append($"{{\\footnotesize {item.secretarySubject!.name}}}");
            getGrades(content, item.subjectId);
            content.Append("\\\\\\hline");
        }
    }

    private void getGrades(StringBuilder content, string subjectId)
    {
        var quantity = 0;
        var gradeAccumulator = 0m;
        
        foreach (var partial in partialList)
        {
            var grade = student.gradeList?.FirstOrDefault(e => e.partialId == partial.partialId && e.subjectId == subjectId);
            if (grade == null)
            {
                content.Append(gradeFormat(null));
                continue;
            }

            content.Append(gradeFormat(grade.grade, grade.label));
            
            quantity++;
            gradeAccumulator += grade.grade;
        }

        var average = quantity is 2 or 4 ? gradeFormat(gradeAccumulator / quantity) : gradeFormat(null);
        content.Append(average);
    }
    
    private static string gradeFormat(decimal? grade, string? label = null)
    {
        if (grade == null)
        {
            return " & & ";
        }

        label ??= GradeEntity.getLabelByGrade((decimal)grade);
        
        return $" & {{\\footnotesize {label}}} & {{\\footnotesize {grade:F2}}}";
    }
    
    public class Builder
    {
        private readonly AcademicRecordLatexBuilder latexBuilder;

        public Builder(string templatesPath, string outPath)
        {
            latexBuilder = new AcademicRecordLatexBuilder(templatesPath, outPath);
        }

        public AcademicRecordLatexBuilder build() => latexBuilder;

        public Builder withStudent(StudentEntity parameter)
        {
            latexBuilder.student = parameter;
            return this;
        }
        
        public Builder withUserAlias(string parameter)
        {
            latexBuilder.userAlias = parameter;
            return this;
        }
        
        public Builder withSchoolyearLabel(string parameter)
        {
            latexBuilder.schoolyearLabel = parameter;
            return this;
        }
        
        public Builder withSubjectList(List<SubjectEntity> parameter)
        {
            latexBuilder.subjectList = parameter;
            return this;
        }
        
        public Builder withSubjectAreaList(List<SubjectAreaEntity> parameter)
        {
            latexBuilder.subjectAreaList = parameter;
            return this;
        }
        
        public Builder withPartialList(List<PartialEntity> parameter)
        {
            latexBuilder.partialList = parameter
                .OrderBy(e => e.semester)
                .ThenBy(e => e.partial)
                .ToList();
            
            return this;
        }
        
        public Builder withEnrollmentLabel(string parameter)
        {
            latexBuilder.enrollmentLabel = parameter;
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