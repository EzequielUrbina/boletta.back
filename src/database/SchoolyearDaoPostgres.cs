using Microsoft.EntityFrameworkCore;
using wsmcbl.src.database.context;
using wsmcbl.src.exception;
using wsmcbl.src.model.secretary;

namespace wsmcbl.src.database;

public class SchoolyearDaoPostgres(PostgresContext context)
    : GenericDaoPostgres<SchoolyearEntity, string>(context), ISchoolyearDao
{
    public async Task<SchoolyearEntity?> getById(string schoolyearId, bool withProperty = false)
    {
        var query = entities.Where(e => e.id == schoolyearId);

        if (withProperty)
        {
            query = query.Include(e => e.exchangeRate)
                .Include(e => e.semesterList)!
                .ThenInclude(e => e.partialList)
                .Include(e => e.degreeList)!
                .ThenInclude(e => e.subjectList)
                .Include(e => e.tariffList);
        }

        return await query.FirstOrDefaultAsync();
    }

    public async Task<SchoolyearEntity> getByLabel(int year)
    {
        var result = await entities.FirstOrDefaultAsync(e => e.label == year.ToString());
        if (result == null)
        {
            throw new EntityNotFoundException($"Entity of type (SchoolyearEntity) with label ({year}) not found.");
        }

        return result;
    }

    public async Task<SchoolyearEntity> getCurrent()
    {
        return await getByLabel(DateTime.Now.Year);
    }

    private static int newOrCurrentLabel() => DateTime.Today.Month >= 10 ? DateTime.Today.Year + 1 : DateTime.Today.Year;
    
    public async Task<SchoolyearEntity> getCurrentOrNew()
    {
        try
        {
            return await getCurrent();
        }
        catch (EntityNotFoundException)
        {
            return await getByLabel(newOrCurrentLabel());
        }
        catch (Exception)
        {
            throw new InternalException("There is not valid schoolyear.");
        }
    }

    public async Task<SchoolyearEntity> getNewOrCurrent()
    {
        try
        {
            return await getByLabel(newOrCurrentLabel());
        }
        catch (EntityNotFoundException)
        {
            return await getCurrent();
        }
        catch (Exception)
        {
            throw new InternalException("There is not valid schoolyear.");
        }
    }

    public async Task<SchoolyearEntity> createNewOrFail()
    {
        if (DateTime.Today.Month < 10)
        {
            throw new ConflictException("The new schoolyear cannot be created on this date. The start date is october 1st.");
        }
    
        await checkNewSchoolyearCreated();
    
        var year = newOrCurrentLabel();
        var schoolYearEntity = new SchoolyearEntity
        {
            label = year.ToString(),
            startDate = new DateOnly(year, 1, 1),
            deadLine = new DateOnly(year, 12, 31),
            isActive = true
        };

        create(schoolYearEntity);
        await context.SaveChangesAsync();

        return schoolYearEntity;
    }
    
    private async Task checkNewSchoolyearCreated()
    {
        var result = await entities
            .FirstOrDefaultAsync(e => e.label == newOrCurrentLabel().ToString());
        
        if (result != null)
        {
            throw new ConflictException("The new schoolyear is already created.");
        }
    }
}