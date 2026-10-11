using System.Diagnostics;
using System.IO;
using wsmcbl.src.exception;
using wsmcbl.src.model.dao;
using wsmcbl.src.model.secretary;

namespace wsmcbl.src.controller.service.document;

public class DocumentMaker(DaoFactory daoFactory) : PdfMaker
{
    public async Task<string> getUserAlias(string userId) => (await daoFactory.userDao!.getById(userId)).getAlias();

    // NUEVO MÉTODO MASIVO OPTIMIZADO (Zero-Dependencies) - Versión Producción
    public async Task<byte[]> GetReportsCardsByEnrollments(string enrollmentId, string? userAlias)
    {
        // 1. Obtener metadatos comunes de la matrícula y año escolar
        var teacher = await daoFactory.teacherDao!.getByEnrollmentId(enrollmentId);
        if (teacher == null)
        {
            throw new EntityNotFoundException($"Teacher with enrollmentId ({enrollmentId}) not found.");
        }

        var enrollment = await daoFactory.enrollmentDao!.getById(enrollmentId);
        var educativonLevel = await daoFactory.degreeDao!.getByEnrollmentId(enrollmentId);
        var schoolyear = await daoFactory.schoolyearDao!.getCurrent();
        var partialList = await daoFactory.partialDao!.getListForCurrentSchoolyear();

        var currentPartial = partialList.LastOrDefault();
        if (currentPartial == null)
        {
            throw new ConflictException("No active partial periods found.");
        }

        // 2. Trae a los 34 alumnos con sus notas de una sola consulta masiva limpia
        var baseStudentList = await daoFactory.academyStudentDao!
            .getListWithAllGradesByEnrollmentId(enrollmentId);

        if (baseStudentList == null || !baseStudentList.Any())
        {
            throw new EntityNotFoundException($"No students found for enrollmentId ({enrollmentId}).");
        }
        
        // 3. Ordenamos alfabéticamente para mantener la consistencia institucional
        var orderedStudentList = baseStudentList
            .OrderBy(e => e.student.sex)
            .ThenBy(e => e.student.name)
            .ToList();

        // 4. Catálogos globales para optimizar el Builder
        var allSubjectAreas = await daoFactory.subjectAreaDao!.getAll();
        var allSubjects = await daoFactory.academySubjectDao!.getByEnrollmentId(enrollmentId);

        // 5. Directorio temporal único para el lote completo (Batch)
        string batchId = Guid.NewGuid().ToString();
        string tempBatchFolder = Path.Combine(resource, "out", $"batch_{batchId}");
        Directory.CreateDirectory(tempBatchFolder);

        List<string> absolutePdfPaths = new();
        List<string> directoriesToClean = new();

        try
        {
            // 6. Generar los PDFs utilizando los datos ya cargados en memoria
            foreach (var studentWithAllGrades in orderedStudentList)
            {
                if (studentWithAllGrades == null) continue;

                // Creamos un aislamiento total en disco para este estudiante específico
                string studentSpecificFolder = Path.Combine(tempBatchFolder, studentWithAllGrades.studentId);
                Directory.CreateDirectory(studentSpecificFolder);
                directoriesToClean.Add(studentSpecificFolder);

                var latexBuilder = new ReportCardLatexBuilder.Builder(resource, studentSpecificFolder)
                    .withTemplate("grade-report") // <-- Forzamos el uso de tu nuevo diseño .tex
                    .withStudent(studentWithAllGrades) // <-- Usamos el objeto en memoria (Sin pegarle a la DB)
                    .withTeacher(teacher)
                    .withUserAlias(userAlias)
                    .withDegree(enrollment!.label)
                    .withEducationLevel(educativonLevel?.educationalLevel ?? "")
                    .withPartialList(partialList)
                    .withSchoolyear(schoolyear.label)
                    .withSubjectAreaList(allSubjectAreas)
                    .withSubjectList(allSubjects)
                    .build();

                string studentOutPath = latexBuilder.getOutPath();
                if (!directoriesToClean.Contains(studentOutPath))
                {
                    directoriesToClean.Add(studentOutPath);
                }

                // Seteamos el builder e invocamos la compilación nativa en Linux
                setLatexBuilder(latexBuilder);
                getPDF();

                // CORRECCIÓN: El PDF de salida ahora adopta el nombre de la plantilla ("grade-report_output.pdf")
                string expectedPdf = Path.Combine(studentOutPath, "grade-report_output.pdf");
                if (File.Exists(expectedPdf))
                {
                    absolutePdfPaths.Add(expectedPdf);
                }
            }

            // 7. Validación de seguridad para pdfunite
            if (absolutePdfPaths.Count == 0)
            {
                throw new ConflictException("No se pudo generar el PDF de ningún estudiante del listado.");
            }

            // Si solo hay un estudiante, no necesitamos pdfunite
            if (absolutePdfPaths.Count == 1)
            {
                return await File.ReadAllBytesAsync(absolutePdfPaths.First());
            }

            // 8. Fusionar de forma nativa en Docker Linux usando pdfunite
            string finalPdfPath = Path.Combine(tempBatchFolder, "reporte_final.pdf");
            string arguments = $"{string.Join(" ", absolutePdfPaths.Select(p => $"\"{p}\""))} \"{finalPdfPath}\"";

            var processInfo = new ProcessStartInfo
            {
                FileName = "pdfunite",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(processInfo))
            {
                await process!.WaitForExitAsync();
                if (process.ExitCode != 0)
                {
                    string error = await process.StandardError.ReadToEndAsync();
                    throw new Exception($"Error en pdfunite: {error}");
                }
            }

            return await File.ReadAllBytesAsync(finalPdfPath);
        }
        finally
        {
            // 9. Limpieza segura de almacenamiento de adentro hacia afuera
            foreach (var dir in directoriesToClean)
            {
                if (Directory.Exists(dir))
                {
                    try
                    {
                        Directory.Delete(dir, true);
                    }
                    catch
                    {
                        /* Evitar caídas en el finally */
                    }
                }
            }

            if (Directory.Exists(tempBatchFolder))
            {
                try
                {
                    Directory.Delete(tempBatchFolder, true);
                }
                catch
                {
                    /* Evitar caídas en el finally */
                }
            }
        }
    }

    public async Task<byte[]> getReportCardByStudent(string studentId, string? userAlias)
    {
        var student = await daoFactory.academyStudentDao!.getCurrentWithGradeById(studentId);
        var teacher = await daoFactory.teacherDao!.getByEnrollmentId(student.enrollmentId!);
        if (teacher == null)
        {
            throw new EntityNotFoundException($"Teacher with enrollmentId ({student.enrollmentId}) not found.");
        }

        var enrollment = await daoFactory.enrollmentDao!.getById(student.enrollmentId!);
        var schoolyear = await daoFactory.schoolyearDao!.getCurrent();
        var partialList = await daoFactory.partialDao!.getListForCurrentSchoolyear();

        var latexBuilder = new ReportCardLatexBuilder.Builder(resource, $"{resource}/out/card")
            .withStudent(student)
            .withTeacher(teacher)
            .withUserAlias(userAlias)
            .withDegree(enrollment!.label)
            .withPartialList(partialList)
            .withSchoolyear(schoolyear.label)
            .withSubjectAreaList(await daoFactory.subjectAreaDao!.getAll())
            .withSubjectList(await daoFactory.academySubjectDao!.getByEnrollmentId(student.enrollmentId!))
            .build();

        setLatexBuilder(latexBuilder);
        return getPDF();
    }

    public async Task<byte[]> getEnrollDocument(string studentId, string userAlias)
    {
        var student = await daoFactory.studentDao!.getFullById(studentId);
        var academyStudent = await daoFactory.academyStudentDao!.getById(studentId);
        var enrollment = await daoFactory.enrollmentDao!.getByStudentId(student.studentId!);
        var schoolyear = await daoFactory.schoolyearDao!.getById(academyStudent!.schoolyearId);

        var latexBuilder = new EnrollSheetLatexBuilder.Builder(resource, $"{resource}/out/enroll")
            .withStudent(student)
            .withAcademyStudent(academyStudent)
            .withGrade(enrollment.label)
            .withSchoolyear(schoolyear?.label)
            .withUserAlias(userAlias)
            .build();

        setLatexBuilder(latexBuilder);
        return getPDF();
    }

    public async Task<byte[]> getInvoiceDocument(string transactionId)
    {
        var transaction = await daoFactory.transactionDao!.getById(transactionId);

        var cashier = await daoFactory.cashierDao!.getById(transaction!.cashierId);
        if (cashier is null)
        {
            throw new EntityNotFoundException("CashierEntity", transaction.cashierId);
        }

        var student = await daoFactory.accountingStudentDao!.getFullById(transaction.studentId);
        var secretaryStudent = await daoFactory.studentDao!.getFullById(transaction.studentId);
        var exchangeRate = await daoFactory.exchangeRateDao!.getLastRate();
        var generalBalance = await daoFactory.debtHistoryDao!.getGeneralBalance(transaction.studentId);
        secretaryStudent.accessToken ??= string.Empty;

        var latexBuilder = new InvoiceLatexBuilder.Builder(resource, $"{resource}/out/invoice")
            .withStudent(student)
            .withStudentPwd(secretaryStudent.accessToken)
            .withTransaction(transaction)
            .withCashier(cashier)
            .withGeneralBalance(generalBalance)
            .withNumber(transaction.number)
            .withSeries("A")
            .withExchangeRate(exchangeRate.value)
            .build();

        setLatexBuilder(latexBuilder);
        return getPDF();
    }

    public async Task<byte[]> getOfficialEnrollmentListDocument(string userAlias)
    {
        var schoolyear = await daoFactory.schoolyearDao!.getCurrentOrNew();

        var degreeList = await daoFactory.degreeDao!.getListForSchoolyearId(schoolyear.id!, true);
        var teacherList = await daoFactory.teacherDao!.getAll();

        var latexBuilder = new OfficialEnrollmentListLatexBuilder.Builder(resource, $"{resource}/out/enrollments")
            .withDegreeList(degreeList)
            .withTeacherList(teacherList)
            .withUserName(userAlias)
            .build();

        setLatexBuilder(latexBuilder);
        return getPDF();
    }

    public async Task<byte[]> getDebtorReport(string userAlias)
    {
        var studentList = await daoFactory.accountingStudentDao!.getDebtorStudentList();
        var degreeList = await daoFactory.degreeDao!.getValidListForNewOrCurrentSchoolyear();

        var latexBuilder = new DebtorReportLatexBuilder.Builder(resource, $"{resource}/out/debtor")
            .withStudentList(studentList)
            .withDegreeList(degreeList)
            .withUserName(userAlias)
            .build();

        setLatexBuilder(latexBuilder);
        return getPDF();
    }

    public async Task<byte[]> getActiveCertificateByStudent(string studentId, string userAlias)
    {
        var student = await daoFactory.academyStudentDao!.getCurrentById(studentId);
        if (!student.student.isActive)
        {
            throw new ConflictException($"The student with id ({studentId}) is not active.");
        }

        var enrollment = await daoFactory.enrollmentDao!.getById(student.enrollmentId!);
        var degree = await daoFactory.degreeDao!.getById(enrollment!.degreeId);
        var schoolyear = await daoFactory.schoolyearDao!.getCurrent();

        var latexBuilder = new ActiveCertificateLatexBuilder.Builder(resource, $"{resource}/out/active")
            .withStudent(student)
            .withUserAlias(userAlias)
            .withEnrollment(enrollment.label)
            .withLevel(degree!.educationalLevel)
            .withSchoolyear(schoolyear.label)
            .build();

        setLatexBuilder(latexBuilder);
        return getPDF();
    }

    public async Task<byte[]> getProformaByStudent(string studentId, string userAlias)
    {
        var student = await daoFactory.academyStudentDao!.getCurrentById(studentId);
        var enrollment = await daoFactory.enrollmentDao!.getById(student.enrollmentId!);

        return await getProformaByDegree(enrollment!.degreeId, student.fullName(), userAlias);
    }

    public async Task<byte[]> getProformaByDegree(string degreeId, string name, string userAlias)
    {
        var degree = await daoFactory.degreeDao!.getById(degreeId);
        var schoolyear = await daoFactory.schoolyearDao!.getCurrent();

        var tariffList = await daoFactory.tariffDao!.getAll();
        tariffList = tariffList
            .Where(e => DegreeDataEntity.getLevelName(e.educationalLevel) == degree!.educationalLevel)
            .OrderBy(e => e.dueDate).ToList();

        var latexBuilder = new ProformaLatexBuilder.Builder(resource, $"{resource}/out/proforma")
            .withStudent(name)
            .withUserAlias(userAlias)
            .withDegree(degree!)
            .withSchoolyear(schoolyear.label)
            .withTariffList(tariffList)
            .build();

        setLatexBuilder(latexBuilder);
        return getPDF();
    }

    public async Task<byte[]> getAccountStatement(string studentId, string userAlias)
    {
        var student = await daoFactory.accountingStudentDao!.getFullById(studentId);
        var schoolyear = await daoFactory.schoolyearDao!.getCurrent();

        var latexBuilder = new AccountStatementLatexBuilder.Builder(resource, $"{resource}/out/statement")
            .withStudent(student)
            .withUserAlias(userAlias)
            .withSchoolyear(schoolyear)
            .withSchoolyearList(await daoFactory.schoolyearDao!.getAll())
            .build();

        setLatexBuilder(latexBuilder);
        return getPDF();
    }

    public async Task<byte[]> getAcademicRecord(string studentId, string schoolyearId, string userAlias)
    {
        var schoolName = Environment.GetEnvironmentVariable("BOLETTA_SCHOOL_NAME") ?? "Colegio De Prueba";
        var schoolEmail = Environment.GetEnvironmentVariable("BOLETTA_SCHOOL_EMAIL") ?? "info@boletta.app";
        
        var schoolyear = await daoFactory.schoolyearDao!.getById(schoolyearId);
        if (schoolyear == null)
        {
            throw new EntityNotFoundException("SchoolyearEntity", schoolyearId);
        }

        var student = await daoFactory.academyStudentDao!.getByIdWithGrade(studentId, schoolyearId);
        var partialList = await daoFactory.partialDao!.getListBySchoolyearId(schoolyearId);
        var enrollment = await daoFactory.enrollmentDao!.getById(student.enrollmentId!);

        var latexBuilder = new AcademicRecordLatexBuilder.Builder(resource, $"{resource}/out/academic-record")
            .withStudent(student)
            .withUserAlias(userAlias)
            .withSchoolyearLabel(schoolyear.label)
            .withPartialList(partialList)
            .withSubjectAreaList(await daoFactory.subjectAreaDao!.getAll())
            .withSubjectList(await daoFactory.academySubjectDao!.getByEnrollmentId(student.enrollmentId!))
            .withEnrollmentLabel(enrollment!.label)
            .withSchoolName(schoolName)
            .withSchoolEmail(schoolEmail)
            .build();

        setLatexBuilder(latexBuilder);
        return getPDF();
    }
}