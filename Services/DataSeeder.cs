using Microsoft.Extensions.Options;
using TodoApi.Models;

namespace TodoApi.Services;

public class DataSeeder : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(IConfiguration configuration, IServiceProvider serviceProvider, ILogger<DataSeeder> logger)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<UserService>();

        await userService.EnsureUniqueIndexAsync();

        var seedUsers = _configuration.GetSection("SeedUsers").Get<List<SeedUser>>() ?? [];
        foreach (var seedUser in seedUsers)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            var existingUser = await userService.GetByUsernameAsync(seedUser.Username);
            if (existingUser is not null)
            {
                continue;
            }

            await userService.CreateAsync(new User
            {
                Username = seedUser.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(seedUser.Password),
                Role = seedUser.Role
            });

            _logger.LogInformation("Created seed user {Username} with role {Role}", seedUser.Username, seedUser.Role);
        }
    }
}
