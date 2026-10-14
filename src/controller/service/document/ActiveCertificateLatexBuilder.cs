using System.Text;
using wsmcbl.src.model.academy;
using wsmcbl.src.utilities;

namespace wsmcbl.src.controller.service.document;

public class ActiveCertificateLatexBuilder(string templatesPath, string outPath) : LatexBuilder(templatesPath, outPath)
{
    private StudentEntity student { get; set; } = null!;
    private string enrollmentLabel { get; set; } = null!;
    private string level { get; set; } = null!;
    private string? userAlias { get; set; }
    private string schoolyear { get; set; } = null!;
    
    private string schoolName { get; set; } = null!;
    private string schoolEmail { get; set; } = null!;
    private string schoolPhone { get; set; } = null!;
    private string schoolAddress { get; set; } = null!;
    private string schoolSecretary { get; set; } = null!;


    protected override string getTemplateName() => "active-certificate";

    protected override string updateContent(string value)
    {
        var content = new StringBuilder(value);
        
        content.Replace("logo.value", $"{getImagesPath()}/logo.png");
        
        content.Replace("schoolyear.value", schoolyear);
        content.Replace("enrollment.value", enrollmentLabel.ToUpper());
        content.Replace("level.value", level);
        
        content.Replace("mined.id.value", student.student.minedId.getOrDefault());
        content.Replace("student.name.value", student.fullName().ToUpper());
        content.Replace("secretary.name.value", Utility.generalSecretary);
        
        content.Replace("user.alias.value", userAlias);
        content.Replace("current.date.value", DateTime.UtcNow.toString("d 'de' MMMM 'de' yyyy"));
        content.Replace("current.datetime.value", DateTime.UtcNow.toStringFull());
        
        content.Replace("school.name.value", schoolName);
        content.Replace("school.email.value", schoolEmail);
        content.Replace("school.phone.value", schoolPhone);
        content.Replace("school.address.value", schoolAddress);
        content.Replace("school.secretary.value", schoolSecretary);

        return content.ToString();
    }

    public class Builder
    {
        private readonly ActiveCertificateLatexBuilder latexBuilder;

        public Builder(string templatesPath, string outPath)
        {
            latexBuilder = new ActiveCertificateLatexBuilder(templatesPath, outPath);
        }

        public ActiveCertificateLatexBuilder build() => latexBuilder;

        public Builder withStudent(StudentEntity parameter)
        {
            latexBuilder.student = parameter;
            return this;
        }
        
        public Builder withEnrollment(string parameter)
        {
            latexBuilder.enrollmentLabel = parameter;
            return this;
        }
        
        public Builder withUserAlias(string? parameter)
        {
            latexBuilder.userAlias = parameter;
            return this;
        }
        
        public Builder withSchoolyear(string parameter)
        {
            latexBuilder.schoolyear = parameter;
            return this;
        }
        
        public Builder withSchoolName(string parameter)
        {
            latexBuilder.schoolName = parameter;
            return this;
        }
        
        public Builder withSchoolMail(string parameter)
        {
            latexBuilder.schoolEmail = parameter;
            return this;
        }
        
        public Builder withSchoolPhone(string parameter)
        {
            latexBuilder.schoolPhone = parameter;
            return this;
        }
        
        public Builder withSchoolAddress(string parameter)
        {
            latexBuilder.schoolAddress = parameter;
            return this;
        }
        
        public Builder withSchoolSecretary(string parameter)
        {
            latexBuilder.schoolSecretary = parameter;
            return this;
        }
        
        public Builder withLevel(string parameter)
        {
            latexBuilder.level = parameter;
            return this;
        }
    }
}