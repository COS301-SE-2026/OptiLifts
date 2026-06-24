using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Storage;
using OptiLifts.Application.Users;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Users;

public sealed class UpdateProfilePictureHandler : IRequestHandler<UploadProfilePictureCommand, string>
{
    private readonly OptiLiftsDbContext _dbContext;
    private readonly IBlobStorageService _blobStorageService;

    public UpdateProfilePictureHandler(OptiLiftsDbContext dbContext, IBlobStorageService blobStorageService)
    {
        _dbContext = dbContext;
        _blobStorageService = blobStorageService;
    }

    public async Task<string> Handle(UploadProfilePictureCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        var imageUrl = await _blobStorageService.UploadFileAsync(
            request.FileStream,
            request.FileName,
            request.ContentType,
            "profile-pictures",
            cancellationToken
        );

        user.ProfileImageUrl = imageUrl;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return imageUrl;
    }
}