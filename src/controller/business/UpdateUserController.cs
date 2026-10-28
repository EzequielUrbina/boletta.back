using wsmcbl.src.controller.service;
using wsmcbl.src.exception;
using wsmcbl.src.model.config;
using wsmcbl.src.model.dao;

namespace wsmcbl.src.controller.business;

public class UpdateUserController : BaseController
{
    private NextcloudUserCreator nextcloudUserCreator { get; set; }
    private UserAuthenticator userAuthenticator { get; set; }
    private HttpClient httpClient { get; set; }

    public UpdateUserController(DaoFactory daoFactory, UserAuthenticator userAuthenticator, HttpClient httpClient) : base(daoFactory)
    {
        this.httpClient = httpClient;
        nextcloudUserCreator = new NextcloudUserCreator(this.httpClient);
        this.userAuthenticator = userAuthenticator;
    }

    public async Task<List<PermissionEntity>> getPermissionList()
    {
        return await daoFactory.permissionDao!.getAll();
    }

    public async Task updateUser(UserEntity value, string nextCloudGroup)
    {
        var user = await daoFactory.userDao!.getById(value.userId.ToString()!);
        if (user == null)
        {
            throw new EntityNotFoundException("UserEntity", value.userId!.ToString());
        }

        user.update(value);
        await daoFactory.ExecuteAsync();

        //await assignNextcloudGroup(user, nextCloudGroup);
    }

    private async Task assignNextcloudGroup(UserEntity user, string nextCloudGroup)
    {
        await nextcloudUserCreator.assignGroup(user.email, nextCloudGroup.Trim());
    }

    public async Task assignPermissions(string userId, List<int> permissionIdList)
    {
        if (permissionIdList.Count == 0)
        {
            return;
        }

        var user = await daoFactory.userDao!.getById(userId);
        if (user == null)
        {
            throw new EntityNotFoundException("UserEntity", userId);
        } 
    
        await daoFactory.permissionDao!.verifyIdListOrFail(permissionIdList);

        var list = permissionIdList.Select(e => new UserPermissionEntity((Guid)user.userId!, e)).ToList();
        
        user.updatePermissionList(list, daoFactory.userPermissionDao!);
        await daoFactory.ExecuteAsync();
    }

    public async Task<string> getNextCloudGroup(UserEntity entity)
    {
        return await nextcloudUserCreator.getGroupByUserMail(entity.email);
    }

    public async Task<UserEntity> updateUserPassword(string userId)
    {
        var user = await daoFactory.userDao!.getById(userId);
        if (user == null)
        {
            throw new EntityNotFoundException("UserEntity", userId);
        }

        var password = generatePassword();
        userAuthenticator.encodePassword(user, password);

        await daoFactory.ExecuteAsync();
        daoFactory.Detached(user);

        user.password = password;

        //Deshabilitado porque no están incluidos en el plan de boletta.
        //await updatePasswordEmailAccount(user);
        //await nextcloudUserCreator.updateUserPassword(user);
        
        return user;
    }

    private static string generatePassword()
    {
        var passwordGenerator = new PasswordGenerator();
        return passwordGenerator.generatePassword(10);
    }
    
    private async Task updatePasswordEmailAccount(UserEntity user)
    {
        var posteUserCreator = new PosteUserCreator(httpClient);
        await posteUserCreator.updateUserPassword(user);
    }

    public async Task changeUserState(string userId)
    {
        var user = await daoFactory.userDao!.getById(userId);
        if (user == null)
        {
            throw new EntityNotFoundException("UserEntity", userId);
        }

        user.changeState();
        daoFactory.userDao!.update(user);
        await daoFactory.ExecuteAsync();

        if (!user.isActive)
        {
            await updateUserPassword(userId);
        }
        
        //Deshabilitados por no ir con el plan actual de Boletta.
       // await nextcloudUserCreator.changeState(user);
    }
}