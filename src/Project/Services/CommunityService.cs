using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TuringMachinesAPI.DataSources;
using TuringMachinesAPI.Dtos;
using TuringMachinesAPI.Enums;
using TuringMachinesAPI.Services;
using TuringMachinesAPI.Utils;

namespace TuringMachinesAPI.Services
{
    public class CommunityService
    {
        private readonly TuringMachinesDbContext db;
        private readonly IConfiguration config;
        private readonly IMemoryCache cache;

        public CommunityService(TuringMachinesDbContext _db, IConfiguration _config, IMemoryCache _cache)
        {
            db = _db;
            config = _config;
            cache = _cache;
        }

        public IEnumerable<Dtos.Discussion> GetDiscussions()
        {
            if (cache.TryGetValue("Discussions", out IEnumerable<Dtos.Discussion>? discussions))
            {
                if (discussions != null)
                {
                    return discussions;
                }
                return Enumerable.Empty<Dtos.Discussion>();
            }
            discussions = db.Discussions
                .AsNoTracking()
                .Include(d => d.InitialPost)
                .Include(d => d.AnswerPost)
                .Select(d => new Dtos.Discussion
                {
                    Id = d.Id,
                    Title = d.Title,
                    AuthorName = d.Author != null ? d.Author.Username : "Deleted User",
                    InitialPost = new Dtos.Post
                    {
                        Id = d.InitialPost.Id,
                        Content = d.InitialPost.Content,
                        AuthorName = d.InitialPost.Author != null ? d.InitialPost.Author.Username : "Deleted User",
                        CreatedAt = d.InitialPost.CreatedAt,
                        UpdatedAt = d.InitialPost.UpdatedAt
                    },
                    AnswerPost = d.AnswerPost != null ? new Dtos.Post
                    {
                        Id = d.AnswerPost.Id,
                        Content = d.AnswerPost.Content,
                        AuthorName = d.AnswerPost.Author != null ? d.AnswerPost.Author.Username : "Deleted User",
                        CreatedAt = d.AnswerPost.CreatedAt,
                        UpdatedAt = d.AnswerPost.UpdatedAt
                    } : null,
                    Category = d.Category.ToString(),
                    IsClosed = d.IsClosed,
                    CreatedAt = d.CreatedAt,
                    UpdatedAt = d.UpdatedAt,
                    PostCount = d.Posts.Count()
                })
                .ToList();

            cache.Set("Discussions", discussions);
            return discussions;
        }

        public Dtos.Discussion? GetDiscussionById(int discussionId)
        {
            var discussions = GetDiscussions();
            var discussion = discussions.FirstOrDefault(d => d.Id == discussionId);
            if (discussion == null)
            {
                return null;
            }
            return discussion;
        }

