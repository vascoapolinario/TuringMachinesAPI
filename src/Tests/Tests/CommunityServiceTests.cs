using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using TuringMachinesAPI.DataSources;
using TuringMachinesAPI.Enums;
using TuringMachinesAPI.Services;
using TuringMachinesAPI.Utils;
using Xunit;
using Dtos = TuringMachinesAPI.Dtos;
using Entities = TuringMachinesAPI.Entities;

namespace TuringMachinesAPITests.Tests
{
    [Collection("SequentialTests")]
    public sealed class CommunityServiceTests : IDisposable
    {
        private readonly TestApplicationDomain applicationDomain;
        private readonly CommunityService service;

        public CommunityServiceTests()
        {
            applicationDomain = new TestApplicationDomain();

            string? connectionString = applicationDomain.configuration.GetConnectionString("DefaultConnection");
            Assert.NotNull(connectionString);

            applicationDomain.Services.AddDbContext<TuringMachinesDbContext>(o => o.UseNpgsql(connectionString));
            applicationDomain.Services.AddScoped<CommunityService>();

            var provider = applicationDomain.ServiceProvider;
            service = provider.GetRequiredService<CommunityService>();

            string? backupPath = applicationDomain.configuration.GetValue<string>("TestsDbBackup:FilePath");
            if (backupPath == null)
                throw new Exception("Não foi possível obter o caminho do ficheiro de configuração.");

            string sql = File.ReadAllText(backupPath);

            using (IServiceScope scope = applicationDomain.ServiceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TuringMachinesDbContext>();
                db.Database.Migrate();
                db.Database.ExecuteSqlRaw(sql);
                db.SaveChanges();
            }
        }

        public void Dispose()
        {
            applicationDomain.Dispose();
        }

        [Fact]
        public void CreateDiscussion_ShouldCreateSuccessfully()
        {
            var discussion = service.CreateDiscussion("Test Discussion", 1, "This is a test discussion content.", DiscussionCategory.General.ToString());

            Assert.NotNull(discussion);
            Assert.Equal("Test Discussion", discussion.Title);

            var InitialPost = discussion.InitialPost;
            Assert.NotNull(InitialPost);
            Assert.Equal("This is a test discussion content.", InitialPost.Content);
        }

        [Fact]
        public void CreatePost_ShouldCreateSuccessfully()
        {
            var discussion = service.CreateDiscussion("Test Discussion for Post", 1, "Initial content.", DiscussionCategory.General.ToString());
            Assert.NotNull(discussion);
            var post = service.CreatePost(discussion.Id, 2, "This is a test post content.");
            Assert.NotNull(post);
            Assert.Equal("This is a test post content.", post.Content);
        }

        [Fact]
        public void GetDiscussions_ShouldReturnDiscussions()
        {
            service.CreateDiscussion("Discussion 1", 1, "Content 1", DiscussionCategory.General.ToString());
            service.CreateDiscussion("Discussion 2", 2, "Content 2", DiscussionCategory.Help.ToString());

            var discussions = service.GetDiscussions();
            Assert.True(discussions.Count() >= 2);
        }

        [Fact]
        public void GetPostsByDiscussionId_ShouldReturnPosts()
        {
            var discussion1 = service.CreateDiscussion("Discussion 1", 1, "Content 1", DiscussionCategory.General.ToString());
            var discussion2 = service.CreateDiscussion("Discussion 2", 2, "Content 2", DiscussionCategory.Help.ToString());

            var post1 = service.CreatePost(discussion1!.Id, 2, "Post Content 1");
            var post2 = service.CreatePost(discussion2!.Id, 3, "Post Content 2");

            var postsForDiscussion1 = service.GetPostsByDiscussionId(discussion1.Id);
            Assert.Equal(2, postsForDiscussion1.Count());

            var postsForDiscussion2 = service.GetPostsByDiscussionId(discussion2.Id);
            Assert.Equal(2, postsForDiscussion2.Count());
        }

        [Fact]
        public void EditPost_ShouldUpdateContent()
        {
            var discussion = service.CreateDiscussion("Discussion for Edit", 1, "Initial Content", DiscussionCategory.General.ToString());
            var post = service.CreatePost(discussion!.Id, 2, "Original Post Content");
            Assert.False(post!.IsEdited);

            var originalUpdatedAt = post!.UpdatedAt;

            var updatedPost = service.EditPost(post.Id, 2, "Updated Post Content");
            Assert.Equal("Updated Post Content", updatedPost!.Content);
            Assert.True(updatedPost.IsEdited);
            Assert.NotEqual(originalUpdatedAt, updatedPost.UpdatedAt);
        }

