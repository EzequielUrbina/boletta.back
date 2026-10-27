using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using wsmcbl.src.controller.business;
using wsmcbl.src.dto.management;
using wsmcbl.src.middleware;

namespace wsmcbl.src.controller.api;

[Route("management")]
[ApiController]
public class ViewPrincipalDashboardActions(ViewPrincipalDashboardController controller) : ActionsBase
{
    /// <summary>Get summary of the revenue for current month.</summary>
    /// <response code="200">Return the value</response>
    /// <response code="401">If the query was made without authentication.</response>
    /// <response code="403">If the query was made without proper permissions.</response>
    [HttpGet]
    [Route("revenues/")]
    [Authorizer("report:principal:read")]
    public async Task<IActionResult> getSummaryRevenue()
    {
        var result = await controller.getSummaryRevenue();
        return Ok(new { result.expectedIncomeThisMonth, result.expectedIncomeReceived, result.totalIncomeThisMonth});
    }
    
    /// <summary>Get last ten incidents.</summary>
    /// <response code="200">Return the value</response>
    /// <response code="401">If the query was made without authentication.</response>
    /// <response code="403">If the query was made without proper permissions.</response>
    [HttpGet]
    [Route("incidents/")]
    [Authorizer("report:principal:read")]
    public async Task<IActionResult> getLastIncidents()
    {
        return Ok(await controller.getLastIncidents());
    }
    
    /// <summary>Get summary of the students for current schoolyear.</summary>
    /// <response code="200">Return the value</response>
    [HttpGet]
    [Route("students/distributions")]
    public async Task<IActionResult> getStudentDistribution()
    {
        var studentList = await controller.getStudentRegisterViewListForCurrentSchoolyear();
        var withdrawnList = await controller.getWithdrawnStudentList();
        var degreeList = await controller.getDegreeListForCurrentSchoolyear();
        
        return Ok(new DistributionStudentDto(studentList, withdrawnList, degreeList));
    }
    
    /// <summary>Get summary of the teachers who entered grades.</summary>
    /// <response code="200">Return the value</response>
    /// <response code="401">If the query was made without authentication.</response>
    /// <response code="403">If the query was made without proper permissions.</response>
    [HttpGet]
    [Route("teachers/grades/summaries")]
    [Authorizer("report:principal:read")]
    public async Task<IActionResult> getSummaryTeacherGrades()
    {
        var result = await controller.getSummaryTeacherGrades();
        return Ok(result.mapListToDto());
    }
    
    /// <summary>Returns subject name list for current schoolyear.</summary>
    /// <response code="200">Returns a list, the list can be empty.</response>
    /// <response code="401">If the query was made without authentication.</response>
    /// <response code="403">If the query was made without proper permissions.</response>
    [HttpGet]
    [Route("subjects")]
    [Authorizer("admin", "cashier", "principal")]
    public async Task<IActionResult> getSubjectList()
    {
        var subjectList = await controller.getSubjectList();
        var degreeList = await controller.getDegreeList();
        
        return Ok(subjectList.mapListToDto(degreeList));
    }
    
    /// <summary>Returns enrollment list for current schoolyear.</summary>
    /// <response code="200">Returns a list, the list can be empty.</response>
    /// <response code="401">If the query was made without authentication.</response>
    /// <response code="403">If the query was made without proper permissions.</response>
    [HttpGet]
    [Route("enrollments")]
    [Authorizer("report:principal:read")]
    public async Task<IActionResult> getEnrollmentList()
    {
        var result = await controller.getEnrollmentList();
        return Ok(result.mapListToDto());
    }
    
    /// <summary>Returns enrollment grade summary by partial in XLSX format.</summary>
    /// <response code="200">Returns a resource.</response>
    /// <response code="401">If the query was made without authentication.</response>
    /// <response code="403">If the query was made without proper permissions.</response>
    /// <response code="404">If enrollment or partial not found.</response>
    [HttpGet]
    [Route("enrollments/{enrollmentId}/grades/export")]
    [Authorizer("report:principal:read")]
    public async Task<IActionResult> getGradeSummaryByEnrollmentId([Required] string enrollmentId, [Required] [FromQuery] int partialId)
    {
        var result = await controller.getGradeSummaryByEnrollmentId(enrollmentId, partialId, getAuthenticatedUserId());
        
        return File(result, getContentType(2), $"{enrollmentId}.grades-summary.xlsx");
    }
    
    /// <summary>Returns statistics summary by partialId in XLSX format.</summary>
    /// <response code="200">Returns a resource.</response>
    /// <response code="401">If the query was made without authentication.</response>
    /// <response code="403">If the query was made without proper permissions.</response>
    /// <response code="404">If enrollment or partial not found.</response>
    [HttpGet]
    [Route("degrees/report/export")]
    [Authorizer("report:principal:read")]
    public async Task<IActionResult> getGradeStatistics([Required] [FromQuery] int partialId)
    {
        var result = await controller.getGradeStatistics(partialId, getAuthenticatedUserId());
        return File(result, getContentType(2), "statistics-summary.xlsx");
    }
    
    /// <summary>Returns failed student by partialId in XLSX format.</summary>
    /// <response code="200">Returns a resource.</response>
    /// <response code="401">If the query was made without authentication.</response>
    /// <response code="403">If the query was made without proper permissions.</response>
    /// <response code="404">If enrollment or partial not found.</response>
    [HttpGet]
    [Route("students/failed/report/export")]
    [Authorizer("report:principal:read")]
    public async Task<IActionResult> getReportFailedStudents([Required] [FromQuery] int partialId)
    {
        var result = await controller.getReportFailedStudents(partialId, getAuthenticatedUserId());
        return File(result, getContentType(2), "statistics-summary.xlsx");
    }
}