        public Dtos.Discussion? CreateDiscussion(string title, int authorId, string content, string category)
        {
            if (ValidationUtils.ContainsDisallowedContent(title)) return null;
            if (ValidationUtils.ContainsDisallowedContent(content)) content = ValidationUtils.CensorDisallowedWords(content);

            var enumCategory = Enum.TryParse<DiscussionCategory>(category, out var parsedCategory) ? parsedCategory : DiscussionCategory.General;
            var playerAuthor = db.Players.Find(authorId);

            var discussion = new Entities.Discussion
            {
                Title = title,
                AuthorId = authorId,
                Author = playerAuthor,
                Category = enumCategory,
                IsClosed = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var InitialPost = new Entities.Post
            {
                Content = content,
                AuthorId = authorId,
                Author = playerAuthor,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Discussions.Add(discussion);
            db.SaveChanges();

            InitialPost.DiscussionId = discussion.Id;
            db.Posts.Add(InitialPost);
            db.SaveChanges();

            discussion.InitialPostId = InitialPost.Id;
            db.Discussions.Update(discussion);
            db.SaveChanges();

            var discussionDto = new Dtos.Discussion
            {
                Id = discussion.Id,
                Title = discussion.Title,
                AuthorName = discussion.Author != null ? discussion.Author.Username : "Deleted User",
                InitialPost = new Dtos.Post
                {
                    Id = InitialPost.Id,
                    Content = InitialPost.Content,
                    AuthorName = InitialPost.Author != null ? InitialPost.Author.Username : "Deleted User",
                    CreatedAt = InitialPost.CreatedAt,
                    UpdatedAt = InitialPost.UpdatedAt,
                    IsEdited = InitialPost.IsEdited,
                    LikeCount = 0,
                    DislikeCount = 0
                },
                AnswerPost = null,
                Category = discussion.Category.ToString(),
                IsClosed = discussion.IsClosed,
                CreatedAt = discussion.CreatedAt,
                UpdatedAt = discussion.UpdatedAt,
                PostCount = 1
            };

            if (cache.TryGetValue("Discussions", out IEnumerable<Dtos.Discussion>? discussions))
            {
                if (discussions != null)
                {
                    var updatedDiscussions = discussions.ToList();
                    updatedDiscussions.Add(discussionDto);
                    cache.Set("Discussions", updatedDiscussions);
                }
            }
            cache.Set("Posts_Discussion_" + discussion.Id, discussionDto.InitialPost);
            return discussionDto;
        }

        public IEnumerable<Dtos.Post> GetPostsByDiscussionId(int discussionId)
        {
            if (cache.TryGetValue("Posts_Discussion_" + discussionId, out IEnumerable<Dtos.Post>? posts))
            {
                if (posts != null)
                {
                    return posts;
                }
                return Enumerable.Empty<Dtos.Post>();
            }
            posts = db.Posts
                .AsNoTracking()
                .Where(p => p.DiscussionId == discussionId)
                .Include(p => p.Author)
                .Select(p => new Dtos.Post
                {
                    Id = p.Id,
                    Content = p.Content,
                    AuthorName = p.Author != null ? p.Author.Username : "Deleted User",
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    IsEdited = p.IsEdited,
                    LikeCount = db.PostVotes
                        .Where(v => v.PostId == p.Id)
                        .Sum(v => v.Vote == 1 ? 1 : 0),
                    DislikeCount = db.PostVotes
                        .Where(v => v.PostId == p.Id)
                        .Sum(v => v.Vote == -1 ? 1 : 0)
                })
                .ToList();
            cache.Set("Posts_Discussion_" + discussionId, posts);
            return posts;
        }

        public Dtos.Post? CreatePost(int discussionId, int authorId, string content)
        {
            if (ValidationUtils.ContainsDisallowedContent(content)) content = ValidationUtils.CensorDisallowedWords(content);
            var discussion = db.Discussions.Find(discussionId);
            if (discussion == null || discussion.IsClosed) return null;
            var authorPlayer = db.Players.Find(authorId);

            var post = new Entities.Post
            {
                DiscussionId = discussionId,
                AuthorId = authorId,
                Author = authorPlayer,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Posts.Add(post);
            db.SaveChanges();

            Dtos.Post postDto = new Dtos.Post
            {
                Id = post.Id,
                Content = post.Content,
                AuthorName = post.Author != null ? post.Author.Username : "Deleted User",
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                IsEdited = post.IsEdited,
                LikeCount = 0,
                DislikeCount = 0
            };
            if (cache.TryGetValue("Posts_Discussion_" + discussionId, out IEnumerable<Dtos.Post>? posts))
            {
                if (posts != null)
                {
                    var updatedPosts = posts.ToList();
                    updatedPosts.Add(postDto);
                    cache.Set("Posts_Discussion_" + discussionId, updatedPosts);
                }
                else
                {
                    cache.Set("Posts_Discussion_" + discussionId, new List<Dtos.Post> { postDto });
                }
            }
            return postDto;
        }

        public bool ToggleLikePost(int postId, int playerId)
        {
            var existingVote = db.PostVotes.FirstOrDefault(pv => pv.PostId == postId && pv.PlayerId == playerId);
            if (existingVote != null)
            {
                if (existingVote.Vote == 1)
                {
                    db.PostVotes.Remove(existingVote);
                }
                else
                {
                    existingVote.Vote = 1;
                    db.PostVotes.Update(existingVote);
                }
            }
            else
            {
                var newVote = new Entities.PostVote
                {
                    PostId = postId,
                    PlayerId = playerId,
                    Vote = 1
                };
                db.PostVotes.Add(newVote);
            }
            db.SaveChanges();

            if (cache.TryGetValue("Posts_Discussion_" + db.Posts.Find(postId)?.DiscussionId, out IEnumerable<Dtos.Post>? posts))
            {
                if (posts != null)
                {
                    var updatedPosts = posts.ToList();
                    var index = updatedPosts.FindIndex(p => p.Id == postId);
                    if (index != -1)
                    {
                        var likeCount = db.PostVotes
                            .Where(v => v.PostId == postId)
                            .Sum(v => v.Vote == 1 ? 1 : 0);
                        var dislikeCount = db.PostVotes
                            .Where(v => v.PostId == postId)
                            .Sum(v => v.Vote == -1 ? 1 : 0);
                        updatedPosts[index].LikeCount = likeCount;
                        updatedPosts[index].DislikeCount = dislikeCount;
                        cache.Set("Posts_Discussion_" + db.Posts.Find(postId)?.DiscussionId, updatedPosts);
                    }
                }
            }

            return true;
        }

        public bool ToggleDislikePost(int postId, int playerId)
        {
            var existingVote = db.PostVotes.FirstOrDefault(pv => pv.PostId == postId && pv.PlayerId == playerId);
            if (existingVote != null)
            {
                if (existingVote.Vote == -1)
                {
                    db.PostVotes.Remove(existingVote);
                }
                else
                {
                    existingVote.Vote = -1;
                    db.PostVotes.Update(existingVote);
                }
            }
            else
            {
                var newVote = new Entities.PostVote
                {
                    PostId = postId,
                    PlayerId = playerId,
                    Vote = -1
                };
                db.PostVotes.Add(newVote);
            }
            db.SaveChanges();

            if (cache.TryGetValue("Posts_Discussion_" + db.Posts.Find(postId)?.DiscussionId, out IEnumerable<Dtos.Post>? posts))
            {
                if (posts != null)
                {
                    var updatedPosts = posts.ToList();
                    var index = updatedPosts.FindIndex(p => p.Id == postId);
                    if (index != -1)
                    {
                        var likeCount = db.PostVotes
                            .Where(v => v.PostId == postId)
                            .Sum(v => v.Vote == 1 ? 1 : 0);
                        var dislikeCount = db.PostVotes
                            .Where(v => v.PostId == postId)
                            .Sum(v => v.Vote == -1 ? 1 : 0);
                        updatedPosts[index].LikeCount = likeCount;
                        updatedPosts[index].DislikeCount = dislikeCount;
                        cache.Set("Posts_Discussion_" + db.Posts.Find(postId)?.DiscussionId, updatedPosts);
                    }
                }
            }

            return true;
        }

        public bool ToggleCloseDiscussion(int discussionId, int playerId)
        {
            var discussion = db.Discussions.Include(d => d.Author).FirstOrDefault(d => d.Id == discussionId);
            if (discussion == null) return false;

            var isAdmin = db.Players.Any(p => p.Id == playerId && p.Role == "Admin");

            if (discussion.AuthorId == playerId || isAdmin)
            {
                if (discussion.IsClosed)
                {
                    if (isAdmin)
                    {
                        discussion.IsClosed = false;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    discussion.IsClosed = true;
                }
            }
            db.Discussions.Update(discussion);
            db.SaveChanges();

            if (cache.TryGetValue("Discussions", out IEnumerable<Dtos.Discussion>? discussions))
            {
                if (discussions != null)
                {
                    var updatedDiscussions = discussions.ToList();
                    var index = updatedDiscussions.FindIndex(d => d.Id == discussion.Id);
                    if (index != -1)
                    {
                        updatedDiscussions[index].IsClosed = discussion.IsClosed;
                        cache.Set("Discussions", updatedDiscussions);
                    }
                }
            }
            return true;
        }

        public bool ToggleDiscussionAnswerPost(int discussionId, int postId, int playerId)
        {
            var discussion = db.Discussions.Include(d => d.Author).FirstOrDefault(d => d.Id == discussionId);
            if (discussion == null) return false;
            var isAdmin = db.Players.Any(p => p.Id == playerId && p.Role == "Admin");
            if (discussion.AuthorId == playerId || isAdmin)
            {
                var post = db.Posts.FirstOrDefault(p => p.Id == postId && p.DiscussionId == discussionId);
                if (post == null) return false;
                if (discussion.AnswerPostId == postId)
                {
                    discussion.AnswerPostId = null;
                }
                else
                {
                    discussion.AnswerPostId = postId;
                }
            }
            db.Discussions.Update(discussion);
            db.SaveChanges();
            if (cache.TryGetValue("Discussions", out IEnumerable<Dtos.Discussion>? discussions))
            {
                if (discussions != null)
                {
                    var updatedDiscussions = discussions.ToList();
                    var index = updatedDiscussions.FindIndex(d => d.Id == discussion.Id);
                    if (index != -1)
                    {
                        if (discussion.AnswerPostId != null)
                        {
                            var answerPost = db.Posts.Include(p => p.Author).FirstOrDefault(p => p.Id == discussion.AnswerPostId);
                            if (answerPost != null)
                            {
                                updatedDiscussions[index].AnswerPost = new Dtos.Post
                                {
                                    Id = answerPost.Id,
                                    Content = answerPost.Content,
                                    AuthorName = answerPost.Author != null ? answerPost.Author.Username : "Deleted User",
                                    CreatedAt = answerPost.CreatedAt,
                                    UpdatedAt = answerPost.UpdatedAt
                                };
                            }
                        }
                        else
                        {
                            updatedDiscussions[index].AnswerPost = null;
                        }
                        cache.Set("Discussions", updatedDiscussions);
                    }
                }
            }
            return true;
        }

        public Dtos.Post? EditPost(int postId, int authorId, string newContent)
        {
            if (ValidationUtils.ContainsDisallowedContent(newContent)) newContent = ValidationUtils.CensorDisallowedWords(newContent);
            var post = db.Posts.Include(p => p.Author).FirstOrDefault(p => p.Id == postId);
            if (post == null || post.AuthorId != authorId) return null;
            post.Content = newContent;
            post.IsEdited = true;
            post.UpdatedAt = DateTime.UtcNow;
            db.Posts.Update(post);
            db.SaveChanges();
            var postDto = new Dtos.Post
            {
                Id = post.Id,
                Content = post.Content,
                AuthorName = post.Author != null ? post.Author.Username : "Deleted User",
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                IsEdited = post.IsEdited,
                LikeCount = db.PostVotes
                    .Where(v => v.PostId == post.Id)
                    .Sum(v => v.Vote == 1 ? 1 : 0),
                DislikeCount = db.PostVotes
                    .Where(v => v.PostId == post.Id)
                    .Sum(v => v.Vote == -1 ? 1 : 0)
            };
            if (cache.TryGetValue("Posts_Discussion_" + post.DiscussionId, out IEnumerable<Dtos.Post>? posts))
            {
                if (posts != null)
                {
                    var updatedPosts = posts.ToList();
                    var index = updatedPosts.FindIndex(p => p.Id == post.Id);
                    if (index != -1)
                    {
                        updatedPosts[index] = postDto;
                        cache.Set("Posts_Discussion_" + post.DiscussionId, updatedPosts);
                    }
                }
            }
            return postDto;
        }

        public bool DeletePost(int postId, int userId)
        {
            var post = db.Posts.Include(p => p.Discussion).FirstOrDefault(p => p.Id == postId);
            if (post == null) return false;
            var isAdmin = db.Players.Any(p => p.Id == userId && p.Role == "Admin");
            if (post.AuthorId == userId || isAdmin)
            {
                if (post.Discussion.InitialPostId == post.Id)
                {
                    return false;
                }
                db.Posts.Remove(post);
                db.SaveChanges();
                if (cache.TryGetValue("Posts_Discussion_" + post.DiscussionId, out IEnumerable<Dtos.Post>? posts))
                {
                    if (posts != null)
                    {
                        var updatedPosts = posts.ToList();
                        var index = updatedPosts.FindIndex(p => p.Id == post.Id);
                        if (index != -1)
                        {
                            updatedPosts.RemoveAt(index);
                            cache.Set("Posts_Discussion_" + post.DiscussionId, updatedPosts);
                        }
                    }
                }
                return true;
            }
            return false;
        }

        public bool DeleteDiscussion(int discussionId, int userId)
        {
            var discussion = db.Discussions.Include(d => d.Posts).FirstOrDefault(d => d.Id == discussionId);
            if (discussion == null) return false;
            var isAdmin = db.Players.Any(p => p.Id == userId && p.Role == "Admin");
            if (discussion.AuthorId == userId || isAdmin)
            {
                discussion.InitialPostId = null;
                discussion.InitialPost = null;
                discussion.AnswerPostId = null;
                discussion.AnswerPost = null;

                db.Discussions.Update(discussion);
                db.SaveChanges();

                db.Discussions.Remove(discussion);
                db.SaveChanges();
                if (cache.TryGetValue("Discussions", out IEnumerable<Dtos.Discussion>? discussions))
                {
                    if (discussions != null)
                    {
                        var updatedDiscussions = discussions.ToList();
                        var index = updatedDiscussions.FindIndex(d => d.Id == discussion.Id);
                        if (index != -1)
                        {
                            updatedDiscussions.RemoveAt(index);
                            cache.Set("Discussions", updatedDiscussions);
                        }
                    }
                }
                cache.Remove("Posts_Discussion_" + discussion.Id);
                return true;
            }
            return false;
        }

        public IEnumerable<(Dtos.Post post, int? userVote)> GetPostsByDiscussionIdWithUserVote(int discussionId, int playerId)
        {
            var posts = GetPostsByDiscussionId(discussionId);
            var result = new List<(Dtos.Post post, int? userVote)>();
            foreach (var post in posts)
            {
                var vote = db.PostVotes.FirstOrDefault(pv => pv.PostId == post.Id && pv.PlayerId == playerId);
                int? userVote = vote != null ? vote.Vote : null;
                result.Add((post, userVote));
            }
            return result;
        }
    }
}