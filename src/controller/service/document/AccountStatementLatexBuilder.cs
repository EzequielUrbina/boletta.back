using System.Text;
using wsmcbl.src.model.accounting;
using wsmcbl.src.model.secretary;
using wsmcbl.src.utilities;
using StudentEntity = wsmcbl.src.model.accounting.StudentEntity;

namespace wsmcbl.src.controller.service.document;

public class AccountStatementLatexBuilder(string templatesPath, string outPath) : LatexBuilder(templatesPath, outPath)
{
    private StudentEntity student { get; set; } = null!;
    private string userAlias { get; set; } = null!;
    private SchoolyearEntity schoolyear { get; set; } = null!;
    private List<SchoolyearEntity> schoolyearList { get; set; } = null!;
    private string SchoolName { get; set; } = null!;
    private string SchoolEmail { get; set; } = null!;
    private string SchoolPhone { get; set; } = null!;
    
    protected override string getTemplateName() => "account-statement";

    protected override string updateContent(string value)
    {
        var content = new StringBuilder(value);
        
        content.Replace("logo.value", $"{getImagesPath()}/logo.png");
        
        content.Replace("schoolyear.value", schoolyear.label);
        content.Replace("level.value", DegreeDataEntity.getLevelName(student.educationalLevel));
        
        content.Replace("student.id.value", student.studentId);
        content.Replace("student.name.value", student.fullName());
        
        content.Replace("body.value", getDebtBody());
        setPastYearDebts(content);
        content.Replace("total.value", $"{total:#,0.00}");
        content.Replace("other.value", "");
        
        content.Replace("user.alias.value", userAlias);
        content.Replace("current.datetime.value", DateTime.UtcNow.toStringFull());
        content.Replace("current.date.value", DateTime.UtcNow.toString());

        content.Replace("school.phone.value", SchoolPhone);
        content.Replace("school.email.value", SchoolEmail);
        content.Replace("school.name.value", SchoolName);

        return content.ToString();
    }

    private void setPastYearDebts(StringBuilder content)
    {       
        var list = student.debtHistory!.Where(debt => debt.schoolyear != schoolyear.id)
            .Where(debt => debt is { isPaid: false, tariff.type: 1 }).ToList();

        if (list.Count == 0)
        {
            return;
        }
        
        content.Replace("\\togglefalse{showTable}", "\\toggletrue{showTable}");
        content.Replace("body.past.years.value", getPastYearsDebtBody(list));
    }

    private string getPastYearsDebtBody(List<DebtHistoryEntity> list)
    {
        var sb = new StringBuilder();        
        foreach (var item in list)
        {
            var tariff = item.tariff;
            var label = schoolyearList.FirstOrDefault(e => e.id == item.schoolyear)?.label;
            
            sb.Append($"{label} & {tariff.concept} & ");
            sb.Append($"{tariff.amount:#,0.00} & {item.calculateDiscount():#,0.00} &");
            sb.Append($"{item.arrears:#,0.00} & {item.amount:#,0.00} &");
            sb.Append($"{item.getDebtBalance():#,0.00} C\\$ \\\\ \n");
            total += item.getDebtBalance();
        }

        return sb.ToString();
    }

    private decimal total { get; set; }
    private string getDebtBody()
    {
        var sb = new StringBuilder();
        var list = student.debtHistory!
            .Where(e => e.tariff.type == 1 && e.schoolyear == schoolyear.id);
        
        foreach (var item in list)
        {
            var tariff = item.tariff;
            sb.Append($"{tariff.concept} & {tariff.dueDate.toString()} & ");
            sb.Append($"{tariff.amount:#,0.00} & {item.calculateDiscount():#,0.00} &");
            sb.Append($"{item.arrears:#,0.00} & {item.amount:#,0.00} &");
            sb.Append($"{item.getDebtBalance():#,0.00} C\\$ \\\\ \n");
            total += item.getDebtBalance();
        }

        return sb.ToString();
    }
    
    public class Builder
    {
        private readonly AccountStatementLatexBuilder latexBuilder;

        public Builder(string templatesPath, string outPath)
        {
            latexBuilder = new AccountStatementLatexBuilder(templatesPath, outPath);
        }

        public AccountStatementLatexBuilder build() => latexBuilder;

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
        
        public Builder withSchoolyear(SchoolyearEntity parameter)
        {
            latexBuilder.schoolyear = parameter;
            return this;
        }

        public Builder withSchoolyearList(List<SchoolyearEntity> parameter)
        {
            latexBuilder.schoolyearList = parameter;
            return this;
        }
        
        public Builder whitSchoolName(string parameter)
        {
            latexBuilder.SchoolName = parameter;
            return this;
        }
        
        public Builder whitSchoolEmail(string parameter)
        {
            latexBuilder.SchoolEmail = parameter;
            return this;
        }
        
        public Builder whitSchoolPhone(string parameter)
        {
            latexBuilder.SchoolPhone = parameter;
            return this;
        }
    }
}