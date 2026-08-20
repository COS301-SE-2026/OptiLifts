using System.Text.RegularExpressions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Auth.Abstractions;
using OptiLifts.Application.Users;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Users;

public sealed class SetPasswordHandler : IRequestHandler<SetPasswordCommand>
{
    private readonly OptiLiftsDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public SetPasswordHandler(OptiLiftsDbContext dbContext, IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task Handle(SetPasswordCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new ArgumentException("New password does not meet complexity requirements.");
        }

        var reg = new Regex("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^A-Za-z0-9]).{8,}$");
        if (!reg.IsMatch(request.NewPassword))
        {
            throw new ArgumentException("New password does not meet complexity requirements.");
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        if (!string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            throw new InvalidOperationException("User already has a password set. Use update password instead.");
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
