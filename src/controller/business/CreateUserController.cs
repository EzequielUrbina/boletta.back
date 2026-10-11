using wsmcbl.src.controller.service;
using wsmcbl.src.model;
using wsmcbl.src.model.academy;
using wsmcbl.src.model.accounting;
using wsmcbl.src.model.config;
using wsmcbl.src.model.dao;

namespace wsmcbl.src.controller.business;

public class CreateUserController : BaseController
{
    private HttpClient httpClient { get; }
    private UserAuthenticator userAuthenticator { get; }

    public CreateUserController(DaoFactory daoFactory, UserAuthenticator userAuthenticator, HttpClient httpClient) :
        base(daoFactory)
    {
        this.httpClient = httpClient;
        this.userAuthenticator = userAuthenticator;
    }

    public async Task<PagedResult<UserEntity>> getPaginatedUser(PagedRequest request)
    {
        return await daoFactory.userDao!.getPaginated(request);
    }

    public async Task<UserEntity> createUser(UserEntity user, string groupName)
    {
        await daoFactory.userDao!.checkForDuplicateUser(user);
        await user.generateEmail(daoFactory.userDao!);

        var password = generatePassword();
        userAuthenticator.encodePassword(user, password);

        daoFactory.userDao!.create(user);
        await daoFactory.ExecuteAsync();
        daoFactory.Detached(user);

        user.password = password;

        await createRole(user);
        //Deshabilitado por motivos de fuera del plan actual.
        //await createEmailAccount(user);
        //await createNextcloudAccount(user, groupName);
        
        return user;
    }

    private async Task createRole(UserEntity user)
    {
        if (user.roleId == 3)
        {
            daoFactory.cashierDao!.create(new CashierEntity((Guid)user.userId!));
            await daoFactory.ExecuteAsync();
        }
        
        if (user.roleId == 4)
        {
            daoFactory.teacherDao!.create(new TeacherEntity((Guid)user.userId!));
            await daoFactory.ExecuteAsync();
        }
    }

    private async Task createNextcloudAccount(UserEntity user, string groupName)
    {
        var nextcloudUserCreator = new NextcloudUserCreator(httpClient);
        await nextcloudUserCreator.createUser(user);
        await nextcloudUserCreator.assignGroup(user.email, groupName.Trim());
    }

    private async Task createEmailAccount(UserEntity user)
    {
        var posteUserCreator = new PosteUserCreator(httpClient);
        await posteUserCreator.createUser(user);
    }

    private static string generatePassword()
    {
        var passwordGenerator = new PasswordGenerator();
        return passwordGenerator.generatePassword(10);
    }

    public async Task<List<string>> getNextcloudGroupList()
    {
        var nextcloudUserCreator = new NextcloudUserCreator(httpClient);
        return await nextcloudUserCreator.getGroupList();
    }
}