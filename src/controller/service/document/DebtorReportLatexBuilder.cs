using System.Text;
using Microsoft.Extensions.Primitives;
using wsmcbl.src.model.accounting;
using wsmcbl.src.model.secretary;
using wsmcbl.src.utilities;

namespace wsmcbl.src.controller.service.document;

public class DebtorReportLatexBuilder : LatexBuilder
{
    private DateTime now { get; set; }
    private string userName { get; set; } = null!;
    private string schoolName { get; set; } = null!;
    private string schoolEmail { get; set; } = null!;
    private List<DebtorStudentView> studentList { get; set; } = null!;
    private List<DegreeEntity> degreeList { get; set; } = null!;

    private DegreeEntity? aDegree { get; set; }

    private DebtorReportLatexBuilder(string templatesPath, string outPath) : base(templatesPath, outPath)
    {
        now = DateTime.UtcNow;
    }

    protected override string getTemplateName() => "debtor-report";

    protected override string updateContent(string value)
    {
        var content = new StringBuilder(value);
        content.ReplaceInLatexFormat("logo.value", $"{getImagesPath()}/logo.png");
        content.ReplaceInLatexFormat("year.value", DateTime.Today.Year.ToString());
        content.ReplaceInLatexFormat("today.value", now.toString());
        content.ReplaceInLatexFormat("school.name.value", schoolName);
        content.ReplaceInLatexFormat("school.email.value", schoolEmail);
        content.Replace("body.value", getDegreeContent());

        return content.ToString();
    }

    private decimal getTotal(int value)
    {
        return value switch
        {
            1 => studentList.Where(e => e.schoolyearId == aDegree!.schoolyearId).Sum(item => item.total),
            2 => getTotal(0) - getTotal(1),
            _ => studentList.Sum(item => item.total)
        };
    }

    private string getDegreeContent()
    {
        if (studentList.Count == 0)
        {
            return "\\begin{center}\n\\textbf{\\large No hay deudores}\n\\end{center}\n";
        }
        
        var std = studentList.First(e => e.schoolyearId == aDegree!.schoolyearId);

        var body = new StringBuilder();
        body.Append($"\\textbf{{Año lectivo {std.schoolyear}}} \\hfill\\textbf{{Total:}} C\\$ {getTotal(1):N2}");
        body.Append("\\begin{longtable}{| l | l | l | c | l |}\n\\hline ");
        body.Append("\\textbf{N\u00b0} & \\textbf{Código} & \\textbf{Nombre} & \\textbf{Cant.} & \\textbf{Total}\\\\\\hline\n");

        foreach (var degree in degreeList.OrderBy(e => e.tag))
        {
            foreach (var enrollment in degree.enrollmentList!.OrderBy(e => e.tag))
            {
                var list = studentList
                    .Where(e => e.enrollmentId == enrollment.enrollmentId).ToList();
                if (list.Count == 0)
                {
                    continue;
                }

                body.Append($"\\multicolumn{{5}}{{l}}{{\\textbf{{\\small -- {enrollment.label} --}}}}\\\\\\hline\n");
                body.Append(getEnrollmentContent(list));
            }
        }
        body.Append("\\end{longtable}\n\n");
        
        body.Append(getFromOtherSchoolyear());
        
        body.Append($"\\hfill\\textbf{{Super total:}} C\\$ {getTotal(0):N2}");
        body.Append($"\\footnotetext{{Impreso por Boletta el {now.toStringFull()}, {userName}.}}\n");

        return body.ToString();
    }
    
    private string getFromOtherSchoolyear()
    {
        var list = studentList.Where(e => e.schoolyearId != aDegree!.schoolyearId).ToList();
        if (list.Count == 0)
        {
            return string.Empty;
        }

        var body = new StringBuilder();
        
        body.Append($"\\textbf{{Año lectivo anteriores}}\\hfill\\textbf{{Total:}} C\\$ {getTotal(2):N2}");
        body.Append("\\begin{longtable}{| l | l | l | c | l |}\n\\hline ");
        body.Append(
            "\\textbf{N\u00b0} & \\textbf{Código} & \\textbf{Nombre} & \\textbf{Cant.} & \\textbf{Total}\\\\\\hline\n");
        body.Append(getEnrollmentContent(list));
        body.Append("\\end{longtable}\n\n");

        return body.ToString();
    }

    private int counter;

    private string getEnrollmentContent(List<DebtorStudentView> list)
    {
        var result = new StringBuilder();
        foreach (var item in list.OrderBy(e => e.fullName))
        {
            counter++;
            result.Append($"{counter} & {item.studentId} & {item.fullName} & {item.quantity} & C\\$ {item.total:N2}\\\\\\hline\n");
        }

        return result.ToString();
    }

    public class Builder
    {
        private readonly DebtorReportLatexBuilder latexBuilder;

        public Builder(string templatesPath, string outPath)
        {
            latexBuilder = new DebtorReportLatexBuilder(templatesPath, outPath);
        }

        public DebtorReportLatexBuilder build() => latexBuilder;

        public Builder withStudentList(List<DebtorStudentView> parameter)
        {
            latexBuilder.studentList = parameter;
            return this;
        }

        public Builder withDegreeList(List<DegreeEntity> parameter)
        {
            latexBuilder.degreeList = parameter;
            latexBuilder.aDegree = latexBuilder.degreeList.FirstOrDefault();
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

        public Builder withSchoolEmail(string parameter)
        {
            latexBuilder.schoolEmail = parameter;
            return this;
        }
    }
}