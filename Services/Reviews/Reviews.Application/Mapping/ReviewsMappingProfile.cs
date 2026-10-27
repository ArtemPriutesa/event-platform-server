using AutoMapper;
using Reviews.Application.Abstractions;
using Reviews.Domain.Entities;

namespace Reviews.Application.Mapping;

public sealed class ReviewsMappingProfile : Profile
{
    public ReviewsMappingProfile()
    {
        CreateMap<ReviewComment, ReviewCommentDto>()
            .ForCtorParam(nameof(ReviewCommentDto.Author), options => options.MapFrom(source => source.Author.DisplayName))
            .ForCtorParam(nameof(ReviewCommentDto.AvatarUrl), options => options.MapFrom(source => source.Author.AvatarUrl));
        CreateMap<Review, ReviewDto>()
            .ForCtorParam(nameof(ReviewDto.Id), options => options.MapFrom(source => source.Id.ToString()))
            .ForCtorParam(nameof(ReviewDto.Author), options => options.MapFrom(source => source.Author.DisplayName))
            .ForCtorParam(nameof(ReviewDto.AvatarUrl), options => options.MapFrom(source => source.Author.AvatarUrl))
            .ForCtorParam(nameof(ReviewDto.AuthorEmail), options => options.MapFrom(source => source.Author.Email == null ? null : source.Author.Email.Value))
            .ForCtorParam(nameof(ReviewDto.Photos), options => options.MapFrom(source => source.Photos.ToArray()))
            .ForCtorParam(nameof(ReviewDto.Comments), options => options.MapFrom(source => source.Comments.ToArray()));
    }
}
