using Microsoft.Extensions.Options;
using MongoDB.Driver;
using TodoApi.Models;

namespace TodoApi.Services;

public class UserService
{
    private readonly IMongoCollection<User>? _userCollection;
    private readonly bool _useInMemoryStore;
    private static readonly List<User> InMemoryUsers = [];
    private static readonly object InMemoryLock = new();

    public UserService(IOptions<TodoDatabaseSettings> dbSettings, IConfiguration configuration)
    {
        _useInMemoryStore = configuration.GetValue<bool>("UseInMemoryStore");
        if (_useInMemoryStore)
        {
            return;
        }

        var settings = dbSettings.Value;
        var client = new MongoClient(settings.ConnectionString);
        var database = client.GetDatabase(settings.DatabaseName);
        _userCollection = database.GetCollection<User>(settings.UserCollectionName);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        if (_useInMemoryStore)
        {
            lock (InMemoryLock)
            {
                var user = InMemoryUsers.FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
                return user is null
                    ? null
                    : new User { Id = user.Id, Username = user.Username, PasswordHash = user.PasswordHash, Role = user.Role };
            }
        }

        return await _userCollection!.Find(u => u.Username == username).FirstOrDefaultAsync();
    }

    public async Task EnsureUniqueIndexAsync()
    {
        if (_useInMemoryStore)
        {
            return;
        }

        var indexModel = new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.Username),
            new CreateIndexOptions { Unique = true });

        await _userCollection!.Indexes.CreateOneAsync(indexModel);
    }

    public async Task CreateAsync(User user)
    {
        if (_useInMemoryStore)
        {
            lock (InMemoryLock)
            {
                var existing = InMemoryUsers.Any(u => string.Equals(u.Username, user.Username, StringComparison.OrdinalIgnoreCase));
                if (existing)
                {
                    return;
                }

                InMemoryUsers.Add(new User
                {
                    Id = user.Id,
                    Username = user.Username,
                    PasswordHash = user.PasswordHash,
                    Role = user.Role
                });
            }

            await Task.CompletedTask;
            return;
        }

        await _userCollection!.InsertOneAsync(user);
    }

    public async Task<User?> ValidateCredentialsAsync(string username, string password)
    {
        var user = await GetByUsernameAsync(username);
        if (user is null)
        {
            return null;
        }

        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash) ? user : null;
    }
}
