using System.Text;
using wsmcbl.src.model.academy;
using wsmcbl.src.model.secretary;
using wsmcbl.src.utilities;
using StudentEntity = wsmcbl.src.model.academy.StudentEntity;
using SubjectEntity = wsmcbl.src.model.academy.SubjectEntity;

namespace wsmcbl.src.controller.service.document;

public class ReportCardLatexBuilder(string templatesPath, string outPath, string templateName = "report-card") : LatexBuilder(templatesPath, outPath)
{   
    private StudentEntity student { get; set; } = null!;
    private TeacherEntity teacher { get; set; } = null!;
    private List<SubjectEntity> subjectList { get; set; } = null!;
    private List<SubjectAreaEntity> subjectAreaList { get; set; } = null!;
    private List<PartialEntity> partialList { get; set; } = null!;
    
    private string degreeLabel { get; set; } = null!;
    private string? educationLevel { get; set; }
    private string? userAlias { get; set; }
    private string schoolyear { get; set; } = null!;
    
    private string schoolName { get; set; } = null!;
    private string schoolPrincipal { get; set; } = null!;
    private string schoolAddress { get; set; } = null!;
    private string schoolMail { get; set; } = null!;

    
    private readonly string _templateName = templateName;
    protected override string getTemplateName() => _templateName;

    protected override string updateContent(string value)
    {
        var content = new StringBuilder(value);
        
        // Reemplazos universales (Compatibles con ambos formatos)
        content.Replace("logo.value", $"{getImagesPath()}/logo.png");
        content.Replace("schoolyear.value", schoolyear);
        content.Replace("degree.value", degreeLabel);
        
        content.Replace("student.id.value", student.studentId);
        content.Replace("student.name.value", student.fullName());
        content.Replace("teacher.name.value", teacher.fullName());
        content.Replace("teacher.mail.value", teacher.user.email);
        
        content.Replace("school.name.value", schoolName); 
        content.Replace("school.email.value", schoolMail); 

        
        // 4. CONDICIONAL DE SEGURIDAD: Solo si es la nueva plantilla, agregamos sus metadatos
        if (_templateName == "grade-report")
        {
            content.Replace("mined.id.value", (student.student != null && !string.IsNullOrEmpty(student.student.minedId)) ? student.student.minedId : student.studentId);
            content.Replace("principal.name.value", schoolPrincipal); 
            content.Replace("school.address.value", schoolAddress); 
            content.Replace("educational.level.value", educationLevel); 
            content.Replace("shift.value", "Matutino");
            content.Replace("department.value", "Managua");
            content.Replace("municipality.value", "Managua");
            content.Replace("school.id.value", "14484");
        }

        content.Replace("detail.value", getDetail());

        var averageList = getAverageList();
        content.Replace("first.average.value", averageList[0]);
        content.Replace("second.average.value", averageList[1]);
        content.Replace("third.average.value", averageList[2]);
        content.Replace("fourth.average.value", averageList[3]);
        content.Replace("final.average.value", averageList[4]);
        
        content.Replace("secretary.name.value", userAlias != null ? $", {userAlias}" : string.Empty);
        content.Replace("current.datetime.value", DateTime.UtcNow.toStringFull());

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
            getGradesBySubject(content, item.subjectId);
            content.Append("\\\\\\hline");
        }
    }

    private void getGradesBySubject(StringBuilder content, string subjectId)
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
        private readonly string _templatesPath;
        private readonly string _outPath;
        private string _templateName = "report-card"; // Plantilla base por defecto

        // Datos de estado del Builder
        private StudentEntity _student = null!;
        private TeacherEntity _teacher = null!;
        private string _degreeLabel = null!;
        private List<PartialEntity> _partialList = null!;
        private List<SubjectEntity> _subjectList = null!;
        private List<SubjectAreaEntity> _subjectAreaList = null!;
        private string? _userAlias;
        private string? _educationLevel;
        private string _schoolyear = null!;
        private string _schoolName = null!;
        private string _schoolEmail = null!;
        private string _schoolAddress = null!;
        private string _schoolPrincipal = null!;

        public Builder(string templatesPath, string outPath)
        {
            _templatesPath = templatesPath;
            _outPath = outPath;
        }

        // NUEVO MÉTODO FLUIDO: Permite cambiar la plantilla desde afuera
        public Builder withTemplate(string templateName)
        {
            _templateName = templateName;
            return this;
        }

        public ReportCardLatexBuilder build()
        {
            // Instanciamos pasando los 3 parámetros al constructor primario
            var latexBuilder = new ReportCardLatexBuilder(_templatesPath, _outPath, _templateName);
            
            // Seteamos las propiedades acumuladas
            latexBuilder.student = _student;
            latexBuilder.teacher = _teacher;
            latexBuilder.degreeLabel = _degreeLabel;
            latexBuilder.educationLevel = _educationLevel;
            latexBuilder.partialList = _partialList;
            latexBuilder.subjectList = _subjectList;
            latexBuilder.subjectAreaList = _subjectAreaList;
            latexBuilder.userAlias = _userAlias;
            latexBuilder.schoolyear = _schoolyear;
            latexBuilder.schoolName = _schoolName;
            latexBuilder.schoolAddress = _schoolAddress;
            latexBuilder.schoolPrincipal = _schoolPrincipal;
            latexBuilder.schoolMail = _schoolEmail;

            return latexBuilder;
        }

        public Builder withStudent(StudentEntity parameter)
        {
            _student = parameter;
            return this;
        }

        public Builder withTeacher(TeacherEntity parameter)
        {
            _teacher = parameter;
            return this;
        }
        
        public Builder withDegree(string parameter)
        {
            _degreeLabel = parameter;
            return this;
        }
        
        public Builder withPartialList(List<PartialEntity> parameter)
        {
            _partialList = parameter
                .OrderBy(e => e.semester)
                .ThenBy(e => e.partial)
                .ToList();
            return this;
        }
        
        public Builder withSubjectList(List<SubjectEntity> parameter)
        {
            _subjectList = parameter;
            return this;
        }
        
        public Builder withSubjectAreaList(List<SubjectAreaEntity> parameter)
        {
            _subjectAreaList = parameter;
            return this;
        }
        
        public Builder withUserAlias(string? parameter)
        {
            _userAlias = parameter;
            return this;
        }
        
        public Builder withSchoolyear(string parameter)
        {
            _schoolyear = parameter;
            return this;
        }
        
        public Builder withEducationLevel(string parameter)
        {
            _educationLevel = parameter;
            return this;
        }
        
        public Builder withSchoolName(string parameter)
        {
            _schoolName = parameter;
            return this;
        }
        
        public Builder withSchoolEmail(string parameter)
        {
            _schoolEmail = parameter;
            return this;
        }
        
        public Builder withSchoolAddress(string parameter)
        {
            _schoolAddress = parameter;
            return this;
        }
        
        public Builder withSchoolPrincipal(string parameter)
        {
            _schoolPrincipal = parameter;
            return this;
        }
    }
}