        [Fact]
        public void LikeAndDislike_ShouldUpdatePost()
        {
            var discussion = service.CreateDiscussion("Discussion for Likes/Dislike", 1, "Initial Content", DiscussionCategory.General.ToString());
            var likePost = service.CreatePost(discussion!.Id, 2, "Post to Like");
            var dislikePost = service.CreatePost(discussion.Id, 3, "Post to Dislike");

            var likedPost = service.ToggleLikePost(likePost!.Id, 4);

            var dislikedPost = service.ToggleDislikePost(dislikePost!.Id, 4);

            var DiscussionPosts = service.GetPostsByDiscussionId(discussion.Id);

            var fetchedLikePost = DiscussionPosts.First(p => p.Id == likePost.Id);
            var fetchedDislikePost = DiscussionPosts.First(p => p.Id == dislikePost.Id);
            Assert.Equal(1, fetchedLikePost.LikeCount);
            Assert.Equal(1, fetchedDislikePost.DislikeCount);

            var VotedPosts = service.GetPostsByDiscussionIdWithUserVote(discussion.Id, 4);
            Assert.True(VotedPosts.First(p => p.post.Id == likePost.Id).userVote == 1);
            Assert.True(VotedPosts.First(p => p.post.Id == dislikePost.Id).userVote == -1);
        }

        [Fact]
        public void ToggleCloseDiscussion_ShouldSetIsClosed()
        {
            var discussion = service.CreateDiscussion("Discussion to Close", 1, "Initial Content", DiscussionCategory.General.ToString());
            Assert.False(discussion!.IsClosed);
            service.ToggleCloseDiscussion(discussion.Id, 1);
            var fetchedDiscussion = service.GetDiscussions().First(d => d.Id == discussion.Id);
            Assert.True(fetchedDiscussion.IsClosed);
        }

        [Fact]
        public void DeletePost_ShouldRemovePost()
        {
            var discussion = service.CreateDiscussion("Discussion for Delete Post", 1, "Initial Content", DiscussionCategory.General.ToString());
            var post = service.CreatePost(discussion!.Id, 2, "Post to be deleted");
            var postsBeforeDelete = service.GetPostsByDiscussionId(discussion.Id);
            Assert.Contains(postsBeforeDelete, p => p.Id == post!.Id);
            service.DeletePost(post!.Id, 2);
            var postsAfterDelete = service.GetPostsByDiscussionId(discussion.Id);
            Assert.DoesNotContain(postsAfterDelete, p => p.Id == post.Id);
        }

        [Fact]
        public void DeleteDiscussion_ShouldRemoveDiscussionAndPosts()
        {
            var discussion = service.CreateDiscussion("Discussion to Delete", 1, "Initial Content", DiscussionCategory.General.ToString());
            var post1 = service.CreatePost(discussion!.Id, 2, "First Post");
            var post2 = service.CreatePost(discussion.Id, 3, "Second Post");
            var discussionsBeforeDelete = service.GetDiscussions();
            Assert.Contains(discussionsBeforeDelete, d => d.Id == discussion.Id);
            var postsBeforeDelete = service.GetPostsByDiscussionId(discussion.Id);
            Assert.Contains(postsBeforeDelete, p => p.Id == post1!.Id);
            Assert.Contains(postsBeforeDelete, p => p.Id == post2!.Id);
            service.DeleteDiscussion(discussion.Id, 1);
            var discussionsAfterDelete = service.GetDiscussions();
            Assert.DoesNotContain(discussionsAfterDelete, d => d.Id == discussion.Id);
            var postsAfterDelete = service.GetPostsByDiscussionId(discussion.Id);
            Assert.Empty(postsAfterDelete);
        }

        [Fact]
        public void GetDiscussionById_ShouldReturnCorrectDiscussion()
        {
            var discussion = service.CreateDiscussion("Discussion to Fetch", 1, "Initial Content", DiscussionCategory.General.ToString());
            var fetchedDiscussion = service.GetDiscussionById(discussion!.Id);
            Assert.NotNull(fetchedDiscussion);
            Assert.Equal(discussion.Id, fetchedDiscussion!.Id);
            Assert.Equal(discussion.Title, fetchedDiscussion.Title);
        }

        [Fact]
        public void ToggleDiscussionAnswerPost_ShouldSetAnswerPost()
        {
            var discussion = service.CreateDiscussion("Discussion for Answer Post", 1, "Initial Content", DiscussionCategory.General.ToString());
            var answerPost = service.CreatePost(discussion!.Id, 2, "This is the answer post.");
            Assert.Null(discussion.AnswerPost);
            service.ToggleDiscussionAnswerPost(discussion.Id, answerPost!.Id, 1);
            var fetchedDiscussion = service.GetDiscussionById(discussion.Id);
            Assert.NotNull(fetchedDiscussion!.AnswerPost);
            Assert.Equal(answerPost.Id, fetchedDiscussion.AnswerPost!.Id);
        }
    }
}