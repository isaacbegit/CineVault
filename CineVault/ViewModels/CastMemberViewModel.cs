using CineVault.Models;

namespace CineVault.ViewModels;

public class CastMemberViewModel
{
    public CastMember Cast { get; }

    public CastMemberViewModel(CastMember cast)
    {
        Cast = cast;
    }

    public string Name => Cast.Name;
    public string? Character => Cast.Character;
    public string? ImagePath => Cast.ProfileLocalPath ?? Cast.ProfileImageUrl;
}
