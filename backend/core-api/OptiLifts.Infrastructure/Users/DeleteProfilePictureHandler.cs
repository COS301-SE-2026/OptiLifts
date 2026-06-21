using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Users;
using OptiLifts.Infrastructure.Database;


namespace OptiLifts.Infrastructure.Users;

public sealed class DeleteProfilePictureHandler : IRequestHandler<DeleteProfilePictureCommand>
{
    private readonly OptiLiftsDbContext _dbContext;

    public DeleteProfilePictureHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(DeleteProfilePictureCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        user.ProfileImageUrl = null;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}