using System.Text;
using wsmcbl.src.model;
using wsmcbl.src.model.academy;
using wsmcbl.src.model.secretary;
using wsmcbl.src.utilities;

namespace wsmcbl.src.controller.service.document;

public class OfficialEnrollmentListLatexBuilder : LatexBuilder
{
    private string userName { get; set; } = null!;
    private List<TeacherEntity> teacherList { get; set; } = null!;
    private List<DegreeEntity> degreeList { get; set; } = null!;
    private DateTime now { get; set; }
    
    private string schoolName { get; set; } = null!;
    private string schoolMail { get; set; } = null!;


    private OfficialEnrollmentListLatexBuilder(string templatesPath, string outPath) : base(templatesPath, outPath)
    {
        now = DateTime.UtcNow;
    }
    
    protected override string getTemplateName() => "official-enrollment-list";
    
    protected override string updateContent(string value)
    {
        var content = new StringBuilder(value);
        content.ReplaceInLatexFormat("logo.value", $"{getImagesPath()}/logo.png");
        content.ReplaceInLatexFormat("year.value", DateTime.Today.Year.ToString());
        content.ReplaceInLatexFormat("school.name.value", schoolName);
        content.ReplaceInLatexFormat("school.email.value", schoolMail);

        var body = new StringBuilder();
        foreach (var item in degreeList.OrderBy(e => e.getTag()))
        {
            getDegreeContent(body, item);
        }
        
        content.Replace("body.value", body.ToString());
        
        return content.ToString();
    }

    private void getDegreeContent(StringBuilder body, DegreeEntity degree)
    {
        var list = degree.enrollmentList!.Where(e => e.studentList!.Count != 0).ToList();
        foreach (var item in list.OrderBy(e => e.tag))
        {
            getEnrollmentContent(body, item);
        }
    }

    private void getEnrollmentContent(StringBuilder body, EnrollmentEntity enrollment)
    {
        body.Append($"\\begin{{center}}\n\\textbf{{\\large {enrollment.label}}}\n\\end{{center}}\n");
        body.Append($"\\textbf{{Docente guía}}: \\aField{{{getTeacherName(enrollment.teacherId)}}}");
        body.Append($"\\hfill \\textbf{{Fecha}}: {now.toString()}\n");
        body.Append($"\\footnotetext{{Impreso por Boletta el {now.toStringFull()}, {userName}.}}\n");

        body.Append("\\begin{longtable}{| c || l || p{\\dimexpr\\textwidth-6cm\\relax} |}\n");
        body.Append("\\hline\\textbf{N\u00b0} & \\textbf{Código} & \\textbf{Nombre}\\\\\\hline\\hline\n");
        
        var counter = 0;
        var womanList = enrollment.studentList!.Where(e => !e.student.sex).ToList();
        foreach (var item in womanList.OrderBy(e => e.fullName()))
        {
            counter++;
            body.Append($"{counter} & {item.studentId} & {item.fullName()}\\\\\\hline\n");
        }

        body.Append("\\hline\n");
        
        var menList = enrollment.studentList!.Where(e => e.student.sex).ToList();
        foreach (var item in menList.OrderBy(e => e.fullName()))
        {
            counter++;
            body.Append($"{counter} & {item.studentId} & {item.fullName()}\\\\\\hline\n");
        }

        body.Append("\\end{longtable}\n\n");
        body.Append("Si encuentra algún error, por favor repórtelo a secretaría para realizar la corrección.\n");
        body.Append($"Esta es la lista oficial de {enrollment.label}, si alguna hoja de matrícula no coincide, es posible que dicha hoja no esté actualizada.\n");
        
        if (counter > 24)
        {
            body.Append($"\\footnotetext{{Impreso por wsmcbl el {now.toStringFull()}, {userName}.}}\n");
        }

        body.Append("\\newpage\n");

        body.Append("\\setcounter{page}{1}\n\n\n");
    }

    private string getTeacherName(string? teacherId)
    {
        var teacher = teacherList.FirstOrDefault(e => e.teacherId == teacherId);
        return teacher != null ? teacher.fullName() : "Sin asignar";
    }

    public class Builder
    {
        private readonly OfficialEnrollmentListLatexBuilder latexBuilder;

        public Builder(string templatesPath, string outPath)
        {
            latexBuilder = new OfficialEnrollmentListLatexBuilder(templatesPath, outPath);
        }

        public OfficialEnrollmentListLatexBuilder build() => latexBuilder;

        public Builder withDegreeList(List<DegreeEntity> parameter)
        {
            latexBuilder.degreeList = parameter;
            return this;
        }

        public Builder withTeacherList(List<TeacherEntity> parameter)
        {
            latexBuilder.teacherList = parameter.Where(e => e.teacherId != Const.DefaultTeacherId).ToList();
            return this;
        }

        public Builder withUserName(string parameter)
        {
            latexBuilder.userName = parameter;
            return this;
        }
        
        public Builder withSchoolName(string parameter)
        {
            latexBuilder.schoolName = parameter;
            return this;
        }
        
        public Builder withSchoolMail(string parameter)
        {
            latexBuilder.schoolMail = parameter;
            return this;
        }
    }
}