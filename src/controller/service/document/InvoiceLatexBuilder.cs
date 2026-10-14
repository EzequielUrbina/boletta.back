using System.Text;
using wsmcbl.src.model.accounting;
using wsmcbl.src.utilities;

namespace wsmcbl.src.controller.service.document;

public class InvoiceLatexBuilder(string templatesPath, string outPath) : LatexBuilder(templatesPath, outPath)
{
    private StudentEntity student = null!;
    private TransactionEntity transaction = null!;
    private CashierEntity cashier = null!;
    private decimal[] generalBalance = null!;
    private int number;
    private string series = null!;
    private string exchangeRate = null!;
    private string password = string.Empty;
    
    private string schoolName = null!;
    private string schoolBusinessName = null!;
    private string schoolRuc = null!;
    private string schoolPhone = null!;
    private string schoolAddress = null!;


    protected override string getTemplateName() => "invoice";

    protected override string updateContent(string value)
    {
        var content = new StringBuilder(value);
        content.ReplaceInLatexFormat("numeration.value", $"{series}{number:00000000}");
        content.ReplaceInLatexFormat("customer.name.value", student.fullName());
        content.ReplaceInLatexFormat("student.pwd", password);
        content.ReplaceInLatexFormat("customer.id.value", student.studentId);
        content.Replace("detail.value", getDetail());
        content.ReplaceInLatexFormat("total.value", $"C$ {total:F2}");
        content.Replace("detail.other.value", getOtherDetailOrEmpty());
        content.ReplaceInLatexFormat("discount.value", getDiscountTotal());
        content.ReplaceInLatexFormat("arrears.value", getArrearsTotal());
        content.ReplaceInLatexFormat("total.aux.value", getAuxTotal());
        content.ReplaceInLatexFormat("total.final.value", $"C$ {transaction.total:F2}");
        content.Replace("balance.other.value", getBalanceOrEmpty());
        content.ReplaceInLatexFormat("cashier.value", cashier.getAlias());
        content.ReplaceInLatexFormat("datetime.value", transaction.date.toStringFull(false));
        content.ReplaceInLatexFormat("exchange.rate.value", exchangeRate);
        content.ReplaceInLatexFormat("general.balance.value", getGeneralBalance());
        
        content.ReplaceInLatexFormat("school.name", schoolName);
        content.ReplaceInLatexFormat("business.name", schoolBusinessName);
        content.ReplaceInLatexFormat("school.ruc", schoolRuc);
        content.ReplaceInLatexFormat("school.phone", schoolPhone);
        content.ReplaceInLatexFormat("school.address", schoolAddress);


        return content.ToString();
    }

    private string getArrearsTotal() => $"C$ {arrearsTotal:F2}";
    private string getDiscountTotal() => $"C$ {discountTotal:F2}";
    private string getAuxTotal() => $"C$ {total + arrearsTotal - discountTotal - detailTotal:F2}";

    private decimal discountTotal;
    private decimal arrearsTotal;
    private decimal total { get; set; }
    private decimal detailTotal;

    private string getDetail()
    {
        var content = "";
        
        foreach (var item in transaction.details)
        {
            discountTotal += item.discount;
            arrearsTotal += item.arrears;
            total += item.officialAmount();

            var tariffConcept = item.concept().ReplaceLatexSpecialSymbols();

            content = item.paidLate() ? $@"{content} *{tariffConcept} & C\$ {item.officialAmount():F2}\\" :
                $@"{content} {tariffConcept} & C\$ {item.officialAmount():F2}\\";
        }

        return content;
    }

    private string getGeneralBalance()
    {
        var value = generalBalance[1] - generalBalance[0];
        return $"C$ {value:F2}";
    }

    private string getBalanceOrEmpty()
    {
        var value = total + arrearsTotal - discountTotal - detailTotal;
        value -= transaction.total;

        return value > 0 ? $"\\multicolumn{{2}}{{l}}{{Pendiente: C\\$ {value:F2}}}\\\\" : string.Empty;
    }

    private string getOtherDetailOrEmpty()
    {
        var detailList = transaction.details.Where(e => e.debtBalance > 0).ToList();
        if (detailList.Count == 0)
        {
            return string.Empty;
        }
        
        var content = "\\multicolumn{2}{c}{\\textbf{Abonos realizados}}\\\\[2mm]";
        
        detailTotal = 0;
        foreach (var item in detailList)
        {
            detailTotal += item.debtBalance;
            var tariffConcept = item.concept().ReplaceLatexSpecialSymbols();
            content = $@"{content} {tariffConcept} & C\$ {item.debtBalance:F2}\\";
        }
        
        content = $"{content}[2mm] \\textbf{{Abonado}} & C\\$ {detailTotal:F2}\\\\\\hline";
        return content;
    }

    public class Builder
    {
        private readonly InvoiceLatexBuilder latexBuilder;

        public Builder(string templatesPath, string outPath)
        {
            latexBuilder = new InvoiceLatexBuilder(templatesPath, outPath);
        }

        public InvoiceLatexBuilder build() => latexBuilder;

        public Builder withStudent(StudentEntity parameter)
        {
            latexBuilder.student = parameter;
            return this;
        }
        
        public Builder withStudentPwd(string parameter)
        {
            latexBuilder.password = parameter;
            return this;
        }

        public Builder withTransaction(TransactionEntity parameter)
        {
            latexBuilder.transaction = parameter;
            return this;
        }

        public Builder withCashier(CashierEntity parameter)
        {
            latexBuilder.cashier = parameter;
            return this;
        }

        public Builder withGeneralBalance(decimal[] parameter)
        {
            latexBuilder.generalBalance = parameter;
            return this;
        }

        public Builder withNumber(int parameter)
        {
            latexBuilder.number = parameter;
            return this;
        }

        public Builder withSeries(string parameter)
        {
            latexBuilder.series = parameter;
            return this;
        }

        public Builder withExchangeRate(decimal parameter)
        {
            latexBuilder.exchangeRate = $"{parameter:F2}";
            return this;
        }
        
        public Builder withSchoolName(string parameter)
        {
            latexBuilder.schoolName = parameter;
            return this;
        }
        
        public Builder withSchoolRuc(string parameter)
        {
            latexBuilder.schoolRuc = parameter;
            return this;
        }
        
        public Builder withSchoolPhone(string parameter)
        {
            latexBuilder.schoolPhone = parameter;
            return this;
        }
        
        public Builder withSchoolBusnissName(string parameter)
        {
            latexBuilder.schoolBusinessName = parameter;
            return this;
        }
        
        public Builder withSchoolAddress(string parameter)
        {
            latexBuilder.schoolAddress = parameter;
            return this;
        }
    }
}