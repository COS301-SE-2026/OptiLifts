using System.Text.RegularExpressions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Auth.Abstractions;
using OptiLifts.Application.Users;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Users;

public sealed class ChangePasswordHandler : IRequestHandler<UpdatePasswordCommand>
{
    private readonly OptiLiftsDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    public ChangePasswordHandler(OptiLiftsDbContext dbContext, IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }
    public async Task Handle(UpdatePasswordCommand request, CancellationToken cancellationToken)
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

        var hasExistingPassword = !string.IsNullOrWhiteSpace(user.PasswordHash);
        if (hasExistingPassword)
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword) || !_passwordHasher.Verify(user.PasswordHash!, request.CurrentPassword))
            {
                throw new UnauthorizedAccessException("Provided current password is incorrect.");
            }
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}